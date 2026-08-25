using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Schermata del profilo: nome visualizzato, password e statistiche.
///
/// Tre cose che il giocatore si aspetta di trovare in un posto solo e che prima
/// erano sparse o assenti: il nome che compare in classifica si poteva scegliere
/// solo in fase di registrazione, la password si poteva cambiare solo dalla
/// schermata di accesso, e i propri risultati non si vedevano da nessuna parte.
///
/// I bottoni vengono agganciati a runtime invece che nell'editor: cosi la
/// schermata funziona appena il comando Oakheart la costruisce, senza dover
/// trascinare niente a mano nell'Inspector.
/// </summary>
public class ProfileUI : MonoBehaviour
{
    [Header("Nome")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button saveNameButton;

    [Header("Password")]
    [SerializeField] private Button passwordButton;

    [Header("Testi")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Chiusura")]
    [SerializeField] private Button closeButton;

    private const int NameMinLength = 3;
    private const int NameMaxLength = 25;

    // Le tre letture arrivano in momenti diversi: si compone quando ci sono tutte.
    private int bestScore, maxLevel, rank;
    private int pending;

    /// <summary>
    /// Awake gira alla PRIMA apertura, non all'avvio della scena: il pannello
    /// nasce spento, e su un oggetto spento Awake non viene chiamato.
    ///
    /// E' esattamente il motivo per cui la prima versione non funzionava:
    /// l'aggancio dei bottoni stava qui e non veniva mai eseguito. Ora ad aprire
    /// la schermata ci pensa MainMenuUI, che sta su un oggetto sempre acceso, e
    /// quando questa si accende Awake fa in tempo ad agganciare i propri bottoni
    /// prima che Show() vada avanti.
    /// </summary>
    private void Awake()
    {
        if (saveNameButton != null) saveNameButton.onClick.AddListener(OnSaveName);
        if (passwordButton != null) passwordButton.onClick.AddListener(OnChangePassword);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    // ------------------------------------------------------------------

    public void Show()
    {
        gameObject.SetActive(true);      // fa partire Awake, che aggancia i bottoni
        SetStatus("", false);

        if (nameInput != null)
        {
            nameInput.text = PlayFabAuth.DisplayName ?? "";
            nameInput.interactable = PlayFabAuth.IsLoggedIn;
        }
        if (saveNameButton != null) saveNameButton.interactable = PlayFabAuth.IsLoggedIn;
        if (passwordButton != null) passwordButton.interactable = PlayFabAuth.IsLoggedIn;

        if (!PlayFabAuth.IsLoggedIn)
        {
            if (statsText != null) statsText.text = "Sign in to see your stats.";
            return;
        }

        LoadStats();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // Nome visualizzato
    // ------------------------------------------------------------------

    public void OnSaveName()
    {
        if (nameInput == null) return;

        string wanted = nameInput.text.Trim();

        if (wanted.Length < NameMinLength || wanted.Length > NameMaxLength)
        {
            SetStatus($"Name: {NameMinLength} to {NameMaxLength} characters.", true);
            return;
        }

        if (wanted == PlayFabAuth.DisplayName)
        {
            SetStatus("That is already your name.", true);
            return;
        }

        SetButtons(false);
        SetStatus("Saving...", false);

        PlayFabAuth.UpdateDisplayName(wanted, (ok, message) =>
        {
            SetButtons(true);

            if (!ok)
            {
                SetStatus(message, true);
                return;
            }

            SetStatus("Name updated.", false);
            RefreshMenuName();
        });
    }

    /// <summary>
    /// Aggiorna la targhetta col nome in fondo al menu principale, che viene
    /// scritta all'apertura della scena e altrimenti resterebbe quella vecchia
    /// finche' non si riavvia il gioco.
    /// </summary>
    private void RefreshMenuName()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Transform found = FindDeep(canvas.transform, "PlayerIdText");
        if (found == null) return;

        var label = found.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = $"Player: {PlayFabAuth.DisplayName}";
    }

    // ------------------------------------------------------------------
    // Password
    // ------------------------------------------------------------------

    /// <summary>
    /// Manda il collegamento per reimpostare la password.
    ///
    /// Non e' una scelta di comodo: con PlayFab non esiste alcun modo di
    /// cambiare una password dall'applicazione. Il Client API non ha nessuna
    /// chiamata che lo faccia, e l'unica che esiste, Admin.ResetPassword,
    /// pretende un Token che viene generato e consegnato SOLO dall'email di
    /// reimpostazione. Nemmeno una funzione lato server potrebbe aggirarlo,
    /// perche' quel token non si puo' ottenere in altro modo.
    ///
    /// Che poi sia anche la scelta giusta e' un di piu': il collegamento via
    /// email dimostra che la casella e' davvero di chi la sta usando, e se
    /// qualcuno si siede al posto tuo con la sessione aperta non gli basta.
    /// </summary>
    public void OnChangePassword()
    {
        string email = PlayFabAuth.GetRememberedEmail();

        if (string.IsNullOrEmpty(email))
        {
            SetStatus("No email on this account. Sign in with email to change the password.", true);
            return;
        }

        SetButtons(false);
        SetStatus("Sending email...", false);

        PlayFabAuth.SendPasswordReset(email, (ok, code, message) =>
        {
            SetButtons(true);
            if (ok) SetStatus($"We sent a link to {Mask(email)}. Open it to set a new password.", false);
            else SetStatus(message, true);
        });
    }

    // ------------------------------------------------------------------
    // Statistiche
    // ------------------------------------------------------------------

    private void LoadStats()
    {
        if (statsText != null) statsText.text = "Loading...";

        bestScore = 0; maxLevel = 1; rank = -1;
        pending = 3;

        PlayFabCloudSave.LoadBestScore(value => { bestScore = value; StatArrived(); });
        PlayFabCloudSave.LoadMaxLevel(value => { maxLevel = value; StatArrived(); });
        PlayFabLeaderboard.GetMyRank((position, score) => { rank = position; StatArrived(); });
    }

    private void StatArrived()
    {
        pending--;
        if (pending > 0) return;
        if (statsText == null) return;

        var sb = new StringBuilder();
        sb.Append("Best score: ").Append(bestScore).Append('\n');

        // max_level_unlocked e' "il piu' alto sbloccato": i livelli finiti sono
        // quelli prima di quello. Con 99 addosso dal bottone di sviluppo il conto
        // sballerebbe, quindi si limita al numero di livelli che esistono.
        int completed = Mathf.Clamp(maxLevel - 1, 0, 5);
        sb.Append("Levels completed: ").Append(completed).Append(" / 5\n");

        sb.Append("Leaderboard: ").Append(rank > 0 ? "#" + rank : "not ranked yet");

        statsText.text = sb.ToString();
    }

    // ------------------------------------------------------------------

    /// <summary>
    /// Mostra l'indirizzo a cui e' partita l'email senza scriverlo per intero:
    /// serve a riconoscere la propria casella, non a farla leggere a chi passa.
    /// </summary>
    private static string Mask(string email)
    {
        int at = email.IndexOf('@');
        if (at <= 1) return email;

        string user = email.Substring(0, at);
        string domain = email.Substring(at);
        if (user.Length <= 2) return user[0] + "***" + domain;

        return user.Substring(0, 2) + new string('*', Mathf.Min(6, user.Length - 2)) + domain;
    }

    private void SetStatus(string message, bool error)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = error ? new Color(0.61f, 0.18f, 0.17f) : new Color(0.29f, 0.44f, 0.16f);
    }

    private void SetButtons(bool enabled)
    {
        if (saveNameButton != null) saveNameButton.interactable = enabled;
        if (passwordButton != null) passwordButton.interactable = enabled;
        if (closeButton != null) closeButton.interactable = enabled;
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
}
