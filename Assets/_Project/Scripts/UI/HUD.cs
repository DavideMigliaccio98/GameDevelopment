using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private PlayerHealth player;

    private void Start()
    {
        // Se non è collegato manualmente, cercalo via tag
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerHealth>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(GameManager.Instance.Score);
        }
        if (player != null)
        {
            player.OnHpChanged += UpdateHp;
            UpdateHp(player.CurrentHP, player.MaxHP);
            Debug.Log($"[HUD] Collegato a Player HP={player.CurrentHP}/{player.MaxHP}");
        }
        else
        {
            Debug.LogWarning("[HUD] PlayerHealth non trovato!");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnScoreChanged -= UpdateScore;
        if (player != null) player.OnHpChanged -= UpdateHp;
    }

    private void UpdateScore(int s)
    {
        scoreText.text = $"Score: {s}";
        StartCoroutine(ScalePop(scoreText.transform));
    }

    private void UpdateHp(int cur, int max)
    {
        if (hpBar != null) hpBar.value = (float)cur / max;
    }

    private System.Collections.IEnumerator ScalePop(Transform t)
    {
        Vector3 baseScale = Vector3.one;
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = elapsed / duration;
            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;
            t.localScale = baseScale * s;
            yield return null;
        }
        t.localScale = baseScale;
    }
}