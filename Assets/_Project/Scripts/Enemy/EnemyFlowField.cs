using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mappa di navigazione condivisa da tutti i nemici.
///
/// Il problema che risolve: l'aggiramento con le antenne (guardo avanti, se e'
/// occupato provo di lato) e' cieco. Vede solo i due metri davanti al muso,
/// quindi contro un ostacolo grande o a forma di U il nemico entra nella
/// rientranza, sbatte, prova a destra, prova a sinistra, e resta li' a
/// oscillare: per uscirne dovrebbe ALLONTANARSI dal giocatore, e nessuna regola
/// locale glielo dira' mai.
///
/// Qui invece la mappa viene guardata dall'alto una volta sola. All'avvio del
/// livello l'area giocabile viene divisa in caselle e si segna quali sono
/// occupate da muri, rocce o vegetazione. Poi, a intervalli, si parte dalla
/// casella del giocatore e si conta a onde quante caselle servono per
/// raggiungerla da ogni punto della mappa: una visita in ampiezza, la stessa
/// cosa che fa l'acqua che si espande.
///
/// A quel punto un nemico non deve piu' ragionare: guarda le otto caselle
/// attorno a se' e va in quella col numero piu' basso. Scende sempre verso il
/// giocatore, esce da solo dalle rientranze, e gira attorno agli ostacoli dal
/// lato giusto, perche' quello sbagliato ha numeri piu' alti.
///
/// Costo: una visita in ampiezza su qualche migliaio di caselle, poche volte al
/// secondo, UNA per tutti i nemici invece di un ragionamento per ciascuno.
/// Con venticinque nemici costa meno del sistema di prima.
/// </summary>
public class EnemyFlowField : MonoBehaviour
{
    public static EnemyFlowField Instance { get; private set; }

    [Header("Griglia")]
    [Tooltip("Lato della casella in unita' di gioco. Piu' piccolo = percorsi piu' precisi " +
             "ma piu' caselle da visitare.")]
    [SerializeField] private float cellSize = 0.5f;

    [Tooltip("Cosa conta come muro. Di serie il solo layer Default, dove stanno muri, " +
             "rocce, vegetazione e tilemap solidi.")]
    [SerializeField] private LayerMask obstacleMask = 1;

    [Tooltip("Ogni quanto rifare il conteggio, se il giocatore ha cambiato casella.")]
    [SerializeField] private float refreshInterval = 0.15f;

    [Tooltip("Disegna la griglia in Scene view quando l'oggetto e' selezionato. Pesante.")]
    [SerializeField] private bool drawGizmos = false;

    private const int Unreachable = int.MaxValue;

    private Bounds area;
    private int width, height;
    private bool[] blocked;
    private int[] distance;
    private int[] queue;

    private Transform player;
    private int lastPlayerCell = -1;
    private float nextRefresh;
    private bool ready;

    // Otto direzioni: le quattro dritte e le quattro diagonali.
    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    /// <summary>
    /// Crea la mappa per la scena corrente. La chiama LevelManager, che e' l'unico
    /// a sapere gia' quanto e' grande l'area giocabile.
    /// </summary>
    public static void Create(Bounds worldArea)
    {
        if (Instance != null) return;
        var go = new GameObject("EnemyFlowField");
        var field = go.AddComponent<EnemyFlowField>();
        field.Build(worldArea);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------------
    // Costruzione
    // ------------------------------------------------------------------

    private void Build(Bounds worldArea)
    {
        area = worldArea;
        width = Mathf.Max(1, Mathf.CeilToInt(area.size.x / cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(area.size.y / cellSize));

        int count = width * height;
        blocked = new bool[count];
        distance = new int[count];
        queue = new int[count];

        var filter = new ContactFilter2D();
        filter.useTriggers = false;          // porte e zone di passaggio non sono muri
        filter.SetLayerMask(obstacleMask);
        filter.useLayerMask = true;

        var hits = new List<Collider2D>(4);
        Vector2 box = Vector2.one * (cellSize * 0.9f);
        int solid = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 center = CellCenter(x, y);
                hits.Clear();
                Physics2D.OverlapBox(center, box, 0f, filter, hits);

                bool isSolid = false;
                for (int i = 0; i < hits.Count; i++)
                {
                    Collider2D c = hits[i];
                    if (c == null) continue;

                    // Giocatore e nemici hanno un corpo dinamico: si muovono, non
                    // sono muri, e segnarli qui congelerebbe nella mappa la loro
                    // posizione al momento della costruzione.
                    var rb = c.attachedRigidbody;
                    if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) continue;

                    isSolid = true;
                    break;
                }

                blocked[y * width + x] = isSolid;
                if (isSolid) solid++;
            }
        }

