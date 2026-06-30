using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabBootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Vai sempre alla scena Login: lì l'utente decide come accedere
        SceneManager.LoadScene("Login");
    }
}