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

    [Header("Potenzia Attacco")]
    [SerializeField] private int boostCostScore = 80;
    [SerializeField] private float boostDuration = 15f;
    [SerializeField] private float boostMultiplier = 2f;

    [Header("Interazione")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private GameObject interactPromptUI;

    private bool playerInRange = false;
    private Transform playerTransform;

    public string NpcName => npcName;
    public List<string> DialogLines => dialogLines;
    public int HealCost => healCostScore;
    public bool HealFull => healFullHP;
    public int BoostCost => boostCostScore;

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
            Debug.Log("[NPC] Score insufficiente per cura!");
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

    public void TryBoostAttack()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.Score < boostCostScore)
        {
            Debug.Log("[NPC] Score insufficiente per potenziamento!");
            return;
        }

        GameManager.Instance.AddScore(-boostCostScore);
        var playerAttack = playerTransform.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.ApplyAttackBoost(boostDuration, boostMultiplier);
            Debug.Log($"[NPC] Attacco potenziato! Score = {GameManager.Instance.Score}");
        }
    }
}