using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerIdText;

    private void Start()
    {
        if (PlayFabAuth.IsLoggedIn && playerIdText != null)
        {
            string nameToShow;
            if (!string.IsNullOrEmpty(PlayFabAuth.DisplayName))
                nameToShow = PlayFabAuth.DisplayName;
            else
                nameToShow = PlayFabAuth.PlayerId.Length > 6 
                    ? PlayFabAuth.PlayerId.Substring(0, 6) 
                    : PlayFabAuth.PlayerId;

            playerIdText.text = $"Player: {nameToShow}";
        }
        else if (playerIdText != null)
        {
            playerIdText.text = "Player: ...";
        }
    }

   public void OnPlay()
    {
        var ls = FindAnyObjectByType<LevelSelection>(FindObjectsInactive.Include);
        if (ls != null) ls.Show();
    }

   public void OnLeaderboard()
    {
        var lb = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
        if (lb != null) lb.Show();
    }

    public void OnQuit()
    {
        // Logout: rimuove credenziali e torna a Login
        if (PlayFabAuth.IsLoggedIn)
        {
            PlayFab.PlayFabClientAPI.ForgetAllCredentials();
            Debug.Log("[Quit] Logout effettuato");
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
    }


    public void OnDevUnlockAll()
    {
        if (!PlayFabAuth.IsLoggedIn)
        {
            Debug.LogWarning("[DEV] Non loggato, impossibile sbloccare livelli.");
            return;
        }

        PlayFabCloudSave.SaveMaxLevel(99, () =>
        {
            Debug.Log("[DEV] Tutti i livelli sbloccati!");
        });
    }
}