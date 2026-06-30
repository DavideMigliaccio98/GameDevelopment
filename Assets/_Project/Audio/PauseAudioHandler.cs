using UnityEngine;

public class PauseAudioHandler : MonoBehaviour
{
    private void OnEnable()
    {
        // Quando il pause panel si attiva, parte la musica del menu
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMainMenuMusic();
    }

private void OnDisable()
{
    if (AudioManager.Instance != null)
        AudioManager.Instance.PlayCurrentSceneMusic();
}
}