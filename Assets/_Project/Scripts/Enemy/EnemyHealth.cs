using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 50;
    [SerializeField] private int scoreOnDeath = 10;

    [SerializeField] private GameObject deathParticlesPrefab;

    [Header("Feedback danno")]
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Morte")]
    [Tooltip("Quanto resta in scena il corpo dopo la morte, per far vedere le particelle.")]
    [SerializeField] private float corpseTime = 0.3f;

    public int CurrentHP { get; private set; }

    /// <summary>
    /// Vero dall'istante esatto in cui gli HP arrivano a zero, non da quando
    /// l'oggetto viene distrutto: tra le due cose passano dei decimi di secondo.
    /// </summary>
    public bool IsDead => isDead;

    public event Action OnDied;

    private Animator anim;
    private SpriteRenderer sr;
    private bool isDead = false;

    // Il colore normale si legge UNA volta all'avvio. Leggerlo dentro la coroutine
    // significava, al secondo colpo ravvicinato, fotografare il rosso del colpo
    // precedente e poi "ripristinarlo": lo sprite restava rosso per sempre.
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        // fallback se lo sprite e' su un figlio
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        CurrentHP = maxHP;
    }

    public void ApplyHpMultiplier(float multiplier)
    {
        maxHP = Mathf.RoundToInt(maxHP * multiplier);
        CurrentHP = maxHP;
        Debug.Log($"[Enemy] HP boosted to {maxHP}");
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        Flash();
        Debug.Log($"Enemy hit! HP rimasti: {CurrentHP}");
        if (CurrentHP <= 0) Die();
    }

    /// <summary>
    /// Morte del nemico.
    ///
    /// L'oggetto non sparisce subito: resta in scena qualche decimo di secondo
    /// per far vedere le particelle. Prima quei decimi erano tempo di gioco
    /// pieno, con EnemyController ancora attivo: un nemico ucciso col colpo che
    /// arrivava per primo faceva comunque in tempo a colpire, e il danno
    /// sembrava piovere dal nulla perche' il colpevole nel frattempo era gia'
    /// scomparso.
    ///
    /// Adesso alla morte il nemico viene spento del tutto: niente movimento,
    /// niente attacchi, niente collisioni. Resta solo il corpo da guardare.
    /// </summary>
    private void Die()
    {
        isDead = true;

        if (deathParticlesPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            Instantiate(deathParticlesPrefab, spawnPos, Quaternion.identity);
        }

        // 1) niente piu' attacchi ne' inseguimento
        var controller = GetComponent<EnemyController>();
        if (controller != null) controller.enabled = false;

        // 2) il corpo non deve scivolare ne' spingere nessuno
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        // 3) e non deve fare da ostacolo per gli ultimi decimi di vita
        foreach (var col in GetComponentsInChildren<Collider2D>())
            if (col != null) col.enabled = false;

        if (anim != null) anim.SetBool("isMoving", false);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyDeath();

        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreOnDeath);
        OnDied?.Invoke();
        Destroy(gameObject, corpseTime);
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

    private void OnDisable()
    {
        // Se l'oggetto viene spento a meta' lampeggio la coroutine muore in
        // silenzio e il colore resterebbe quello alterato.
        if (sr != null) sr.color = baseColor;
        flashRoutine = null;
    }
}
