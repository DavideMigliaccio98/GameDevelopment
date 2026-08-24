using UnityEngine;

/// <summary>
/// Alone attorno al Player mentre il potenziamento d'attacco e' attivo.
///
/// Sta su un oggetto figlio con il proprio SpriteRenderer, non sul Player: tingere
/// lo sprite del Player entrerebbe in conflitto con il lampeggio rosso del danno,
/// che memorizza il colore base all'avvio e lo ripristina a ogni colpo.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoostAura : MonoBehaviour
{
    [SerializeField] private PlayerAttack source;
    [SerializeField] private Color auraColor = new Color32(0xE8, 0x48, 0x40, 0xFF);

    [Header("Pulsazione")]
    [SerializeField] private float minAlpha = 0.45f;
    [SerializeField] private float maxAlpha = 0.95f;
    [SerializeField] private float period = 1.1f;
    [SerializeField] private float scalePulse = 0.06f;

    [Header("Posizione")]
    [Tooltip("Ritocco fine rispetto al centro calcolato dallo script Editor. " +
             "Il corpo del personaggio non riempie tutto il fotogramma, quindi il " +
             "centro dello sprite non coincide sempre con il centro visivo.")]
    [SerializeField] private Vector2 extraOffset = Vector2.zero;

    [Header("Avviso di scadenza")]
    [Tooltip("Sotto questi secondi rimasti la pulsazione accelera, per far capire che sta finendo.")]
    [SerializeField] private float warningSeconds = 3f;
    [SerializeField] private float warningPeriod = 0.35f;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private Vector3 basePos;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        basePos = transform.localPosition;
        transform.localPosition = basePos + (Vector3)extraOffset;

        if (source == null) source = GetComponentInParent<PlayerAttack>();
        sr.enabled = false;
    }

    private void LateUpdate()
    {
        if (source == null) { sr.enabled = false; return; }

        bool on = source.IsBoosted;
        if (sr.enabled != on) sr.enabled = on;
        if (!on)
        {
            transform.localScale = baseScale;
            return;
        }

        float p = source.BoostRemaining <= warningSeconds ? warningPeriod : period;
        float t = (Mathf.Sin(Time.time * Mathf.PI * 2f / p) + 1f) * 0.5f;

        Color c = auraColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        sr.color = c;

        transform.localScale = baseScale * (1f + scalePulse * t);
    }
}
