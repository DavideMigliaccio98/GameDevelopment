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

        MenuTitleLift.Raise(panel != null ? panel.transform : transform,
                            TitlePlates, titleGap, titleTopMargin, logTitleFit);

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
        MenuTitleLift.Restore();
    }

    private static readonly string[] TitlePlates = { "LSPlate", "LSTitle", "LSBox" };

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