using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float aggroRange = 5f;
    [SerializeField] private float attackRange = 1f;

    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1.2f;

    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void ApplyMultipliers(float speedMult, float damageMult)
    {
        speed *= speedMult;
        damage = Mathf.RoundToInt(damage * damageMult);
        Debug.Log($"[Enemy] Speed={speed:F2}, Damage={damage}");
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (player == null) { Stop(); return; }

        if (playerHealth != null && playerHealth.IsDead) { Stop(); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > aggroRange)
        {
            Stop();
            return;
        }

        if (dist <= attackRange)
        {
            Stop();
            TryAttack();
            return;
        }

        // insegui
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * speed;
        anim.SetBool("isMoving", true);
        if (Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0f;
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("isMoving", false);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        anim.SetTrigger("attack");
        if (playerHealth != null) playerHealth.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}