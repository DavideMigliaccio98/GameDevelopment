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