using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private Animator anim;          // preso dal figlio Visual
    [SerializeField] private SpriteRenderer sr;      // preso dal figlio Visual

    [SerializeField] private ParticleSystem dustParticles;

    private Rigidbody2D rb;
    private Vector2 lastDir = Vector2.down;

    // Finestra in cui comanda la spinta del colpo subito e non il joystick.
    private float knockbackUntil = 0f;

    public Vector2 FacingDir => lastDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // fallback se non assegnati nell'Inspector
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Spinge il giocatore lontano da chi lo ha colpito.
    ///
    /// Per la durata della spinta il movimento normale viene sospeso: senza
    /// questo, Update riscriverebbe la velocita' nel frame successivo e la
    /// spinta non si vedrebbe affatto, perche' qui la velocita' viene imposta
    /// ogni frame invece di essere accumulata come forza.
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (rb == null || duration <= 0f) return;

        knockbackUntil = Time.time + duration;
        rb.linearVelocity = direction.normalized * force;
    }

    private void Update()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

        if (input.sqrMagnitude < 0.01f)
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        bool isMoving = input.sqrMagnitude > 0.01f;
        anim.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            lastDir = input.normalized;
            if (Mathf.Abs(input.x) > 0.01f)
                sr.flipX = input.x < 0f;
        }

        // Durante la spinta si lascia fare al Rigidbody: si aggiornano solo
        // animazione e direzione, non la velocita'.
        if (Time.time >= knockbackUntil)
        {
            if (isMoving)
                rb.linearVelocity = input.normalized * moveSpeed;
            else
                rb.linearVelocity = Vector2.zero;
        }

        // gestione polvere
        if (dustParticles != null)
        {
            var emission = dustParticles.emission;
            emission.rateOverTime = isMoving ? 20f : 0f;
        }
    }
}
