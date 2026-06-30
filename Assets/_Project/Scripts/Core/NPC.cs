using UnityEngine;
using System.Collections.Generic;

public class NPC : MonoBehaviour
{
    [Header("Identità")]
    [SerializeField] private string npcName = "Saggio";
    [TextArea(3, 5)]
    [SerializeField] private List<string> dialogLines = new List<string>
    {
        "Salve, eroe!",
        "Vedo che hai combattuto duramente.",
        "Posso aiutarti a recuperare la tua vita."
    };

    [Header("Cura HP")]
    [SerializeField] private int healCostScore = 50;
    [SerializeField] private bool healFullHP = true;

    [Header("Interazione")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private GameObject interactPromptUI; // bottone "Parla"

    private bool playerInRange = false;
    private Transform playerTransform;

    public string NpcName => npcName;
    public List<string> DialogLines => dialogLines;
    public int HealCost => healCostScore;
    public bool HealFull => healFullHP;

    private void Start()
    {
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactionRange;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactPromptUI != null) interactPromptUI.SetActive(playerInRange);
        }

        // Su PC: tasto E per interagire
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenDialog();
        }
    }

    public void OpenDialog()
    {
        if (!playerInRange) return;
        var dialogUI = FindAnyObjectByType<DialogUI>(FindObjectsInactive.Include);
        if (dialogUI != null) dialogUI.Open(this);
    }

    public void TryHeal()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.Score < healCostScore)
        {
            Debug.Log("[NPC] Score insufficiente!");
            return;
        }

        GameManager.Instance.AddScore(-healCostScore);
        var playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            if (healFullHP) playerHealth.HealFull();
            Debug.Log($"[NPC] Curato! Score = {GameManager.Instance.Score}");
        }
    }
}