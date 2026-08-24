using System.Collections;
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

    [Header("Esito operazioni")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Color successColor = new Color32(0x4A, 0x6F, 0x28, 0xFF);
    [SerializeField] private Color failColor = new Color32(0x9C, 0x2D, 0x2B, 0xFF);
    [Tooltip("Quanto resta visibile il messaggio di riuscita prima che il dialogo si chiuda.")]
    [SerializeField] private float closeDelay = 1.2f;
    [Tooltip("Tinta dei bottoni che il giocatore non puo' ancora permettersi.")]
    [SerializeField] private Color unaffordableTint = new Color(0.74f, 0.71f, 0.66f, 1f);

    private NPC currentNpc;
    private int currentLine = 0;
    private Coroutine closeRoutine;

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
        HideFeedback();
        ShowLine();
    }

    private void ShowLine()
    {
        if (currentNpc == null) return;
        var lines = currentNpc.DialogLines;
        if (currentLine >= lines.Count) return;

        if (dialogText != null) dialogText.text = lines[currentLine];

        bool isLast = currentLine == lines.Count - 1;
        if (nextButton != null) nextButton.gameObject.SetActive(!isLast);

        // Opzioni negozio: appaiono all'ultima frase
        bool showHeal = isLast && currentNpc.HealFull;
        if (healButton != null) healButton.gameObject.SetActive(showHeal);
        if (boostButton != null) boostButton.gameObject.SetActive(isLast);

        if (isLast)
        {
            if (healButtonLabel != null)
                healButtonLabel.text = $"CURA ({currentNpc.HealCost} pt)";
            if (boostButtonLabel != null)
                boostButtonLabel.text = $"POTENZIA ({currentNpc.BoostCost} pt)";

            // Un bottone che non ti puoi permettere resta premibile: cosi tocandolo
            // scopri perche'. Viene solo smorzato, per farlo capire prima di provarci.
            Tint(healButton, currentNpc.CanAfford(currentNpc.HealCost));
            Tint(boostButton, currentNpc.CanAfford(currentNpc.BoostCost));
        }
    }

    private void Tint(Button b, bool affordable)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img != null) img.color = affordable ? Color.white : unaffordableTint;
    }

    public void OnNext()
    {
        currentLine++;
        HideFeedback();
        ShowLine();
    }

    public void OnHeal()
    {
        if (currentNpc == null) return;
        Report(currentNpc.TryHeal());
    }

    public void OnBoost()
    {
        if (currentNpc == null) return;
        Report(currentNpc.TryBoostAttack());
    }

    /// <summary>
    /// In caso di riuscita mostra l'esito e chiude dopo un attimo. In caso di
    /// rifiuto il dialogo RESTA APERTO: il giocatore legge il motivo e puo'
    /// scegliere l'altra opzione senza dover riaprire tutto.
    /// </summary>
    private void Report(ShopOutcome outcome)
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = outcome.Message;
            feedbackText.color = outcome.Success ? successColor : failColor;
        }
        else
        {
            Debug.Log("[DialogUI] " + outcome.Message);
        }

        if (outcome.Success)
        {
            // le etichette e le tinte vanno riviste: il punteggio e' cambiato
            ShowLine();
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(CloseAfter(closeDelay));
        }
        else
        {
            ShowLine();
        }
    }

    private IEnumerator CloseAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        closeRoutine = null;
        Close();
    }

    private void HideFeedback()
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void Close()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
        HideFeedback();
        if (panel != null) panel.SetActive(false);
        currentNpc = null;
    }
}
