using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aggiunge i collider mancanti alle decorazioni.
///
/// Nella scena Game le rocce erano soltanto SpriteRenderer: nessun collider.
/// Cosi' non fermavano nessuno, e soprattutto non fermavano nemmeno il
/// controllo di spazio libero dello spawn: i nemici comparivano dentro e dietro
/// le rocce perche' per il motore fisico li' non c'era niente.
///
/// Menu: Tools > Oakheart > Collisioni > ...
/// </summary>
public static class OakheartColliders
{
    /// Messo a true dal batch: niente finestre di dialogo tra una scena e l'altra.
    public static bool Silent = false;

    private struct Box
    {
        public string Name;
        public Vector2 Offset, Size;
        public Box(string n, Vector2 o, Vector2 s) { Name = n; Offset = o; Size = s; }
    }

    // Solo l'impronta al suolo: in vista dall'alto l'altezza del disegno e'
    // l'elevazione dell'edificio, non l'area che occupa a terra.
    private static readonly Box[] CastleBoxes =
    {
        new Box("base sinistra", new Vector2(-1.37f, -1.125f), new Vector2(1.64f, 1.09f)),
        new Box("base destra",   new Vector2( 1.37f, -1.125f), new Vector2(1.64f, 1.09f)),
    };

    /// Posizione della soglia, in unita' locali del castello.
    private static readonly Vector3 EntranceLocal = new Vector3(0f, -1.25f, 0f);

    // ------------------------------------------------------------------
    // Castello. NON fa parte del batch: i box sono stati poi ritoccati a mano
    // nella scena, e rilanciare il comando li sovrascriverebbe.
    // ------------------------------------------------------------------
    [MenuItem("Tools/Oakheart/Collisioni/Castello e ingresso (sovrascrive i ritocchi a mano)")]
    public static void CastleColliders()
    {
        GameObject castle = GameObject.Find("Castle");
        if (castle == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Nessun oggetto 'Castle' in questa scena.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Oakheart",
            "Questo comando cancella i BoxCollider2D attuali del castello e li rimette " +
            "come da script. Se li avevi sistemati a mano, li perdi.\n\nProcedere?",
            "Sovrascrivi", "Annulla"))
            return;

        Undo.RegisterFullObjectHierarchyUndo(castle, "Collider castello");

        foreach (var old in castle.GetComponents<BoxCollider2D>())
            Undo.DestroyObjectImmediate(old);

        foreach (var b in CastleBoxes)
        {
            var col = Undo.AddComponent<BoxCollider2D>(castle);
            col.offset = b.Offset;
            col.size = b.Size;
            col.isTrigger = false;
        }

        GameObject entrance = GameObject.Find("CastleEntrance");
        if (entrance != null)
        {
            Undo.RecordObject(entrance.transform, "Sposta ingresso");
            Vector3 target = castle.transform.TransformPoint(EntranceLocal);
            target.z = entrance.transform.position.z;
            entrance.transform.position = target;
            Debug.Log($"[Oakheart] CastleEntrance spostato sulla soglia: {target}");
        }
        else
        {
            Debug.LogWarning("[Oakheart] CastleEntrance non trovato: controlla a mano dove si entra.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Castello: 2 collider sull'impronta al suolo, varco aperto.");
    }

    // ------------------------------------------------------------------
    // Hitbox del nemico.
    //
    // Storia di due errori, per non rifarli.
    //
    // Lo sprite del nemico e' un foglio 1536x192 tagliato in 8 fotogrammi da
    // 192x192 SENZA ritaglio, con pivot in basso al centro. Quindi il disegno
    // sta tutto SOPRA l'origine, da 0 a 1.92, e il corpo visibile va da 0.55
    // (i piedi) a 1.44 (la testa).
    //
    // Il file .meta ha anche un campo spritePivot 0.5,0.5, che pero' vale solo
    // per gli sprite singoli: qui comanda il pivot 0.5,0 scritto dentro ogni
    // fotogramma. Aver letto quello sbagliato mi ha fatto credere che il
    // disegno fosse centrato sull'origine, e da li' sono nati due collider
    // sbagliati di fila, tutti e due sotto i piedi del nemico invece che sul
    // nemico. Da qui il "non riesco piu' a ucciderli".
    //
    // Misura giusta: dai piedi in su, larghezza poco piu' del corpo.
    // Copre da 0.55 a 1.15, cioe' gambe e busto.
    // ------------------------------------------------------------------
    private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy.prefab";
    //
    // Seconda passata: la prima versione era un'impronta ai piedi di 0.45 x 0.24.
    // Posizione giusta, ma area piu' che dimezzata rispetto all'originale
    // (0.4 x 0.6): la spada ci passava accanto senza toccare niente.
    // Ora il collider copre il corpo visibile del nemico (da -0.41 a +0.35),
    // quindi e' piu' facile colpirlo di prima, e sta comunque appoggiato a terra.
    private static readonly Vector2 EnemyBoxSize = new Vector2(0.55f, 0.6f);
    private static readonly Vector2 EnemyBoxOffset = new Vector2(0.05f, 0.85f);

