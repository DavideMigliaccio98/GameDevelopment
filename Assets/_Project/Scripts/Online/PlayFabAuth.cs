using System;
using System.Collections.Generic;
using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabAuth
{
    private const string CUSTOM_ID_KEY = "playfab_custom_id";
    private const string EMAIL_KEY = "playfab_remember_email";

    // Regole di PlayFab, riportate qui per poterle dire all'utente PRIMA di
    // fargli fare il giro fino al server e tornare con un errore generico.
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 20;
    public const int PasswordMinLength = 6;
    public const int PasswordMaxLength = 100;

    public static bool IsLoggedIn { get; private set; } = false;
    public static string PlayerId { get; private set; } = "";
    public static string DisplayName { get; private set; } = "";

    public static event Action OnLoginSuccess;
    public static event Action<string> OnLoginFailed;

    // ----------- LOGIN OSPITE (Custom ID anonimo) -----------
    public static void LoginAsGuest()
    {
        string customId = PlayerPrefs.GetString(CUSTOM_ID_KEY, "");
        if (string.IsNullOrEmpty(customId))
        {
            customId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(CUSTOM_ID_KEY, customId);
            PlayerPrefs.Save();
        }

        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        Debug.Log("[PlayFab] Login ospite (Custom ID)...");
        PlayFabClientAPI.LoginWithCustomID(request, OnSuccess, OnError);
    }

    // ----------- LOGIN CON EMAIL -----------
    public static void LoginWithEmail(string email, string password)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        Debug.Log($"[PlayFab] Login email: {email}");
        PlayFabClientAPI.LoginWithEmailAddress(request,
            r => { PlayerPrefs.SetString(EMAIL_KEY, email); PlayerPrefs.Save(); OnSuccess(r); },
            OnError);
    }

    // ----------- REGISTRAZIONE -----------
    public static void Register(string email, string password, string username)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = username,
            DisplayName = username,
            RequireBothUsernameAndEmail = true
        };

        Debug.Log($"[PlayFab] Registrazione: {email} ({username})");
        PlayFabClientAPI.RegisterPlayFabUser(request,
            r =>
            {
                IsLoggedIn = true;
                PlayerId = r.PlayFabId;
                DisplayName = username;
                PlayerPrefs.SetString(EMAIL_KEY, email);
                PlayerPrefs.Save();
                Debug.Log($"[PlayFab] Registrato OK! PlayerId={PlayerId}");

                // aggiunge la contact email -> scatena la rule -> invia email di verifica
                AddContactEmail(email);

                OnLoginSuccess?.Invoke();
            },
            OnError);
    }

    // ----------- NOME VISUALIZZATO -----------
    /// <summary>
    /// Cambia il nome che compare in classifica.
    ///
    /// E' il DisplayName del titolo, non lo Username con cui si accede: quello
    /// resta com'e'. Cambiarlo non tocca le credenziali, quindi non serve
    /// rifare l'accesso.
    ///
    /// PlayFab accetta da 3 a 25 caratteri e rifiuta i nomi gia' presi da altri
    /// giocatori dello stesso titolo.
    /// </summary>
    public static void UpdateDisplayName(string newName, Action<bool, string> onDone)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newName };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            r =>
            {
                DisplayName = r.DisplayName;
                Debug.Log($"[PlayFab] Nome visualizzato aggiornato: {DisplayName}");
                onDone?.Invoke(true, "");
            },
            e =>
            {
                Debug.LogWarning($"[PlayFab] Cambio nome fallito: {e.GenerateErrorReport()}");
                onDone?.Invoke(false, Describe(e));
            });
    }

    // ----------- RECUPERO PASSWORD -----------
    /// <summary>
    /// Chiede a PlayFab di mandare l'email di reimpostazione password.
    ///
    /// Non passa dal server SMTP configurato nel titolo: quello serve solo se si
    /// vuole un modello di email personalizzato. Senza EmailTemplateId, PlayFab
    /// usa il proprio modello e la propria infrastruttura, quindi questa funziona
    /// anche mentre la verifica email e' ferma.
    ///
    /// La chiamata e' AuthType.None: si puo' invocare senza essere loggati, che e'
    /// esattamente il caso di chi ha perso la password.
    /// </summary>
    public static void SendPasswordReset(string email, Action<bool, PlayFabErrorCode, string> onDone)
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = email,
            TitleId = PlayFabSettings.TitleId
        };

        Debug.Log($"[PlayFab] Recupero password per {email}...");
        PlayFabClientAPI.SendAccountRecoveryEmail(request,
            r =>
            {
                Debug.Log("[PlayFab] Email di recupero inviata.");
                onDone?.Invoke(true, PlayFabErrorCode.Success, "");
            },
            e =>
            {
                Debug.LogWarning($"[PlayFab] Recupero password fallito: {e.GenerateErrorReport()}");
                onDone?.Invoke(false, e.Error, Describe(e));
            });
    }

    // ----------- MESSAGGI DI ERRORE -----------
    /// <summary>
    /// Trasforma un errore di PlayFab in una frase che dice cosa c'e' da
    /// cambiare.
    ///
    /// Prima si mostrava direttamente error.ErrorMessage, e in registrazione
    /// quello e' quasi sempre "Invalid input parameters": vero ma inutile, non
    /// dice quale campo. Il dettaglio sta in ErrorDetails, un dizionario
    /// campo -> elenco di problemi, che nessuno guardava.
    /// </summary>
    public static string Describe(PlayFabError error)
    {
        if (error == null) return "Unknown error.";

        switch (error.Error)
        {
            case PlayFabErrorCode.EmailAddressNotAvailable:
                return "This email is already registered. Try signing in.";

            case PlayFabErrorCode.UsernameNotAvailable:
                return "That username is already taken. Choose another one.";

            case PlayFabErrorCode.InvalidEmailAddress:
                return "Invalid email address.";

            case PlayFabErrorCode.InvalidUsername:
                return $"Invalid username: {UsernameMinLength} to {UsernameMaxLength} characters, "
                       + "letters and numbers only.";

            case PlayFabErrorCode.InvalidPassword:
                return $"Invalid password: {PasswordMinLength} to {PasswordMaxLength} characters.";

            case PlayFabErrorCode.InvalidEmailOrPassword:
            case PlayFabErrorCode.InvalidUsernameOrPassword:
            case PlayFabErrorCode.AccountNotFound:
                return "Wrong email or password.";

            case PlayFabErrorCode.AccountBanned:
                return "This account has been banned.";

            case PlayFabErrorCode.ConnectionError:
                return "No connection. Check your network and try again.";

            case PlayFabErrorCode.ServiceUnavailable:
            case PlayFabErrorCode.InternalServerError:
                return "Service unavailable. Try again shortly.";
        }

        // InvalidParams e simili: il codice non basta, ma i campi ci sono
        string detailed = FromDetails(error);
        if (!string.IsNullOrEmpty(detailed)) return detailed;

        return string.IsNullOrEmpty(error.ErrorMessage) ? "Something went wrong." : error.ErrorMessage;
    }

    /// <summary>
    /// Ricava il messaggio dai campi segnalati da PlayFab.
    ///
    /// Si traduce il NOME del campo, non il testo inglese che arriva: quel testo
    /// puo' cambiare da un aggiornamento all'altro del servizio, mentre il nome
    /// del campo e' stabile. Al massimo due righe: la riga di stato e' una sola
    /// e su un telefono non ci sta un elenco.
    /// </summary>
    private static string FromDetails(PlayFabError error)
    {
        if (error.ErrorDetails == null || error.ErrorDetails.Count == 0) return "";

        var sb = new StringBuilder();
        int shown = 0;

        foreach (KeyValuePair<string, List<string>> field in error.ErrorDetails)
        {
            if (shown >= 2) break;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(FieldRule(field.Key));
            shown++;

            Debug.LogWarning($"[PlayFab] Campo '{field.Key}': "
                             + string.Join(" | ", field.Value ?? new List<string>()));
        }

        return sb.ToString();
    }

    private static string FieldRule(string field)
    {
        switch (field)
        {
            case "Email":
                return "Invalid email address.";
            case "Password":
                return $"Password: {PasswordMinLength} to {PasswordMaxLength} characters.";
            case "Username":
                return $"Username: {UsernameMinLength} to {UsernameMaxLength} characters, "
                       + "letters and numbers only, no spaces.";
            case "DisplayName":
                return "Invalid display name: 3 to 25 characters.";
            case "TitleId":
                return "Invalid game configuration.";
        }
        return field + ": invalid value.";
    }

    // ----------- CONTACT EMAIL (per verifica) -----------
    private static void AddContactEmail(string email)
    {
        var request = new AddOrUpdateContactEmailRequest
        {
            EmailAddress = email
        };

        PlayFabClientAPI.AddOrUpdateContactEmail(request,
            result =>
            {
                Debug.Log($"[PlayFab] Contact email aggiunta ({email}). Email di verifica in arrivo.");
            },
            error =>
            {
                Debug.LogWarning($"[PlayFab] Impossibile aggiungere contact email: {error.GenerateErrorReport()}");
            });
    }

    public static string GetRememberedEmail()
    {
        return PlayerPrefs.GetString(EMAIL_KEY, "");
    }

    public static void Logout()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        IsLoggedIn = false;
        PlayerId = "";
        DisplayName = "";
    }

    private static void OnSuccess(LoginResult result)
    {
        IsLoggedIn = true;
        PlayerId = result.PlayFabId;
        if (result.InfoResultPayload?.PlayerProfile != null)
            DisplayName = result.InfoResultPayload.PlayerProfile.DisplayName ?? "";
        Debug.Log($"[PlayFab] Login OK! PlayerId={PlayerId}, DisplayName={DisplayName}");
        OnLoginSuccess?.Invoke();
    }

    private static void OnError(PlayFabError error)
    {
        IsLoggedIn = false;
        // In Console resta il rapporto completo, per poter indagare;
        // all'utente va la frase leggibile.
        Debug.LogError($"[PlayFab] Errore: {error.GenerateErrorReport()}");
        OnLoginFailed?.Invoke(Describe(error));
    }
}
