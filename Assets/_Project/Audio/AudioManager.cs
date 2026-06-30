using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Musica")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;

    [Header("SFX")]
    [SerializeField] private AudioClip sfxSwordAttack;
    [SerializeField] private AudioClip sfxPlayerHurt;
    [SerializeField] private AudioClip sfxEnemyDeath;
    [SerializeField] private AudioClip sfxMenuClick;

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crea AudioSource se non assegnati
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        // Ascolta cambio scena per cambiare musica
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
{
    // Forza la logica della scena corrente all'avvio
    // (OnSceneLoaded non scatta per la scena gi� attiva quando il manager nasce)
    Scene currentScene = SceneManager.GetActiveScene();
    OnSceneLoaded(currentScene, LoadSceneMode.Single);
}

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene di menu: MainMenu, Login, Boot
        if (scene.name == "MainMenu" || scene.name == "Login" || scene.name == "Boot")
        {
            PlayMusic(mainMenuMusic);
        }
        else
{
    if (gameplayMusic != null) PlayMusic(gameplayMusic);
    else StopMusic();
}
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) { StopMusic(); return; }
        if (musicSource.clip == clip && musicSource.isPlaying) return; // gi� in play, non ripartire
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayMainMenuMusic()
{
    PlayMusic(mainMenuMusic);
}

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayCurrentSceneMusic()
{
    // Riproduce la musica appropriata per la scena attiva
    Scene s = SceneManager.GetActiveScene();
    if (s.name == "MainMenu" || s.name == "Login" || s.name == "Boot")
        PlayMusic(mainMenuMusic);
    else
    {
        if (gameplayMusic != null) PlayMusic(gameplayMusic);
        else StopMusic();
    }
}

    // SFX helpers
    public void PlaySwordAttack() => PlaySfx(sfxSwordAttack);
    public void PlayPlayerHurt() => PlaySfx(sfxPlayerHurt);
    public void PlayEnemyDeath() => PlaySfx(sfxEnemyDeath);
    public void PlayMenuClick() => PlaySfx(sfxMenuClick);

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // Per il futuro slider
    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }
}