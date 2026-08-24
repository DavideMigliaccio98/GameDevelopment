using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private PlayerHealth player;

    [Header("Reazione al danno")]
    [SerializeField] private Color hpDamageTint = new Color(1f, 0.95f, 0.75f);
    [SerializeField] private float hpFlashTime = 0.22f;

    private Image hpFill;
    private Color hpFillBase = Color.white;
    private Coroutine hpFlashRoutine;
    private int lastHp = -1;

    private void Start()
    {
        // Se non e' collegato manualmente, cercalo via tag
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerHealth>();
        }

        if (hpBar != null && hpBar.fillRect != null)
        {
            hpFill = hpBar.fillRect.GetComponent<Image>();
            if (hpFill != null) hpFillBase = hpFill.color;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            UpdateScore(GameManager.Instance.Score);
        }
        if (player != null)
        {
            player.OnHpChanged += UpdateHp;
            lastHp = player.CurrentHP;
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
        if (scoreText == null) return;
        scoreText.text = s.ToString();
        StartCoroutine(ScalePop(scoreText.transform));
    }

    private void UpdateHp(int cur, int max)
    {
        if (hpBar != null && max > 0) hpBar.value = (float)cur / max;

        // Solo in perdita: curarsi non deve far lampeggiare la barra come un colpo.
        bool damaged = lastHp >= 0 && cur < lastHp;
        lastHp = cur;
        if (!damaged) return;

        if (hpBar != null) StartCoroutine(ScalePop(hpBar.transform));

        if (hpFill != null)
        {
            if (hpFlashRoutine != null) StopCoroutine(hpFlashRoutine);
            hpFlashRoutine = StartCoroutine(FlashHpBar());
        }
    }

    /// <summary>
    /// La barra schiarisce di colpo e torna al suo colore: cosi l'occhio la
    /// trova anche quando sta guardando il personaggio dall'altra parte dello schermo.
    /// </summary>
    private IEnumerator FlashHpBar()
    {
        float t = 0f;
        while (t < hpFlashTime)
        {
            t += Time.unscaledDeltaTime;
            hpFill.color = Color.Lerp(hpDamageTint, hpFillBase, t / hpFlashTime);
            yield return null;
        }
        hpFill.color = hpFillBase;
        hpFlashRoutine = null;
    }

    private IEnumerator ScalePop(Transform t)
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
