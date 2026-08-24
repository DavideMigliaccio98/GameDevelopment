using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applica la skin pixel-art Oakheart al DialogPanel della scena aperta,
/// lo salva come Prefab e lo re-inietta nelle altre scene.
///
/// Il box si adatta in altezza al testo (Vertical Layout Group + Content Size Fitter)
/// con un minimo di 3 righe, cosi non "salta" passando da una battuta all'altra.
///
/// Menu: Tools > Oakheart > DialogPanel > ...
/// </summary>
public static class OakheartDialogSkin
{
    // ---------- percorsi ----------
    private const string ArtRoot    = "Assets/_Project/Art/UI/Oakheart/";
    private const string FontPath   = "Assets/_Project/Fonts/VT323 SDF.asset";
    private const string PrefabDir  = "Assets/_Project/Prefabs/UI";
    private const string PrefabPath = PrefabDir + "/DialogPanel.prefab";

    // ---------- palette (estratta dagli sprite del pack) ----------
    private static readonly Color Cream    = new Color32(0xF6, 0xE7, 0xC0, 0xFF);
    private static readonly Color DarkWood = new Color32(0x2B, 0x1A, 0x10, 0xFF);
    private static readonly Color Overlay  = new Color32(0x00, 0x00, 0x00, 0x8C); // ~55%

    // ---------- misure, in px su Canvas 1080x1920 ----------
    // Sono tutte qui: se vuoi ritoccare, cambia un numero e rilancia il passo 1.
    private const float BoxW        = 1000f; // larghezza fissa del riquadro
    private const float BoxBottom   = 140f;  // distanza dal fondo schermo
    private const float PadSide     = 70f;   // margine sinistro/destro interno
    private const float PadTop      = 90f;   // spazio sotto la targhetta del nome
    private const float PadBottom   = 40f;   // spazio sotto i bottoni
    private const float Spacing     = 28f;   // testo <-> riga bottoni
    private const float TextMinH    = 150f;  // altezza minima testo (~3 righe)
    private const float PlateW      = 520f;
    private const float PlateH      = 92f;
    private const float BtnW        = 420f;
    private const float BtnH        = 108f;
    private const float NextBtnW    = 340f;
    private const float BtnGap      = 20f;
    private const float CloseSize   = 88f;
    private const float FontDialog  = 44f;
    private const float FontName    = 54f;
    private const float FontButton  = 44f;
    private const float FontClose   = 52f;
    private const float PpuMult     = 0.5f;  // < 1 ingrossa il bordo 9-slice

    // ================================================================
    [MenuItem("Tools/Oakheart/DialogPanel/1. Applica skin (scena attiva)")]
    public static void ApplySkin()
    {
        var dialog = Object.FindAnyObjectByType<DialogUI>(FindObjectsInactive.Include);
        if (dialog == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Nessun DialogUI in questa scena.", "OK");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Font non trovato:\n" + FontPath, "OK");
            return;
        }

        Sprite panelOrnate = Load("panels/panel_ornate_48x48");
        Sprite nameplate   = Load("panels/nameplate_44x14");
        Sprite pNormal     = Load("buttons/button_primary_normal_40x16");
        Sprite pHover      = Load("buttons/button_primary_hover_40x16");
        Sprite pPressed    = Load("buttons/button_primary_pressed_40x16");
        Sprite sNormal     = Load("buttons/button_secondary_normal_40x16");
        Sprite sHover      = Load("buttons/button_secondary_hover_40x16");
        Sprite sPressed    = Load("buttons/button_secondary_pressed_40x16");
        if (panelOrnate == null || pNormal == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Sprite Oakheart non trovati. Hai lanciato\nTools > Oakheart > Configura sprite UI ?", "OK");
            return;
        }

        GameObject panelGO = dialog.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(panelGO, "Skin DialogPanel");

