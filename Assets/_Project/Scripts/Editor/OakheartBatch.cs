using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applica in blocco a tutte le scene la stessa configurazione di UI, cosi da
/// non doverla ripetere a mano scena per scena (ed evitare che una resti indietro).
///
/// Menu: Tools > Oakheart > Batch > ...
/// </summary>
public static class OakheartBatch
{
    private const string SceneDir = "Assets/_Project/Scenes/";

    /// Scene di gioco: SafeArea + Canvas Scaler + skin HUD + battute NPC.
    private static readonly string[] FullScenes =
    {
        "Game", "Game_Field", "Game_Desert", "Game_Snow", "Game_Lava",
        "CastleInterior", "CabinInterior", "HouseInterior", "TentInterior"
    };

    /// Scene di contorno: per ora solo il Canvas Scaler, la skin la faremo dopo.
    /// Niente SafeArea qui: hanno sfondi a tutto schermo che devono restare tali,
    /// altrimenti sui telefoni col notch compaiono bande nere ai bordi.
    private static readonly string[] ScalerOnlyScenes = { "MainMenu", "Login" };

    // FishingHutInterior: bozza non usata, esclusa di proposito.

    [MenuItem("Tools/Oakheart/Batch/Applica a tutte le scene")]
    public static void RunAll()
    {
        int total = FullScenes.Length + ScalerOnlyScenes.Length;
        bool go = EditorUtility.DisplayDialog("Oakheart - batch",
            "Sto per aprire, modificare e SALVARE " + total + " scene.\n\n" +
            "Scene di gioco (" + FullScenes.Length + "):\n" +
            "  - contenitore SafeArea attorno alla UI\n" +
            "  - Canvas Scaler: Match = 0 (Width)\n" +
            "  - skin pixel-art dell'HUD\n" +
            "  - battute NPC aggiornate negli interni\n\n" +
            "MainMenu e Login (" + ScalerOnlyScenes.Length + "):\n" +
            "  - solo Canvas Scaler\n\n" +
            "Fai un commit prima, se vuoi poter tornare indietro.",
            "Procedi", "Annulla");
        if (!go) return;

        // Se ci sono modifiche non salvate, Unity chiede cosa farne: se l'utente
        // annulla ci si ferma, altrimenti si perderebbe il lavoro in corso.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[Oakheart] Batch annullato dall'utente.");
            return;
        }

        string original = SceneManager.GetActiveScene().path;
        var report = new List<string>();
        int ok = 0, failed = 0;

        OakheartHudSkin.Silent = true;
        OakheartNpcLines.Silent = true;

        try
        {
            for (int i = 0; i < FullScenes.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Oakheart - batch",
                    FullScenes[i], (float)i / total);
                if (Process(FullScenes[i], true, report)) ok++; else failed++;
            }

            for (int i = 0; i < ScalerOnlyScenes.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Oakheart - batch",
                    ScalerOnlyScenes[i], (float)(FullScenes.Length + i) / total);
                if (Process(ScalerOnlyScenes[i], false, report)) ok++; else failed++;
            }
        }
        finally
        {
            OakheartHudSkin.Silent = false;
            OakheartNpcLines.Silent = false;
            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(original))
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        }

        string summary = string.Join("\n", report);
        Debug.Log("[Oakheart] Batch completato: " + ok + " ok, " + failed + " falliti.\n" + summary);
        EditorUtility.DisplayDialog("Oakheart - batch",
            ok + " scene aggiornate, " + failed + " fallite.\n\n" + summary +
            "\n\nIl dettaglio completo e' in Console.", "OK");
    }

    private static bool Process(string sceneName, bool full, List<string> report)
    {
        string path = SceneDir + sceneName + ".unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            report.Add(sceneName + ": FILE NON TROVATO");
            return false;
        }

        try
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                report.Add(sceneName + ": nessun Canvas, saltata");
                return false;
            }

            var notes = new List<string>();

            if (SetScaler(canvas)) notes.Add("scaler");

            if (full)
            {
                int moved = EnsureSafeArea(canvas);
                notes.Add(moved > 0 ? "safearea(+" + moved + ")" : "safearea");

                OakheartHudSkin.ApplySkin();
                notes.Add("hud");

                OakheartNpcLines.UpdateActiveScene();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            report.Add(sceneName + ": " + string.Join(", ", notes));
            return true;
        }
        catch (System.Exception e)
        {
            report.Add(sceneName + ": ERRORE - " + e.Message);
            Debug.LogError("[Oakheart] " + sceneName + ": " + e);
            return false;
        }
    }

    /// <summary>
    /// Match = 0 significa "scala sulla larghezza". Con 0.5 su uno schermo piu'
    /// allungato del 16:9 la UI cresce e gli elementi ancorati ai due lati opposti
    /// della riga in alto finiscono per sovrapporsi.
    /// </summary>
    private static bool SetScaler(Canvas canvas)
    {
        var cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) return false;

        Undo.RecordObject(cs, "Canvas Scaler");
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1080f, 1920f);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0f;
        cs.referencePixelsPerUnit = 100f;
        EditorUtility.SetDirty(cs);
        return true;
    }

    /// <summary>
    /// Crea (se manca) il contenitore SafeArea sotto il Canvas e ci sposta dentro
    /// tutta la UI, mantenendo l'ordine dei fratelli: l'ordine di disegno non cambia.
    /// </summary>
    private static int EnsureSafeArea(Canvas canvas)
    {
        Transform ct = canvas.transform;
        Transform sa = ct.Find("SafeArea");

        if (sa == null)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "SafeArea");
            sa = go.transform;
            sa.SetParent(ct, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (sa.GetComponent<SafeArea>() == null)
            Undo.AddComponent<SafeArea>(sa.gameObject);

        var toMove = new List<Transform>();
        for (int i = 0; i < ct.childCount; i++)
        {
            Transform c = ct.GetChild(i);
            if (c != sa) toMove.Add(c);
        }

        foreach (Transform c in toMove)
            c.SetParent(sa, false);   // false = conserva anchor e posizione locali

        return toMove.Count;
    }
}
