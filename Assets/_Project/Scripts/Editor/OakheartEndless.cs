using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aggiunge la voce ENDLESS in fondo all'elenco dei livelli.
///
/// Il bottone e' una copia di quello del livello 5, cosi eredita da solo skin,
/// dimensioni e carattere: non c'e' niente da ritoccare a mano e resta allineato
/// agli altri anche se un domani la skin cambia.
///
/// L'elenco sta dentro un contenitore che dispone i figli da solo e si allarga
/// per contenerli, quindi basta infilare il bottone al posto giusto: la
/// posizione la calcola Unity.
///
/// Chi decide se e' aperto o chiuso e' LevelSelection a runtime, guardando il
/// progresso su PlayFab. Qui si costruisce soltanto.
///
/// Menu: Tools > Oakheart > Menu > Voce Endless
/// </summary>
public static class OakheartEndless
{
    /// Messo a true dal batch: niente finestre di dialogo tra una scena e l'altra.
    public static bool Silent = false;

    private const string EndlessName = "LevelButtonEndless";
    private const string SourceName = "LevelButton5";
    private const string StartLabel = "ENDLESS  [ LOCKED ]";

    [MenuItem("Tools/Oakheart/Menu/Voce Endless")]
    public static void ApplyActive()
    {
        var selection = Object.FindAnyObjectByType<LevelSelection>(FindObjectsInactive.Include);
        if (selection == null) return;   // non e' la scena del menu

        Transform source = FindDeep(selection.transform.root, SourceName);
        if (source == null)
        {
            Debug.LogWarning("[Oakheart] Non trovo " + SourceName + ": voce Endless non aggiunta.");
            return;
        }

        Transform existing = FindDeep(selection.transform.root, EndlessName);
        GameObject button;

        if (existing != null)
        {
            button = existing.gameObject;
        }
        else
        {
            button = Object.Instantiate(source.gameObject, source.parent);
            button.name = EndlessName;
            Undo.RegisterCreatedObjectUndo(button, "Voce Endless");
            Debug.Log("[Oakheart] Aggiunta la voce " + EndlessName + " dopo " + SourceName + ".");
        }

        PlaceAfterSource(button.transform, source);

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && label.text != StartLabel)
        {
            Undo.RecordObject(label, "Etichetta Endless");
            label.text = StartLabel;
            EditorUtility.SetDirty(label);
        }

        // Aggancio al campo dello script, cosi non va trascinato a mano.
        var so = new SerializedObject(selection);
        SerializedProperty field = so.FindProperty("endlessButton");
        if (field != null)
        {
            var component = button.GetComponent<Button>();
            if (field.objectReferenceValue != component)
            {
                field.objectReferenceValue = component;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(selection);
            }
        }
        else
        {
            Debug.LogWarning("[Oakheart] LevelSelection non ha il campo endlessButton: "
                             + "ricompila prima di rilanciare.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                "Voce ENDLESS aggiunta in fondo all'elenco dei livelli.\n\n" +
                "Resta chiusa col lucchetto finche' non si finisce il livello 5: " +
                "il conto e' lo stesso che sblocca gli altri livelli.", "OK");
    }

    /// <summary>
    /// Rimette la voce subito sotto il livello 5.
    ///
    /// Va rifatto a ogni passata, non solo quando il bottone viene creato: la
    /// skin del menu gira prima di questo comando e rimette in ordine i figli
    /// che conosce, spingendo in fondo quello che non conosce. Fissando la
    /// posizione solo alla creazione, al secondo giro la voce ENDLESS finiva
    /// sotto il conteggio dei livelli e sotto il bottone BACK.
    ///
    /// Prima si sposta in fondo e poi la si reinserisce: cosi l'indice del
    /// livello 5 e' gia' quello definitivo quando lo si legge, e non serve
    /// tenere conto dello scorrimento degli altri.
    /// </summary>
    private static void PlaceAfterSource(Transform button, Transform source)
    {
        int wanted = source.GetSiblingIndex() + 1;
        if (button.GetSiblingIndex() == wanted) return;

        Undo.RecordObject(button, "Posizione della voce Endless");
        button.SetAsLastSibling();
        button.SetSiblingIndex(source.GetSiblingIndex() + 1);
        EditorUtility.SetDirty(button);

        Debug.Log("[Oakheart] Voce " + EndlessName + " rimessa subito dopo " + SourceName + ".");
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
