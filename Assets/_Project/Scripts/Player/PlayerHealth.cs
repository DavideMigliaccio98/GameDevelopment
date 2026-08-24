using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;

    [Header("Feedback danno")]
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f);
    [Tooltip("Numero di lampeggi. Uno solo, com'era prima, su schermo piccolo non si vede.")]
    [SerializeField] private int flashBlinks = 3;
    [Tooltip("Durata di un singolo lampeggio (acceso + spento).")]
    [SerializeField] private float flashBlinkTime = 0.09f;

    [Header("Spinta")]
    [Tooltip("Quanto il giocatore viene respinto dal nemico che lo colpisce. " +
             "Tenerlo basso: una spinta forte ti sbatte fuori dalla portata della " +
             "tua stessa spada a ogni colpo incassato, e non riesci piu' a combattere.")]
    [SerializeField] private float knockbackForce = 3.5f;
    [SerializeField] private float knockbackTime = 0.1f;

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHpChanged;
    public event Action OnDied;

    private SpriteRenderer sr;
    private PlayerController controller;

    // Il colore normale si legge UNA volta all'avvio. Leggerlo dentro la coroutine
    // significava, al secondo colpo ravvicinato, fotografare il rosso del colpo
    // precedente e poi "ripristinarlo": il player restava rosso fisso.
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    private void Awake()
    {
        sr = FindBodyRenderer();
        if (sr != null) baseColor = sr.color;
        controller = GetComponent<PlayerController>();

        if (GameManager.Instance != null && GameManager.Instance.LastPlayerHP > 0)
        {
            CurrentHP = GameManager.Instance.LastPlayerHP;
            Debug.Log($"[PlayerHealth] Ripristinato HP={CurrentHP}");
        }
        else
        {
            CurrentHP = maxHP;
        }
    }

    /// <summary>
    /// Lo sprite del personaggio, non quello dell'alone del potenziamento:
    /// sono entrambi figli del Player e prendere il primo che capita
    /// significherebbe, a volte, far lampeggiare l'alone al posto del corpo.
    /// </summary>
    private SpriteRenderer FindBodyRenderer()
    {
        Transform visual = transform.Find("Visual");
        if (visual != null)
        {
            var v = visual.GetComponent<SpriteRenderer>();
            if (v != null) return v;
        }

        foreach (var candidate in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (candidate.GetComponent<BoostAura>() != null) continue;
            return candidate;
        }
        return null;
    }

    private void Start()
    {
        OnHpChanged?.Invoke(CurrentHP, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        TakeDamage(dmg, transform.position);
    }

    /// <summary>
    /// sourcePos serve per capire da che parte e' arrivato il colpo e spingere
    /// il giocatore dalla parte opposta.
    /// </summary>
    public void TakeDamage(int dmg, Vector2 sourcePos)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        OnHpChanged?.Invoke(CurrentHP, maxHP);

        Flash();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerHurt();

        // Bordo rosso, micro-fermo immagine, scossa e vibrazione: sono i segnali
        // che si notano su un telefono, dove il personaggio e' piccolo e spesso
        // coperto dal pollice.
        float severity = maxHP > 0 ? (float)dmg / maxHP : 0.5f;
        DamageFeedback.Ensure().PlayerHit(severity * 3f);   // 1/3 della vita = colpo pieno

        Knockback(sourcePos);

        Debug.Log($"Player HP: {CurrentHP}/{maxHP}");

        if (CurrentHP <= 0)
        {
            IsDead = true;
            Debug.Log("Player died!");

            // il lampeggio non deve sopravvivere alla morte, altrimenti il
            // cadavere resta rosso in schermata di game over
            StopFlash();

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
            var pa = GetComponent<PlayerAttack>();
            if (pa != null) pa.enabled = false;
            OnDied?.Invoke();
        }
    }

    private void Knockback(Vector2 sourcePos)
    {
        if (controller == null || knockbackForce <= 0f) return;

        Vector2 away = (Vector2)transform.position - sourcePos;
        if (away.sqrMagnitude < 0.0001f) return;   // colpo senza direzione: niente spinta

        controller.ApplyKnockback(away.normalized, knockbackForce, knockbackTime);
    }

    /// <summary>
    /// Un solo lampeggio alla volta: un colpo che arriva mentre il precedente e'
    /// ancora in corso lo fa ripartire da capo invece di accodarsi.
    /// </summary>
    private void Flash()
    {
        if (sr == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRed());
    }

    private IEnumerator FlashRed()
    {
        int blinks = Mathf.Max(1, flashBlinks);
        float half = Mathf.Max(0.01f, flashBlinkTime * 0.5f);

        for (int i = 0; i < blinks; i++)
        {
            sr.color = flashColor;
            // tempo non scalato: il micro-fermo immagine non deve allungare il lampeggio
            yield return new WaitForSecondsRealtime(half);
            sr.color = baseColor;
            if (i < blinks - 1) yield return new WaitForSecondsRealtime(half);
        }

        sr.color = baseColor;
        flashRoutine = null;
    }

    private void StopFlash()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = null;
        if (sr != null) sr.color = baseColor;
    }

    private void OnDisable()
    {
        // Se l'oggetto viene spento a meta' lampeggio la coroutine muore in
        // silenzio e il colore resterebbe quello alterato.
        StopFlash();
    }

    public void HealFull()
    {
        CurrentHP = maxHP;
        OnHpChanged?.Invoke(CurrentHP, maxHP);
        Debug.Log($"[Player] Curato a pieno! HP={CurrentHP}");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LastPlayerHP = CurrentHP;
            GameManager.Instance.LastPlayerMaxHP = maxHP;
            Debug.Log($"[PlayerHealth] Salvato HP={CurrentHP}/{maxHP}");
        }
    }
}
