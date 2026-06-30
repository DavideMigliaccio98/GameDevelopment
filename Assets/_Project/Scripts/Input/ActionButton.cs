using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float npcDetectionRange = 2f;

    private Transform playerTransform;
    private PlayerAttack playerAttack;
    private NPC currentNPC;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerAttack = player.GetComponent<PlayerAttack>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnAction);
        }
    }

    private void Update()
    {
        // ogni frame, cerca l'NPC più vicino entro range
        if (playerTransform == null) return;
        currentNPC = FindNearestNPC();
        UpdateLabel();
    }

    private NPC FindNearestNPC()
    {
        var npcs = FindObjectsByType<NPC>(FindObjectsInactive.Exclude);
        NPC nearest = null;
        float minDist = npcDetectionRange;
        foreach (var npc in npcs)
        {
            float dist = Vector2.Distance(playerTransform.position, npc.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = npc;
            }
        }
        return nearest;
    }

  private void UpdateLabel()
{
    if (label == null) return;
    if (currentNPC != null) label.text = "A";
    else label.text = "X";
}

    public void OnAction()
    {
        if (currentNPC != null)
        {
            currentNPC.OpenDialog();
        }
        else if (playerAttack != null)
        {
            // chiama il vecchio metodo di attacco
        playerAttack.TryAttack();        }
    }
}