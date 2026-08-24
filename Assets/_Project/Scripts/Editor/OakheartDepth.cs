using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Ordine di disegno per profondita' in vista dall'alto.
///
/// Rocce, cespugli, castello, giocatore e nemici stanno tutti sullo stesso
/// sorting order (5): senza un criterio, chi finisce davanti e chi dietro lo
/// decide il caso. Con l'asse di ordinamento sulla Y, chi sta piu' in basso
/// viene disegnato davanti, che e' esattamente come si legge una mappa vista
/// dall'alto.
///
/// Poi c'e' il tremolio. Lo sprite del giocatore e' tagliato con i fotogrammi
/// RITAGLIATI: ogni posa ha un rettangolo di altezza diversa (127, 130, 135,
/// 122, 112, 117, 112, 107 pixel). Lo SpriteRenderer di serie ordina usando il
/// CENTRO del rettangolo, che con quelle altezze si alza e si abbassa di 0.14
/// unita' a ogni respiro dell'animazione da fermo. Vicino a una roccia quel
/// mezzo centimetro basta a far scavalcare la soglia avanti e indietro, e il
/// personaggio sfarfalla davanti/dietro. Mettendo il punto di ordinamento sul
/// PIVOT, che sta ai piedi e non si muove mai, il tremolio sparisce.
///
/// Menu: Tools > Oakheart > Grafica > ...
/// </summary>
public static class OakheartDepth
{
    /// Messo a true dal batch: niente finestre di dialogo tra una scena e l'altra.
    public static bool Silent = false;

    /// <summary>
    /// Colore che si vede dove non c'e' mappa.
    ///
    /// Le rocce del bordo hanno l'arte con i vuoti tra un pilastro e l'altro, e
    /// sotto quei tasselli non c'e' nessun terreno perche' ogni livello ha un
    /// solo Tilemap: da quelle fessure passava il fondo della camera, che era un
    /// verde erba scuro (#22501E) diverso da tutto il resto e si notava.
    ///
    /// La cura e' dare al fondo la tinta dominante del terreno di quel livello:
    /// nelle fessure sparisce, e fuori dalla mappa il bordo continua invece di
    /// tagliare di netto. Per questo la tinta e' per scena e non una sola: il
    /// verde del prato sulla neve o nel deserto stonerebbe.
    ///
    /// I valori sono la tinta piu' frequente del terreno di ciascun livello.
    /// Le scene non elencate (gli interni) non vengono toccate: sono stanze
    /// chiuse, il fondo non si vede.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<string, Color32> VoidColors =
        new System.Collections.Generic.Dictionary<string, Color32>
    {
        { "Game",        new Color32(0x90, 0xAC, 0x49, 0xFF) },   // prato
        { "Game_Field",  new Color32(0x74, 0xA3, 0x34, 0xFF) },   // campi
        { "Game_Desert", new Color32(0xEC, 0xD5, 0xAD, 0xFF) },   // sabbia
        { "Game_Snow",   new Color32(0xE7, 0xCB, 0x9C, 0xFF) },   // neve
        { "Game_Lava",   new Color32(0x38, 0x32, 0x30, 0xFF) },   // roccia lavica
    };