        ready = true;
        Debug.Log($"[EnemyFlowField] Griglia {width}x{height} ({count} caselle, {solid} occupate) "
                  + $"su x {area.min.x:F1}..{area.max.x:F1}, y {area.min.y:F1}..{area.max.y:F1}.");
    }

    // ------------------------------------------------------------------
    // Aggiornamento
    // ------------------------------------------------------------------

    private void LateUpdate()
    {
        if (!ready) return;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + refreshInterval;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            player = p.transform;
        }

        int cell = CellIndex(player.position);
        if (cell < 0) return;

        // Se il giocatore non ha cambiato casella, il conteggio precedente vale ancora.
        if (cell == lastPlayerCell) return;
        lastPlayerCell = cell;

        Flood(cell);
    }

    /// <summary>
    /// Visita in ampiezza a partire dalla casella del giocatore.
    ///
    /// Ogni casella riceve il numero di passi che servono per arrivare a lui.
    /// Le diagonali sono ammesse solo se sono libere anche le due caselle dritte
    /// che le compongono: senza questa regola i nemici tagliano gli spigoli e
    /// finiscono con mezzo corpo dentro il muro.
    /// </summary>
    private void Flood(int start)
    {
        for (int i = 0; i < distance.Length; i++) distance[i] = Unreachable;

        // La casella del giocatore puo' risultare occupata se lui sta rasente a un
        // muro: in quel caso si parte lo stesso, altrimenti non lo raggiunge nessuno.
        distance[start] = 0;

        int head = 0, tail = 0;
        queue[tail++] = start;

        while (head < tail)
        {
            int current = queue[head++];
            int cx = current % width;
            int cy = current / width;
            int next = distance[current] + 1;

            for (int d = 0; d < 8; d++)
            {
                int nx = cx + DX[d];
                int ny = cy + DY[d];
                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

                int ni = ny * width + nx;
                if (blocked[ni] || distance[ni] != Unreachable) continue;

                // niente tagli d'angolo
                if (DX[d] != 0 && DY[d] != 0)
                {
                    if (blocked[cy * width + nx] || blocked[ny * width + cx]) continue;
                }

                distance[ni] = next;
                queue[tail++] = ni;
            }
        }
    }

    // ------------------------------------------------------------------
    // Interrogazione
    // ------------------------------------------------------------------

    /// <summary>
    /// La direzione da prendere per avvicinarsi al giocatore.
    /// Falso se il punto e' fuori dalla mappa o se da li' non si arriva a lui.
    /// </summary>
    public bool TryGetDirection(Vector2 from, out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!ready) return false;

        int cell = CellIndex(from);
        if (cell < 0) return false;

        int cx = cell % width;
        int cy = cell / width;

        int best = distance[cell];
        int bestCell = -1;

        for (int d = 0; d < 8; d++)
        {
            int nx = cx + DX[d];
            int ny = cy + DY[d];
            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

            int ni = ny * width + nx;
            if (blocked[ni]) continue;

            if (DX[d] != 0 && DY[d] != 0)
            {
                if (blocked[cy * width + nx] || blocked[ny * width + cx]) continue;
            }

            if (distance[ni] < best) { best = distance[ni]; bestCell = ni; }
        }

        if (bestCell < 0) return false;   // gia' addosso al giocatore, oppure chiuso fuori

        Vector2 target = CellCenter(bestCell % width, bestCell / width);
        Vector2 delta = target - from;
        if (delta.sqrMagnitude < 0.000001f) return false;

        direction = delta.normalized;
        return true;
    }

    // ------------------------------------------------------------------

    private Vector2 CellCenter(int x, int y)
    {
        return new Vector2(
            area.min.x + (x + 0.5f) * cellSize,
            area.min.y + (y + 0.5f) * cellSize);
    }

    private int CellIndex(Vector2 world)
    {
        int x = Mathf.FloorToInt((world.x - area.min.x) / cellSize);
        int y = Mathf.FloorToInt((world.y - area.min.y) / cellSize);
        if (x < 0 || y < 0 || x >= width || y >= height) return -1;
        return y * width + x;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !ready) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                if (blocked[i]) Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                else if (distance[i] == Unreachable) Gizmos.color = new Color(0f, 0f, 0f, 0.25f);
                else Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.12f);

                Gizmos.DrawCube(CellCenter(x, y), Vector3.one * cellSize * 0.9f);
            }
        }
    }
}
