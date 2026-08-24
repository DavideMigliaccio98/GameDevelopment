using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Radar dei nemici, in alto a destra sotto il tasto pausa.
///
/// Serve perche' dal quarto livello in poi le ondate arrivano a diciotto e
/// venticinque nemici, e la parte difficile non e' combatterli ma capire da
/// dove stanno arrivando: lo schermo verticale inquadra pochissimo, e un nemico
/// alle spalle non si vede finche' non ti e' addosso.
///
/// Si costruisce da solo all'apertura di una scena di gioco, riconosciuta dalla
/// presenza di un LevelManager: negli interni e nei menu non compare. Cosi non
/// va aggiunto a mano in nessuna delle nove scene.
/// </summary>
public class EnemyRadar : MonoBehaviour
{
    [Header("Portata")]
    [Tooltip("Raggio in unita' di gioco coperto dal radar. I nemici piu' lontani " +
             "restano appoggiati al bordo, sbiaditi.")]
    [SerializeField] private float range = 22f;

    [Header("Aspetto")]
    [SerializeField] private float size = 240f;
    [Tooltip("Posizione rispetto all'angolo in alto a destra. La quota verticale " +
             "viene comunque abbassata se la targhetta dell'ondata sta piu' in giu'.")]
    [SerializeField] private Vector2 margin = new Vector2(-40f, -300f);
    [Tooltip("Spazio da lasciare sotto la targhetta dell'ondata.")]
    [SerializeField] private float gapUnderBanner = 26f;
    [SerializeField] private float dotSize = 14f;
    [SerializeField] private float playerDotSize = 16f;

    private static readonly Color Wood = new Color32(0x3B, 0x2A, 0x1B, 0xC4);
    private static readonly Color WoodInner = new Color32(0x4A, 0x36, 0x22, 0xFF);
    private static readonly Color Outline = new Color32(0x22, 0x17, 0x08, 0xFF);
    private static readonly Color Gold = new Color32(0xC9, 0xA2, 0x27, 0xFF);
    private static readonly Color EnemyDot = new Color32(0xE8, 0x48, 0x40, 0xFF);
    private static readonly Color PlayerDot = new Color32(0xF2, 0xE2, 0xB6, 0xFF);

    private RectTransform field;      // area circolare in cui si muovono i puntini
    private readonly List<Image> dots = new List<Image>();
    private Transform player;
    private Sprite dotSprite;

    private float nextFallbackScan;
    private readonly List<GameObject> fallbackEnemies = new List<GameObject>();

