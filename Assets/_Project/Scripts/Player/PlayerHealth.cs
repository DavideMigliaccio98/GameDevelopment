using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHpChanged;
    public event Action OnDied;

    private SpriteRenderer sr;

    private void Awake()
    {
        // FIX: assegna lo SpriteRenderer (era null -> flash non partiva mai)
        sr = GetComponentInChildren<SpriteRenderer>();

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
        StartCoroutine(FlashRed());
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPlayerHurt();
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.12f);

        Debug.Log($"Player HP: {CurrentHP}/{maxHP}");

        if (CurrentHP <= 0)
        {
            IsDead = true;
            Debug.Log("Player died!");
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
            var pa = GetComponent<PlayerAttack>();
            if (pa != null) pa.enabled = false;
            OnDied?.Invoke();
        }
    }

    private IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(1f, 0.3f, 0.3f);
        yield return new WaitForSeconds(0.12f);
        sr.color = orig;
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