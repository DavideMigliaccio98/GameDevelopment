using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject endlessButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float fadeDuration = 0.5f;

    [SerializeField] private GameObject nextButton;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelCompleted += Show;
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelCompleted -= Show;
        }
    }

    private void Show()
    {
        if (panel == null) return;
        panel.SetActive(true);

        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        if (scoreText != null) scoreText.text = $"Score: {score}";

        bool isLastLevel = SelectedLevel.Current != null && SelectedLevel.Current.levelNumber == 5;
        if (titleText != null)
            titleText.text = isLastLevel ? "VITTORIA!" : "LIVELLO COMPLETATO!";
        if (endlessButton != null)
            endlessButton.SetActive(isLastLevel);

        if (nextButton != null)
            nextButton.SetActive(!isLastLevel);  // nascondi se ultimo livello

        if (PlayFabAuth.IsLoggedIn)
        {
            PlayFabCloudSave.LoadBestScore(prevBest =>
            {
                if (score > prevBest) PlayFabCloudSave.SaveBestScore(score);
            });
            PlayFabLeaderboard.SubmitScore(score);

            if (SelectedLevel.Current != null)
            {
                int currentLevelNum = SelectedLevel.Current.levelNumber;
                PlayFabCloudSave.LoadMaxLevel(currentMax =>
                {
                    int newMax = Mathf.Max(currentMax, currentLevelNum + 1);
                    if (newMax > currentMax)
                    {
                        PlayFabCloudSave.SaveMaxLevel(newMax);
                        Debug.Log($"[LevelComplete] Sbloccato livello {newMax}!");
                    }
                });
            }
        }

        StartCoroutine(FadeIn());
    }

    public void OnEndless()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ResetScore();
        // assegna il LevelData Endless (lo passiamo via SelectedLevel)
        var endless = Resources.Load<LevelData>("LevelEndless");
        if (endless == null)
        {
            // fallback: cerca tra tutti gli asset
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("LevelEndless t:LevelData");
            if (guids.Length > 0)
                endless = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            #endif
        }
        if (endless != null) SelectedLevel.Current = endless;
        SceneManager.LoadScene("Game");
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void OnNextLevel()
    {
        Time.timeScale = 1f;
        // NON resettiamo lo score: lo accumuliamo tra livelli

        if (SelectedLevel.Current == null)
        {
            Debug.LogWarning("[LevelComplete] SelectedLevel.Current è null, ricarico Game di default");
            SceneManager.LoadScene("Game");
            return;
        }

        int currentNum = SelectedLevel.Current.levelNumber;
        int nextNum = currentNum + 1;

        // cerca il LevelData del prossimo livello tra le risorse
        var nextLevel = Resources.Load<LevelData>($"Level{nextNum}");
        if (nextLevel == null)
        {
            // fallback: cerca in tutti gli asset con AssetDatabase (solo Editor)
            #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets($"Level{nextNum} t:LevelData");
            if (guids.Length > 0)
                nextLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            #endif
        }

        if (nextLevel == null)
        {
            Debug.LogWarning($"[LevelComplete] Level{nextNum} non trovato, torno al menu");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        SelectedLevel.Current = nextLevel;
        Debug.Log($"[LevelComplete] Carico Livello {nextNum} ({nextLevel.sceneName})");
        SceneManager.LoadScene(nextLevel.sceneName);
    }

    public void OnBackToMenu()
    {
        Time.timeScale = 1f;
        // se vuoi resettare lo score quando torni al menu, decommenta:
        // if (GameManager.Instance != null) GameManager.Instance.ResetScore();
        SceneManager.LoadScene("MainMenu");
    }
}