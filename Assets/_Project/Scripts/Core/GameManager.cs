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

    /// <summary>
    /// Chiude la partita in corso: punteggio a zero, vita da rigenerare al
    /// massimo, potenziamento scaduto.
    ///
    /// Serve un punto unico perche' al menu principale ci si arriva da tre
    /// bottoni diversi (pausa, game over, livello completato) e ognuno faceva
    /// storia a se': da "livello completato" il punteggio non veniva azzerato
    /// affatto, e la vita residua restava memorizzata in LastPlayerHP, quindi
    /// la partita successiva partiva con gli HP di quella vecchia.
    ///
    /// Viene chiamato anche all'apertura del menu principale, cosi vale
    /// qualunque strada si sia presa per arrivarci.
    /// </summary>
    public void EndRun()
    {
        Score = 0;
        LastPlayerHP = -1;
        LastPlayerMaxHP = -1;
        ClearBoost();
        OnScoreChanged?.Invoke(Score);
        Debug.Log("[GameManager] Partita chiusa: punteggio, vita e potenziamento azzerati.");
    }

    public void ClearBoost()
    {
        BoostEndTime = 0f;
        BoostMultiplier = 1f;
    }
}
