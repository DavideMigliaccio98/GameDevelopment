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
            // Se è l'ultima frase, mostra il bottone cura
            bool isLast = currentLine == lines.Count - 1;
            if (nextButton != null) nextButton.gameObject.SetActive(!isLast);
            if (healButton != null) healButton.gameObject.SetActive(isLast);
            if (isLast && healButtonLabel != null)
                healButtonLabel.text = $"CURA ({currentNpc.HealCost} pt)";
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

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        currentNpc = null;
    }
}