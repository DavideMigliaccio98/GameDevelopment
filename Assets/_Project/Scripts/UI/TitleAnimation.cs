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

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        if (label != null)
        {
            Color c = label.color;
            c.a = 0f;
            label.color = c;
        }
        transform.localScale = baseScale;
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;

        // Fade in (graduale al primo apparire)
        if (label != null && elapsedTime < fadeInDuration)
        {
            Color c = label.color;
            c.a = Mathf.Clamp01(elapsedTime / fadeInDuration);
            label.color = c;
        }

        // Pulse (parte appena il fade e' a met�)
        if (elapsedTime > fadeInDuration * 0.5f)
        {
            float pulse = Mathf.Sin(elapsedTime * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            transform.localScale = baseScale * (1f + pulse);
        }
    }
}