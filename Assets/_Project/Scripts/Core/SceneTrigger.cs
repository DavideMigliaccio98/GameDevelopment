using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetScene = "CastleInterior";

    [Header("Position handling")]
    [SerializeField] private bool saveReturnPosition = true;
    [SerializeField] private bool useReturnPosition = false;
    [SerializeField] private Vector2 spawnPosition = Vector2.zero;

    [Header("Cooldown")]
    [SerializeField] private float disableOnSpawnSeconds = 1.5f;

    [Header("Restrictions")]
    [Tooltip("Se true, il trigger è disponibile SOLO quando non ci sono nemici vivi")]
    [SerializeField] private bool onlyBetweenWaves = false;

    private bool used = false;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag("Player")) return;

        // Se è impostata la restrizione "solo tra le wave", verifica che non ci siano nemici
        if (onlyBetweenWaves && LevelManager.Instance != null && LevelManager.Instance.EnemiesAlive > 0)
        {
            Debug.Log("[Trigger] Bloccato: ci sono ancora nemici!");
            return;
        }

        used = true;
        Debug.Log($"[Trigger] Player entered. Loading {targetScene}");

        if (saveReturnPosition)
            PlayerSpawnInfo.SaveReturn(other.transform.position, SceneManager.GetActiveScene().name);

        SceneManager.sceneLoaded += OnSceneLoadedHandler;
        SceneManager.LoadScene(targetScene);
    }

    private void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedHandler;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 targetPos = (useReturnPosition && PlayerSpawnInfo.HasReturnPosition)
            ? PlayerSpawnInfo.ReturnPosition
            : (Vector3)spawnPosition;
        player.transform.position = targetPos;
        if (useReturnPosition) PlayerSpawnInfo.Clear();

        DisableTriggersInScene(disableOnSpawnSeconds);
    }

    private void DisableTriggersInScene(float seconds)
    {
        var triggers = FindObjectsByType<SceneTrigger>(FindObjectsInactive.Exclude);
        foreach (var trigger in triggers)
            trigger.StartCoroutine(trigger.TempDisable(seconds));
    }

    private IEnumerator TempDisable(float seconds)
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        yield return new WaitForSecondsRealtime(seconds);
        if (col != null) col.enabled = true;
    }
}