using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Endless mode")]
    [SerializeField] private bool isEndless = false;
    [SerializeField] private int endlessStartEnemies = 5;
    [SerializeField] private float endlessWaveDelay = 4f;

    [Header("Setup")]
    [SerializeField] private LevelData levelData;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float delayBeforeFirstWave = 2f;

    [Header("Spawn")]
    [Tooltip("Distanza minima dal player: sotto questa il nemico comparirebbe addosso.")]
    [SerializeField] private float spawnMinDistance = 7f;
    [Tooltip("Distanza massima dal player: oltre questa i nemici impiegano troppo ad arrivare.")]
    [SerializeField] private float spawnMaxDistance = 12f;
    [Tooltip("Distanza sotto la quale non si scende mai, nemmeno quando la mappa e' stretta.")]
    [SerializeField] private float spawnAbsoluteMinDistance = 4.5f;
    [Tooltip("Spazio libero richiesto attorno al punto di comparsa. Il nemico e' circa 0.4x0.6.")]
    [SerializeField] private float spawnClearance = 0.6f;
    [SerializeField] private int spawnAttempts = 40;
    [Tooltip("Cosa conta come occupato. Lasciare tutto: contano muri, decorazioni e altri nemici.")]
    [SerializeField] private LayerMask spawnBlockMask = ~0;
    [Tooltip("Scostamento provato quando uno spawn point esplicito e' gia' occupato.")]
    [SerializeField] private float spawnJitter = 1.2f;
    [Tooltip("Pausa tra un nemico e l'altro dentro la stessa ondata. A zero compaiono tutti insieme.")]
    [SerializeField] private float spawnInterval = 0.08f;
    [Tooltip("Scrive in Console distanza e modalita' di ogni comparsa. Serve solo per diagnosticare.")]
    [SerializeField] private bool logSpawns = false;

    [Header("Area giocabile")]
    [Tooltip("Tilemap che definisce il terreno. Se vuoto viene preso il piu' grande della scena.")]
    [SerializeField] private Tilemap groundTilemap;
    [Tooltip("Quanto restare dentro il bordo dell'area, per non far comparire nessuno dentro il muro.")]
    [SerializeField] private float areaMargin = 1.5f;

    public int CurrentWave { get; private set; } = 0;
    public int TotalWaves => levelData != null ? levelData.waves.Length : 0;
    public int EnemiesAlive => activeEnemies.Count;

    /// <summary>
    /// I nemici vivi, per chi ha bisogno di sapere dove sono (il radar).
    /// Puo' contenere caselle vuote: chi la legge deve saltare i null, perche'
    /// la ripulitura avviene nel ciclo dell'ondata e non a ogni fotogramma.
    /// </summary>
    public IReadOnlyList<GameObject> ActiveEnemies => activeEnemies;

    public event Action<int, int> OnWaveStarted; // current, total
    public event Action OnLevelCompleted;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Transform playerTransform;
    private Collider2D playerCollider;

    // Rettangolo entro cui e' lecito far comparire un nemico.
    private Bounds spawnArea;
    private bool hasArea;

    // Le scene sono state salvate quando i valori di serie erano piu' bassi
    // (0.45 e 30), e Unity tiene quelli salvati, non quelli scritti qui sopra.
    // Con queste due soglie il minimo vale comunque, senza dover riaprire e
    // risalvare tutte le scene a mano.
    private float Clearance => Mathf.Max(spawnClearance, 0.55f);
    private int Attempts => Mathf.Max(spawnAttempts, 40);

    // Scarto tra l'origine del nemico e il suo collider. Nel prefab il collider
    // non e' centrato sull'origine, quindi controllare che sia libero il punto
    // dove finisce l'origine non dice niente su dove finisce il corpo.
    // Tutti i controlli qui sotto ragionano sulla posizione del CORPO, e
    // l'origine viene ricavata togliendo questo scarto al momento di creare il nemico.
    private Vector3 enemyBodyOffset = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (SelectedLevel.Current != null)
        {
            levelData = SelectedLevel.Current;
        }

        // determina se e' endless
        if (levelData != null && levelData.levelNumber == 99)
        {
            isEndless = true;
        }

        // ora il check di sicurezza
        if (levelData == null)
        {
            Debug.LogError("[LevelManager] No level data assigned!");
            return;
        }
        if (!isEndless && levelData.waves.Length == 0)
        {
            Debug.LogError("[LevelManager] Level has no waves!");
            return;
        }

        ResolveSpawnArea();
        ResolveEnemyBodyOffset();
        BuildNavigation();
        Debug.Log($"[LevelManager] Caricato {levelData.levelName}");
        StartCoroutine(RunLevel());
    }

    private IEnumerator RunLevel()
    {
        yield return new WaitForSeconds(delayBeforeFirstWave);

        if (isEndless)
        {
            yield return RunEndless();
        }
        else
        {
            for (int i = 0; i < levelData.waves.Length; i++)
            {
                CurrentWave = i + 1;
                Debug.Log($"[Level] Wave {CurrentWave}/{TotalWaves}");
                OnWaveStarted?.Invoke(CurrentWave, TotalWaves);

                // si aspetta che l'ondata sia comparsa tutta prima di stare a
                // guardare se e' finita, altrimenti al primo controllo la lista
                // e' ancora vuota e l'ondata risulta gia' completata
                yield return SpawnWave(levelData.waves[i]);

                while (activeEnemies.Count > 0)
                {
                    activeEnemies.RemoveAll(e => e == null);
                    yield return null;
                }

                if (i < levelData.waves.Length - 1)
                    yield return new WaitForSeconds(levelData.waves[i].delayAfter);
            }

            Debug.Log("[Level] COMPLETED!");
            OnLevelCompleted?.Invoke();
        }
    }

    private IEnumerator RunEndless()
    {
        int waveNum = 1;
        float speedMult = 1f;
        float hpMult = 1f;
        float damageMult = 1f;

        while (true)
        {
            CurrentWave = waveNum;
            Debug.Log($"[Endless] Wave {waveNum} - x{hpMult:F2} HP, x{speedMult:F2} speed");
            OnWaveStarted?.Invoke(waveNum, 0); // 0 = infinito

            int count = endlessStartEnemies + waveNum;

            levelData.enemyHpMultiplier = hpMult;
            levelData.enemySpeedMultiplier = speedMult;
            levelData.enemyDamageMultiplier = damageMult;

            for (int i = 0; i < count; i++)
            {
                SpawnOne();
                if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
            }

            while (activeEnemies.Count > 0)
            {
                activeEnemies.RemoveAll(e => e == null);
                yield return null;
            }

            yield return new WaitForSeconds(endlessWaveDelay);

            waveNum++;
            hpMult += 0.1f;
            speedMult += 0.05f;
            damageMult += 0.05f;
        }
    }

    /// <summary>
    /// I nemici dell'ondata non compaiono tutti nello stesso fotogramma.
    ///
    /// Dal quarto livello in poi le ondate arrivano a 18 e 25 nemici: crearli
    /// tutti insieme fa uno scatto visibile (ognuno fa decine di controlli
    /// sulla fisica per trovare posto) e li fa arrivare addosso in blocco.
    /// Distanziati di un soffio l'uno dall'altro entrano in scena come un
    /// gruppo che avanza invece che come un muro comparso dal nulla.
    /// </summary>
    private IEnumerator SpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnOne();
            if (spawnInterval > 0f && i < wave.enemyCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        Vector3 bodyPos = GetRandomSpawnPos();
        GameObject enemy = Instantiate(enemyPrefab, bodyPos - enemyBodyOffset, Quaternion.identity);

        // Senza questo il collider del nemico appena creato non risulta ancora
        // al motore fisico, e il nemico successivo dello stesso frame puo'
        // comparire esattamente sopra di lui.
        Physics2D.SyncTransforms();

        ApplyDifficulty(enemy);
        activeEnemies.Add(enemy);
    }

    // ------------------------------------------------------------------
    // Posizionamento
    // ------------------------------------------------------------------

    /// <summary>
    /// Prepara la mappa di navigazione dei nemici.
    ///
    /// Copre l'area giocabile INTERA, non quella di comparsa: il margine serve a
    /// non far comparire nessuno incollato al muro, ma camminarci accanto e'
    /// lecito, e togliere quella fascia dalla mappa vorrebbe dire che un nemico
    /// finito li' non sa piu' dove andare.
    /// </summary>
    private void BuildNavigation()
    {
        if (!hasArea)
        {
            Debug.LogWarning("[LevelManager] Area non delimitata: i nemici useranno "
                             + "solo l'aggiramento locale, senza mappa di navigazione.");
            return;
        }

        Bounds nav = spawnArea;
        nav.Expand(2f * areaMargin);
        EnemyFlowField.Create(nav);
    }

    /// <summary>
    /// Dove sta il collider del nemico rispetto alla sua origine, letto dal prefab.
    /// </summary>
    private void ResolveEnemyBodyOffset()
    {
        enemyBodyOffset = Vector3.zero;
        if (enemyPrefab == null) return;

        var col = enemyPrefab.GetComponentInChildren<Collider2D>();
        if (col == null) return;

        Vector3 world = col.transform.TransformPoint(col.offset);
        Vector3 rel = enemyPrefab.transform.InverseTransformPoint(world);
        enemyBodyOffset = new Vector3(rel.x, rel.y, 0f);

        if (enemyBodyOffset.sqrMagnitude > 0.0001f)
            Debug.Log($"[LevelManager] Il collider del nemico e' spostato di {enemyBodyOffset} "
                      + "rispetto alla sua origine: le posizioni di comparsa vengono corrette di conseguenza.");
    }

    private Transform Player
    {
        get
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
            return playerTransform;
        }
    }

    /// <summary>
    /// Dove far comparire un nemico.
    ///
    /// Il metodo NON e' "estraggo un punto a caso e vedo se va bene". Quella era
    /// la versione precedente, e in un livello pieno di alberi come Game_Field
    /// falliva quasi sempre: a 7-12 unita' dal giocatore la linea d'aria e'
    /// interrotta da qualcosa in quasi tutte le direzioni, tutti i tentativi
    /// venivano scartati e si finiva ogni volta sull'ultimo ripiego, che piazza
    /// vicino. Risultato: i nemici comparivano tutti addosso al giocatore.
    ///
    /// Qui invece si sceglie una DIREZIONE e si misura quanto si puo' andare
    /// lontano da quella parte prima di incontrare qualcosa o di uscire
    /// dall'area. Poi ci si ferma al minimo tra la distanza voluta e quella
    /// disponibile. Il punto che ne esce e' per costruzione dentro la mappa,
    /// libero e raggiungibile in linea retta: non c'e' niente da scartare, e
    /// una direzione stretta produce comunque un punto valido, solo piu' vicino.
    /// </summary>
    private Vector3 GetRandomSpawnPos()
    {
        Transform pl = Player;

        // 1) spawn point espliciti, se presenti: hanno precedenza ma sono comunque verificati
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int start = UnityEngine.Random.Range(0, spawnPoints.Length);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform sp = spawnPoints[(start + i) % spawnPoints.Length];
                if (sp == null) continue;
                if (Acceptable(sp.position, pl)) return sp.position;

                for (int j = 0; j < 6; j++)
                {
                    Vector3 jittered = sp.position
                        + (Vector3)(UnityEngine.Random.insideUnitCircle * spawnJitter);
                    if (Acceptable(jittered, pl)) return jittered;
                }
            }
        }

        if (pl == null)
        {
            Debug.LogWarning("[LevelManager] Nessun Player in scena: spawn all'origine.");
            return Vector3.zero;
        }

        Vector3 origin = PlayerBodyPos();

        Vector3 best = Vector3.zero;
        float bestDist = 0f;
        int tooTight = 0, occupied = 0;

        for (int i = 0; i < Attempts; i++)
        {
            float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            // margine di sicurezza: ci si ferma prima dell'ostacolo, non addosso
            float room = FreeDistance(origin, dir, spawnMaxDistance) - Clearance * 1.2f;
            if (room < spawnAbsoluteMinDistance) { tooTight++; continue; }

            float dist = PickDistance(room);

            Vector3 pos = origin + (Vector3)(dir * dist);
            if (!IsFree(pos)) { occupied++; continue; }   // di solito un altro nemico appena creato

            if (dist >= spawnMinDistance)
            {
                if (logSpawns)
                    Debug.Log($"[Spawn] {dist:F1} unita dal player, direzione libera (tentativo {i + 1})");
                return pos;
            }

            // direzione stretta: si tiene da parte la migliore e si continua a cercarne una piena
            if (dist > bestDist) { bestDist = dist; best = pos; }
        }

        if (bestDist > 0f)
        {
            if (logSpawns)
                Debug.Log($"[Spawn] nessuna direzione piena: ripiego a {bestDist:F1} unita "
                          + $"({tooTight} direzioni troppo strette, {occupied} punti occupati)");
            return best;
        }

        // 2) niente ha funzionato: il giocatore e' chiuso in uno spazio minuscolo.
        //    Si prende comunque il punto piu' lontano possibile, sotto il minimo.
        Vector3 last = FurthestFreePointAround(origin);
        Debug.LogWarning($"[LevelManager] Spazio troppo stretto attorno al giocatore: "
                         + $"nemico a {Vector2.Distance(last, origin):F1} unita "
                         + $"({tooTight} direzioni strette, {occupied} occupate su {Attempts} tentativi).");
        return last;
    }

    /// <summary>
    /// Sceglie a che distanza fermarsi, dato lo spazio disponibile in quella
    /// direzione.
    ///
    /// La versione precedente faceva Min(distanza voluta, spazio disponibile).
    /// Sembra ragionevole e invece e' il motivo per cui i nemici comparivano
    /// TUTTI IN FILA: quando lo spazio e' meno di quanto si vorrebbe, quel Min
    /// restituisce sempre e comunque lo spazio disponibile, cioe' esattamente
    /// il bordo dell'area giocabile. Con un'ondata da diciotto nemici e un
    /// bordo vicino, diciotto punti finivano tutti sulla stessa riga, allineati
    /// sul perimetro e appiccicati l'uno all'altro.
    ///
    /// Qui invece si pesca una distanza a caso DENTRO lo spazio disponibile, e
    /// non si prende mai il massimo secco: si resta larghi quando c'e' posto,
    /// ci si stringe quando non ce n'e', ma sparsi.
    /// </summary>
    private float PickDistance(float room)
    {
        float hi = Mathf.Min(spawnMaxDistance, room);

        // Si punta alla distanza voluta, ma lasciando sempre almeno un paio di
        // unita' di gioco tra il piu' vicino e il piu' lontano, altrimenti in
        // uno spazio stretto si ricasca nell'allineamento.
        float lo = Mathf.Max(spawnAbsoluteMinDistance, Mathf.Min(spawnMinDistance, hi - 2f));
        if (lo > hi) lo = hi;

        return UnityEngine.Random.Range(lo, hi);
    }

    /// <summary>
    /// Il punto va bene se e' dentro l'area, libero da collider e non separato
    /// dal player da un ostacolo. Serve per gli spawn point messi a mano, dove
    /// la posizione e' data e va solo verificata.
    /// </summary>
    private bool Acceptable(Vector3 pos, Transform pl)
    {
        if (!InsideArea(pos)) return false;
        if (!IsFree(pos)) return false;
        if (pl != null && BlockedBetween(PlayerBodyPos(), pos)) return false;
        return true;
    }

    /// <summary>
    /// Il centro del collider del giocatore, non l'origine del suo oggetto.
    ///
    /// Gli ostacoli sono collider, quindi misurare da un punto che non e' il suo
    /// collider vuol dire misurare una linea che in gioco non esiste. Sul
    /// giocatore lo scarto e' 0.26 unita': poco, ma basta a far passare una
    /// linea sopra un cespuglio che invece lo ferma.
    /// </summary>
    private Vector3 PlayerBodyPos()
    {
        Transform pl = Player;
        if (pl == null) return Vector3.zero;

        if (playerCollider == null)
        {
            foreach (var c in pl.GetComponentsInChildren<Collider2D>())
            {
                if (c != null && !c.isTrigger) { playerCollider = c; break; }
            }
        }

        if (playerCollider != null) return playerCollider.bounds.center;
        return pl.position;
    }

    /// <summary>
    /// Quanto si puo' andare lontano da origin in direzione dir prima di
    /// incontrare un ostacolo o di uscire dall'area giocabile.
    ///
    /// Il cast e' un cerchio largo quanto lo spazio che serve al nemico, non una
    /// linea: cosi un varco troppo stretto per starci non viene contato come
    /// passaggio libero.
    /// </summary>
    private float FreeDistance(Vector3 origin, Vector2 dir, float max)
    {
        float reach = max;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, Clearance, dir, max, spawnBlockMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (IsActor(c)) continue;                  // player e nemici non sono muri
            if (hits[i].distance < reach) reach = hits[i].distance;
        }

        return Mathf.Min(reach, DistanceToAreaEdge(origin, dir, max));
    }

    /// <summary>
    /// Distanza dal bordo del rettangolo giocabile lungo una direzione.
    /// </summary>
    private float DistanceToAreaEdge(Vector3 origin, Vector2 dir, float max)
    {
        if (!hasArea) return max;

        float t = max;
        const float Eps = 0.0001f;

        if (dir.x > Eps) t = Mathf.Min(t, (spawnArea.max.x - origin.x) / dir.x);
        else if (dir.x < -Eps) t = Mathf.Min(t, (spawnArea.min.x - origin.x) / dir.x);

        if (dir.y > Eps) t = Mathf.Min(t, (spawnArea.max.y - origin.y) / dir.y);
        else if (dir.y < -Eps) t = Mathf.Min(t, (spawnArea.min.y - origin.y) / dir.y);

        return Mathf.Max(0f, t);
    }

    /// <summary>
    /// Ultima spiaggia: prova 16 direzioni fisse e tiene quella dove si arriva
    /// piu' lontano. Serve solo quando il giocatore e' in uno spazio talmente
    /// stretto che nemmeno la distanza minima assoluta ci sta.
    /// </summary>
    private Vector3 FurthestFreePointAround(Vector3 origin)
    {
        const int Directions = 16;
        float startAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        Vector3 best = origin;
        float bestDist = -1f;

        for (int i = 0; i < Directions; i++)
        {
            float ang = startAngle + i * (Mathf.PI * 2f / Directions);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            float room = FreeDistance(origin, dir, spawnMaxDistance) - Clearance * 1.2f;
            if (room <= 0f) continue;

            // anche qui non si prende il massimo secco, per non allineare tutti
            float dist = room * UnityEngine.Random.Range(0.7f, 1f);

            Vector3 candidate = origin + (Vector3)(dir * dist);
            if (!IsFree(candidate)) continue;

            if (dist > bestDist) { bestDist = dist; best = candidate; }
        }

        if (bestDist < 0f)
            Debug.LogWarning("[LevelManager] Nessuna direzione libera attorno al giocatore.");

        return best;
    }

    /// <summary>
    /// Ricava l'area giocabile. Due sorgenti, e vale l'intersezione:
    ///
    /// - i muri, quando la scena ne ha (oggetti che si chiamano Wall...): danno
    ///   il rettangolo interno esatto in cui si gioca;
    /// - il Tilemap del terreno, che e' il ripiego per le scene senza muri.
    ///
    /// Nella scena Game il Tilemap e' 57x38 unita' mentre i muri ne racchiudono
    /// 36x22: prendere solo il Tilemap significava dichiarare "area valida" una
    /// fascia larghissima tutto attorno alla mappa, fuori dai muri. Era la
    /// causa principale dei nemici comparsi fuori campo.
    /// </summary>
    private void ResolveSpawnArea()
    {
        Bounds tileRect;
        Bounds wallRect;
        bool haveTile = TryTilemapRect(out tileRect);
        bool haveWall = TryWallRect(out wallRect);

        string source;
        Bounds area;

        if (haveWall && haveTile)
        {
            area = Intersect(wallRect, tileRect);
            source = "muri + tilemap";
        }
        else if (haveWall) { area = wallRect; source = "muri"; }
        else if (haveTile) { area = tileRect; source = "tilemap"; }
        else
        {
            hasArea = false;
            Debug.LogWarning("[LevelManager] Nessun muro e nessun Tilemap: area di comparsa non delimitata.");
            return;
        }

        area.Expand(-2f * areaMargin);   // Expand somma meta' per lato
        spawnArea = area;
        hasArea = spawnArea.size.x > 1f && spawnArea.size.y > 1f;

        if (hasArea)
            Debug.Log($"[LevelManager] Area di comparsa ({source}): "
                      + $"x {spawnArea.min.x:F1}..{spawnArea.max.x:F1}, y {spawnArea.min.y:F1}..{spawnArea.max.y:F1}");
        else
            Debug.LogWarning("[LevelManager] Area troppo piccola dopo il margine: vincolo disattivato.");
    }

    private bool TryTilemapRect(out Bounds rect)
    {
        rect = new Bounds();
        Tilemap tm = groundTilemap;
        if (tm == null)
        {
            float best = -1f;
            foreach (var t in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                t.CompressBounds();
                Vector3 sz = t.localBounds.size;
                float areaSize = sz.x * sz.y;
                if (areaSize > best) { best = areaSize; tm = t; }
            }
        }
        if (tm == null) return false;

        tm.CompressBounds();
        Bounds lb = tm.localBounds;
        Vector3 centerWorld = tm.transform.TransformPoint(lb.center);
        Vector3 sizeWorld = Vector3.Scale(lb.size, tm.transform.lossyScale);
        sizeWorld.z = 1f;
        rect = new Bounds(new Vector3(centerWorld.x, centerWorld.y, 0f), sizeWorld);
        return true;
    }

    /// <summary>
    /// Rettangolo interno delimitato dai muri perimetrali. Ogni muro viene
    /// classificato come orizzontale o verticale in base alla sua forma, e
    /// spinge dentro il lato corrispondente.
    /// </summary>
    private bool TryWallRect(out Bounds rect)
    {
        rect = new Bounds();
        var walls = new List<Collider2D>();

        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (c == null || c.isTrigger || !c.enabled) continue;
            Transform t = c.transform;
            bool isWall = t.name.StartsWith("Wall")
                          || (t.parent != null && t.parent.name.StartsWith("Wall"));
            if (isWall) walls.Add(c);
        }

        if (walls.Count < 3) return false;

        Bounds outer = walls[0].bounds;
        for (int i = 1; i < walls.Count; i++) outer.Encapsulate(walls[i].bounds);

        float left = outer.min.x, right = outer.max.x;
        float bottom = outer.min.y, top = outer.max.y;

        foreach (var c in walls)
        {
            Bounds b = c.bounds;
            if (b.size.x >= b.size.y)
            {
                if (b.center.y > outer.center.y) top = Mathf.Min(top, b.min.y);
                else bottom = Mathf.Max(bottom, b.max.y);
            }
            else
            {
                if (b.center.x > outer.center.x) right = Mathf.Min(right, b.min.x);
                else left = Mathf.Max(left, b.max.x);
            }
        }

        if (right - left < 2f || top - bottom < 2f) return false;

        rect = new Bounds(
            new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f),
            new Vector3(right - left, top - bottom, 1f));
        return true;
    }

    private static Bounds Intersect(Bounds a, Bounds b)
    {
        float minX = Mathf.Max(a.min.x, b.min.x);
        float maxX = Mathf.Min(a.max.x, b.max.x);
        float minY = Mathf.Max(a.min.y, b.min.y);
        float maxY = Mathf.Min(a.max.y, b.max.y);

        if (maxX <= minX || maxY <= minY) return a;   // non si toccano: si tiene il piu' affidabile

        return new Bounds(
            new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f),
            new Vector3(maxX - minX, maxY - minY, 1f));
    }

    private bool InsideArea(Vector3 pos)
    {
        if (!hasArea) return true;
        return pos.x >= spawnArea.min.x && pos.x <= spawnArea.max.x
            && pos.y >= spawnArea.min.y && pos.y <= spawnArea.max.y;
    }


    /// <summary>
    /// Libero = nessun collider solido nel raggio, compresi gli altri nemici:
    /// due nemici sovrapposti si spingono a vicenda e finiscono dentro i muri.
    /// </summary>
    private bool IsFree(Vector3 pos)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(pos, Clearance, spawnBlockMask);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D c = cols[i];
            if (c == null || c.isTrigger) continue;
            return false;
        }
        return true;
    }

    /// <summary>
    /// C'e' un ostacolo fisso tra i due punti? Player e nemici non contano:
    /// sono corpi che si spostano, non muri, e considerarli avrebbe scartato
    /// meta' dei punti buoni solo perche' un altro nemico passava di li'.
    /// </summary>
    private bool BlockedBetween(Vector3 a, Vector3 b)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(a, b, spawnBlockMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (IsActor(c)) continue;
            return true;
        }
        return false;
    }

    /// Player e nemici hanno un Rigidbody2D dinamico; muri, rocce e tilemap no.
    private static bool IsActor(Collider2D c)
    {
        var rb = c.attachedRigidbody;
        return rb != null && rb.bodyType == RigidbodyType2D.Dynamic;
    }

    private void ApplyDifficulty(GameObject enemy)
    {
        if (levelData == null) return;

        var health = enemy.GetComponent<EnemyHealth>();
        var controller = enemy.GetComponent<EnemyController>();

        if (health != null) health.ApplyHpMultiplier(levelData.enemyHpMultiplier);
        if (controller != null) controller.ApplyMultipliers(levelData.enemySpeedMultiplier, levelData.enemyDamageMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Transform pl = Application.isPlaying ? playerTransform : null;
        if (pl != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pl.position, spawnMinDistance);
            Gizmos.DrawWireSphere(pl.position, spawnMaxDistance);
        }
        if (hasArea)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        }
    }
}
