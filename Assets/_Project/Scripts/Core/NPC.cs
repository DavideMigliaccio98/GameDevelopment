using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Esito di un'operazione al negozio. Prima TryHeal e TryBoostAttack fallivano
/// con un Debug.Log, che il giocatore non vede: il dialogo si chiudeva e non
/// succedeva niente. Ora l'esito torna a chi ha chiesto l'operazione, con la
/// frase da mostrare.
/// </summary>
public readonly struct ShopOutcome
{
    public readonly bool Success;
    public readonly string Message;

    public ShopOutcome(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

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
    [Tooltip("Se spento, questo NPC non offre la cura e il bottone non compare.")]
    [SerializeField] private bool healFullHP = true;
    [SerializeField] private int healCostScore = 50;

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

    /// <summary>Punti che mancano per potersi permettere un costo, 0 se bastano.</summary>
    public int Missing(int cost)
    {
        int score = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        return Mathf.Max(0, cost - score);
    }

    public bool CanAfford(int cost) => Missing(cost) == 0;

    public ShopOutcome TryHeal()
    {
        if (!healFullHP)
            return new ShopOutcome(false, "Non ho di che curarti.");

        if (GameManager.Instance == null)
            return new ShopOutcome(false, "Non posso aiutarti adesso.");

        var playerHealth = playerTransform != null
            ? playerTransform.GetComponent<PlayerHealth>() : null;

        if (playerHealth == null)
            return new ShopOutcome(false, "Non posso aiutarti adesso.");

        // Prima si controllava solo il punteggio: a vita piena il giocatore
        // pagava e non otteneva niente.
        if (playerHealth.CurrentHP >= playerHealth.MaxHP)
            return new ShopOutcome(false, "Sei gia' in piena forma.");

        int missing = Missing(healCostScore);
        if (missing > 0)
            return new ShopOutcome(false, $"Ti mancano {missing} punti.");

        // Il punteggio si scala solo dopo che l'operazione e' andata a buon fine.
        GameManager.Instance.AddScore(-healCostScore);
        playerHealth.HealFull();
        return new ShopOutcome(true, "Le tue ferite sono chiuse.");
    }

    public ShopOutcome TryBoostAttack()
    {
        if (GameManager.Instance == null)
            return new ShopOutcome(false, "Non posso aiutarti adesso.");

        var playerAttack = playerTransform != null
            ? playerTransform.GetComponent<PlayerAttack>() : null;

        if (playerAttack == null)
            return new ShopOutcome(false, "Non posso aiutarti adesso.");

        int missing = Missing(boostCostScore);
        if (missing > 0)
            return new ShopOutcome(false, $"Ti mancano {missing} punti.");

        GameManager.Instance.AddScore(-boostCostScore);
        playerAttack.ApplyAttackBoost(boostDuration, boostMultiplier);
        return new ShopOutcome(true, $"La tua lama e' benedetta per {Mathf.RoundToInt(boostDuration)}s.");
    }
}
