using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerIdText;

    private void Start()
    {
        // Rete di sicurezza: al menu principale ci si arriva da bottoni diversi
        // (pausa, game over, livello completato) e basta che uno solo si
        // dimentichi di chiudere la partita perche' il punteggio se lo porti
        // dietro. Qui vale sempre, comunque ci si sia arrivati.
        if (GameManager.Instance != null) GameManager.Instance.EndRun();

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

   /// <summary>
    /// Apre la schermata del profilo.
    ///
    /// Sta qui e non sul pannello perche' questo script vive su un oggetto
    /// sempre acceso: il pannello nasce spento, e chi e' spento non puo'
    /// agganciare da solo il bottone che dovrebbe accenderlo.
    /// </summary>
    public void OnProfile()
    {
        var profile = FindAnyObjectByType<ProfileUI>(FindObjectsInactive.Include);
        if (profile != null) profile.Show();
        else Debug.LogWarning("[MainMenu] Nessuna schermata profilo in questa scena.");
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