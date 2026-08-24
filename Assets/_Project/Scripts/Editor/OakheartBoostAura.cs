using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea (o aggiorna) l'alone dorato del potenziamento sul Player della scena aperta.
///
/// Il Player e' definito dentro ogni scena e non e' un Prefab, quindi l'operazione
/// va ripetuta scena per scena: se ne occupa il batch.
///
/// Menu: Tools > Oakheart > Aura potenziamento > Crea sul Player (scena attiva)
/// </summary>
public static class OakheartBoostAura
{
    private const string SpritePath = "Assets/_Project/Sprites/FX/aura_boost.png";
    private const string ChildName = "BoostAura";
    private const float SizeFactor = 1.25f;   // rispetto all'altezza dello sprite del Player

    public static bool Silent = false;

    [MenuItem("Tools/Oakheart/Aura potenziamento/Crea sul Player (scena attiva)")]
    public static void CreateOnPlayer()
    {
        var sprite = EnsureSpriteSettings();
        if (sprite == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart",
                "Sprite non trovato:\n" + SpritePath, "OK");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart",
                "Nessun oggetto con tag Player in questa scena.", "OK");
            return;
        }

        var attack = player.GetComponent<PlayerAttack>();
        if (attack == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart",
                "Il Player non ha PlayerAttack.", "OK");
            return;
        }

        // Va preso PRIMA di creare l'alone, altrimenti la ricerca potrebbe
        // restituire il renderer dell'alone stesso.
        SpriteRenderer playerSR = FindPlayerRenderer(player);

        Undo.RegisterFullObjectHierarchyUndo(player, "Aura potenziamento");

        Transform aura = player.transform.Find(ChildName);
        if (aura == null)
        {
            var go = new GameObject(ChildName, typeof(SpriteRenderer), typeof(BoostAura));
            Undo.RegisterCreatedObjectUndo(go, ChildName);
            go.transform.SetParent(player.transform, false);
            aura = go.transform;
        }

        var sr = aura.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(aura.gameObject);
        sr.sprite = sprite;
        sr.color = new Color32(0xE8, 0x48, 0x40, 0x00);   // acceso a runtime dallo script
        sr.enabled = false;

        if (playerSR != null)
        {
            // dietro al Player, sullo stesso layer di ordinamento
            sr.sortingLayerID = playerSR.sortingLayerID;
            sr.sortingOrder = playerSR.sortingOrder - 1;

            float targetH = playerSR.bounds.size.y * SizeFactor;
            float spriteH = sprite.rect.height / sprite.pixelsPerUnit;
            if (spriteH > 0.0001f)
                aura.localScale = Vector3.one * (targetH / spriteH);

            // Centro dello sprite riportato nello spazio locale del Player:
            // InverseTransformPoint tiene conto di pivot e scala, cosa che il
            // solo scarto su Y non faceva. Se il corpo non e' centrato nel
            // fotogramma, si rifinisce con Extra Offset nell'Inspector.
            Vector3 localCenter = player.transform.InverseTransformPoint(playerSR.bounds.center);
            aura.localPosition = new Vector3(localCenter.x, localCenter.y, 0f);
        }

        var comp = aura.GetComponent<BoostAura>();
        if (comp == null) comp = Undo.AddComponent<BoostAura>(aura.gameObject);
        var so = new SerializedObject(comp);
        var p = so.FindProperty("source");
        if (p != null) { p.objectReferenceValue = attack; so.ApplyModifiedProperties(); }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Aura potenziamento pronta in " + SceneManager.GetActiveScene().name
                  + " (scala " + aura.localScale.x.ToString("F2") + ").");
        if (!Silent) EditorUtility.DisplayDialog("Oakheart",
            "Aura creata sul Player di " + SceneManager.GetActiveScene().name + ".", "OK");
    }

    private static SpriteRenderer FindPlayerRenderer(GameObject player)
    {
        foreach (var r in player.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (r.gameObject.name == ChildName) continue;
            if (r.sprite != null) return r;
        }
        return null;
    }

    /// <summary>
    /// L'alone e' pixel art come tutto il resto: filtro Point e nessuna compressione,
    /// altrimenti i gradini dell'alone diventano una sfumatura sporca.
    /// </summary>
    private static Sprite EnsureSpriteSettings()
    {
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null) return null;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 32f)) { importer.spritePixelsPerUnit = 32f; changed = true; }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
}
