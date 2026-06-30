using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private float delayBeforeMenu = 2f;

    private bool isHandling = false;

    private void OnEnable()
    {
        if (player != null) player.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (player != null) player.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (isHandling) return;
        isHandling = true;

        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        Debug.Log($"[GameOver] Score finale: {score}");

        if (!PlayFabAuth.IsLoggedIn)
        {
            Debug.LogWarning("[GameOver] Non loggato, salto save+submit. Avvia da Boot scene.");
            StartCoroutine(BackToMenu());
            return;
        }

        // 1. Carica best precedente, salva solo se score migliore
        PlayFabCloudSave.LoadBestScore(prevBest =>
        {
            if (score > prevBest)
                PlayFabCloudSave.SaveBestScore(score);

            // 2. Submit alla leaderboard (PlayFab tiene il max grazie ad Aggregation Method)
            PlayFabLeaderboard.SubmitScore(score, () =>
            {
                StartCoroutine(BackToMenu());
            });
        });
    }

    private IEnumerator BackToMenu()
    {
        yield return new WaitForSeconds(delayBeforeMenu);
        if (GameManager.Instance != null) GameManager.Instance.ResetScore();
        SceneManager.LoadScene("MainMenu");
    }
}