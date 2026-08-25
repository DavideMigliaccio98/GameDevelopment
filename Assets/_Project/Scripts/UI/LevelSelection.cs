using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Modalita' infinita")]
    [Tooltip("La sesta voce dell'elenco. Resta chiusa finche' non si finisce l'ultimo livello.")]
    [SerializeField] private Button endlessButton;
    [Tooltip("Nome dell'asset LevelData della modalita' infinita, dentro una cartella Resources.")]
    [SerializeField] private string endlessResourceName = "LevelEndless";
    [Tooltip("Scena in cui gira la modalita' infinita.")]
    [SerializeField] private string endlessSceneName = "Game";

    private int maxUnlocked = 1;
    private bool isLoading = true; // NUOVO: stato caricamento

    // Il titolo del gioco, che sta nel Canvas accanto a questo pannello.
    private RectTransform title;
    private int titleOriginalIndex = -1;
    private Vector2 titleOriginalPos;
    private bool titleMoved;

    [Header("Titolo sopra il pannello")]
    [Tooltip("Spazio minimo tra il fondo del titolo e la targhetta del pannello.")]
    [SerializeField] private float titleGap = 90f;
    [Tooltip("Quanto puo' salire al massimo il titolo, misurato dal bordo alto.")]
    [SerializeField] private float titleTopMargin = 40f;
    [Tooltip("Scrive in Console le misure usate per posizionare il titolo. Solo per diagnosticare.")]
    [SerializeField] private bool logTitleFit = false;

    private void Awake()
    {
        if (panel != null) { panel.alpha = 0f; panel.gameObject.SetActive(false); }
    }

    private void OnEnable()
    {
        UpdateButtons();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (panel != null) { panel.gameObject.SetActive(true); panel.alpha = 1f; }

        BringTitleForward();

        if (PlayFabAuth.IsLoggedIn)
        {
            isLoading = true; // stato caricamento attivo
            if (statusText != null) statusText.text = "Loading progress...";
            UpdateButtons();

            PlayFabCloudSave.LoadMaxLevel(maxLevel =>
            {
                maxUnlocked = maxLevel;
                isLoading = false; // caricamento finito
                if (statusText != null) statusText.text = $"Levels unlocked: {maxUnlocked} / {allLevels.Length}";
                UpdateButtons();
            });
        }
        else
        {
            isLoading = false;
            maxUnlocked = 1;
            if (statusText != null) statusText.text = "Guest mode";
            UpdateButtons();
        }
    }

    public void Hide()
    {
        if (panel != null) { panel.alpha = 0f; panel.gameObject.SetActive(false); }
        RestoreTitle();
    }

    /// <summary>
    /// Porta il titolo del gioco davanti al pannello, e se serve lo alza.
    ///
    /// Il titolo non spariva: il pannello e' a schermo intero e si porta dietro
    /// il proprio fondale, quindi lo copriva. Disegnarlo dopo risolve meta' del
    /// problema.
    ///
    /// L'altra meta' e' che il riquadro dei livelli e' centrato in verticale
    /// mentre il titolo e' ancorato in alto. Su uno schermo alto e stretto come
    /// il telefono il riquadro sta basso e i due non si toccano; su uno schermo
    /// piu' tozzo il riquadro sale e ci finisce addosso. Una quota fissa
    /// funzionerebbe su un formato solo.
    ///
    /// Quindi la quota non si sceglie, si misura: si guarda dove comincia
    /// davvero la targhetta del pannello e si alza il titolo quel tanto che
    /// serve a stargli sopra, non un pixel di piu'. Sul telefono non si muove
    /// affatto, perche' li' lo spazio c'e' gia'.
    /// </summary>
    private void BringTitleForward()
    {
        if (title == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Transform found = canvas.transform.Find("Title");
            title = found as RectTransform;
            if (title == null) return;
        }

        if (titleOriginalIndex < 0)
        {
            titleOriginalIndex = title.GetSiblingIndex();
            titleOriginalPos = title.anchoredPosition;
        }

        title.SetAsLastSibling();
        LiftTitleAbovePanel();
        titleMoved = true;
    }

    /// <summary>
    /// Alza il titolo finche' il suo TESTO non sta sopra la targhetta del pannello.
    ///
    /// La prima versione misurava il riquadro del titolo invece del testo, e il
    /// riquadro e' 800x250 mentre la scritta ne occupa una sessantina in mezzo.
    /// Chiedere che stesse sopra la targhetta tutto il riquadro voleva dire
    /// chiedere uno spostamento enorme, che sbatteva contro il limite in alto e
    /// si fermava a meta': il testo restava addosso alla targhetta lo stesso.
    ///
    /// Qui si misurano i confini reali della scritta, quelli che TextMeshPro
    /// calcola dopo aver composto le lettere. Il conto e' in coordinate di
    /// mondo, cosi vale anche se titolo e targhetta hanno ancoraggi diversi,
    /// che qui e' proprio il caso.
    /// </summary>
    private void LiftTitleAbovePanel()
    {
        title.anchoredPosition = titleOriginalPos;

        RectTransform plate = FindPanelTop();
        if (plate == null) return;

        float scale = title.lossyScale.y;
        if (scale <= 0.0001f) return;

        float textBottom, textTop;
        MeasureTitle(out textBottom, out textTop);

        var plateCorners = new Vector3[4];
        plate.GetWorldCorners(plateCorners);
        float plateTop = plateCorners[1].y;              // angolo in alto a sinistra

        float needed = (plateTop + titleGap * scale) - textBottom;
        if (needed <= 0f)
        {
            if (logTitleFit) Debug.Log("[LevelSelection] Titolo gia' sopra la targhetta: non lo muovo.");
            return;
        }

        // Limite: la scritta non deve uscire dal bordo alto dello schermo.
        float room = float.MaxValue;
        RectTransform parent = title.parent as RectTransform;
        if (parent != null)
        {
            var parentCorners = new Vector3[4];
            parent.GetWorldCorners(parentCorners);
            float canvasTop = parentCorners[1].y;
            room = (canvasTop - titleTopMargin * scale) - textTop;
        }

        float applied = Mathf.Min(needed, room);
        if (applied <= 0f)
        {
            if (logTitleFit)
                Debug.LogWarning($"[LevelSelection] Non c'e' spazio sopra la targhetta: "
                                 + $"servirebbero {needed / scale:F0}, disponibili {room / scale:F0}.");
            return;
        }

        title.anchoredPosition = titleOriginalPos + new Vector2(0f, applied / scale);

        if (logTitleFit)
            Debug.Log($"[LevelSelection] Titolo alzato di {applied / scale:F0} "
                      + $"(servivano {needed / scale:F0}, disponibili {room / scale:F0}). "
                      + $"y da {titleOriginalPos.y:F0} a {title.anchoredPosition.y:F0}.");
    }

    /// <summary>
    /// I confini verticali della scritta, non del riquadro che la contiene.
    /// Se per qualche motivo non si trova il testo si ripiega sul riquadro.
    /// </summary>
    private void MeasureTitle(out float bottom, out float top)
    {
        TMPro.TMP_Text label = title.GetComponentInChildren<TMPro.TMP_Text>();
        if (label != null)
        {
            label.ForceMeshUpdate();
            Bounds b = label.textBounds;
            if (b.size.y > 0.0001f)
            {
                bottom = label.transform.TransformPoint(new Vector3(0f, b.min.y, 0f)).y;
                top = label.transform.TransformPoint(new Vector3(0f, b.max.y, 0f)).y;
                return;
            }
        }

        var corners = new Vector3[4];
        title.GetWorldCorners(corners);
        bottom = corners[0].y;
        top = corners[1].y;
    }

    /// <summary>
    /// Il bordo superiore del pannello: la targhetta se c'e', altrimenti il riquadro.
    /// </summary>
    private RectTransform FindPanelTop()
    {
        Transform root = panel != null ? panel.transform : transform;

        string[] candidates = { "LSPlate", "LSTitle", "LSBox" };
        for (int i = 0; i < candidates.Length; i++)
        {
            Transform t = FindDeep(root, candidates[i]);
            RectTransform rt = t as RectTransform;
            if (rt != null) return rt;
        }
        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private void RestoreTitle()
    {
        if (title == null || !titleMoved) return;
        if (titleOriginalIndex >= 0) title.SetSiblingIndex(titleOriginalIndex);
        title.anchoredPosition = titleOriginalPos;
        titleMoved = false;
    }

    private void UpdateButtons()
    {
        if (levelButtons == null) return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            int levelIndex = i;

            var label = levelButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            if (isLoading)
            {
                // Durante il caricamento: bottoni neutri, non interagibili
                levelButtons[i].interactable = false;
                if (label != null) label.text = $"LEVEL {i + 1}"; // mostra sempre il numero, senza [BLOCCATO]
            }
            else
            {
                bool unlocked = (i + 1) <= maxUnlocked;
                levelButtons[i].interactable = unlocked;
                if (label != null)
                {
                    if (unlocked)
                        label.text = $"LEVEL {i + 1}";
                    else
                        label.text = $"LEVEL {i + 1}  [ LOCKED ]"; // o "[ BLOCCATO ]"
                }
            }

            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => LoadLevel(levelIndex));
        }

        UpdateEndlessButton();
    }

    /// <summary>
    /// La voce della modalita' infinita.
    ///
    /// Si apre quando si e' finito l'ultimo livello. Il conto e' lo stesso che
    /// sblocca i livelli: PlayFab tiene un numero, "il piu' alto raggiunto", e
    /// finito il quinto quel numero diventa sei. Quindi la condizione e'
    /// semplicemente "il numero supera quanti livelli ci sono".
    ///
    /// Il bottone resta visibile anche da chiuso, col lucchetto, come gli altri:
    /// nascondere una cosa che esiste fa credere che non ci sia.
    /// </summary>
    private void UpdateEndlessButton()
    {
        if (endlessButton == null) return;

        int levels = allLevels != null ? allLevels.Length : 5;
        bool unlocked = !isLoading && maxUnlocked > levels;

        endlessButton.interactable = unlocked;

        var label = endlessButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = unlocked ? "ENDLESS" : "ENDLESS  [ LOCKED ]";

        endlessButton.onClick.RemoveAllListeners();
        endlessButton.onClick.AddListener(OnEndlessPressed);
    }

    /// <summary>
    /// Avvia la modalita' infinita.
    ///
    /// Il punteggio si azzera: e' una partita nuova, non il seguito di quella
    /// appena finita, e portarsi dietro i punti dei cinque livelli falserebbe
    /// la classifica.
    /// </summary>
    public void OnEndlessPressed()
    {
        var endless = Resources.Load<LevelData>(endlessResourceName);
        if (endless == null)
        {
            Debug.LogError($"[LevelSelection] Non trovo '{endlessResourceName}' in Resources: "
                           + "la modalita' infinita non parte.");
            if (statusText != null) statusText.text = "Endless mode is not available.";
            return;
        }

        if (GameManager.Instance != null) GameManager.Instance.EndRun();
        SelectedLevel.Current = endless;
        Time.timeScale = 1f;
        SceneManager.LoadScene(endlessSceneName);
    }

    private void LoadLevel(int index)
    {
        if (index < 0 || index >= allLevels.Length) return;
        LevelData lvl = allLevels[index];
        SelectedLevel.Current = lvl;
        SceneManager.LoadScene(lvl.sceneName);
    }
}

public static class SelectedLevel
{
    public static LevelData Current;
}