        // ---- 1. overlay a tutto schermo ----
        Stretch(panelGO.GetComponent<RectTransform>());
        var panelImg = panelGO.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.sprite = null;
            panelImg.color = Overlay;
            panelImg.raycastTarget = true;
        }

        // ---- 2. la cornice, in larghezza fissa e altezza automatica ----
        Transform box = FindDeep(panelGO.transform, "DialogBox");
        if (box == null) { EditorUtility.DisplayDialog("Oakheart", "Figlio 'DialogBox' non trovato.", "OK"); return; }
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0f);
        boxRT.anchorMax = new Vector2(0.5f, 0f);
        boxRT.pivot     = new Vector2(0.5f, 0f);
        boxRT.anchoredPosition = new Vector2(0f, BoxBottom);
        boxRT.sizeDelta = new Vector2(BoxW, boxRT.sizeDelta.y); // l'altezza la decide il Fitter
        SetPanel(box.gameObject, panelOrnate);

        var vlg = Get<VerticalLayoutGroup>(box.gameObject);
        vlg.padding = new RectOffset((int)PadSide, (int)PadSide, (int)PadTop, (int)PadBottom);
        vlg.spacing = Spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childScaleWidth = false;
        vlg.childScaleHeight = false;

        var fitter = Get<ContentSizeFitter>(box.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ---- 3. targhetta col nome dell'NPC (fuori dal layout) ----
        Transform nameText = FindDeep(box, "NameText");
        Transform plate = FindDeep(box, "NamePlate");
        if (plate == null)
        {
            var go = new GameObject("NamePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "NamePlate");
            plate = go.transform;
            plate.SetParent(box, false);
        }
        Ignore(plate.gameObject);
        var plateRT = plate.GetComponent<RectTransform>();
        plateRT.anchorMin = new Vector2(0.5f, 1f);
        plateRT.anchorMax = new Vector2(0.5f, 1f);
        plateRT.pivot     = new Vector2(0.5f, 0.5f);
        plateRT.anchoredPosition = Vector2.zero;   // a cavallo del bordo superiore
        plateRT.sizeDelta = new Vector2(PlateW, PlateH);
        SetPanel(plate.gameObject, nameplate);

        if (nameText != null)
        {
            nameText.SetParent(plate, false);
            Stretch(nameText.GetComponent<RectTransform>());
            var tmp = nameText.GetComponent<TextMeshProUGUI>();
            Style(tmp, font, FontName, Cream, TextAlignmentOptions.Center);
            if (tmp != null) tmp.margin = new Vector4(12f, 4f, 12f, 8f);
        }

        // ---- 4. testo del dialogo: primo elemento del layout ----
        Transform dialogText = FindDeep(box, "DialogText");
        if (dialogText != null)
        {
            if (dialogText.parent != box) dialogText.SetParent(box, false);
            Style(dialogText.GetComponent<TextMeshProUGUI>(), font, FontDialog,
                  DarkWood, TextAlignmentOptions.TopLeft);
            var le = Get<LayoutElement>(dialogText.gameObject);
            le.ignoreLayout = false;
            le.minHeight = TextMinH;
            le.flexibleHeight = 0f;
            dialogText.SetSiblingIndex(0);
        }

        // ---- 5. riga bottoni: secondo elemento del layout ----
        Transform row = FindDeep(box, "ButtonRow");
        if (row == null)
        {
            var go = new GameObject("ButtonRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "ButtonRow");
            row = go.transform;
            row.SetParent(box, false);
        }
        var hlg = Get<HorizontalLayoutGroup>(row.gameObject);
        hlg.spacing = BtnGap;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childScaleWidth = false;
        hlg.childScaleHeight = false;
        var rowLE = Get<LayoutElement>(row.gameObject);
        rowLE.ignoreLayout = false;
        rowLE.minHeight = BtnH;
        rowLE.preferredHeight = BtnH;
        row.SetSiblingIndex(1);

        SkinButton(box, row, "NextButton",  null,               pNormal, pHover, pPressed,
                   new Vector2(NextBtnW, BtnH), font, FontButton);
        SkinButton(box, row, "HealButton",  "HealButtonLabel",  pNormal, pHover, pPressed,
                   new Vector2(BtnW, BtnH), font, FontButton);
        SkinButton(box, row, "BoostButton", "BoostButtonLabel", sNormal, sHover, sPressed,
                   new Vector2(BtnW, BtnH), font, FontButton);

        // ---- 6. chiudi: angolo alto a destra, fuori dal layout ----
        Transform close = FindDeep(box, "CloseButton");
        if (close != null)
        {
            SkinButton(box, box, "CloseButton", null, sNormal, sHover, sPressed,
                       new Vector2(CloseSize, CloseSize), font, FontClose);
            Ignore(close.gameObject);
            var rt = close.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-18f, -18f);
            rt.sizeDelta = new Vector2(CloseSize, CloseSize);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(boxRT);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Skin applicata al DialogPanel di " + SceneManager.GetActiveScene().name
                  + " - altezza box: " + boxRT.rect.height.ToString("F0") + "px");
    }

    // ================================================================
    [MenuItem("Tools/Oakheart/DialogPanel/2. Salva come Prefab")]
    public static void SaveAsPrefab()
    {
        var dialog = Object.FindAnyObjectByType<DialogUI>(FindObjectsInactive.Include);
        if (dialog == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Nessun DialogUI in questa scena.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");

        PrefabUtility.SaveAsPrefabAssetAndConnect(
            dialog.gameObject, PrefabPath, InteractionMode.UserAction);

        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Prefab salvato in " + PrefabPath);
        EditorUtility.DisplayDialog("Oakheart", "Prefab salvato:\n" + PrefabPath, "OK");
    }

    // ================================================================
    [MenuItem("Tools/Oakheart/DialogPanel/3. Sostituisci con il Prefab (scena attiva)")]
    public static void ReplaceWithPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Prefab non trovato:\n" + PrefabPath + "\n\nLancia prima il passo 2.", "OK");
            return;
        }

        var existing = Object.FindAnyObjectByType<DialogUI>(FindObjectsInactive.Include);
        Transform parent = null;
        int siblingIndex = -1;
        bool wasActive = true;

        if (existing != null)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(existing.gameObject))
            {
                EditorUtility.DisplayDialog("Oakheart",
                    "Il DialogPanel di questa scena e' gia' un'istanza del Prefab. Niente da fare.", "OK");
                return;
            }
            parent = existing.transform.parent;
            siblingIndex = existing.transform.GetSiblingIndex();
            wasActive = existing.gameObject.activeSelf;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        if (parent == null)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Oakheart", "Nessun Canvas in questa scena.", "OK");
                return;
            }
            parent = canvas.transform;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "DialogPanel da Prefab");
        instance.name = "DialogPanel";
        if (siblingIndex >= 0) instance.transform.SetSiblingIndex(siblingIndex);
        instance.SetActive(wasActive);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] DialogPanel sostituito con il Prefab in " + SceneManager.GetActiveScene().name);
    }

    // ================================================================
    /// <summary>
    /// Aggiunge al Prefab la riga che mostra l'esito di CURA e POTENZIA.
    /// Va fatto sul Prefab e non sulla scena: le quattro scene ne sono istanze,
    /// modificarne una creerebbe un override che le altre tre non vedono.
    /// </summary>
    [MenuItem("Tools/Oakheart/DialogPanel/4. Aggiungi riga esito al Prefab")]
    public static void AddFeedbackRow()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Prefab non trovato:\n" + PrefabPath + "\n\nLancia prima il passo 2.", "OK");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Font non trovato:\n" + FontPath, "OK");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform box = FindDeep(contents.transform, "DialogBox");
            if (box == null)
            {
                EditorUtility.DisplayDialog("Oakheart", "DialogBox non trovato nel Prefab.", "OK");
                return;
            }

            Transform fb = FindDeep(contents.transform, "FeedbackText");
            TextMeshProUGUI tmp;
            if (fb == null)
            {
                var go = new GameObject("FeedbackText", typeof(RectTransform));
                go.transform.SetParent(box, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
                fb = go.transform;
            }
            else
            {
                if (fb.parent != box) fb.SetParent(box, false);
                tmp = fb.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = fb.gameObject.AddComponent<TextMeshProUGUI>();
            }

            var rt = fb.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BoxW - 140f, 56f);

            tmp.text = string.Empty;
            Style(tmp, font, 40f, new Color32(0x9C, 0x2D, 0x2B, 0xFF), TextAlignmentOptions.Center);

            var le = fb.GetComponent<LayoutElement>();
            if (le == null) le = fb.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = false;
            le.minHeight = 56f;

            fb.SetAsLastSibling();          // sotto la riga dei bottoni
            fb.gameObject.SetActive(false); // spento non occupa spazio nel layout

            var ui = contents.GetComponent<DialogUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                var p = so.FindProperty("feedbackText");
                if (p != null) { p.objectReferenceValue = tmp; so.ApplyModifiedProperties(); }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            Debug.Log("[Oakheart] Riga esito aggiunta al Prefab e collegata a DialogUI.");
            EditorUtility.DisplayDialog("Oakheart",
                "Riga esito aggiunta al Prefab.\nTutte e 4 le scene la ricevono.", "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // ================================================================ helper
    private static Sprite Load(string relative)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + relative + ".png");
    }

    private static T Get<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    private static void Ignore(GameObject go)
    {
        Get<LayoutElement>(go).ignoreLayout = true;
    }

    private static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetPanel(GameObject go, Sprite sprite)
    {
        var img = Get<Image>(go);
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PpuMult;
        img.color = Color.white;
        img.raycastTarget = true;
    }

    private static void Style(TextMeshProUGUI tmp, TMP_FontAsset font,
                              float size, Color color, TextAlignmentOptions align)
    {
        if (tmp == null) return;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.color = color;
        tmp.alignment = align;
        tmp.richText = true;
    }

    private static void SkinButton(Transform box, Transform newParent,
                                   string goName, string labelName,
                                   Sprite normal, Sprite hover, Sprite pressed,
                                   Vector2 size, TMP_FontAsset font, float fontSize)
    {
        Transform t = FindDeep(box, goName);
        if (t == null) { Debug.LogWarning("[Oakheart] Bottone non trovato: " + goName); return; }

        if (t.parent != newParent) t.SetParent(newParent, false);

        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;   // la posizione la decide il layout
        rt.sizeDelta = size;

        var img = Get<Image>(t.gameObject);
        img.sprite = normal;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PpuMult;
        img.color = Color.white;

        var btn = t.GetComponent<Button>();
        if (btn != null)
        {
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                pressedSprite     = pressed,
                selectedSprite    = normal,
                disabledSprite    = normal
            };
        }

        TextMeshProUGUI tmp = null;
        if (!string.IsNullOrEmpty(labelName))
        {
            var lt = FindDeep(t, labelName);
            if (lt != null) tmp = lt.GetComponent<TextMeshProUGUI>();
        }
        if (tmp == null) tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            Stretch(tmp.GetComponent<RectTransform>());
            Style(tmp, font, fontSize, DarkWood, TextAlignmentOptions.Center);
            tmp.margin = new Vector4(8f, 0f, 8f, 6f);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var r = FindDeep(root.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }
}
