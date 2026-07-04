using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    
    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI scoreText;

    private bool isHandling = false;

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Auto-find player if not assigned
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerHealth>();
        }

        if (player != null) player.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (player != null) player.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (isHandling) return;
        isHandling = true;

        Time.timeScale = 0f; // freeza il gioco

        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        Debug.Log($"[GameOver] Score finale: {score}");

        // mostra il pannello
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (scoreText != null) scoreText.text = $"Score: {score}";

        if (!PlayFabAuth.IsLoggedIn)
        {
            Debug.LogWarning("[GameOver] Non loggato, salto save+submit.");
            return;
        }

        // salva/submit come prima
        PlayFabCloudSave.LoadBestScore(prevBest =>
        {
            if (score > prevBest)
                PlayFabCloudSave.SaveBestScore(score);

            PlayFabLeaderboard.SubmitScore(score);
        });
    }

    // chiamati dai bottoni
    public void OnRetry()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ResetScore();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnBackToMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ResetScore();
        SceneManager.LoadScene("MainMenu");
    }
}