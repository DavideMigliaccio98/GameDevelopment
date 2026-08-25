using System.Collections.Generic;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private GameObject rowPrefab;      // RowItem prefab
    [SerializeField] private Transform contentParent;   // LBList
    [SerializeField] private TextMeshProUGUI statusText; // "Loading..."
    [SerializeField] private int maxEntries = 10;

    [Header("Colori Top 3")]
    [SerializeField] private Color goldColor = new Color(0.91f, 0.77f, 0.28f);
    [SerializeField] private Color silverColor = new Color(0.78f, 0.78f, 0.82f);
    [SerializeField] private Color bronzeColor = new Color(0.75f, 0.51f, 0.35f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightRowColor = new Color(0.23f, 0.35f, 0.37f, 0.6f);

    // Chiamalo quando apri il pannello leaderboard

    public void Show()
{
    gameObject.SetActive(true);
    Refresh();
}

public void Hide()
{
    gameObject.SetActive(false);
}

    
    public void Refresh()
    {
        ClearRows();
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Loading...";
        }

        PlayFabLeaderboard.GetTop(maxEntries, entries =>
        {
            if (entries == null || entries.Count == 0)
            {
                if (statusText != null) statusText.text = "No scores yet.";
                return;
            }

            if (statusText != null) statusText.gameObject.SetActive(false);
            PopulateRows(entries);
        });
    }

    private void PopulateRows(List<PlayerLeaderboardEntry> entries)
    {
        string myId = PlayFabAuth.PlayerId;

        foreach (var e in entries)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            row.SetActive(true);

            int rank = e.Position + 1; // Position parte da 0
            string name = string.IsNullOrEmpty(e.DisplayName) ? "???" : e.DisplayName;
            int score = e.StatValue;

            // Trova i 3 testi nel prefab (per nome del GameObject figlio)
            var pos = row.transform.Find("RowPos")?.GetComponent<TextMeshProUGUI>();
            var nm = row.transform.Find("RowName")?.GetComponent<TextMeshProUGUI>();
            var sc = row.transform.Find("RowScore")?.GetComponent<TextMeshProUGUI>();

            if (pos != null) pos.text = rank.ToString();
            if (nm != null) nm.text = name;
            if (sc != null) sc.text = score.ToString();

            // Colore rank per top 3
            Color c = normalColor;
            if (rank == 1) c = goldColor;
            else if (rank == 2) c = silverColor;
            else if (rank == 3) c = bronzeColor;

            if (pos != null) pos.color = c;
            if (sc != null) sc.color = c;

            // Evidenzia la riga del giocatore corrente
            if (!string.IsNullOrEmpty(myId) && e.PlayFabId == myId)
            {
                var bg = row.GetComponent<Image>();
                if (bg == null) bg = row.AddComponent<Image>();
                bg.color = highlightRowColor;
                if (nm != null) nm.text = name + " (TU)";
            }
        }
    }

    private void ClearRows()
    {
        if (contentParent == null) return;
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
}