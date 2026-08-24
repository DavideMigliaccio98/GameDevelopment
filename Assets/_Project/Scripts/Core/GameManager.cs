using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public int LastPlayerHP { get; set; } = -1; // -1 = non impostato
    public int LastPlayerMaxHP { get; set; } = -1;

    // Il potenziamento d'attacco si compra dentro un interno e si usa fuori:
    // deve sopravvivere al cambio scena, altrimenti si paga per niente.
    // Time.time e' continuo tra una scena e l'altra, quindi la scadenza assoluta
    // resta valida; si azzera solo al riavvio dell'app, che e' il comportamento voluto.
    public float BoostEndTime { get; set; } = 0f;
    public float BoostMultiplier { get; set; } = 1f;

    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        OnScoreChanged?.Invoke(Score);
        Debug.Log($"Score: {Score}");
    }

    public void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }

    public void ClearBoost()
    {
        BoostEndTime = 0f;
        BoostMultiplier = 1f;
    }
}
