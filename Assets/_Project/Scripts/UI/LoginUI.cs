using PlayFab;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Pannelli")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Login Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Register Fields")]
    [SerializeField] private TMP_InputField regEmailInput;
    [SerializeField] private TMP_InputField regPasswordInput;
    [SerializeField] private TMP_InputField regUsernameInput;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button guestButton;
    [SerializeField] private Button forgotButton;

    private bool isRegistering = false; // per sapere se stiamo registrando

    private void Start()
    {
        if (statusText != null) statusText.text = "";

        string remembered = PlayFabAuth.GetRememberedEmail();
        if (!string.IsNullOrEmpty(remembered) && emailInput != null)
            emailInput.text = remembered;

        PlayFabAuth.OnLoginSuccess += OnSuccess;
        PlayFabAuth.OnLoginFailed += OnFail;

        ShowLogin(); // parte dal login
    }

    private void OnDestroy()
    {
        PlayFabAuth.OnLoginSuccess -= OnSuccess;
        PlayFabAuth.OnLoginFailed -= OnFail;
    }

    // ---------- SWITCH PANNELLI ----------
    public void ShowLogin()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        SetStatus("", false);
    }

    public void ShowRegister()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        SetStatus("", false);
    }

    // ---------- LOGIN ----------
    public void OnLoginPressed()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Inserisci email e password.", true);
            return;
        }

        isRegistering = false;
        SetButtons(false);
        SetStatus("Login in corso...", false);
        PlayFabAuth.LoginWithEmail(email, pass);
    }

    // ---------- REGISTRAZIONE ----------
    /// <summary>
    /// I controlli si fanno qui, prima di chiamare il server.
    ///
    /// PlayFab, davanti a un campo sbagliato, risponde "Invalid input
    /// parameters": e' vero ma non dice QUALE campo, e l'utente resta a
    /// indovinare. Verificare le stesse regole in locale permette di dire
    /// subito cosa c'e' da cambiare, e senza aspettare il giro in rete.
    /// Il server ricontrolla comunque tutto: questo e' un aiuto, non una difesa.
    /// </summary>
    public void OnRegisterConfirmPressed()
    {
        string email = regEmailInput.text.Trim();
        string pass = regPasswordInput.text;
        string user = regUsernameInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(user))
        {
            SetStatus("Compila email, password e nome utente.", true);
            return;
        }

        string problem = FirstProblem(email, pass, user);
        if (problem != null)
        {
            SetStatus(problem, true);
            return;
        }

        isRegistering = true; // segnala che stiamo registrando
        SetButtons(false);
        SetStatus("Registrazione in corso...", false);
        PlayFabAuth.Register(email, pass, user);
    }

    /// <summary>
    /// Il primo campo che non va bene, con la sua regola. Null se e' tutto a posto.
    /// </summary>
    private static string FirstProblem(string email, string pass, string user)
    {
        if (!LooksLikeEmail(email))
            return "Indirizzo email non valido.";

        if (user.Length < PlayFabAuth.UsernameMinLength || user.Length > PlayFabAuth.UsernameMaxLength)
            return $"Nome utente: da {PlayFabAuth.UsernameMinLength} a "
                   + $"{PlayFabAuth.UsernameMaxLength} caratteri.";

        foreach (char c in user)
        {
            if (!char.IsLetterOrDigit(c))
                return "Nome utente: solo lettere e numeri, senza spazi ne' simboli.";
        }

        if (pass.Length < PlayFabAuth.PasswordMinLength || pass.Length > PlayFabAuth.PasswordMaxLength)
            return $"Password: da {PlayFabAuth.PasswordMinLength} a "
                   + $"{PlayFabAuth.PasswordMaxLength} caratteri.";

        return null;
    }

    // ---------- OSPITE ----------
    public void OnGuestPressed()
    {
        isRegistering = false;
        SetButtons(false);
        SetStatus("Login ospite...", false);
        PlayFabAuth.LoginAsGuest();
    }

    // ---------- RECUPERO PASSWORD ----------
    /// <summary>
    /// Due esiti soli, e detti chiaramente:
    /// l'email parte, oppure quell'indirizzo non va bene.
    ///
    /// C'era prima un messaggio volutamente vago ("se l'indirizzo e' registrato
    /// riceverai un'email"), pensato per non far capire a nessuno quali
    /// indirizzi esistono nel gioco. Non serviva a niente: PlayFab risponde
    /// InvalidEmailAddress per un indirizzo non registrato, quindi l'altro ramo
    /// lo diceva comunque. Il risultato era il peggiore dei due mondi, vago
    /// quando funzionava e esplicito quando no.
    /// </summary>
    public void OnForgotPasswordPressed()
    {
        string email = emailInput != null ? emailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email))
        {
            SetStatus("Scrivi la tua email qui sopra, poi premi di nuovo.", true);
            return;
        }

        // Controllo di forma prima di disturbare il server: un indirizzo senza
        // chiocciola o senza punto e' sbagliato e basta.
        if (!LooksLikeEmail(email))
        {
            SetStatus("Indirizzo email non valido.", true);
            return;
        }

        SetButtons(false);
        SetStatus("Invio email di recupero...", false);

        PlayFabAuth.SendPasswordReset(email, (ok, code, message) =>
        {
            SetButtons(true);

            if (ok)
            {
                SetStatus("Riceverai un'email con le istruzioni.", false);
                return;
            }

            if (IsUnknownAccount(code))
            {
                SetStatus("Indirizzo email non valido.", true);
                return;
            }

            SetStatus("Invio non riuscito: " + message, true);
        });
    }

    /// <summary>
    /// Controllo minimo della forma dell'indirizzo: una chiocciola, qualcosa
    /// prima, e un punto dopo. Non serve di piu': la verifica vera la fa il
    /// server, questa evita solo di chiamarlo per un errore di battitura.
    /// </summary>
    private static bool LooksLikeEmail(string value)
    {
        int at = value.IndexOf('@');
        if (at <= 0) return false;
        if (value.IndexOf('@', at + 1) >= 0) return false;   // piu' di una chiocciola

        int dot = value.IndexOf('.', at + 2);
        return dot > 0 && dot < value.Length - 1;
    }

    /// <summary>
    /// Errori che vogliono dire "questo indirizzo non risulta". PlayFab ne usa
    /// piu' di uno a seconda che l'account non esista, che esista senza email
    /// di contatto, o che l'indirizzo sia scritto male.
    /// </summary>
    private static bool IsUnknownAccount(PlayFabErrorCode code)
    {
        return code == PlayFabErrorCode.AccountNotFound
            || code == PlayFabErrorCode.NoContactEmailAddressFound
            || code == PlayFabErrorCode.InvalidEmailAddress;
    }

    // ---------- CALLBACK ----------
    private void OnSuccess()
    {
        if (isRegistering)
        {
            // Registrazione riuscita: NON entrare nel gioco, torna al login
            isRegistering = false;
            PlayFabAuth.Logout(); // scollega la sessione appena creata
            SetButtons(true);
            ShowLogin();
            // precompila l'email nel login
            if (emailInput != null) emailInput.text = regEmailInput.text.Trim();
            SetStatus("Registrazione completata! Ora accedi.", false);
        }
        else
        {
            // Login normale: entra nel gioco
            SetStatus("Login OK!", false);
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnFail(string err)
    {
        isRegistering = false;
        SetButtons(true);
        SetStatus(err, true);
    }

    private void SetStatus(string msg, bool error)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = error ? new Color(1f, 0.5f, 0.3f) : Color.white;
    }

    private void SetButtons(bool enabled)
    {
        if (loginButton != null) loginButton.interactable = enabled;
        if (registerButton != null) registerButton.interactable = enabled;
        if (guestButton != null) guestButton.interactable = enabled;
        if (forgotButton != null) forgotButton.interactable = enabled;
    }
}
