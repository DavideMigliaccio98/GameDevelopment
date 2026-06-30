using UnityEngine;

public static class PlayerSpawnInfo
{
    public static bool HasReturnPosition { get; private set; } = false;
    public static Vector3 ReturnPosition { get; private set; }
    public static string ReturnScene { get; private set; }

    public static void SaveReturn(Vector3 position, string sceneName)
    {
        ReturnPosition = position;
        ReturnScene = sceneName;
        HasReturnPosition = true;
        Debug.Log($"[SpawnInfo] Saved return to {sceneName} at {position}");
    }

    public static void Clear()
    {
        HasReturnPosition = false;
        ReturnScene = "";
    }
}