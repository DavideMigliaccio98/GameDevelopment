using UnityEngine;
using TMPro;

public class TitleAnimation : MonoBehaviour
{
    [Header("Fade In")]
    [SerializeField] private float fadeInDuration = 1.5f;

    [Header("Pulse")]
    [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.04f;
    [SerializeField] private float pulseSpeed = 0.8f;

    private TextMeshProUGUI label;
    private Vector3 baseScale;
    private float elapsedTime;
    private bool initialized = false;

    private void Awake()
    {
        CacheLabel();
        baseScale = transform.localScale;
        initialized = true;
    }

    private void CacheLabel()
    {
        // Prova prima sul GameObject stesso, poi nei figli
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        CacheLabel();
        elapsedTime = 0f;

        if (label != null)
        {
            Color c = label.color;
            c.a = 0f;
            label.color = c;
        }

        transform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
    }

    private void Update()
    {
        if (label == null)
        {
            // fallback: se non abbiamo il label, riprova a prenderlo
            CacheLabel();
            if (label == null) return;
        }

        elapsedTime += Time.unscaledDeltaTime;

        // Fade in
        if (elapsedTime < fadeInDuration)
        {
            Color c = label.color;
            c.a = Mathf.Clamp01(elapsedTime / fadeInDuration);
            label.color = c;
        }
        else
        {
            // SICUREZZA: assicura alpha pieno una volta finito il fade
            Color c = label.color;
            if (c.a < 1f)
            {
                c.a = 1f;
                label.color = c;
            }
        }

        // Pulse (parte a met� del fade)
        if (elapsedTime > fadeInDuration * 0.5f)
        {
            Vector3 sScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
            float pulse = Mathf.Sin(elapsedTime * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            transform.localScale = sScale * (1f + pulse);
        }
    }
}