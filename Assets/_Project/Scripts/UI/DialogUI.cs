using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button healButton;
    [SerializeField] private TextMeshProUGUI healButtonLabel;
    [SerializeField] private Button boostButton;
    [SerializeField] private TextMeshProUGUI boostButtonLabel;
    [SerializeField] private Button closeButton;

    private NPC currentNpc;
    private int currentLine = 0;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open(NPC npc)
    {
        currentNpc = npc;
        currentLine = 0;
        if (panel != null) panel.SetActive(true);
        if (nameText != null) nameText.text = npc.NpcName;
        ShowLine();
    }

    private void ShowLine()
    {
        if (currentNpc == null) return;
        var lines = currentNpc.DialogLines;
        if (currentLine < lines.Count)
        {
            if (dialogText != null) dialogText.text = lines[currentLine];

            bool isLast = currentLine == lines.Count - 1;
            if (nextButton != null) nextButton.gameObject.SetActive(!isLast);

            // Opzioni negozio: appaiono all'ultima frase
            if (healButton != null) healButton.gameObject.SetActive(isLast);
            if (boostButton != null) boostButton.gameObject.SetActive(isLast);

            if (isLast)
            {
                if (healButtonLabel != null)
                    healButtonLabel.text = $"CURA ({currentNpc.HealCost} pt)";
                if (boostButtonLabel != null)
                    boostButtonLabel.text = $"POTENZIA ({currentNpc.BoostCost} pt)";
            }
        }
    }

    public void OnNext()
    {
        currentLine++;
        ShowLine();
    }

    public void OnHeal()
    {
        if (currentNpc != null) currentNpc.TryHeal();
        Close();
    }

    public void OnBoost()
    {
        if (currentNpc != null) currentNpc.TryBoostAttack();
        Close();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentNpc = null;
    }
}