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

    /// Nome del bottone di debug da eliminare dal menu principale.
    private const string DevButtonName = "DevUnlockButton";

    /// Scene di contorno: Canvas Scaler e skin dei menu, niente HUD.
    /// Niente SafeArea qui: hanno sfondi a tutto schermo che devono restare tali,
    /// altrimenti sui telefoni col notch compaiono bande nere ai bordi.
    /// Boot dura circa un frame (PlayFabBootstrap carica Login in Start), ma se
    /// resta indietro e' l'unico punto del gioco con il fondale originale.
    private static readonly string[] ScalerOnlyScenes = { "MainMenu", "Login", "Boot" };

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
            "  - skin dei pannelli Livello completato e Game over\n" +
            "  - battute NPC aggiornate negli interni\n" +
            "  - alone del potenziamento sul Player\n" +
            "  - collider alla base delle rocce\n" +
            "  - collider alle tilemap di ostacoli\n" +
            "  - ordinamento per profondita' (asse Y, pivot)\n" +
            "  - portata dell'attacco del giocatore\n\n" +
            "Tutte le scene: scritte dei menu in inglese.\n\n" +
            "Prefab Enemy: hitbox riportata ai piedi (era sopra la testa).\n\n" +
            "MainMenu, Login e Boot (" + ScalerOnlyScenes.Length + "):\n" +
            "  - Canvas Scaler\n" +
            "  - rimozione del bottone di debug " + DevButtonName + "\n" +
            "  - skin di menu, selezione livelli, classifica e login\n" +
            "  - voce ENDLESS in fondo alla selezione livelli\n" +
            "  - fondale in duotone al posto del teal\n\n" +
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
        OakheartPanelSkin.Silent = true;
        OakheartMenuSkin.Silent = true;
        OakheartBoostAura.Silent = true;
        OakheartColliders.Silent = true;
        OakheartDepth.Silent = true;
        OakheartGameplay.Silent = true;
        OakheartLanguage.Silent = true;
        OakheartEndless.Silent = true;

        try
        {
            // Il nemico e' un prefab, non sta in una scena: si sistema una volta sola.
            OakheartColliders.EnemyHitbox();

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
            OakheartPanelSkin.Silent = false;
            OakheartMenuSkin.Silent = false;
            OakheartBoostAura.Silent = false;
            OakheartColliders.Silent = false;
            OakheartDepth.Silent = false;
            OakheartGameplay.Silent = false;
            OakheartLanguage.Silent = false;
            OakheartEndless.Silent = false;
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

            // Le scritte dei menu stanno nelle scene, non negli script: vale
            // per tutte, sia quelle di gioco sia quelle di contorno.
            OakheartLanguage.ApplyActive();
            notes.Add("lingua");

            // Valori salvati nelle scene, che vincono su quelli scritti negli
            // script. Vale per tutte: la portata dell'attacco sta nelle scene di
            // gioco, lo stacco del titolo sta in MainMenu.
            OakheartGameplay.ApplyActive();
            notes.Add("valori");

            if (full)
            {
                int moved = EnsureSafeArea(canvas);
                notes.Add(moved > 0 ? "safearea(+" + moved + ")" : "safearea");

                OakheartHudSkin.ApplySkin();
                notes.Add("hud");

                OakheartPanelSkin.ApplySkin();
                notes.Add("esito");

                OakheartNpcLines.UpdateActiveScene();

                OakheartBoostAura.CreateOnPlayer();
                notes.Add("aura");

                // Le rocce erano solo disegno: senza collider i nemici ci
                // comparivano dentro e dietro.
                OakheartColliders.DecorationColliders();
                notes.Add("rocce");

                // Tilemap di ostacoli senza collider: in Game_Lava si camminava
                // sopra gli alberi.
                OakheartColliders.TilemapObstacles();
                notes.Add("tilemap");

                // Ordine di disegno per profondita': senza, chi finisce davanti
                // e chi dietro lo decideva il caso.
                OakheartDepth.ApplyActive();
                notes.Add("profondita");
            }
            else
            {
                if (RemoveDevButton(canvas)) notes.Add("rimosso " + DevButtonName);
                OakheartMenuSkin.ApplyActive();
                notes.Add("menu");

                // La sesta voce dell'elenco livelli, copiata dalla quinta.
                OakheartEndless.ApplyActive();
                notes.Add("endless");
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
    /// Elimina il bottone di sblocco livelli usato durante lo sviluppo. E' quello
    /// che ha scritto maxLevel=99 sul profilo PlayFab, e stava in bella vista nel
    /// menu principale: nella versione da consegnare non ci deve essere.
    /// </summary>
    private static bool RemoveDevButton(Canvas canvas)
    {
        Transform uiRoot = OakheartHudSkin.UiRoot(canvas);
        Transform dev = uiRoot.Find(DevButtonName);
        if (dev == null) return false;

        Undo.DestroyObjectImmediate(dev.gameObject);
        Debug.Log("[Oakheart] Rimosso " + DevButtonName + " da "
                  + SceneManager.GetActiveScene().name + ".");
        return true;
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
