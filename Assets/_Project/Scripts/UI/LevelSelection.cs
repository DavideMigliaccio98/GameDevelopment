using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] private LevelData[] allLevels;
    [SerializeField] private Button[] levelButtons; // un bottone per livello (in ordine)
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI statusText;

    private int maxUnlocked = 1;

    private void Awake()
    {
        if (panel != null) { panel.alpha = 0f; panel.gameObject.SetActive(false); }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (panel != null) { panel.gameObject.SetActive(true); panel.alpha = 1f; }
        if (statusText != null) statusText.text = "Caricamento progressi...";
        SetAllButtons(false);

        PlayFabCloudSave.LoadMaxLevel(maxLevel =>
        {
            maxUnlocked = maxLevel;
            if (statusText != null) statusText.text = $"Livelli sbloccati: {maxUnlocked} / {allLevels.Length}";
            UpdateButtons();
        });
    }

    public void Hide()
    {
        if (panel != null) { panel.alpha = 0f; panel.gameObject.SetActive(false); }
    }

    private void UpdateButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i; // copia locale per la closure
            bool unlocked = (i + 1) <= maxUnlocked;
            levelButtons[i].interactable = unlocked;
            // setta il testo
            var label = levelButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (unlocked)
                    label.text = $"LIVELLO {i + 1}";
                else
                    label.text = $"[ BLOCCATO ] {i + 1}";
            }
            // listener
            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => LoadLevel(levelIndex));
        }
    }

    private void SetAllButtons(bool enabled)
    {
        foreach (var b in levelButtons) b.interactable = enabled;
    }

    private void LoadLevel(int index)
    {
        if (index < 0 || index >= allLevels.Length) return;
        LevelData lvl = allLevels[index];

        // salviamo il livello scelto da qualche parte (statico)
        SelectedLevel.Current = lvl;
        SceneManager.LoadScene(lvl.sceneName);
    }
}

// Mini-classe statica per passare il livello scelto alla scena Game
public static class SelectedLevel
{
    public static LevelData Current;
}