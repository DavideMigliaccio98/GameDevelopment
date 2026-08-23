using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rete di sicurezza.
///
/// Alcuni pannelli a tutto schermo ospitano loro stessi lo script che li governa
/// (LevelCompleteUI sta su LevelCompletePanel, DialogUI su DialogPanel). Quegli
/// script si iscrivono agli eventi dentro Start(): se il pannello viene salvato
/// disattivato, Start() non parte, l'iscrizione non avviene e la schermata non
/// compare mai piu' in partita.
///
/// Qui ci si aggancia al salvataggio della scena e si riattivano prima che il
/// file venga scritto. Cosi nasconderli in Edit mode per lavorare e' sempre
/// un'operazione reversibile, anche se ci si dimentica di rimostrarli.
/// </summary>
[InitializeOnLoad]
public static class OakheartOverlayGuard
{
    public static readonly string[] PanelNames =
    {
        "PausePannel", "LevelCompletePanel", "GameOverPanel", "DialogPanel"
    };

    static OakheartOverlayGuard()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        int fixedCount = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                Transform uiRoot = canvas.transform.Find("SafeArea");
                if (uiRoot == null) uiRoot = canvas.transform;

                foreach (string n in PanelNames)
                {
                    Transform t = uiRoot.Find(n);
                    if (t != null && !t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(true);
                        fixedCount++;
                    }
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log("[Oakheart] Riattivati " + fixedCount + " pannelli prima di salvare "
                      + scene.name + ": salvarli spenti avrebbe impedito ai loro script "
                      + "di iscriversi agli eventi in Start().");
        }
    }
}
