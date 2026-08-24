using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applica la skin Oakheart ai due pannelli di esito partita: LevelCompletePanel
/// e GameOverPanel. Stessa grammatica del dialogo e della pausa: cornice ornata,
/// targhetta a cavallo del bordo superiore, riga punteggio incassata, bottoni.
///
/// Menu: Tools > Oakheart > Pannelli > Applica skin esito (scena attiva)
/// </summary>
public static class OakheartPanelSkin
{
    private const string ArtRoot  = "Assets/_Project/Art/UI/Oakheart/";
    private const string FontPath = "Assets/_Project/Fonts/VT323 SDF.asset";

    private static readonly Color Cream    = new Color32(0xF6, 0xE7, 0xC0, 0xFF);
    private static readonly Color DarkWood = new Color32(0x2B, 0x1A, 0x10, 0xFF);
    private static readonly Color Overlay  = new Color32(0x00, 0x00, 0x00, 0xDB);
    /// Tinta applicata allo stesso sprite della targhetta per il GAME OVER:
    /// nessun asset in piu' da gestire, solo un colore di moltiplicazione.
    private static readonly Color PlateRed = new Color32(0xEB, 0x78, 0x6E, 0xFF);

    private const float BoxW = 880f, PlateW = 660f, PlateH = 92f;
    private const float ScoreW = 640f, ScoreH = 100f;
    private const float BtnW = 640f, BtnH = 120f;
    private const float PpuMult = 0.5f;

    public static bool Silent = false;

