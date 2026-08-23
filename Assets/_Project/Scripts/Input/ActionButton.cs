using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private float npcDetectionRange = 2f;

    [Header("Icone (se assegnate sostituiscono l'etichetta testuale)")]
    [SerializeField] private Image icon;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite talkIcon;

    private Transform playerTransform;
    private PlayerAttack playerAttack;
    private NPC currentNPC;

    // Gli NPC sono piazzati nella scena e non nascono a runtime: li cerchiamo
    // una volta sola. Prima FindObjectsByType girava a ogni frame, su mobile
    // e' uno spreco che si sente.
    private NPC[] npcs;
    private bool lastWasTalk;
    private bool stateInitialized;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerAttack = player.GetComponent<PlayerAttack>();
        }

        npcs = FindObjectsByType<NPC>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnAction);
        }

        RefreshVisual(true);
    }

    private void Update()
    {
        if (playerTransform == null) return;
        currentNPC = FindNearestNPC();
        RefreshVisual(false);
    }

    private NPC FindNearestNPC()
    {
        if (npcs == null || npcs.Length == 0) return null;

        NPC nearest = null;
        float minDist = npcDetectionRange;
        Vector2 p = playerTransform.position;

        foreach (var npc in npcs)
        {
            if (npc == null) continue;
            float dist = Vector2.Distance(p, npc.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = npc;
            }
        }
        return nearest;
    }

    /// <summary>Spada quando si attacca, fumetto quando c'e' un NPC a portata.</summary>
    private void RefreshVisual(bool force)
    {
        bool talk = currentNPC != null;
        if (!force && stateInitialized && talk == lastWasTalk) return;
        lastWasTalk = talk;
        stateInitialized = true;

        if (icon != null && attackIcon != null && talkIcon != null)
        {
            icon.sprite = talk ? talkIcon : attackIcon;
            // Questo componente vive SULL'oggetto dell'etichetta: disattivarne il
            // GameObject spegnerebbe questo stesso script. Si disabilita solo il testo.
            if (label != null && label.enabled) label.enabled = false;
            return;
        }

        if (label != null)
        {
            label.enabled = true;
            label.text = talk ? "A" : "X";
        }
    }

    public void OnAction()
    {
        if (currentNPC != null)
        {
            currentNPC.OpenDialog();
        }
        else if (playerAttack != null)
        {
            playerAttack.TryAttack();
        }
    }
}
