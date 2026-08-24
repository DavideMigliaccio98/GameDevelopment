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
    [Tooltip("Spazio libero richiesto attorno al punto di comparsa.")]
    [SerializeField] private float spawnClearance = 0.45f;
    [SerializeField] private int spawnAttempts = 30;
    [Tooltip("Scarta i punti separati dal player da un muro: evita nemici bloccati in stanze chiuse.")]
    [SerializeField] private bool requireLineOfSight = true;
    [Tooltip("Cosa conta come occupato. Lasciare tutto: contano muri, decorazioni e altri nemici.")]
    [SerializeField] private LayerMask spawnBlockMask = ~0;
    [Tooltip("Scostamento provato quando uno spawn point esplicito e' gia' occupato.")]
    [SerializeField] private float spawnJitter = 1.2f;
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

    public event Action<int, int> OnWaveStarted; // current, total
    public event Action OnLevelCompleted;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Transform playerTransform;

    // Rettangolo entro cui e' lecito far comparire un nemico. Senza questo il
    // controllo "posizione libera" accettava anche i punti OLTRE i muri, dove
    // ovviamente non c'e' nessun collider.
    private Bounds spawnArea;
    private bool hasArea;

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

        // determina se è endless
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

                SpawnWave(levelData.waves[i]);

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

            // applica difficoltà progressiva via override temporaneo
            levelData.enemyHpMultiplier = hpMult;
            levelData.enemySpeedMultiplier = speedMult;
            levelData.enemyDamageMultiplier = damageMult;

            for (int i = 0; i < count; i++)
            {
                SpawnOne();
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

    private void SpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        Vector3 pos = GetRandomSpawnPos();
        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        ApplyDifficulty(enemy);
        activeEnemies.Add(enemy);
    }

    // ------------------------------------------------------------------
    // Posizionamento
    // ------------------------------------------------------------------

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
    /// I nemici compaiono in un anello attorno al player invece che dentro un
    /// rettangolo fisso: cosi la posizione e' sempre dentro la mappa qualunque
    /// sia il livello, e non serve tarare coordinate a mano scena per scena.
    /// Ogni candidato deve essere libero da collider e (se richiesto) in linea
    /// d'aria col player, per non far comparire nemici chiusi in un'altra stanza.
    /// </summary>
    private Vector3 GetRandomSpawnPos()
    {
        // 1) se qualcuno ha configurato degli spawn point espliciti, hanno precedenza,
        //    ma vengono comunque verificati: prima erano usati alla cieca e piu'
        //    nemici finivano nello stesso identico punto.
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int start = UnityEngine.Random.Range(0, spawnPoints.Length);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform sp = spawnPoints[(start + i) % spawnPoints.Length];
                if (sp == null) continue;
                if (IsFree(sp.position)) return sp.position;

                for (int j = 0; j < 6; j++)
                {
                    Vector3 jittered = sp.position
                        + (Vector3)(UnityEngine.Random.insideUnitCircle * spawnJitter);
                    if (IsFree(jittered)) return jittered;
                }
            }
        }

        Transform pl = Player;
        if (pl == null)
        {
            Debug.LogWarning("[LevelManager] Nessun Player in scena: spawn all'origine.");
            return Vector3.zero;
        }

        // 2) anello attorno al player, con linea d'aria
        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            Vector3 pos = RandomRingPoint(pl.position);
            if (!InsideArea(pos)) continue;
            if (!IsFree(pos)) continue;
            if (requireLineOfSight && BlockedBetween(pl.position, pos)) continue;
            if (logSpawns)
                Debug.Log($"[Spawn] anello, {Vector2.Distance(pos, pl.position):F1} unita dal player (tentativo {attempt + 1})");
            return pos;
        }

        // 3) ripiego: si rinuncia alla linea d'aria, basta che sia libero
        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            Vector3 pos = RandomRingPoint(pl.position);
            if (!InsideArea(pos)) continue;   // il vincolo dell'area NON si molla mai
            if (IsFree(pos))
            {
                if (logSpawns)
                    Debug.LogWarning($"[Spawn] SENZA linea d'aria, {Vector2.Distance(pos, pl.position):F1} unita: separato dal player da un muro");
                return pos;
            }
        }

        // 4) ultimo ripiego: appena fuori dal raggio d'azione del player.
        //    Meglio di Vector3.zero, che accatastava tutti i nemici nello stesso
        //    punto della mappa, spesso dentro un muro.
        Debug.LogWarning("[LevelManager] Nessuna posizione libera trovata in "
                         + (spawnAttempts * 2) + " tentativi: ripiego a distanza minima.");
        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
        if (dir == Vector2.zero) dir = Vector2.right;
        return pl.position + (Vector3)(dir * spawnMinDistance);
    }

    /// <summary>
    /// Ricava l'area giocabile dal Tilemap del terreno. Se non ne trova, il
    /// vincolo resta disattivato e il comportamento e' quello di prima.
    /// </summary>
    private void ResolveSpawnArea()
    {
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

        if (tm == null)
        {
            hasArea = false;
            Debug.LogWarning("[LevelManager] Nessun Tilemap: i nemici possono comparire ovunque.");
            return;
        }

        tm.CompressBounds();
        Bounds lb = tm.localBounds;
        Vector3 centerWorld = tm.transform.TransformPoint(lb.center);
        Vector3 sizeWorld = Vector3.Scale(lb.size, tm.transform.lossyScale);

        spawnArea = new Bounds(centerWorld, sizeWorld);
        spawnArea.Expand(-2f * areaMargin);   // si resta dentro il bordo
        hasArea = spawnArea.size.x > 0f && spawnArea.size.y > 0f;

        if (hasArea)
            Debug.Log($"[LevelManager] Area di comparsa da '{tm.name}': "
                      + $"x {spawnArea.min.x:F1}..{spawnArea.max.x:F1}, y {spawnArea.min.y:F1}..{spawnArea.max.y:F1}");
        else
            Debug.LogWarning("[LevelManager] Area troppo piccola dopo il margine: vincolo disattivato.");
    }

    private bool InsideArea(Vector3 pos)
    {
        if (!hasArea) return true;
        return pos.x >= spawnArea.min.x && pos.x <= spawnArea.max.x
            && pos.y >= spawnArea.min.y && pos.y <= spawnArea.max.y;
    }

    private Vector3 RandomRingPoint(Vector3 center)
    {
        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float r = UnityEngine.Random.Range(spawnMinDistance, spawnMaxDistance);
        return center + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
    }

    /// <summary>
    /// Libero = nessun collider solido nel raggio. La versione precedente guardava
    /// solo il primo collider trovato, quindi un trigger davanti a un muro faceva
    /// passare per buona una posizione dentro il muro.
    /// </summary>
    private bool IsFree(Vector3 pos)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(pos, spawnClearance, spawnBlockMask);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D c = cols[i];
            if (c == null || c.isTrigger) continue;
            return false;
        }
        return true;
    }

    private bool BlockedBetween(Vector3 a, Vector3 b)
    {
        RaycastHit2D[] hits = Physics2D.LinecastAll(a, b, spawnBlockMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (playerTransform != null && c.transform == playerTransform) continue;
            return true;
        }
        return false;
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
        if (pl == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pl.position, spawnMinDistance);
        Gizmos.DrawWireSphere(pl.position, spawnMaxDistance);
        if (hasArea)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        }
    }
}
