using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private int damage = 25;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private GameObject hitParticlesPrefab;

    private Animator anim;
    private PlayerController pc;
    private float lastAttackTime = -999f;

    // >>> BOOST ATTACCO temporaneo
    private int baseDamage;
    private float boostEndTime = 0f;

    public bool IsBoosted => boostEndTime > Time.time;
    public float BoostRemaining => Mathf.Max(0f, boostEndTime - Time.time);

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        pc = GetComponent<PlayerController>();
        baseDamage = damage; // salva il danno base

        // Il Player viene ricreato a ogni scena: senza questo, uscendo dal negozio
        // il potenziamento appena comprato spariva insieme al vecchio PlayerAttack.
        if (GameManager.Instance != null && GameManager.Instance.BoostEndTime > Time.time)
        {
            boostEndTime = GameManager.Instance.BoostEndTime;
            damage = Mathf.RoundToInt(baseDamage * GameManager.Instance.BoostMultiplier);
            Debug.Log($"[Boost] Ripreso dal cambio scena: danno={damage}, restano {BoostRemaining:F1}s");
        }
    }

    private void Update()
    {
        // Fine boost: ripristina danno normale
        if (boostEndTime > 0f && Time.time >= boostEndTime)
        {
            damage = baseDamage;
            boostEndTime = 0f;
            if (GameManager.Instance != null) GameManager.Instance.ClearBoost();
            Debug.Log("[Boost] Attacco tornato normale");
        }
    }

    public void ApplyAttackBoost(float duration, float multiplier)
    {
        // se non gia' boostato, salva il danno base corrente
        if (boostEndTime <= Time.time) baseDamage = damage;
        damage = Mathf.RoundToInt(baseDamage * multiplier);
        boostEndTime = Time.time + duration;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BoostEndTime = boostEndTime;
            GameManager.Instance.BoostMultiplier = multiplier;
        }

        Debug.Log($"[Boost] Attacco potenziato! Danno={damage} per {duration}s");
    }

    public void TryAttack()
    {
        if (Time.time - lastAttackTime < cooldown) return;
        lastAttackTime = Time.time;

        // SFX attacco (anche a vuoto)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySwordAttack();

        anim.SetTrigger("attack");

        Vector2 origin = (Vector2)transform.position + pc.FacingDir * 0.6f + Vector2.up * 0.5f;
        var hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayer);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<EnemyHealth>(out var eh))
            {
                eh.TakeDamage(damage);

                if (hitParticlesPrefab != null)
                {
                    Vector3 spawnPos = h.transform.position + Vector3.up * 0.5f;
                    Instantiate(hitParticlesPrefab, spawnPos, Quaternion.identity);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || pc == null) return;
        Gizmos.color = Color.red;
        Vector2 origin = (Vector2)transform.position + pc.FacingDir * 0.6f + Vector2.up * 0.5f;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}
