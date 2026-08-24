using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;

    [Header("Feedback danno")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float flashDuration = 0.12f;

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHpChanged;
    public event Action OnDied;

    private SpriteRenderer sr;

    // Il colore normale si legge UNA volta all'avvio. Leggerlo dentro la coroutine
    // significava, al secondo colpo ravvicinato, fotografare il rosso del colpo
    // precedente e poi "ripristinarlo": il player restava rosso fisso.
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    private void Awake()
    {
        // FIX: assegna lo SpriteRenderer (era null -> flash non partiva mai)
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;

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

    private void Start()
    {
        OnHpChanged?.Invoke(CurrentHP, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        OnHpChanged?.Invoke(CurrentHP, maxHP);

        // Feedback visivo + sonoro
        Flash();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerHurt();
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.12f);

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
        sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
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
