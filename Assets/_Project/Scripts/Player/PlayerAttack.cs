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

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        pc = GetComponent<PlayerController>();
    }

    public void TryAttack()
    {
        if (Time.time - lastAttackTime < cooldown) return;
        lastAttackTime = Time.time;

        // >>> SFX attacco (anche a vuoto)
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySwordAttack();
        // <

        anim.SetTrigger("attack");

        Vector2 origin = (Vector2)transform.position + pc.FacingDir * 0.6f + Vector2.up * 0.5f;
        var hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayer);
        foreach (var h in hits)
        {
            if (h.TryGetComponent<EnemyHealth>(out var eh))
            {
                eh.TakeDamage(damage);

                // spawna le particelle al colpo
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