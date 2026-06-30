using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private Animator anim;          // ora preso dal figlio Visual
    [SerializeField] private SpriteRenderer sr;      // ora preso dal figlio Visual

    [SerializeField] private ParticleSystem dustParticles;


    private Rigidbody2D rb;
    private Vector2 lastDir = Vector2.down;

    public Vector2 FacingDir => lastDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // fallback se non assegnati nell'Inspector
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
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

        if (input.sqrMagnitude > 0.01f)
            rb.linearVelocity = input.normalized * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;

            // gestione polvere
        if (dustParticles != null)
        {
            var emission = dustParticles.emission;
            emission.rateOverTime = isMoving ? 20f : 0f;
        }
    }
}