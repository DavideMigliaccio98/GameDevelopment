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

    private int maxUnlocked = 1;
    private bool isLoading = true; // NUOVO: stato caricamento

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

        if (PlayFabAuth.IsLoggedIn)
        {
            isLoading = true; // stato caricamento attivo
            if (statusText != null) statusText.text = "Caricamento progressi...";
            UpdateButtons();

            PlayFabCloudSave.LoadMaxLevel(maxLevel =>
            {
                maxUnlocked = maxLevel;
                isLoading = false; // caricamento finito
                if (statusText != null) statusText.text = $"Livelli sbloccati: {maxUnlocked} / {allLevels.Length}";
                UpdateButtons();
            });
        }
        else
        {
            isLoading = false;
            maxUnlocked = 1;
            if (statusText != null) statusText.text = "Modalità ospite";
            UpdateButtons();
        }
    }

    public void Hide()
    {
        if (panel != null) { panel.alpha = 0f; panel.gameObject.SetActive(false); }
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
                if (label != null) label.text = $"LIVELLO {i + 1}"; // mostra sempre il numero, senza [BLOCCATO]
            }
            else
            {
                bool unlocked = (i + 1) <= maxUnlocked;
                levelButtons[i].interactable = unlocked;
                if (label != null)
                {
                    if (unlocked)
                        label.text = $"LIVELLO {i + 1}";
                    else
                        label.text = $"LIVELLO {i + 1}  [ BLOCCATO ]"; // o "[ BLOCCATO ]"
                }
            }

            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => LoadLevel(levelIndex));
        }
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