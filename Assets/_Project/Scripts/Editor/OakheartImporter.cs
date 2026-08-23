using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura in blocco gli sprite del pack "Pixelkiln UI - Oakheart".
/// Imposta Point filter, nessuna compressione, Full Rect e i bordi 9-slice
/// misurati sui PNG 2x del pack.
///
/// Menu: Tools > Oakheart > Configura sprite UI
/// </summary>
public static class OakheartImporter
{
    private const string RootFolder = "Assets/_Project/Art/UI/Oakheart";

    // 100 = stesso valore di "Reference Pixels Per Unit" del Canvas,
    // cosi "Set Native Size" su una Image restituisce i pixel reali dello sprite.
    private const float PixelsPerUnit = 100f;

    // Bordi 9-slice in pixel. Ordine Unity: X=left, Y=bottom, Z=right, W=top.
    private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        // pannelli
        { "panel_48x48",                    new Vector4( 8, 10,  8, 12) },
        { "panel_ornate_48x48",             new Vector4(14, 14, 14, 14) },
        { "panel_sunken_24x24",             new Vector4( 4,  4,  4,  4) },
        { "nameplate_44x14",                new Vector4( 4,  4,  4,  6) },
        { "tooltip_56x36",                  new Vector4( 8, 14,  8,  8) },

        // bottoni
        { "button_primary_normal_40x16",    new Vector4( 4,  8,  4, 14) },
        { "button_primary_hover_40x16",     new Vector4( 4,  8,  4, 14) },
        { "button_primary_pressed_40x16",   new Vector4( 6,  4,  4, 10) },
        { "button_secondary_normal_40x16",  new Vector4( 4,  8,  4, 14) },
        { "button_secondary_hover_40x16",   new Vector4( 4,  8,  4, 14) },
        { "button_secondary_pressed_40x16", new Vector4( 6,  4,  4, 10) },

        // barre
        { "bar_track_64x10",                new Vector4( 6,  4,  4,  6) },
        { "bar_health_75_64x10",            new Vector4( 6,  8,  8,  8) },
        { "bar_mana_75_64x10",              new Vector4( 6,  8,  8,  8) },
        { "bar_xp_75_64x10",                new Vector4( 6,  8,  8,  8) },
        { "bar_fill_health",                new Vector4( 2,  4,  2,  4) }, // generato: riempimento estratto da bar_health

        // icons, badges, toggles, cursors, handles: nessun bordo (non vanno stirati)
    };

    [MenuItem("Tools/Oakheart/Configura sprite UI")]
    public static void ConfigureAll()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Cartella non trovata:\n" + RootFolder, "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { RootFolder });
        int done = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Oakheart",
                    Path.GetFileName(path), (float)i / guids.Length);
                if (Configure(path)) done++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Oakheart] Configurati {done}/{guids.Length} sprite in {RootFolder}.");
        EditorUtility.DisplayDialog("Oakheart",
            $"Configurati {done} sprite su {guids.Length}.\n\n" +
            "Point filter, nessuna compressione, Full Rect, bordi 9-slice.", "OK");
    }

    [MenuItem("Tools/Oakheart/Verifica bordi (solo log)")]
    public static void LogBorders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { RootFolder });
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) continue;
            Debug.Log($"[Oakheart] {sp.name}  {sp.rect.width}x{sp.rect.height}  border={sp.border}");
        }
    }

    private static bool Configure(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        string key = Path.GetFileNameWithoutExtension(path);
        Vector4 border;
        if (!Borders.TryGetValue(key, out border)) border = Vector4.zero;

        var s = new TextureImporterSettings();
        importer.ReadTextureSettings(s);

        s.textureType         = TextureImporterType.Sprite;
        s.spriteMode          = (int)SpriteImportMode.Single;
        s.spriteMeshType      = SpriteMeshType.FullRect;   // indispensabile per il 9-slice
        s.spriteExtrude       = 0;
        s.spritePixelsPerUnit = PixelsPerUnit;
        s.spriteAlignment     = (int)SpriteAlignment.Center;
        s.spriteBorder        = border;
        s.filterMode          = FilterMode.Point;          // niente sfocatura sul pixel art
        s.mipmapEnabled       = false;
        s.alphaIsTransparency = true;
        s.wrapMode            = TextureWrapMode.Clamp;
        s.npotScale           = TextureImporterNPOTScale.None;
        s.readable            = false;

        importer.SetTextureSettings(s);
        importer.spriteBorder       = border;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize     = 2048;

        // Su Android ETC/ASTC introdurrebbe artefatti sui bordi netti: forziamo RGBA32.
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden         = true;
        android.format             = TextureImporterFormat.RGBA32;
        android.maxTextureSize     = 2048;
        android.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(android);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }
}
