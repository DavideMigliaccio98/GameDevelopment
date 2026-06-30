using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Endless mode")]
    [SerializeField] private bool isEndless = false;
    [SerializeField] private int endlessStartEnemies = 5;
    [SerializeField] private float endlessWaveDelay = 4f;

    [Header("Setup")]
    [SerializeField] private LevelData levelData;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float delayBeforeFirstWave = 2f;

    public int CurrentWave { get; private set; } = 0;
    public int TotalWaves => levelData != null ? levelData.waves.Length : 0;
    public int EnemiesAlive => activeEnemies.Count;

    public event Action<int, int> OnWaveStarted; // current, total
    public event Action OnLevelCompleted;

    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (SelectedLevel.Current != null)
        {
            levelData = SelectedLevel.Current;
        }

        // determina se è endless
        if (levelData != null && levelData.levelNumber == 99)
        {
            isEndless = true;
        }

        // ora il check di sicurezza
        if (levelData == null)
        {
            Debug.LogError("[LevelManager] No level data assigned!");
            return;
        }
        if (!isEndless && levelData.waves.Length == 0)
        {
            Debug.LogError("[LevelManager] Level has no waves!");
            return;
        }

        Debug.Log($"[LevelManager] Caricato {levelData.levelName}");
        StartCoroutine(RunLevel());
    }

   private IEnumerator RunLevel()
    {
        yield return new WaitForSeconds(delayBeforeFirstWave);

        if (isEndless)
        {
            yield return RunEndless();
        }
        else
        {
            for (int i = 0; i < levelData.waves.Length; i++)
            {
                CurrentWave = i + 1;
                Debug.Log($"[Level] Wave {CurrentWave}/{TotalWaves}");
                OnWaveStarted?.Invoke(CurrentWave, TotalWaves);

                SpawnWave(levelData.waves[i]);

                while (activeEnemies.Count > 0)
                {
                    activeEnemies.RemoveAll(e => e == null);
                    yield return null;
                }

                if (i < levelData.waves.Length - 1)
                    yield return new WaitForSeconds(levelData.waves[i].delayAfter);
            }

            Debug.Log("[Level] COMPLETED!");
            OnLevelCompleted?.Invoke();
        }
    }

    private IEnumerator RunEndless()
    {
        int waveNum = 1;
        float speedMult = 1f;
        float hpMult = 1f;
        float damageMult = 1f;

        while (true)
        {
            CurrentWave = waveNum;
            Debug.Log($"[Endless] Wave {waveNum} - x{hpMult:F2} HP, x{speedMult:F2} speed");
            OnWaveStarted?.Invoke(waveNum, 0); // 0 = infinito

            int count = endlessStartEnemies + waveNum;

            // applica difficoltà progressiva via override temporaneo
            levelData.enemyHpMultiplier = hpMult;
            levelData.enemySpeedMultiplier = speedMult;
            levelData.enemyDamageMultiplier = damageMult;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomSpawnPos();
                GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
                ApplyDifficulty(enemy);
                activeEnemies.Add(enemy);
            }

            while (activeEnemies.Count > 0)
            {
                activeEnemies.RemoveAll(e => e == null);
                yield return null;
            }

            yield return new WaitForSeconds(endlessWaveDelay);

            waveNum++;
            hpMult += 0.1f;
            speedMult += 0.05f;
            damageMult += 0.05f;
        }
    }

    private Vector3 GetRandomSpawnPos()
    {
        // Se ci sono spawn points definiti, usali
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
        }

        // Altrimenti cerca una posizione libera (max 20 tentativi)
        const int maxAttempts = 20;
        const float checkRadius = 0.5f;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(-8f, 8f),
                UnityEngine.Random.Range(-5f, 5f),
                0
            );
            
            // Verifica che non ci sia un collider in quella posizione
            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius);
            if (hit == null || hit.isTrigger)
            {
                // Posizione libera!
                return pos;
            }
        }
        
        // Fallback: ritorna una posizione default sicura
        Debug.LogWarning("[LevelManager] Non ho trovato posizione libera dopo 20 tentativi, uso fallback");
        return new Vector3(0, 0, 0);
    }

    private void SpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            Vector3 pos = GetRandomSpawnPos();
            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            ApplyDifficulty(enemy);
            activeEnemies.Add(enemy);
        }
    }

    private void ApplyDifficulty(GameObject enemy)
    {
        if (levelData == null) return;

        var health = enemy.GetComponent<EnemyHealth>();
        var controller = enemy.GetComponent<EnemyController>();

        if (health != null) health.ApplyHpMultiplier(levelData.enemyHpMultiplier);
        if (controller != null) controller.ApplyMultipliers(levelData.enemySpeedMultiplier, levelData.enemyDamageMultiplier);
    }
}