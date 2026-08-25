using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Valori di gioco che stanno su componenti messi nelle scene.
///
/// Cambiare il valore di serie nello script non basta: Unity conserva nella
/// scena il numero salvato al momento in cui la scena e' stata scritta, e
/// quello vince. Con dieci scene che contengono il giocatore, ritoccarle a mano
/// una per una e' il modo migliore per dimenticarsene una.
///
/// Menu: Tools > Oakheart > Gioco > ...
/// </summary>
public static class OakheartGameplay
{
    /// Messo a true dal batch: niente finestre di dialogo tra una scena e l'altra.
    public static bool Silent = false;

    /// <summary>
    /// Raggio del cerchio d'attacco del giocatore.
    ///
    /// Il cerchio non parte dal giocatore ma 0.6 unita' davanti a lui e mezza
    /// unita' piu' in alto, quindi la portata che si sente in mano e' circa
    /// questo valore piu' mezza unita'. Con 1.0 la spada prendeva a un metro e
    /// mezzo, e sembrava colpire il vuoto.
    /// </summary>
    private const float PlayerAttackRange = 0.75f;

    /// <summary>
    /// Spazio tra il fondo del titolo e la targhetta della selezione livelli.
    ///
    /// Con 40 le due scritte si sfioravano. Il valore e' in unita' di canvas,
    /// quindi resta proporzionato su qualunque schermo.
    /// </summary>
    private const float TitleGap = 90f;

    [MenuItem("Tools/Oakheart/Gioco/Portata dell'attacco")]
    public static void ApplyActive()
    {
        int changed = 0;

        foreach (var attack in Object.FindObjectsByType<PlayerAttack>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(attack);
            SerializedProperty range = so.FindProperty("attackRange");
            if (range == null) continue;
            if (Mathf.Approximately(range.floatValue, PlayerAttackRange)) continue;

            Undo.RecordObject(attack, "Portata attacco");
            float was = range.floatValue;
            range.floatValue = PlayerAttackRange;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(attack);
            changed++;

            Debug.Log($"[Oakheart] {SceneManager.GetActiveScene().name}: portata attacco "
                      + $"da {was} a {PlayerAttackRange}.");
        }

        changed += ApplyTitleGap();

        if (changed == 0) return;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                "Portata dell'attacco impostata a " + PlayerAttackRange + ".\n\n" +
                "Il cerchio parte 0.6 unita' davanti al giocatore, quindi la portata " +
                "che si sente e' circa 1.25 invece di 1.5.", "OK");
    }

    /// <summary>
    /// Lo stacco del titolo sopra la selezione livelli, anche questo salvato in scena.
    /// </summary>
    private static int ApplyTitleGap()
    {
        int changed = 0;

        foreach (var selection in Object.FindObjectsByType<LevelSelection>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(selection);
            SerializedProperty gap = so.FindProperty("titleGap");
            if (gap == null) continue;
            if (Mathf.Approximately(gap.floatValue, TitleGap)) continue;

            Undo.RecordObject(selection, "Stacco del titolo");
            float was = gap.floatValue;
            gap.floatValue = TitleGap;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(selection);
            changed++;

            Debug.Log($"[Oakheart] Stacco del titolo da {was} a {TitleGap}.");
        }

        return changed;
    }
}
