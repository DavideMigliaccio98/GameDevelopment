using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabAuth
{
    private const string CUSTOM_ID_KEY = "playfab_custom_id";
    private const string EMAIL_KEY = "playfab_remember_email";

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
                OnLoginSuccess?.Invoke();
            },
            OnError);
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
        Debug.LogError($"[PlayFab] Errore: {error.GenerateErrorReport()}");
        OnLoginFailed?.Invoke(error.ErrorMessage);
    }
}