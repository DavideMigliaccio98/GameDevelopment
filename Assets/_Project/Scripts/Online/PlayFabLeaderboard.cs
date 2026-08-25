using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabLeaderboard
{
    public const string STAT_NAME = "top_scores";

    public static void SubmitScore(int score, Action onDone = null)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = STAT_NAME, Value = score }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request,
            r => { Debug.Log($"[Leaderboard] Score inviato: {score}"); onDone?.Invoke(); },
            e => { Debug.LogError($"[Leaderboard] Errore submit: {e.GenerateErrorReport()}"); onDone?.Invoke(); });
    }

    /// <summary>
    /// La posizione del giocatore in classifica, contando da 1.
    /// Restituisce -1 se non ha ancora un punteggio registrato.
    ///
    /// Si usa la classifica "attorno al giocatore" invece di scaricare i primi
    /// N e cercarsi dentro: quella risponde anche se si e' millesimi, mentre la
    /// lista dei primi dieci direbbe soltanto che non ci si sta.
    /// </summary>
    public static void GetMyRank(Action<int, int> onDone)
    {
        var request = new GetLeaderboardAroundPlayerRequest
        {
            StatisticName = STAT_NAME,
            MaxResultsCount = 1
        };

        PlayFabClientAPI.GetLeaderboardAroundPlayer(request,
            r =>
            {
                if (r.Leaderboard == null || r.Leaderboard.Count == 0)
                {
                    onDone?.Invoke(-1, 0);
                    return;
                }

                PlayerLeaderboardEntry me = r.Leaderboard[0];
                onDone?.Invoke(me.Position + 1, me.StatValue);   // Position parte da zero
            },
            e =>
            {
                Debug.LogWarning($"[Leaderboard] Posizione non disponibile: {e.GenerateErrorReport()}");
                onDone?.Invoke(-1, 0);
            });
    }

    public static void GetTop(int limit, Action<List<PlayerLeaderboardEntry>> onDone)
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = STAT_NAME,
            StartPosition = 0,
            MaxResultsCount = limit,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(request,
            r => { onDone?.Invoke(r.Leaderboard ?? new List<PlayerLeaderboardEntry>()); },
            e =>
            {
                Debug.LogError($"[Leaderboard] Errore lettura: {e.GenerateErrorReport()}");
                onDone?.Invoke(new List<PlayerLeaderboardEntry>());
            });
    }
}