    // ------------------------------------------------------------------
    // Creazione automatica
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate();
    }

    private static void TryCreate()
    {
        // Solo nelle scene di gioco: il LevelManager e' quello che le distingue
        // dagli interni e dai menu.
        if (FindAnyObjectByType<LevelManager>() == null) return;
        if (FindAnyObjectByType<EnemyRadar>() != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform root = canvas.transform.Find("SafeArea");
        if (root == null) root = canvas.transform;

        var go = new GameObject("EnemyRadar", typeof(RectTransform));
        go.transform.SetParent(root, false);
        go.AddComponent<EnemyRadar>();
    }

    // ------------------------------------------------------------------

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = margin;
        rt.sizeDelta = new Vector2(size, size);
        PlaceBelowWaveBanner(rt);

        var frameGo = new GameObject("Quadrante", typeof(Image));
        frameGo.transform.SetParent(transform, false);
        var frameRt = (RectTransform)frameGo.transform;
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;

        var frame = frameGo.GetComponent<Image>();
        frame.sprite = BuildDialSprite();
        frame.raycastTarget = false;   // non deve rubare i tocchi

        // I puntini stanno in un contenitore leggermente piu' stretto del
        // quadrante, cosi non finiscono mai sopra la cornice dorata.
        var fieldGo = new GameObject("Puntini", typeof(RectTransform));
        fieldGo.transform.SetParent(transform, false);
        field = (RectTransform)fieldGo.transform;
        field.anchorMin = new Vector2(0.5f, 0.5f);
        field.anchorMax = new Vector2(0.5f, 0.5f);
        field.pivot = new Vector2(0.5f, 0.5f);
        field.anchoredPosition = Vector2.zero;
        field.sizeDelta = new Vector2(size, size);

        dotSprite = BuildDotSprite();

        // Il puntino del giocatore sta fisso al centro ed e' sempre il primo.
        var me = NewDot();
        me.color = PlayerDot;
        me.rectTransform.sizeDelta = new Vector2(playerDotSize, playerDotSize);
        me.rectTransform.anchoredPosition = Vector2.zero;
        me.enabled = true;
        me.gameObject.SetActive(true);
    }

    /// <summary>
    /// Abbassa il radar quanto basta a stare sotto la targhetta dell'ondata.
    ///
    /// La targhetta e' centrata e larga 560, quindi arriva a 820 px dal bordo
    /// sinistro; il radar, ancorato a destra, comincia a 800. Venti pixel di
    /// sovrapposizione, e il quadrante finiva sopra la scritta.
    ///
    /// Invece di fissare una quota buona oggi, la si ricava dalla targhetta:
    /// se un domani cambia altezza o posizione, il radar la segue.
    /// </summary>
    private void PlaceBelowWaveBanner(RectTransform rt)
    {
        Transform root = rt.parent;
        if (root == null) return;

        RectTransform plate = null;
        foreach (var candidate in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (candidate.name == "WavePlate") { plate = candidate; break; }
        }
        if (plate == null) return;

        // Entrambi sono ancorati al bordo superiore, quindi le quote sono confrontabili.
        // Con il pivot in alto il bordo inferiore sta a y - altezza; in generale
        // sta a y - altezza * pivot.
        float plateBottom = plate.anchoredPosition.y - plate.sizeDelta.y * plate.pivot.y;
        float wanted = plateBottom - gapUnderBanner;

        if (rt.anchoredPosition.y > wanted)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, wanted);
    }

    /// <summary>
    /// Quadrante disegnato a codice: niente file da importare, e a filtro Point
    /// il bordo resta scalettato come il resto della grafica del gioco.
    /// </summary>
    private static Sprite BuildDialSprite()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = (N - 1) * 0.5f;
        float r = c;
        var pixels = new Color32[N * N];

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                Color col;

                if (d > r) col = new Color(0f, 0f, 0f, 0f);
                else if (d > r - 3f) col = Gold;              // cornice
                else if (d > r - 5f) col = Outline;           // riga scura sotto la cornice
                else if (d > r * 0.52f && d < r * 0.56f) col = WoodInner;  // cerchio interno
                else col = Wood;

                pixels[y * N + x] = col;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite BuildDotSprite()
    {
        const int N = 4;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[N * N];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    private Image NewDot()
    {
        var go = new GameObject("Puntino", typeof(Image));
        go.transform.SetParent(field, false);

        var img = go.GetComponent<Image>();
        img.sprite = dotSprite;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dotSize, dotSize);

        dots.Add(img);
        return img;
    }

    // ------------------------------------------------------------------

    private void LateUpdate()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (player == null) return;
        }

        IReadOnlyList<GameObject> enemies = CurrentEnemies();
        float pixelsPerUnit = (size * 0.5f - dotSize) / range;
        Vector2 origin = player.position;

        int used = 1;   // 0 e' il puntino del giocatore, sta fermo al centro

        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            Vector2 delta = (Vector2)e.transform.position - origin;
            float dist = delta.magnitude;

            // Oltre la portata il puntino resta appoggiato al bordo e sbiadisce:
            // la direzione da cui arrivano conta anche quando sono lontani.
            float alpha = 1f;
            if (dist > range)
            {
                delta = delta.normalized * range;
                alpha = 0.45f;
            }

            Image dot = used < dots.Count ? dots[used] : NewDot();
            dot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            dot.rectTransform.anchoredPosition = delta * pixelsPerUnit;

            Color col = EnemyDot;
            col.a = alpha;
            dot.color = col;

            if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);
            used++;
        }

        for (int i = used; i < dots.Count; i++)
        {
            if (dots[i].gameObject.activeSelf) dots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// L'elenco dei nemici lo tiene gia' il LevelManager: chiederglielo costa
    /// nulla. La ricerca in scena e' solo un ripiego, e viene rifatta poche
    /// volte al secondo invece che a ogni fotogramma.
    /// </summary>
    private IReadOnlyList<GameObject> CurrentEnemies()
    {
        if (LevelManager.Instance != null) return LevelManager.Instance.ActiveEnemies;

        if (Time.unscaledTime >= nextFallbackScan)
        {
            nextFallbackScan = Time.unscaledTime + 0.25f;
            fallbackEnemies.Clear();
            foreach (var h in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
                if (h != null && !h.IsDead) fallbackEnemies.Add(h.gameObject);
        }
        return fallbackEnemies;
    }
}