    [MenuItem("Tools/Oakheart/Collisioni/Hitbox del nemico")]
    public static void EnemyHitbox()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        if (root == null)
        {
            Debug.LogError("[Oakheart] Prefab del nemico non trovato: " + EnemyPrefabPath);
            return;
        }

        try
        {
            var col = root.GetComponentInChildren<BoxCollider2D>();
            if (col == null)
            {
                Debug.LogWarning("[Oakheart] Il prefab del nemico non ha un BoxCollider2D.");
                return;
            }

            if (col.size == EnemyBoxSize && col.offset == EnemyBoxOffset)
            {
                Debug.Log("[Oakheart] Hitbox del nemico gia' a posto.");
                return;
            }

            Vector2 wasSize = col.size, wasOffset = col.offset;
            col.size = EnemyBoxSize;
            col.offset = EnemyBoxOffset;

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            Debug.Log($"[Oakheart] Hitbox del nemico: da size {wasSize} offset {wasOffset} "
                      + $"a size {EnemyBoxSize} offset {EnemyBoxOffset}.");

            if (!Silent)
                EditorUtility.DisplayDialog("Oakheart",
                    "Hitbox del nemico appoggiata ai piedi.\n\n" +
                    "Copre da 0.55 a 1.15, cioe' gambe e busto: e' larga quanto il " +
                    "corpo, quindi colpirla e' piu' facile di prima, e parte da terra, " +
                    "quindi contro rocce e muri il nemico si ferma dove lo vedi.", "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------
    // Rocce. Questo invece sta nel batch: e' idempotente, salta quelle che
    // hanno gia' un collider e non tocca nient'altro.
    // ------------------------------------------------------------------
    [MenuItem("Tools/Oakheart/Collisioni/Base alle rocce")]
    public static void DecorationColliders()
    {
        // Solo le rocce. I cespugli restano attraversabili di proposito: sono
        // bassi, stanno in mezzo all'arena e renderli solidi cambierebbe il
        // movimento in campo aperto senza risolvere niente.
        string[] prefixes = { "Rock" };

        const float widthFrac = 0.66f;   // piu' stretto dello sprite: tra una roccia
                                         // e l'altra deve restare un varco percorribile

        // Quanto della roccia e' solido, dal basso. Con 0.42 ci si saliva sopra
        // arrivando da nord: il collider del giocatore sta 0.67 sopra i suoi
        // piedi, quindi scavalcava la fascia solida senza toccarla. Con 0.62 lo
        // scavalco non riesce piu', ma resta il passaggio dietro la cima della
        // roccia, che in vista dall'alto e' giusto che ci sia.
        const float baseFrac  = 0.62f;

        var done = new List<string>();
        int skipped = 0;

        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string n = sr.gameObject.name;
            bool match = false;
            foreach (var p in prefixes) if (n.StartsWith(p)) { match = true; break; }
            if (!match || sr.sprite == null) continue;
            Undo.RegisterFullObjectHierarchyUndo(sr.gameObject, "Collider decorazioni");

            // Se c'e' gia' un box lo si aggiorna invece di saltarlo: altrimenti
            // dopo la prima passata il comando non correggerebbe piu' niente.
            var col = sr.GetComponent<BoxCollider2D>();
            bool isNew = col == null;
            if (isNew) col = Undo.AddComponent<BoxCollider2D>(sr.gameObject);
            else skipped++;

            // sprite.bounds e' gia' relativo al pivot, come l'offset del collider
            Bounds lb = sr.sprite.bounds;
            float h = lb.size.y * baseFrac;
            col.size = new Vector2(lb.size.x * widthFrac, h);
            col.offset = new Vector2(lb.center.x, lb.min.y + h * 0.5f);
            col.isTrigger = false;
            if (isNew) done.Add(n);
        }

        if (done.Count == 0 && skipped == 0) return;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Oakheart] Rocce: {done.Count} collider nuovi, {skipped} aggiornati.");

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                done.Count + " rocce nuove, " + skipped + " aggiornate.\n\n" +
                "Il collider copre solo la base dello sprite, non tutta l'altezza: " +
                "si passa davanti senza restare incastrati, ma i nemici non ci " +
                "compaiono piu' dentro ne' dietro.", "OK");
    }
}
