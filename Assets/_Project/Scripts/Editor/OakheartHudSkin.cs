using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applica la skin pixel-art Oakheart all'HUD di gioco della scena aperta:
/// barra HP, punteggio, pausa, ondata, joystick e bottone azione.
///
/// Gli oggetti vengono cercati per nome tra i figli diretti del Canvas, cosi
/// non si fa confusione con gli omonimi dentro GameOverPanel e LevelCompletePanel.
///
/// Menu: Tools > Oakheart > HUD > Applica skin (scena attiva)
/// </summary>
public static class OakheartHudSkin
{
    private const string ArtRoot  = "Assets/_Project/Art/UI/Oakheart/";
    private const string FontPath = "Assets/_Project/Fonts/VT323 SDF.asset";

    // ---------- palette ----------
    private static readonly Color Cream    = new Color32(0xF6, 0xE7, 0xC0, 0xFF);
    private static readonly Color DarkWood = new Color32(0x2B, 0x1A, 0x10, 0xFF);
    private static readonly Color Overlay  = new Color32(0x00, 0x00, 0x00, 0x99);
    private static readonly Color JoyBase  = new Color32(0x2B, 0x1A, 0x10, 0x8C);
    private static readonly Color JoyKnob  = new Color32(0xE7, 0xB2, 0x3A, 0xEB);
    private static readonly Color AtkColor = new Color32(0x8B, 0x54, 0x30, 0xEB);

    // ---------- misure, px su Canvas 1080x1920 ----------
    private const float HpX = 108f, HpY = -52f, HpW = 380f, HpH = 52f, HeartSize = 56f;
    private const float ScoreX = -190f, ScoreY = -44f, ScoreW = 330f, ScoreH = 80f;
    private const float PauseX = -40f, PauseY = -44f, PauseBtn = 110f;
    private const float WaveY = -190f, WaveW = 560f, WaveH = 92f;
    private const float JoyBaseSize = 300f, JoyKnobSize = 150f;
    private const float AtkSize = 220f, AtkIcon = 120f;
    private const float PauseBoxW = 760f, PauseBtnW = 560f, PauseBtnH = 120f, PausePlateW = 400f;
    private const float PpuMult = 0.5f;

    /// <summary>Attivata dal batch per non far comparire una finestra per scena.</summary>
    public static bool Silent = false;

    // il track ha 4px di cornice per lato (2 sotto) sull'art a 2x:
    // con il bordo raddoppiato diventano 8/8/8/4 in px di Canvas
    private static readonly Vector2 TrackInsetMin = new Vector2(8f, 4f);
    private static readonly Vector2 TrackInsetMax = new Vector2(-8f, -8f);

