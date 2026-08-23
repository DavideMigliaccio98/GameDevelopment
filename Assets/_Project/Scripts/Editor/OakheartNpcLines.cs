using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Riscrive le battute degli NPC negozio.
///
/// Le vecchie erano 4 e citavano solo la cura con il suo costo: la penultima
/// anticipava l'ultima, e nessuna nominava POTENZIA. Ora sono 3, senza
/// ripetizioni, e i costi restano solo sulle etichette dei bottoni, cosi
/// cambiare healCostScore/boostCostScore non manda in disaccordo il dialogo.
///
/// Menu: Tools > Oakheart > Dialoghi > Aggiorna battute NPC (scena attiva)
/// </summary>
public static class OakheartNpcLines
{
    // scena -> battute complete, in ordine
    private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
    {
        { "CastleInterior", new[] {
            "Salve, eroe!",
            "Vedo che hai combattuto duramente.",
            "Posso ridarti le forze, oppure benedire la tua lama."
        }},
        { "CabinInterior", new[] {
            "Brrr, fa freddo qui fuori.",
            "Vieni dentro a riscaldarti, eroe.",
            "Ho brodo caldo per le ferite, oppure olio per affilare la lama."
        }},
        { "HouseInterior", new[] {
            "Benvenuto nella mia locanda, eroe!",
            "Vedo che hai combattuto con coraggio là fuori.",
            "Un pasto caldo ti rimette in sesto, oppure ti affilo la spada."
        }},
        { "TentInterior", new[] {
            "Salve, viaggiatore del deserto!",
            "Le mie pozioni sono famose da queste parti.",
            "Ho pozioni di vita ed elisir di forza. Quale ti serve?"
        }},
        // FishingHutInterior: bozza non usata, esclusa di proposito
    };

    /// <summary>Attivata dal batch per non far comparire una finestra per scena.</summary>
    public static bool Silent = false;

    [MenuItem("Tools/Oakheart/Dialoghi/Aggiorna battute NPC (scena attiva)")]
    public static void UpdateActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        string[] newLines;
        if (!Lines.TryGetValue(scene.name, out newLines))
        {
            if (!Silent)
                EditorUtility.DisplayDialog("Oakheart",
                    "Nessuna battuta prevista per la scena '" + scene.name + "'.", "OK");
            return;
        }

        var npcs = Object.FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (npcs.Length == 0)
        {
            if (!Silent)
                EditorUtility.DisplayDialog("Oakheart", "Nessun NPC in questa scena.", "OK");
            return;
        }

        int changed = 0;
        foreach (var npc in npcs)
        {
            var so = new SerializedObject(npc);
            var prop = so.FindProperty("dialogLines");
            if (prop == null || !prop.isArray) continue;

            if (SameAs(prop, newLines)) continue;

            Debug.Log("[Oakheart] " + npc.NpcName + ": " + prop.arraySize
                      + " battute -> " + newLines.Length);

            prop.arraySize = newLines.Length;
            for (int i = 0; i < newLines.Length; i++)
                prop.GetArrayElementAtIndex(i).stringValue = newLines[i];

            so.ApplyModifiedProperties();   // registra anche l'Undo
            changed++;
        }

        if (changed > 0) EditorSceneManager.MarkSceneDirty(scene);

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
            changed > 0
                ? "Riscritte le battute di " + changed + " NPC in " + scene.name + "."
                : "Le battute erano gia' aggiornate.",
            "OK");
    }

    private static bool SameAs(SerializedProperty prop, string[] target)
    {
        if (prop.arraySize != target.Length) return false;
        for (int i = 0; i < target.Length; i++)
            if (prop.GetArrayElementAtIndex(i).stringValue != target[i]) return false;
        return true;
    }
}
