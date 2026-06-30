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
        // notifica l'HUD del valore corrente DOPO che è sottoscritto
        OnHpChanged?.Invoke(CurrentHP, maxHP);
    }

    public void TakeDamage(int dmg)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        OnHpChanged?.Invoke(CurrentHP, maxHP);
        StartCoroutine(FlashRed());
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
        sr.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.1f);
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