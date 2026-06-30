using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button guestButton;

    private void Start()
    {
        if (statusText != null) statusText.text = "";

        // riempi email se ricordata
        string remembered = PlayFabAuth.GetRememberedEmail();
        if (!string.IsNullOrEmpty(remembered) && emailInput != null)
            emailInput.text = remembered;

        PlayFabAuth.OnLoginSuccess += OnSuccess;
        PlayFabAuth.OnLoginFailed += OnFail;
    }

    private void OnDestroy()
    {
        PlayFabAuth.OnLoginSuccess -= OnSuccess;
        PlayFabAuth.OnLoginFailed -= OnFail;
    }

    public void OnLoginPressed()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Inserisci email e password.", true);
            return;
        }

        SetButtons(false);
        SetStatus("Login in corso...", false);
        PlayFabAuth.LoginWithEmail(email, pass);
    }

    public void OnRegisterPressed()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;
        string user = usernameInput.text.Trim();
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

        SetButtons(false);
        SetStatus("Registrazione in corso...", false);
        PlayFabAuth.Register(email, pass, user);
    }

    public void OnGuestPressed()
    {
        SetButtons(false);
        SetStatus("Login ospite...", false);
        PlayFabAuth.LoginAsGuest();
    }

    private void OnSuccess()
    {
        SetStatus("Login OK!", false);
        SceneManager.LoadScene("MainMenu");
    }

    private void OnFail(string err)
    {
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