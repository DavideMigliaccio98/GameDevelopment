using System.Collections;
using System.Text;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI listText;

    private void Awake()
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        if (panel == null) return;
        panel.gameObject.SetActive(true);
        StartCoroutine(LoadAndFadeIn());
    }

    public void Hide()
    {
        if (panel == null) return;
        StartCoroutine(FadeOutAndHide());
    }

    private IEnumerator LoadAndFadeIn()
    {
        listText.text = "Caricamento...";
        yield return Fade(0f, 1f, 0.3f);

        bool done = false;
        System.Collections.Generic.List<PlayerLeaderboardEntry> entries = null;
        PlayFabLeaderboard.GetTop(10, list => { entries = list; done = true; });

        while (!done) yield return null;

        if (entries == null || entries.Count == 0)
        {
            listText.text = "Nessun punteggio ancora.";
            yield break;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string id = string.IsNullOrEmpty(e.PlayFabId) ? "Player" : e.PlayFabId.Substring(0, 6);
            string name = !string.IsNullOrEmpty(e.DisplayName) ? e.DisplayName : id;
            sb.AppendLine($"{(i + 1),2}. {name,-10}  {e.StatValue}");
        }
        listText.text = sb.ToString();
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return Fade(1f, 0f, 0.25f);
        panel.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        panel.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        panel.alpha = to;
    }
}