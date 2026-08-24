using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reazione a schermo quando il giocatore viene colpito.
///
/// Prima il colpo si notava solo da un lampeggio rosso di 0.12 secondi sullo
/// sprite: su un telefono, con il pollice sopra il personaggio, era invisibile.
/// Qui si aggiungono i segnali che si leggono anche senza guardare il
/// personaggio: bordo rosso a tutto schermo, micro-fermo immagine, scossa della
/// camera piu' decisa e una vibrazione corta.
///
/// Si costruisce da sola al primo colpo e sopravvive ai cambi scena, cosi non
/// va aggiunta a mano in nessuna delle scene di gioco.
/// </summary>
public class DamageFeedback : MonoBehaviour
{
    public static DamageFeedback Instance { get; private set; }

    [Header("Bordo rosso")]
    [SerializeField] private Color vignetteColor = new Color(0.72f, 0.05f, 0.05f);
    [Tooltip("Opacita' massima del bordo, per un colpo che toglie molta vita.")]
    [SerializeField] private float vignettePeak = 0.85f;
    [SerializeField] private float vignetteFade = 0.45f;

    [Header("Fermo immagine")]
    [Tooltip("Quanto rallenta il tempo nell'istante del colpo. 1 = niente fermo.")]
    [SerializeField] private float hitStopScale = 0.05f;
    [SerializeField] private float hitStopDuration = 0.06f;

    [Header("Scossa camera")]
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeMagnitude = 0.26f;

    [Header("Vibrazione")]
    [SerializeField] private bool vibrate = true;
    [SerializeField] private int vibrationMs = 45;
    [Tooltip("Se il telefono non accetta la vibrazione corta, usa quella lunga di sistema (mezzo secondo). Di solito e' troppo.")]
    [SerializeField] private bool allowLongVibration = false;

    private Image vignette;
    private Coroutine fadeRoutine;
    private Coroutine stopRoutine;

    /// <summary>
    /// Restituisce l'istanza, creandola al volo se non esiste ancora.
    /// </summary>
    public static DamageFeedback Ensure()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("DamageFeedback");
        DontDestroyOnLoad(go);
        // AddComponent fa partire Awake subito, ed e' li' che si costruisce
        // l'overlay: chiamare Build() anche qui creerebbe un secondo canvas.
        go.AddComponent<DamageFeedback>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (vignette == null) Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("Overlay", typeof(Canvas), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;   // sopra HUD e pannelli: e' un effetto, non UI

        var group = canvasGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var imgGo = new GameObject("Vignette", typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);

        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        vignette = imgGo.GetComponent<Image>();
        vignette.sprite = BuildVignetteSprite();
        vignette.raycastTarget = false;   // non deve rubare i tocchi al joystick
        vignette.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
    }

    /// <summary>
    /// Texture generata a codice: trasparente al centro, piena sui bordi. Cosi'
    /// non serve importare nessun file e non si tocca il centro dello schermo,
    /// dove sta l'azione.
    /// </summary>
    private static Sprite BuildVignetteSprite()
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[N * N];
        for (int y = 0; y < N; y++)
        {
            float v = y / (float)(N - 1);
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)(N - 1);
                float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 2f;
                float a = Mathf.Pow(1f - Mathf.Clamp01(edge), 2.5f);
                pixels[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// severity 0..1: quanto pesa il colpo rispetto alla vita totale.
    /// </summary>
    public void PlayerHit(float severity)
    {
        severity = Mathf.Clamp01(severity);
        float weight = Mathf.Lerp(0.55f, 1f, severity);   // anche un colpo leggero si deve vedere

        if (vignette != null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeVignette(vignettePeak * weight));
        }

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude * weight);

        if (stopRoutine == null && hitStopScale < 1f)
            stopRoutine = StartCoroutine(HitStop());

        if (vibrate) Vibrate(Mathf.RoundToInt(vibrationMs * weight));
    }

    private IEnumerator FadeVignette(float peak)
    {
        Color c = vignetteColor;
        c.a = peak;
        vignette.color = c;

        float t = 0f;
        while (t < vignetteFade)
        {
            t += Time.unscaledDeltaTime;      // deve scorrere anche durante il fermo immagine
            c.a = Mathf.Lerp(peak, 0f, t / vignetteFade);
            vignette.color = c;
            yield return null;
        }

        c.a = 0f;
        vignette.color = c;
        fadeRoutine = null;
    }

    /// <summary>
    /// Micro-fermo immagine: e' quello che da' peso al colpo.
    /// Alla fine il tempo si rimette a 1 SOLO se nel frattempo non l'ha toccato
    /// qualcun altro; altrimenti la schermata di game over, che mette timeScale
    /// a 0, verrebbe fatta ripartire da qui.
    /// </summary>
    private IEnumerator HitStop()
    {
        if (!Mathf.Approximately(Time.timeScale, 1f)) { stopRoutine = null; yield break; }

        Time.timeScale = hitStopScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);

        if (Mathf.Approximately(Time.timeScale, hitStopScale))
            Time.timeScale = 1f;

        stopRoutine = null;
    }

    // ------------------------------------------------------------------
    // Vibrazione
    // ------------------------------------------------------------------

    private void Vibrate(int ms)
    {
        if (ms <= 0) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (TryShortVibrate(ms)) return;
        if (allowLongVibration) Handheld.Vibrate();
#else
        // In editor non si vibra; il riferimento sotto serve solo a far
        // aggiungere a Unity il permesso VIBRATE nel manifest della build.
        if (allowLongVibration && Application.isMobilePlatform) Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdkInt = -1;

    /// <summary>
    /// Vibrazione breve tramite l'API di sistema: Handheld.Vibrate() dura mezzo
    /// secondo, che per un colpo incassato e' fastidioso.
    /// Se il permesso VIBRATE non c'e', la chiamata solleva un'eccezione che
    /// viene assorbita qui: il gioco continua senza vibrazione.
    /// </summary>
    private static bool TryShortVibrate(int ms)
    {
        try
        {
            if (vibrator == null)
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (vibrator == null) return false;
            if (!vibrator.Call<bool>("hasVibrator")) return false;

            if (sdkInt < 0)
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdkInt = version.GetStatic<int>("SDK_INT");

            if (sdkInt >= 26)
            {
                using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effects.CallStatic<AndroidJavaObject>(
                           "createOneShot", (long)ms, -1))   // -1 = DEFAULT_AMPLITUDE
                    vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", (long)ms);
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[DamageFeedback] Vibrazione non disponibile: " + e.Message);
            vibrator = null;
            return false;
        }
    }
#endif
}
