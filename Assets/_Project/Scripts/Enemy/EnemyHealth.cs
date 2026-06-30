using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 50;
    [SerializeField] private int scoreOnDeath = 10;

    [SerializeField] private GameObject deathParticlesPrefab;

    public int CurrentHP { get; private set; }
    public event Action OnDied;

    private Animator anim;
    private SpriteRenderer sr;
    private bool isDead = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
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
        StartCoroutine(FlashRed());
        Debug.Log($"Enemy hit! HP rimasti: {CurrentHP}");
        if (CurrentHP <= 0) Die();
    }

    private void Die()
    {
        if (deathParticlesPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            Instantiate(deathParticlesPrefab, spawnPos, Quaternion.identity);
        }
        isDead = true;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(scoreOnDeath);
        OnDied?.Invoke();
        Destroy(gameObject, 0.3f);
    }

    private IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        Color orig = sr.color;
        sr.color = new Color(1f, 0.4f, 0.4f);
        yield return new WaitForSeconds(0.1f);
        sr.color = orig;
    }
}