    [MenuItem("Tools/Oakheart/Pannelli/Applica skin esito (scena attiva)")]
    public static void ApplySkin()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart", "Font non trovato:\n" + FontPath, "OK");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart", "Nessun Canvas in questa scena.", "OK");
            return;
        }
        Transform root = OakheartHudSkin.UiRoot(canvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Skin pannelli esito");

        var sprites = new Sprites
        {
            Ornate  = Load("panels/panel_ornate_48x48"),
            Plate   = Load("panels/nameplate_44x14"),
            Sunken  = Load("panels/panel_sunken_24x24"),
            Star    = Load("icons/icon_star"),
            PNormal = Load("buttons/button_primary_normal_40x16"),
            PHover  = Load("buttons/button_primary_hover_40x16"),
            PPress  = Load("buttons/button_primary_pressed_40x16"),
            SNormal = Load("buttons/button_secondary_normal_40x16"),
            SHover  = Load("buttons/button_secondary_hover_40x16"),
            SPress  = Load("buttons/button_secondary_pressed_40x16"),
            Font    = font
        };

        int done = 0;

        Transform lc = root.Find("LevelCompletePanel");
        if (lc != null)
        {
            Build(lc, sprites, "CompleteBox", "CompletePlate", "CompleteTitle", "CompleteScore",
                  Color.white, 50f,
                  new[]
                  {
                      new Btn("NextLevelButton", true),
                      new Btn("EndlessButton", false),
                      new Btn("MenuButtonComplete", false)
                  });
            done++;
        }

        Transform go = root.Find("GameOverPanel");
        if (go != null)
        {
            Build(go, sprites, "GameOverBox", "GameOverPlate", "TitleText", "ScoreText",
                  PlateRed, 50f,
                  new[]
                  {
                      new Btn("RetryButton", true),
                      new Btn("MenuButton", false)
                  });
            done++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Pannelli esito aggiornati in "
                  + SceneManager.GetActiveScene().name + ": " + done);
        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                done + " pannelli di esito aggiornati in "
                + SceneManager.GetActiveScene().name + ".", "OK");
    }

    private struct Btn
    {
        public string Name;
        public bool Primary;
        public Btn(string n, bool p) { Name = n; Primary = p; }
    }

    private class Sprites
    {
        public Sprite Ornate, Plate, Sunken, Star;
        public Sprite PNormal, PHover, PPress, SNormal, SHover, SPress;
        public TMP_FontAsset Font;
    }

    private static void Build(Transform panel, Sprites s,
                              string boxName, string plateName,
                              string titleName, string scoreName,
                              Color plateTint, float titleSize,
                              Btn[] buttons)
    {
        // velo scuro a tutto schermo
        Stretch(panel);
        var overlayImg = panel.GetComponent<Image>();
        if (overlayImg != null) { overlayImg.sprite = null; overlayImg.color = Overlay; }

        // cornice, altezza guidata dal contenuto
        Transform box = Ensure(panel, boxName);
        var brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot     = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(BoxW, brt.sizeDelta.y);
        SetSliced(box.gameObject, s.Ornate, Color.white);

        var vlg = Comp<VerticalLayoutGroup>(box.gameObject);
        vlg.padding = new RectOffset(60, 60, 90, 50);
        vlg.spacing = 24f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        var fit = Comp<ContentSizeFitter>(box.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // targhetta del titolo, fuori dal flusso del layout
        Transform plate = Ensure(box, plateName);
        Comp<LayoutElement>(plate.gameObject).ignoreLayout = true;
        var prt = plate.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(PlateW, PlateH);
        SetSliced(plate.gameObject, s.Plate, plateTint);
        NoRaycast(plate.gameObject);

        Transform title = FindDeep(panel, titleName);
        if (title != null)
        {
            title.SetParent(plate, false);
            Stretch(title);
            Style(title.GetComponent<TextMeshProUGUI>(), s.Font, titleSize, Cream,
                  TextAlignmentOptions.Center);
        }

        // riga punteggio incassata, primo elemento del layout
        Transform scoreBox = Ensure(box, "ScoreBox");
        var srt = scoreBox.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.pivot     = new Vector2(0.5f, 0.5f);
        srt.sizeDelta = new Vector2(ScoreW, ScoreH);
        SetSliced(scoreBox.gameObject, s.Sunken, Color.white);
        NoRaycast(scoreBox.gameObject);
        Comp<LayoutElement>(scoreBox.gameObject).ignoreLayout = false;
        scoreBox.SetSiblingIndex(0);

        Transform score = FindDeep(panel, scoreName);
        if (score != null)
        {
            score.SetParent(scoreBox, false);
            var trt = score.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot     = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(90f, 0f);
            trt.offsetMax = new Vector2(-28f, 0f);
            Style(score.GetComponent<TextMeshProUGUI>(), s.Font, 56f, DarkWood,
                  TextAlignmentOptions.MidlineRight);
        }

        Transform icon = Ensure(scoreBox, "ScoreIcon");
        var irt = icon.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0.5f);
        irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot     = new Vector2(0f, 0.5f);
        irt.anchoredPosition = new Vector2(24f, 0f);
        irt.sizeDelta = new Vector2(64f, 64f);
        SetSimple(icon.gameObject, s.Star);
        NoRaycast(icon.gameObject);

        // bottoni, nell'ordine dichiarato
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform b = FindDeep(panel, buttons[i].Name);
            if (b == null) { Debug.LogWarning("[Oakheart] Bottone non trovato: " + buttons[i].Name); continue; }

            b.SetParent(box, false);
            var rt = b.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BtnW, BtnH);
            Comp<LayoutElement>(b.gameObject).ignoreLayout = false;

            bool p = buttons[i].Primary;
            SkinButton(b.gameObject,
                       p ? s.PNormal : s.SNormal,
                       p ? s.PHover  : s.SHover,
                       p ? s.PPress  : s.SPress);

            var lbl = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lbl != null)
            {
                Stretch(lbl.transform);
                Style(lbl, s.Font, 48f, DarkWood, TextAlignmentOptions.Center);
                lbl.margin = new Vector4(8f, 0f, 8f, 6f);
            }

            b.SetSiblingIndex(i + 1);   // 0 e' la riga punteggio
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(brt);
    }

    // ================================================================ helper
    private static Sprite Load(string rel)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + rel + ".png");
    }

    private static T Comp<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    private static Transform Ensure(Transform parent, string name)
    {
        Transform t = FindDeep(parent, name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, name);
            t = go.transform;
        }
        if (t.parent != parent) t.SetParent(parent, false);
        return t;
    }

    private static void Stretch(Transform t)
    {
        var rt = t.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetSliced(GameObject go, Sprite s, Color c)
    {
        var img = Comp<Image>(go);
        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PpuMult;
        img.color = c;
    }

    private static void SetSimple(GameObject go, Sprite s)
    {
        var img = Comp<Image>(go);
        img.sprite = s;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;
    }

    private static void NoRaycast(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    private static void SkinButton(GameObject go, Sprite normal, Sprite hover, Sprite pressed)
    {
        var img = Comp<Image>(go);
        img.sprite = normal;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PpuMult;
        img.color = Color.white;

        var btn = go.GetComponent<Button>();
        if (btn == null) return;
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

    private static void Style(TextMeshProUGUI tmp, TMP_FontAsset font,
                              float size, Color color, TextAlignmentOptions align)
    {
        if (tmp == null) return;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.color = color;
        tmp.alignment = align;
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
