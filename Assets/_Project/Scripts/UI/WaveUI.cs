using System.Collections;
using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.4f;

    private void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnWaveStarted += ShowWave;
        }
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnWaveStarted -= ShowWave;
        }
    }

    private void ShowWave(int current, int total)
    {
        if (waveText == null || canvasGroup == null) return;
        if (total == 0)
            waveText.text = $"ENDLESS - WAVE {current}";
        else
            waveText.text = $"WAVE {current} / {total}";
        StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}