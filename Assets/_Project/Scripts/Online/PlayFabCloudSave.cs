using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabCloudSave
{
    private const string KEY_BEST = "best_score";

    public static void SaveBestScore(int score, Action onDone = null)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { KEY_BEST, score.ToString() } }
        };

        PlayFabClientAPI.UpdateUserData(request,
            r => { Debug.Log($"[CloudSave] best_score salvato: {score}"); onDone?.Invoke(); },
            e => { Debug.LogError($"[CloudSave] Errore: {e.GenerateErrorReport()}"); onDone?.Invoke(); });
    }

    public static void LoadBestScore(Action<int> onDone)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request,
            r =>
            {
                int value = 0;
                if (r.Data != null && r.Data.TryGetValue(KEY_BEST, out var item))
                    int.TryParse(item.Value, out value);
                Debug.Log($"[CloudSave] best_score letto: {value}");
                onDone?.Invoke(value);
            },
            e =>
            {
                Debug.LogError($"[CloudSave] Errore lettura: {e.GenerateErrorReport()}");
                onDone?.Invoke(0);
            });
    }

    private const string KEY_MAX_LEVEL = "max_level_unlocked";

    public static void SaveMaxLevel(int level, System.Action onDone = null)
    {
        var data = new System.Collections.Generic.Dictionary<string, string> { { KEY_MAX_LEVEL, level.ToString() } };
        var request = new UpdateUserDataRequest { Data = data };
        PlayFabClientAPI.UpdateUserData(request,
            r => { Debug.Log($"[CloudSave] max_level salvato: {level}"); onDone?.Invoke(); },
            e => { Debug.LogError($"[CloudSave] Errore: {e.GenerateErrorReport()}"); onDone?.Invoke(); });
    }

    public static void LoadMaxLevel(System.Action<int> onDone)
    {
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request,
            r =>
            {
                int value = 1; // di default solo il livello 1 è sbloccato
                if (r.Data != null && r.Data.TryGetValue(KEY_MAX_LEVEL, out var item))
                    int.TryParse(item.Value, out value);
                Debug.Log($"[CloudSave] max_level letto: {value}");
                onDone?.Invoke(value);
            },
            e =>
            {
                Debug.LogError($"[CloudSave] Errore lettura: {e.GenerateErrorReport()}");
                onDone?.Invoke(1);
            });
    }

    
}