    [MenuItem("Tools/Oakheart/Grafica/Ordina per profondita'")]
    public static void ApplyActive()
    {
        int renderers = SetRendererSortAxis();
        int changed = SetSortAxis();
        int points = SetSortPoints();
        int backs = SetCameraBackground();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Oakheart] Profondita': {renderers} renderer URP corretti, "
                  + $"asse impostato su {changed} camere, "
                  + $"{points} sprite ordinati dal pivot, {backs} fondi camera aggiornati.");

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                "Ordinamento per profondita' attivo.\n\n" +
                "Chi sta piu' in basso viene disegnato davanti, e il punto di " +
                "riferimento e' il pivot (i piedi) invece del centro del " +
                "fotogramma: cosi l'animazione da fermo non fa piu' sfarfallare " +
                "il personaggio davanti e dietro le rocce.\n\n" +
                "Il fondo della camera prende la tinta del terreno di questa " +
                "scena, cosi le fessure nell'arte delle rocce di bordo non si " +
                "vedono piu'.", "OK");
    }

    /// <summary>
    /// L'asse di ordinamento del renderer 2D di URP.
    ///
    /// Questa e' l'impostazione che comanda davvero, e per un bel po' e' stata
    /// quella sbagliata. Il progetto usa URP con il Renderer 2D, e quel renderer
    /// ha una PROPRIA voce Transparency Sort Mode dentro Assets/Settings/
    /// Renderer2D.asset che ha la precedenza su quella di Project Settings e su
    /// quella delle camere. Era su Default, cioe' "ordina per distanza dalla
    /// camera": e siccome in un gioco 2D tutti gli sprite stanno a z = 0, quella
    /// distanza e' identica per tutti. A parita' di valore l'ordine lo decide
    /// un criterio interno, che cambia quando cambia lo sprite disegnato: da
    /// qui il personaggio che, fermo dentro un cespuglio, sfarfalla davanti e
    /// dietro a ogni fotogramma dell'animazione da fermo.
    ///
    /// L'asse era gia' scritto correttamente (0, 1, 0), ma con la modalita' su
    /// Default veniva semplicemente ignorato.
    /// </summary>
    private static int SetRendererSortAxis()
    {
        const int CustomAxis = 3;   // UnityEngine.TransparencySortMode.CustomAxis
        Vector3 wanted = new Vector3(0f, 1f, 0f);

        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableRendererData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (data == null) continue;

            var so = new SerializedObject(data);
            SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
            if (mode == null) continue;              // non e' un renderer 2D

            SerializedProperty axis = so.FindProperty("m_TransparencySortAxis");

            bool changed = false;
            if (mode.intValue != CustomAxis) { mode.intValue = CustomAxis; changed = true; }
            if (axis != null && axis.vector3Value != wanted) { axis.vector3Value = wanted; changed = true; }
            if (!changed) continue;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            n++;
            Debug.Log($"[Oakheart] Renderer 2D '{path}': ordinamento sull'asse Y.");
        }

        if (n > 0) AssetDatabase.SaveAssets();
        return n;
    }

    /// <summary>
    /// Ordinamento lungo l'asse Y, sia a livello di progetto sia sulle camere
    /// della scena. Le camere possono avere un'impostazione propria che vince
    /// su quella di progetto, quindi si mettono d'accordo tutt'e due.
    /// </summary>
    private static int SetSortAxis()
    {
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);

        int n = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
        {
            if (cam.transparencySortMode == TransparencySortMode.CustomAxis
                && cam.transparencySortAxis == new Vector3(0f, 1f, 0f))
                continue;

            Undo.RecordObject(cam, "Asse di ordinamento");
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
            EditorUtility.SetDirty(cam);
            n++;
        }
        return n;
    }

    /// <summary>
    /// Punto di ordinamento sul pivot per tutti gli sprite di scena.
    ///
    /// Rocce, cespugli e castello hanno il pivot alla base: ordinarli da li'
    /// vuol dire ordinarli da dove appoggiano a terra, che e' quello che serve.
    /// Il giocatore ha il pivot ai piedi e i fotogrammi ritagliati, quindi e'
    /// l'unico modo per avere un valore che non balla.
    ///
    /// Il nemico NON viene toccato ed e' voluto: il suo foglio non e' ritagliato
    /// (fotogrammi tutti 192x192), quindi il centro e' stabile, e siccome il
    /// pivot sta 0.55 sotto i suoi piedi il centro e' anche il piu' vicino al
    /// punto giusto.
    /// </summary>
    private static int SetSortPoints()
    {
        int n = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None))
        {
            if (sr == null || sr.spriteSortPoint == SpriteSortPoint.Pivot) continue;

            Undo.RecordObject(sr, "Punto di ordinamento");
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            EditorUtility.SetDirty(sr);
            n++;
        }
        return n;
    }

    /// <summary>
    /// Fondo della camera dove non c'e' mappa, nella tinta del terreno di questa scena.
    /// </summary>
    private static int SetCameraBackground()
    {
        string scene = SceneManager.GetActiveScene().name;
        Color32 tint;
        if (!VoidColors.TryGetValue(scene, out tint))
        {
            Debug.Log($"[Oakheart] '{scene}' non ha una tinta di fondo indicata: fondo camera lasciato com'e'.");
            return 0;
        }

        Color target = tint;
        int n = 0;
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
        {
            // Le camera della UI non ripuliscono lo sfondo: non c'entrano niente.
            if (cam.clearFlags != CameraClearFlags.SolidColor) continue;
            if (cam.backgroundColor == target) continue;

            Undo.RecordObject(cam, "Fondo camera");
            cam.backgroundColor = target;
            EditorUtility.SetDirty(cam);
            n++;
        }
        return n;
    }
}
