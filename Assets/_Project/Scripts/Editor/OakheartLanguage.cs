using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Porta in inglese le scritte dei menu che stanno dentro le scene.
///
/// Le etichette dei bottoni non sono scritte dagli script: sono testo salvato
/// nelle scene, messo a mano nell'editor. Tradurle una per una in dieci scene
/// vuol dire dimenticarsene qualcuna, ed e' esattamente da li' che nasceva il
/// misto di italiano e inglese.
///
/// La sostituzione avviene per TESTO ESATTO, non per nome dell'oggetto: cosi
/// tocca solo le voci elencate qui sotto e non rischia di riscrivere niente
/// altro. Rilanciarlo non fa danni, perche' la seconda volta non trova piu'
/// niente da cambiare.
///
/// Restano in italiano di proposito: le battute degli NPC e i messaggi della
/// bottega, che sono narrativa e non interfaccia.
///
/// Menu: Tools > Oakheart > Lingua > Menu in inglese
/// </summary>
public static class OakheartLanguage
{
    /// Messo a true dal batch: niente finestre di dialogo tra una scena e l'altra.
    public static bool Silent = false;

    private static readonly Dictionary<string, string> Menu = new Dictionary<string, string>
    {
        // menu principale e classifica
        { "CHIUDI",                 "CLOSE" },
        { "GIOCATORE",              "PLAYER" },
        { "CLASSIFICA",             "LEADERBOARD" },
        { "SELEZIONA LIVELLO",      "SELECT LEVEL" },
        { "INDIETRO",               "BACK" },
        { "Caricamento...",         "Loading..." },

        // accesso e registrazione
        { "CONFERMA REGISTRAZIONE", "CREATE ACCOUNT" },
        { "PASSWORD DIMENTICATA?",  "FORGOT PASSWORD?" },
        { "REGISTRATI",             "SIGN UP" },
        { "TORNA AL LOGIN",         "BACK TO LOGIN" },

        // pannelli di fine partita e pausa
        { "PAUSA",                  "PAUSED" },
        { "RIPRENDI",               "RESUME" },
        { "MENU PRINCIPALE",        "MAIN MENU" },
        { "RIPROVA",                "RETRY" },
        { "LIVELLO COMPLETATO!",    "LEVEL COMPLETE!" },
        { "LIVELLO SUCCESSIVO",     "NEXT LEVEL" },
        { "MODALITÀ ENDLESS",  "ENDLESS MODE" },
    };

    [MenuItem("Tools/Oakheart/Lingua/Menu in inglese")]
    public static void ApplyActive()
    {
        var changed = new List<string>();

        foreach (var label in Object.FindObjectsByType<TMP_Text>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (label == null) continue;

            string current = label.text;
            if (string.IsNullOrEmpty(current)) continue;

            string translated;
            if (!Menu.TryGetValue(current.Trim(), out translated)) continue;
            if (current == translated) continue;

            Undo.RecordObject(label, "Traduzione menu");
            label.text = translated;
            EditorUtility.SetDirty(label);
            changed.Add(current + " -> " + translated);
        }

        if (changed.Count == 0) return;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Oakheart] {SceneManager.GetActiveScene().name}: {changed.Count} scritte tradotte.\n"
                  + string.Join("\n", changed.ToArray()));

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                changed.Count + " scritte tradotte in questa scena.\n\n" +
                string.Join("\n", changed.ToArray()), "OK");
    }
}
