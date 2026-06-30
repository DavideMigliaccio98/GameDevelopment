using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public int levelNumber = 1;
    public string levelName = "Livello 1";
    public string sceneName = "Game"; // nome scena da caricare

    [Header("Waves")]
    public WaveData[] waves;

    [Header("Difficulty modifiers")]
    [Tooltip("Moltiplicatore HP nemici (1.0 = normale, 1.5 = +50% HP)")]
    public float enemyHpMultiplier = 1f;
    [Tooltip("Moltiplicatore velocità nemici")]
    public float enemySpeedMultiplier = 1f;
    [Tooltip("Moltiplicatore danno nemici")]
    public float enemyDamageMultiplier = 1f;
}

[System.Serializable]
public class WaveData
{
    public int enemyCount = 3;
    [Tooltip("Pausa in secondi prima dell'inizio della prossima wave")]
    public float delayAfter = 3f;
}