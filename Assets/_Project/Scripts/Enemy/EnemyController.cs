using System.Collections.Generic;
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

    [Header("Ostacoli")]
    [Tooltip("Layer considerati muro. Il Player viene escluso a parte, non e' un ostacolo.")]
    [SerializeField] private LayerMask obstacleMask = 1;   // 1 = layer Default
    [Tooltip("Quanto avanti guarda il nemico per accorgersi di un muro.")]
    [SerializeField] private float probeDistance = 0.7f;
    [Tooltip("Meta' larghezza del nemico, piu' un margine.")]
    [SerializeField] private float probeRadius = 0.22f;

    // Direzioni provate in ordine: dritto verso il player, poi sempre piu' di lato.
    // La prima libera vince, cosi il nemico "striscia" lungo il muro invece di
    // restarci incollato.
    private static readonly float[] ProbeAngles = { 0f, 25f, -25f, 50f, -50f, 75f, -75f };

    private Transform player;
    private PlayerHealth playerHealth;
    private EnemyHealth health;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private float nextAttackTime = 0f;

    private ContactFilter2D filter;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

    private Collider2D myCollider;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    // rilevamento blocco
    private Vector2 lastPos;
    private float stuckTimer;
    private Vector2 detourDir;
    private float detourUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        health = GetComponent<EnemyHealth>();

        filter = new ContactFilter2D();
        filter.useTriggers = false;              // le porte e le zone di passaggio non fermano nessuno
        filter.SetLayerMask(obstacleMask);
        filter.useLayerMask = true;

        lastPos = rb != null ? rb.position : (Vector2)transform.position;
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
            IgnorePlayerCollision(p);
        }
    }

    /// <summary>
    /// Nemico e giocatore non si urtano fisicamente: si attraversano, come e'
    /// sempre stato in questo gioco.
    ///
    /// Prima succedeva per sbaglio, perche' il collider del nemico stava un
    /// metro sopra la sua testa e non incrociava mai quello del giocatore. Ora
    /// che il collider e' al posto giusto si incrocerebbero, e i nemici
    /// diventerebbero muri che ti bloccano e ti spingono. Lo si esclude qui
    /// esplicitamente, senza toccare la matrice dei layer, che serve invece a
    /// far fermare i nemici contro muri e rocce.
    /// </summary>
    private void IgnorePlayerCollision(GameObject playerObject)
    {
        var mine = GetComponents<Collider2D>();
        var theirs = playerObject.GetComponentsInChildren<Collider2D>();

        foreach (var a in mine)
        {
            if (a == null || a.isTrigger) continue;
            foreach (var b in theirs)
            {
                if (b == null || b.isTrigger) continue;
                Physics2D.IgnoreCollision(a, b, true);
            }
        }
    }

    // Il movimento sta in FixedUpdate perche' scrive sul Rigidbody2D: in Update
    // andava fuori passo con la fisica e le collisioni contro i muri erano ballerine.
    private void FixedUpdate()
    {
        // Alla morte EnemyHealth spegne questo componente, quindi qui non si
        // arriva nemmeno. Il controllo resta come rete: se qualcuno lo
        // riaccendesse, un cadavere non deve tornare a menare.
        if (health != null && health.IsDead) { Stop(); return; }

        Unstick();

        if (player == null) { Stop(); return; }
        if (playerHealth != null && playerHealth.IsDead) { Stop(); return; }

        Vector2 pos = rb.position;
        Vector2 target = player.position;
        float dist = Vector2.Distance(pos, target);

        if (dist > aggroRange) { Stop(); return; }

        if (dist <= attackRange)
        {
            Stop();
            TryAttack();
            return;
        }

        Vector2 toPlayer = (target - pos).normalized;
        Vector2 move;

        if (Time.time < detourUntil)
        {
            // sta aggirando: tiene la rotta scelta per un momento, altrimenti
            // tornerebbe subito a spingere contro lo stesso spigolo
            move = detourDir;
        }
        else
        {
            move = SteerAround(pos, toPlayer);
            CheckIfStuck(pos, toPlayer);
        }

        rb.linearVelocity = move * speed;
        anim.SetBool("isMoving", true);
        if (Mathf.Abs(move.x) > 0.01f) sr.flipX = move.x < 0f;

        lastPos = pos;
    }

    /// <summary>
    /// Prova la direzione verso il player e, se e' occupata, ventagli sempre piu'
    /// larghi a destra e sinistra. Restituisce la prima direzione libera.
    /// </summary>
    private Vector2 SteerAround(Vector2 origin, Vector2 desired)
    {
        for (int i = 0; i < ProbeAngles.Length; i++)
        {
            Vector2 dir = Rotate(desired, ProbeAngles[i]);
            if (!Blocked(origin, dir)) return dir;
        }
        return desired;   // circondato: spinge comunque, ci pensera' il detour
    }

    private bool Blocked(Vector2 origin, Vector2 dir)
    {
        int n = Physics2D.CircleCast(origin, probeRadius, dir, filter, castHits, probeDistance);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = castHits[i].collider;
            if (c == null) continue;
            if (player != null && c.transform == player) continue;   // il player non e' un muro
            if (c.transform == transform) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Se il nemico prova a muoversi ma non avanza (incastrato in uno spigolo, o
    /// spinto contro il muro dagli altri), sceglie una direzione laterale per
    /// mezzo secondo e si sgancia.
    /// </summary>
    /// <summary>
    /// Se il nemico si ritrova DENTRO un muro o una roccia, lo spinge fuori.
    ///
    /// L'aggiramento con le antenne evita di entrarci, ma non tira fuori chi ci
    /// e' gia' dentro: un nemico compenetrato viene respinto dal motore fisico
    /// in modo imprevedibile e spesso resta incastrato a vibrare sul posto.
    /// Qui si chiede al motore di quanto e' la compenetrazione e in che
    /// direzione, e lo si sposta di quel tanto piu' un pelo.
    ///
    /// Il filtro esclude gia' i trigger; gli altri corpi dinamici (giocatore e
    /// altri nemici) vengono saltati a parte, perche' spingersi a vicenda e'
    /// normale e non e' l'incastro che ci interessa.
    /// </summary>
    private void Unstick()
    {
        if (myCollider == null || rb == null) return;
        if (myCollider.Overlap(filter, overlaps) <= 0) return;

        for (int i = 0; i < overlaps.Count; i++)
        {
            Collider2D other = overlaps[i];
            if (other == null || other.isTrigger) continue;

            var otherBody = other.attachedRigidbody;
            if (otherBody != null && otherBody.bodyType == RigidbodyType2D.Dynamic) continue;

            ColliderDistance2D d = myCollider.Distance(other);
            if (!d.isOverlapped) continue;

            // distance e' negativa quando si e' compenetrati, e normal punta
            // verso l'altro collider: normal * distance spinge quindi via.
            rb.position += d.normal * (d.distance - 0.02f);
            stuckTimer = 0f;
        }
    }

    private void CheckIfStuck(Vector2 pos, Vector2 toPlayer)
    {
        float expected = speed * Time.fixedDeltaTime * 0.3f;
        if ((pos - lastPos).sqrMagnitude < expected * expected)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= 0.4f)
            {
                float sign = Random.value < 0.5f ? 1f : -1f;
                detourDir = new Vector2(-toPlayer.y, toPlayer.x) * sign;
                detourUntil = Time.time + 0.5f;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(r), c = Mathf.Cos(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    private void Stop()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("isMoving", false);
        stuckTimer = 0f;
    }

    private void TryAttack()
    {
        if (health != null && health.IsDead) return;
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        anim.SetTrigger("attack");
        // si passa la propria posizione: serve al giocatore per capire da che
        // parte e' arrivato il colpo e farsi spingere dalla parte opposta
        if (playerHealth != null) playerHealth.TakeDamage(damage, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, probeRadius);
    }
}
