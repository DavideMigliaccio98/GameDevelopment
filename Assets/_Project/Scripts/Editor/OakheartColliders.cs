using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aggiunge i collider mancanti alle decorazioni.
///
/// Nella scena Game castello, rocce e cespugli sono soltanto SpriteRenderer:
/// nessun collider, quindi ci passano attraverso sia i nemici sia il giocatore.
/// Negli altri livelli gli ostacoli sono Tilemap che il collider ce l'hanno.
///
/// Menu: Tools > Oakheart > Collisioni > ...
/// </summary>
public static class OakheartColliders
{
    // Misure in unita' locali dello sprite (pivot al centro, 320x256 px a 64 PPU
    // = 5.0 x 4.0 unita'). Ricavate guardando l'arte: il cancello sta in basso
    // al centro e va lasciato aperto.
    private struct Box
    {
        public string Name;
        public Vector2 Offset, Size;
        public Box(string n, Vector2 o, Vector2 s) { Name = n; Offset = o; Size = s; }
    }

    // Solo l'impronta al suolo: in vista dall'alto l'altezza del disegno e'
    // l'elevazione dell'edificio, non l'area che occupa a terra. Coprire tutto
    // lo sprite bloccherebbe un rettangolo molto piu' grande del castello.
    private static readonly Box[] CastleBoxes =
    {
        new Box("base sinistra", new Vector2(-1.37f, -1.125f), new Vector2(1.64f, 1.09f)),
        new Box("base destra",   new Vector2( 1.37f, -1.125f), new Vector2(1.64f, 1.09f)),
    };

    /// Posizione della soglia, in unita' locali del castello.
    private static readonly Vector3 EntranceLocal = new Vector3(0f, -1.25f, 0f);

    [MenuItem("Tools/Oakheart/Collisioni/Castello e ingresso")]
    public static void CastleColliders()
    {
        GameObject castle = GameObject.Find("Castle");
        if (castle == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Nessun oggetto 'Castle' in questa scena.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(castle, "Collider castello");

        // Si ripulisce prima, cosi rilanciare il comando non accumula collider.
        foreach (var old in castle.GetComponents<BoxCollider2D>())
            Undo.DestroyObjectImmediate(old);

        foreach (var b in CastleBoxes)
        {
            var col = Undo.AddComponent<BoxCollider2D>(castle);
            col.offset = b.Offset;
            col.size = b.Size;
            col.isTrigger = false;
        }

        // Il trigger d'ingresso stava dentro la muratura: finche' il castello era
        // attraversabile funzionava, ma con i muri solidi diventerebbe irraggiungibile.
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
        EditorUtility.DisplayDialog("Oakheart",
            "Castello reso solido alla base.\nIl varco del cancello resta aperto e il trigger e' sulla soglia.", "OK");
    }

    [MenuItem("Tools/Oakheart/Collisioni/Base a rocce e cespugli")]
    public static void DecorationColliders()
    {
        string[] prefixes = { "Rock", "Bush" };
        const float widthFrac = 0.72f;   // piu' stretto dello sprite: si passa rasenti
        const float baseFrac  = 0.42f;   // solo la parte bassa, quella appoggiata a terra

        var done = new List<string>();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string n = sr.gameObject.name;
            bool match = false;
            foreach (var p in prefixes) if (n.StartsWith(p)) { match = true; break; }
            if (!match || sr.sprite == null) continue;
            if (sr.GetComponent<Collider2D>() != null) continue;   // gia' fatto

            Undo.RegisterFullObjectHierarchyUndo(sr.gameObject, "Collider decorazioni");
            var col = Undo.AddComponent<BoxCollider2D>(sr.gameObject);

            // sprite.bounds e' gia' relativo al pivot, come l'offset del collider
            Bounds lb = sr.sprite.bounds;
            float h = lb.size.y * baseFrac;
            col.size = new Vector2(lb.size.x * widthFrac, h);
            col.offset = new Vector2(lb.center.x, lb.min.y + h * 0.5f);
            done.Add(n);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Collider aggiunti a " + done.Count + " decorazioni: "
                  + string.Join(", ", done.ToArray()));
        EditorUtility.DisplayDialog("Oakheart",
            done.Count + " decorazioni ora bloccano il passaggio.\n\n" +
            "Il collider copre solo la base dello sprite, non tutta l'altezza: " +
            "cosi si puo' passare davanti senza restare incastrati.", "OK");
    }
}
