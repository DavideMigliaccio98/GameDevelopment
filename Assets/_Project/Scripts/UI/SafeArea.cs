using UnityEngine;

/// <summary>
/// Restringe questo contenitore all'area realmente utilizzabile dello schermo,
/// quella che Android riporta in Screen.safeArea.
///
/// Serve sui telefoni con notch, foro per la fotocamera o barra di navigazione
/// a gesti: senza, la riga in alto dell'HUD (vita, punteggio, pausa) finisce
/// sotto la fotocamera, e i comandi in basso sotto la barra di sistema.
///
/// Va messo su un figlio del Canvas che contiene tutta la UI, non sul Canvas
/// stesso: il RectTransform del Canvas e' guidato dal Canvas e non si tocca.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    [Tooltip("Ricontrolla a ogni frame invece che solo ai cambi di orientamento. Lascia spento: costa e non serve.")]
    [SerializeField] private bool continuousCheck = false;

    private RectTransform rt;
    private Rect lastSafeArea = new Rect(0f, 0f, 0f, 0f);
    private Vector2Int lastResolution = Vector2Int.zero;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (!continuousCheck
            && Screen.safeArea == lastSafeArea
            && Screen.width == lastResolution.x
            && Screen.height == lastResolution.y
            && Screen.orientation == lastOrientation) return;

        Apply();
    }

    private void Apply()
    {
        if (rt == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        // In editor e su alcuni device safeArea puo' arrivare sballata: in quel
        // caso si lascia la UI a schermo pieno invece di deformarla.
        if (min.x < 0f || min.y < 0f || max.x > 1f || max.y > 1f) return;
        if (max.x - min.x <= 0f || max.y - min.y <= 0f) return;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
