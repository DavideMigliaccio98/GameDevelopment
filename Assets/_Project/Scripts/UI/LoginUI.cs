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
    public void OnRegisterConfirmPressed()
    {
        string email = regEmailInput.text.Trim();
        string pass = regPasswordInput.text;
        string user = regUsernameInput.text.Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(user))
        {
            SetStatus("Compila email, password e username.", true);
            return;
        }
        if (pass.Length < 6)
        {
            SetStatus("Password almeno 6 caratteri.", true);
            return;
        }

        isRegistering = true; // segnala che stiamo registrando
        SetButtons(false);
        SetStatus("Registrazione in corso...", false);
        PlayFabAuth.Register(email, pass, user);
    }

    // ---------- OSPITE ----------
    public void OnGuestPressed()
    {
        isRegistering = false;
        SetButtons(false);
        SetStatus("Login ospite...", false);
        PlayFabAuth.LoginAsGuest();
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
    }
}