    [MenuItem("Tools/Oakheart/HUD/Applica skin (scena attiva)")]
    public static void ApplySkin()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Font non trovato:\n" + FontPath, "OK");
            return;
        }

        Sprite track    = Load("bars/bar_track_64x10");
        Sprite fill     = Load("bars/bar_fill_health");
        Sprite plate    = Load("panels/nameplate_44x14");
        Sprite ornate   = Load("panels/panel_ornate_48x48");
        Sprite pNormal  = Load("buttons/button_primary_normal_40x16");
        Sprite pHover   = Load("buttons/button_primary_hover_40x16");
        Sprite pPressed = Load("buttons/button_primary_pressed_40x16");
        Sprite sNormal  = Load("buttons/button_secondary_normal_40x16");
        Sprite sHover   = Load("buttons/button_secondary_hover_40x16");
        Sprite sPressed = Load("buttons/button_secondary_pressed_40x16");
        Sprite heart    = Load("icons/icon_heart");
        Sprite star     = Load("icons/icon_star");
        Sprite sword    = Load("icons/icon_sword");
        Sprite speech   = Load("icons/icon_speech");
        Sprite knob     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        if (fill == null)
        {
            EditorUtility.DisplayDialog("Oakheart",
                "Manca bar_fill_health.png in " + ArtRoot + "bars/.\n\n" +
                "Copialo nel progetto e rilancia Tools > Oakheart > Configura sprite UI.", "OK");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Nessun Canvas in questa scena.", "OK");
            return;
        }
        Transform root = UiRoot(canvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Skin HUD");

        int touched = 0;

        // ============ barra HP ============
        Transform hp = root.Find("HpBar");
        if (hp != null)
        {
            TopLeft(hp, HpX, HpY, HpW, HpH);

            var slider = hp.GetComponent<Slider>();
            if (slider != null)
            {
                slider.interactable = false;               // e' un indicatore, non un comando
                slider.transition = Selectable.Transition.None;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.direction = Slider.Direction.LeftToRight;
            }

            Transform bg = hp.Find("Background");
            if (bg != null) { Stretch(bg); SetSliced(bg.gameObject, track); NoRaycast(bg.gameObject); }

            Transform area = hp.Find("Fill Area");
            if (area != null)
            {
                var art = area.GetComponent<RectTransform>();
                art.anchorMin = Vector2.zero;
                art.anchorMax = Vector2.one;
                art.offsetMin = TrackInsetMin;
                art.offsetMax = TrackInsetMax;

                Transform f = area.Find("Fill");
                if (f != null)
                {
                    var frt = f.GetComponent<RectTransform>();
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = Vector2.one;
                    frt.offsetMin = Vector2.zero;
                    frt.offsetMax = Vector2.zero;
                    frt.sizeDelta = Vector2.zero;          // lo Slider guida solo gli anchor
                    SetSliced(f.gameObject, fill);
                    NoRaycast(f.gameObject);
                }
            }

            // cuore a sinistra della barra
            Transform ic = EnsureChild(hp, "HpIcon");
            var irt = ic.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot     = new Vector2(1f, 0.5f);
            irt.anchoredPosition = new Vector2(-14f, 0f);
            irt.sizeDelta = new Vector2(HeartSize, HeartSize);
            SetSimple(ic.gameObject, heart);
            NoRaycast(ic.gameObject);
            ic.SetAsFirstSibling();
            touched++;
        }

        // ============ punteggio, su targhetta con stella ============
        Transform scoreText = root.Find("ScoreText");
        if (scoreText != null)
        {
            int idx = scoreText.GetSiblingIndex();
            Transform sp = root.Find("ScorePlate");
            if (sp == null)
            {
                var go = new GameObject("ScorePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "ScorePlate");
                sp = go.transform;
                sp.SetParent(root, false);
                sp.SetSiblingIndex(idx);
            }
            TopRight(sp, ScoreX, ScoreY, ScoreW, ScoreH);
            SetSliced(sp.gameObject, plate);
            NoRaycast(sp.gameObject);

            scoreText.SetParent(sp, false);
            var trt = scoreText.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot     = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(78f, 0f);
            trt.offsetMax = new Vector2(-22f, 0f);
            Style(scoreText.GetComponent<TextMeshProUGUI>(), font, 50f, Cream, TextAlignmentOptions.MidlineRight);

            Transform si = EnsureChild(sp, "ScoreIcon");
            var srt = si.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.5f);
            srt.anchorMax = new Vector2(0f, 0.5f);
            srt.pivot     = new Vector2(0f, 0.5f);
            srt.anchoredPosition = new Vector2(16f, 2f);
            srt.sizeDelta = new Vector2(60f, 60f);
            SetSimple(si.gameObject, star);
            NoRaycast(si.gameObject);
            touched++;
        }

        // ============ bottone pausa ============
        Transform pause = root.Find("PauseButton");
        if (pause != null)
        {
            TopRight(pause, PauseX, PauseY, PauseBtn, PauseBtn);
            SkinButton(pause.gameObject, sNormal, sHover, sPressed);
            var t = pause.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null)
            {
                Stretch(t.transform);
                Style(t, font, 64f, DarkWood, TextAlignmentOptions.Center);
                t.margin = new Vector4(0f, 0f, 0f, 6f);
            }
            touched++;
        }

        // ============ annuncio ondata, su targhetta ============
        Transform wave = root.Find("WaveText");
        if (wave != null)
        {
            int idx = wave.GetSiblingIndex();
            Transform wp = root.Find("WavePlate");
            if (wp == null)
            {
                var go = new GameObject("WavePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "WavePlate");
                wp = go.transform;
                wp.SetParent(root, false);
                wp.SetSiblingIndex(idx);
            }
            TopCenter(wp, 0f, WaveY, WaveW, WaveH);
            SetSliced(wp.gameObject, plate);
            NoRaycast(wp.gameObject);

            var waveUI = wave.GetComponent<WaveUI>();
            wave.SetParent(wp, false);
            Stretch(wave);
            Style(wave.GetComponent<TextMeshProUGUI>(), font, 54f, Cream, TextAlignmentOptions.Center);

            // la dissolvenza deve nascondere anche la targhetta, non solo il testo:
            // il CanvasGroup si sposta sul contenitore e WaveUI viene ricollegato.
            var plateCg = wp.GetComponent<CanvasGroup>();
            if (plateCg == null) plateCg = Undo.AddComponent<CanvasGroup>(wp.gameObject);
            plateCg.alpha = 0f;

            var oldCg = wave.GetComponent<CanvasGroup>();
            if (oldCg != null) Undo.DestroyObjectImmediate(oldCg);

            if (waveUI != null)
            {
                var so = new SerializedObject(waveUI);
                var p = so.FindProperty("canvasGroup");
                if (p != null) { p.objectReferenceValue = plateCg; so.ApplyModifiedProperties(); }
            }
            touched++;
        }

        // ============ joystick ============
        Transform joy = root.Find("JoystickBg");
        if (joy != null)
        {
            var jrt = joy.GetComponent<RectTransform>();
            jrt.sizeDelta = new Vector2(JoyBaseSize, JoyBaseSize);
            SetSimple(joy.gameObject, knob);
            var ji = joy.GetComponent<Image>();
            if (ji != null) ji.color = JoyBase;

            Transform h = joy.Find("JoystickHandle");
            if (h != null)
            {
                var hrt = h.GetComponent<RectTransform>();
                hrt.sizeDelta = new Vector2(JoyKnobSize, JoyKnobSize);
                SetSimple(h.gameObject, knob);
                var hi = h.GetComponent<Image>();
                if (hi != null) { hi.color = JoyKnob; hi.raycastTarget = false; }
            }
            touched++;
        }

        // ============ bottone azione: spada / fumetto ============
        Transform atk = root.Find("AttackButton");
        if (atk != null)
        {
            var art = atk.GetComponent<RectTransform>();
            art.sizeDelta = new Vector2(AtkSize, AtkSize);
            SetSimple(atk.gameObject, knob);
            var ai = atk.GetComponent<Image>();
            if (ai != null) ai.color = AtkColor;

            Transform icon = EnsureChild(atk, "ActionIcon");
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot     = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(AtkIcon, AtkIcon);
            SetSimple(icon.gameObject, sword);
            NoRaycast(icon.gameObject);

            // ATTENZIONE: il componente ActionButton NON sta sul bottone, sta sul
            // figlio ActionLabel. Disattivare quel GameObject spegne lo script e il
            // bottone smette di rispondere al tocco: si disabilita solo il testo.
            Transform lbl = atk.Find("ActionLabel");
            if (lbl != null)
            {
                lbl.gameObject.SetActive(true);          // ripara eventuali run precedenti
                var lt = lbl.GetComponent<TextMeshProUGUI>();
                if (lt != null) lt.enabled = false;
            }

            var ab = atk.GetComponentInChildren<ActionButton>(true);
            if (ab != null)
            {
                var so = new SerializedObject(ab);
                Assign(so, "icon", icon.GetComponent<Image>());
                Assign(so, "attackIcon", sword);
                Assign(so, "talkIcon", speech);
                so.ApplyModifiedProperties();
            }
            touched++;
        }

        // ============ pannello di pausa ============
        Transform pp = root.Find("PausePannel");
        if (pp != null)
        {
            Stretch(pp);
            var ovImg = pp.GetComponent<Image>();
            if (ovImg != null) { ovImg.sprite = null; ovImg.color = Overlay; ovImg.raycastTarget = true; }

            Transform boxT = FindDeep(pp, "PauseBox");
            if (boxT == null)
            {
                var go = new GameObject("PauseBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "PauseBox");
                boxT = go.transform;
                boxT.SetParent(pp, false);
            }
            var brt = boxT.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(PauseBoxW, brt.sizeDelta.y);
            SetSliced(boxT.gameObject, ornate);

            var vlg = Comp<VerticalLayoutGroup>(boxT.gameObject);
            vlg.padding = new RectOffset(60, 60, 90, 50);
            vlg.spacing = 24f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var cf = Comp<ContentSizeFitter>(boxT.gameObject);
            cf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            // titolo su targhetta, come il nome dell'NPC nel dialogo
            Transform title = FindDeep(pp, "PauseTitle");
            Transform tp = FindDeep(pp, "PausePlate");
            if (tp == null)
            {
                var go = new GameObject("PausePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "PausePlate");
                tp = go.transform;
                tp.SetParent(boxT, false);
            }
            tp.SetParent(boxT, false);
            Comp<LayoutElement>(tp.gameObject).ignoreLayout = true;
            var tprt = tp.GetComponent<RectTransform>();
            tprt.anchorMin = new Vector2(0.5f, 1f);
            tprt.anchorMax = new Vector2(0.5f, 1f);
            tprt.pivot     = new Vector2(0.5f, 0.5f);
            tprt.anchoredPosition = Vector2.zero;
            tprt.sizeDelta = new Vector2(PausePlateW, WaveH);
            SetSliced(tp.gameObject, plate);
            NoRaycast(tp.gameObject);

            Transform resume = FindDeep(pp, "ResumeButton");
            Transform menu   = FindDeep(pp, "MenuButton");

            if (title != null)
            {
                title.SetParent(tp, false);
                Stretch(title);
                Style(title.GetComponent<TextMeshProUGUI>(), font, 56f, Cream, TextAlignmentOptions.Center);
            }

            if (resume != null)
            {
                resume.SetParent(boxT, false);
                Boxed(resume, PauseBtnW, PauseBtnH);
                SkinButton(resume.gameObject, pNormal, pHover, pPressed);
                LabelOf(resume, font, 56f);
                resume.SetSiblingIndex(0);
            }
            if (menu != null)
            {
                menu.SetParent(boxT, false);
                Boxed(menu, PauseBtnW, PauseBtnH);
                SkinButton(menu.gameObject, sNormal, sHover, sPressed);
                LabelOf(menu, font, 48f);
                menu.SetSiblingIndex(1);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(brt);
            touched++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] HUD aggiornato in " + SceneManager.GetActiveScene().name
                  + " - " + touched + " gruppi.");
        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                "HUD aggiornato in " + SceneManager.GetActiveScene().name + ".\n" +
                touched + " gruppi elaborati.", "OK");
    }

    // ================================================================
    /// <summary>
    /// In Edit mode i pannelli di pausa, livello completato e game over sono tutti
    /// attivi insieme: si sovrappongono e ognuno ha il suo velo nero, quindi l'HUD
    /// sotto sembra sparito. A runtime si spengono da soli nei rispettivi Start().
    /// Questo comando li accende/spegne per poter guardare l'HUD in pace.
    /// Non modifica la scena: e' solo un aiuto visivo, e OakheartOverlayGuard
    /// li riattiva comunque al salvataggio.
    /// </summary>
    [MenuItem("Tools/Oakheart/HUD/Mostra-nascondi pannelli sovrapposti")]
    public static void ToggleOverlays()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Nessun Canvas in questa scena.", "OK");
            return;
        }

        string[] targets = { "PausePannel", "LevelCompletePanel", "GameOverPanel", "DialogPanel" };
        var found = new System.Collections.Generic.List<GameObject>();
        Transform uiRoot = UiRoot(canvas);
        foreach (string n in targets)
        {
            Transform t = uiRoot.Find(n);
            if (t != null) found.Add(t.gameObject);
        }

        if (found.Count == 0)
        {
            EditorUtility.DisplayDialog("Oakheart", "Nessun pannello sovrapposto in questa scena.", "OK");
            return;
        }

        bool anyOn = found.Exists(g => g.activeSelf);
        foreach (var g in found)
        {
            Undo.RecordObject(g, "Toggle pannelli");
            g.SetActive(!anyOn);
        }

        // Di proposito NON si marca la scena come modificata: nascondere i pannelli
        // e' un aiuto visivo temporaneo, non una modifica da salvare. Se salvi lo
        // stesso, OakheartOverlayGuard li riaccende prima che il file venga scritto.
        Debug.Log("[Oakheart] Pannelli sovrapposti " + (anyOn ? "nascosti" : "mostrati")
                  + ": " + string.Join(", ", found.ConvertAll(g => g.name)));
    }

    // ================================================================ helper
    /// <summary>
    /// La UI puo' stare direttamente sotto il Canvas oppure dentro il contenitore
    /// "SafeArea" aggiunto per i telefoni con notch: qui si sceglie quello giusto.
    /// </summary>
    public static Transform UiRoot(Canvas c)
    {
        Transform sa = c.transform.Find("SafeArea");
        return sa != null ? sa : c.transform;
    }

    private static Sprite Load(string rel)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + rel + ".png");
    }

    private static void Assign(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
    }

    private static T Comp<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, name);
            t = go.transform;
            t.SetParent(parent, false);
        }
        return t;
    }

    private static void Stretch(Transform t)
    {
        var rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void TopLeft(Transform t, float x, float y, float w, float h)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void TopRight(Transform t, float x, float y, float w, float h)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void TopCenter(Transform t, float x, float y, float w, float h)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void Boxed(Transform t, float w, float h)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetSliced(GameObject go, Sprite s)
    {
        var img = Comp<Image>(go);
        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PpuMult;
        img.color = Color.white;
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

    private static void LabelOf(Transform btn, TMP_FontAsset font, float size)
    {
        var t = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t == null) return;
        Stretch(t.transform);
        Style(t, font, size, DarkWood, TextAlignmentOptions.Center);
        t.margin = new Vector4(8f, 0f, 8f, 6f);
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
