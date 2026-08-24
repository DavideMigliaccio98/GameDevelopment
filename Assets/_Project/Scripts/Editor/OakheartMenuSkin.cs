using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Skin Oakheart per le schermate di contorno: menu principale, selezione
/// livelli, classifica e login/registrazione.
///
/// Menu: Tools > Oakheart > Menu > Applica skin (scena attiva)
/// </summary>
public static class OakheartMenuSkin
{
    private const string ArtRoot    = "Assets/_Project/Art/UI/Oakheart/";
    private const string FontPath   = "Assets/_Project/Fonts/VT323 SDF.asset";
    private const string DuotonePath = "Assets/_Project/Sprites/Backgrounds/F_portale_duotone.png";
    private const string RowItemPath = "Assets/_Project/Prefabs/RowItem.prefab";
    private const string PirataPath  = "Assets/_Project/Fonts/PirataOne-Regular SDF.asset";
    private const string TitleMatPath = "Assets/_Project/Fonts/PirataOne Titolo Oakheart.mat";

    private static readonly Color Cream    = new Color32(0xF6, 0xE7, 0xC0, 0xFF);
    private static readonly Color DarkWood = new Color32(0x2B, 0x1A, 0x10, 0xFF);
    private static readonly Color Wood     = new Color32(0x8A, 0x54, 0x30, 0xFF);
    private static readonly Color Gold     = new Color32(0xE7, 0xB2, 0x3A, 0xFF);

    // Il pannello crema stacca solo se il fondale scende a un'ombra: con un velo
    // leggero finivano tutti sullo stesso valore tonale e non si distingueva nulla.
    private const float PanelOverlay = 0.80f;   // schermate con un riquadro sopra
    private const float MenuOverlay  = 0.55f;   // menu principale, i bottoni stanno sul fondale

    // Le posizioni top 3 su fondo pergamena vanno scurite: l'oro chiaro della
    // versione precedente era pensato per un pannello scuro e qui sparirebbe.
    private static readonly Color GoldDark   = new Color32(0x8A, 0x6A, 0x12, 0xFF);
    private static readonly Color SilverDark = new Color32(0x6E, 0x6E, 0x78, 0xFF);
    private static readonly Color BronzeDark = new Color32(0x7A, 0x4A, 0x28, 0xFF);
    private static readonly Color RowHighlight = new Color32(0xE7, 0xB2, 0x3A, 0x59);

    private const float PpuMult = 0.5f;

    // Corpi del testo, tutti qui. VT323 e' un font pixel sottile: sotto i 46
    // su schermo telefono si fatica, per questo sono piu' generosi del solito.
    private const float FontInput    = 52f;
    private const float FontButton   = 50f;
    private const float FontStatus   = 40f;
    private const float FontHeader   = 38f;
    private const float FontRow      = 40f;
    private const float FontPlayerId = 40f;
    private const float FontLevelBtn = 48f;
    private const float FontPlateBig = 56f;
    // Il segnaposto resta piu' chiaro del testo vero, ma al 50% era quasi assente.
    private const float PlaceholderAlpha = 0.62f;
    // Il recupero password non e' un'azione dello stesso peso di LOGIN: si presenta
    // come collegamento, non come bottone, per non competere con le azioni principali.
    private const float FontLink = 38f;
    private static readonly Color LinkColor = new Color32(0x6B, 0x45, 0x28, 0xFF);

    public static bool Silent = false;

    private class Kit
    {
        public Sprite Ornate, Plate, Sunken, Duotone;
        public Sprite PNormal, PHover, PPress, SNormal, SHover, SPress;
        public TMP_FontAsset Font;
    }

    [MenuItem("Tools/Oakheart/Menu/Applica skin (scena attiva)")]
    public static void ApplyActive()
    {
        var kit = LoadKit();
        if (kit == null) return;

        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart", "Nessun Canvas in questa scena.", "OK");
            return;
        }
        Transform root = OakheartHudSkin.UiRoot(canvas);
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Skin menu");

        string scene = SceneManager.GetActiveScene().name;
        int done = 0;

        if (root.Find("PlayButton") != null)           { SkinMainMenu(root, kit); done++; }
        if (root.Find("LevelSelectionPanel") != null)  { SkinLevelSelection(root, kit); done++; }
        if (root.Find("LeaderboardPanel") != null)     { SkinLeaderboard(root, kit); done++; }
        if (root.Find("LoginPanel") != null)           { SkinLogin(root, kit); done++; }

        // Scene senza pannelli riconosciuti (Boot): diventano il fermo immagine
        // di quello che vedra' subito dopo.
        if (done == 0 && root.Find("BackgroundImage") != null)
        {
            SkinBootSplash(root, kit);
            done++;
        }

        SkinRowItemPrefab(kit);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Oakheart] Menu aggiornati in " + scene + ": " + done + " blocchi.");
        if (!Silent)
            EditorUtility.DisplayDialog("Oakheart",
                done + " blocchi di menu aggiornati in " + scene + ".", "OK");
    }

    // ================================================================
    private const string PreviewPrefix = "__anteprima_";

    /// <summary>
    /// Riempie l'elenco con righe finte per poter controllare l'allineamento
    /// delle colonne senza dover prima segnare dei punteggi veri su PlayFab.
    /// Le righe non vengono salvate come modifica della scena, e in ogni caso
    /// LeaderboardUI.ClearRows() le elimina appena si apre la classifica in gioco.
    /// </summary>
    [MenuItem("Tools/Oakheart/Menu/Anteprima classifica (righe finte)")]
    public static void PreviewLeaderboard()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return;
        Transform root = OakheartHudSkin.UiRoot(canvas);

        Transform panel = root.Find("LeaderboardPanel");
        Transform list = panel != null ? FindDeep(panel, "LBList") : null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowItemPath);
        if (list == null || prefab == null)
        {
            EditorUtility.DisplayDialog("Oakheart", "Classifica o prefab riga non trovati.", "OK");
            return;
        }

        ClearPreview(list);

        string[,] fake =
        {
            { "1", "Davide", "4820" },
            { "2", "Milicchio", "3990" },
            { "3", "Aldo", "3120" },
            { "4", "Locandiera", "2740" },
            { "5", "Mercante (TU)", "1980" }
        };

        var ui = panel.GetComponent<LeaderboardUI>();
        var so = ui != null ? new SerializedObject(ui) : null;
        Color[] rank = { GoldDark, SilverDark, BronzeDark, DarkWood, DarkWood };

        for (int i = 0; i < fake.GetLength(0); i++)
        {
            var row = (GameObject)PrefabUtility.InstantiatePrefab(prefab, list);
            row.name = PreviewPrefix + (i + 1);
            row.SetActive(true);
            SetRowText(row, "RowPos", fake[i, 0], rank[i]);
            SetRowText(row, "RowName", fake[i, 1], DarkWood);
            SetRowText(row, "RowScore", fake[i, 2], rank[i]);
            if (i == 4)
            {
                var bg = row.GetComponent<Image>();
                if (bg != null) bg.color = RowHighlight;
            }
        }

        Transform status = FindDeep(panel, "StatusText");
        if (status != null) status.gameObject.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(list.GetComponent<RectTransform>());
        Debug.Log("[Oakheart] Anteprima classifica: 5 righe finte. "
                  + "Usa Tools > Oakheart > Menu > Pulisci anteprima classifica per toglierle.");
    }

    [MenuItem("Tools/Oakheart/Menu/Pulisci anteprima classifica")]
    public static void ClearPreviewMenu()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return;
        Transform panel = OakheartHudSkin.UiRoot(canvas).Find("LeaderboardPanel");
        Transform list = panel != null ? FindDeep(panel, "LBList") : null;
        if (list != null) ClearPreview(list);
        Transform status = panel != null ? FindDeep(panel, "StatusText") : null;
        if (status != null) status.gameObject.SetActive(true);
    }

    private static void ClearPreview(Transform list)
    {
        for (int i = list.childCount - 1; i >= 0; i--)
        {
            Transform c = list.GetChild(i);
            if (c.name.StartsWith(PreviewPrefix)) Object.DestroyImmediate(c.gameObject);
        }
    }

    private static void SetRowText(GameObject row, string child, string text, Color color)
    {
        Transform t = row.transform.Find(child);
        if (t == null) return;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp == null) return;
        tmp.text = text;
        tmp.color = color;
    }

    // ================================================================
    /// <summary>
    /// Boot dura un frame, ma quel frame resta congelato sullo schermo per tutto
    /// il caricamento sincrono di Login. Invece di accorciarlo, lo si rende
    /// identico al fondo di Login: cosi il passaggio non e' uno stacco, e' il
    /// riquadro del login che compare sopra una scena che non cambia.
    /// </summary>
    private static void SkinBootSplash(Transform root, Kit k)
    {
        Transform bg = root.Find("BackgroundImage");
        if (bg == null) return;
        SetBackground(bg, k);

        Transform ov = root.Find("DarkOverlay");
        if (ov == null) ov = NewImage("DarkOverlay", root);
        ov.SetSiblingIndex(bg.GetSiblingIndex() + 1);
        FixOverlay(ov, PanelOverlay);   // stesso velo di Login

        Transform title = root.Find("Title");
        if (title == null)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Title");
            go.transform.SetParent(root, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = "The Last Knight";
            var pirata = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PirataPath);
            if (pirata != null) t.font = pirata;
            t.fontSize = 120f;
            t.alignment = TextAlignmentOptions.Center;
            title = go.transform;
        }

        // stessa posizione del titolo in Login e MainMenu
        var rt = title.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -300f);
        rt.sizeDelta = new Vector2(800f, 250f);
        SkinTitle(title);
        title.SetAsLastSibling();

        Debug.Log("[Oakheart] Boot allineata al fondo di Login: fondale, velo e titolo.");
    }

    // ================================================================ menu principale
    private static void SkinMainMenu(Transform root, Kit k)
    {
        Transform bg = root.Find("BackgroundImage");
        SetBackground(bg, k);

        // Il menu non aveva nessun velo: i bottoni stavano direttamente sul
        // fondale chiaro e non staccavano.
        Transform ov = root.Find("MenuOverlay");
        if (ov == null) ov = NewImage("MenuOverlay", root);
        ov.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
        FixOverlay(ov, MenuOverlay);

        // Il titolo resta in Pirata One: e' il marchio del gioco, non un elemento
        // di interfaccia. Tutto il resto passa a VT323.
        SkinTitle(root.Find("Title"));

        SkinMenuButton(root.Find("PlayButton"), k, true, FontPlateBig, 560f, 130f);
        SkinMenuButton(root.Find("LeaderboardButton"), k, false, FontPlateBig, 560f, 130f);
        SkinMenuButton(root.Find("QuitButton"), k, false, FontPlateBig, 560f, 130f);

        // targhetta col nome giocatore, in basso
        Transform pid = root.Find("PlayerIdText");
        if (pid != null)
        {
            Transform plate = root.Find("PlayerPlate");
            if (plate == null)
            {
                plate = NewImage("PlayerPlate", root);
                plate.SetSiblingIndex(pid.GetSiblingIndex());
            }
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0f);
            prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot     = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 100f);
            prt.sizeDelta = new Vector2(620f, 76f);
            SetSliced(plate.gameObject, k.Plate, Color.white);
            NoRaycast(plate.gameObject);

            pid.SetParent(plate, false);
            Stretch(pid);
            Style(pid.GetComponent<TextMeshProUGUI>(), k.Font, FontPlayerId, Cream, TextAlignmentOptions.Center);
        }
    }

    // ================================================================ selezione livelli
    private static void SkinLevelSelection(Transform root, Kit k)
    {
        Transform panel = root.Find("LevelSelectionPanel");
        if (panel == null) return;
        Stretch(panel);

        SetBackground(FindDeep(panel, "BackgroundImage_LevelSelect"), k);
        FixOverlay(FindDeep(panel, "DarkOverlay"), PanelOverlay);

        Transform box = EnsureBox(panel, "LSBox", k, 880f, new RectOffset(50, 50, 90, 50), 20f);

        Transform title = FindDeep(panel, "LSTitle");
        AttachPlate(box, "LSPlate", title, k, 620f, FontPlateBig);

        for (int i = 1; i <= 5; i++)
        {
            Transform b = FindDeep(panel, "LevelButton" + i);
            if (b == null) continue;
            MoveIntoBox(b, box, 660f, 110f, i - 1);
            SkinButton(b.gameObject, k.SNormal, k.SHover, k.SPress,
                       disabled: k.SPress);   // un livello bloccato si legge come incassato
            LabelOf(b, k.Font, FontLevelBtn);
        }

        Transform status = FindDeep(panel, "LSStatusText");
        if (status != null)
        {
            MoveIntoBox(status, box, 660f, 50f, 5);
            Style(status.GetComponent<TextMeshProUGUI>(), k.Font, FontStatus, DarkWood, TextAlignmentOptions.Center);
        }

        Transform close = FindDeep(panel, "LSCloseButton");
        if (close != null)
        {
            MoveIntoBox(close, box, 420f, 100f, 6);
            SkinButton(close.gameObject, k.SNormal, k.SHover, k.SPress);
            LabelOf(close, k.Font, FontLevelBtn);
        }

        RebuildBox(box);
    }

    // ================================================================ classifica
    // Geometria a colonne condivisa da intestazione e righe: se cambia un numero
    // qui, testata e dati restano allineati per costruzione.
    private const float FrameW = 900f, FrameH = 1300f, LbSide = 60f;
    private const float ColPos = 96f, ColScore = 220f, ColGap = 60f;
    private const float PillW = 64f, PillH = 44f;
    private const float RowH = 60f;

    private static void SkinLeaderboard(Transform root, Kit k)
    {
        Transform panel = root.Find("LeaderboardPanel");
        if (panel == null) return;
        Stretch(panel);

        var pimg = panel.GetComponent<Image>();
        if (pimg != null)
        {
            pimg.sprite = null;
            pimg.color = new Color(0f, 0f, 0f, PanelOverlay);
            pimg.raycastTarget = true;   // deve fermare i click sui bottoni del menu sotto
        }
        FixOverlay(FindDeep(panel, "DarkOverlay"), 0f);

        Transform frame = FindDeep(panel, "Frame");
        if (frame == null) return;

        var frt = frame.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0.5f, 0.5f);
        frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot     = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = new Vector2(FrameW, FrameH);
        SetSliced(frame.gameObject, k.Ornate, Color.white);

        AttachPlate(frame, "LBPlate", FindDeep(frame, "LBTitle"), k, 560f, FontPlateBig);

        // --- intestazione, sotto la targhetta ---
        Transform header = FindDeep(frame, "Header");
        if (header == null) header = NewRect("Header", frame);
        if (header.parent != frame) header.SetParent(frame, false);
        TopStretch(header, -104f, 56f);
        LayoutColumns(header, "HeaderPos", "HeaderName", "HeaderScore", k.Font, FontHeader, DarkWood);

        // --- riga separatrice ---
        Transform sep = FindDeep(frame, "Separator");
        if (sep != null)
        {
            TopStretch(sep, -150f, 4f);
            // Puo' essere una Image o una RawImage: Graphic copre entrambe.
            var g = sep.GetComponent<Graphic>();
            if (g != null) g.color = new Color(DarkWood.r, DarkWood.g, DarkWood.b, 0.35f);
        }

        // --- elenco ---
        Transform list = FindDeep(frame, "LBList");
        if (list != null)
        {
            TopStretch(list, -172f, 0f);
            var vlg = Comp<VerticalLayoutGroup>(list.gameObject);
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;      // le righe prendono la larghezza dell'elenco
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;    // l'altezza la decide il prefab
            vlg.childForceExpandHeight = false;
            var fit = Comp<ContentSizeFitter>(list.gameObject);
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        }

        // --- messaggio di stato, al centro dell'area elenco ---
        Transform status = FindDeep(frame, "StatusText");
        if (status != null)
        {
            var rt = status.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720f, 90f);
            Style(status.GetComponent<TMP_Text>(), k.Font, FontStatus, DarkWood, TextAlignmentOptions.Center);
        }

        // --- chiudi, dentro la cornice ---
        Transform close = FindDeep(frame, "CloseButton");
        if (close != null)
        {
            var rt = close.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 44f);
            rt.sizeDelta = new Vector2(420f, 110f);
            SkinButton(close.gameObject, k.SNormal, k.SHover, k.SPress);
            LabelOf(close, k.Font, FontButton);
        }

        // I colori del podio erano tarati su un pannello scuro: su pergamena
        // vanno scuriti, altrimenti il primo classificato e' il meno leggibile.
        var ui = panel.GetComponent<LeaderboardUI>();
        if (ui != null)
        {
            var so = new SerializedObject(ui);
            SetColor(so, "goldColor", GoldDark);
            SetColor(so, "silverColor", SilverDark);
            SetColor(so, "bronzeColor", BronzeDark);
            SetColor(so, "normalColor", DarkWood);
            SetColor(so, "highlightRowColor", RowHighlight);
            so.ApplyModifiedProperties();
        }
    }

    private static void TopStretch(Transform t, float y, float height)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(-2f * LbSide, height);
    }

    /// <summary>
    /// Dispone le tre colonne dentro il contenitore che le ospita, usando gli
    /// anchor invece di posizioni assolute: intestazione e righe si allineano da
    /// sole anche se cambia la larghezza del pannello.
    /// </summary>
    private static void LayoutColumns(Transform parent, string posName, string nameName,
                                      string scoreName, TMP_FontAsset font, float size, Color color,
                                      Sprite pill = null)
    {
        // La pastiglia va creata prima del testo: in UGUI i fratelli precedenti
        // vengono disegnati sotto, quindi il numero resta leggibile sopra.
        if (pill != null)
        {
            Transform bg = FindDeep(parent, posName + "Bg");
            if (bg == null) bg = NewImage(posName + "Bg", parent);
            if (bg.parent != parent) bg.SetParent(parent, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0.5f);
            brt.anchorMax = new Vector2(0f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(ColPos * 0.5f, 0f);
            brt.sizeDelta = new Vector2(PillW, PillH);
            SetSliced(bg.gameObject, pill, Color.white);
            NoRaycast(bg.gameObject);
            bg.SetAsFirstSibling();
        }

        Transform p = FindDeep(parent, posName);
        if (p != null)
        {
            var rt = p.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ColPos, 0f);
            Style(p.GetComponent<TMP_Text>(), font, size, color, TextAlignmentOptions.Center);
            if (pill != null) p.SetAsLastSibling();
        }

        Transform n = FindDeep(parent, nameName);
        if (n != null)
        {
            var rt = n.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(ColPos + ColGap, 0f);
            rt.offsetMax = new Vector2(-(ColScore + ColGap), 0f);
            Style(n.GetComponent<TMP_Text>(), font, size, color, TextAlignmentOptions.MidlineLeft);
        }

        Transform sc = FindDeep(parent, scoreName);
        if (sc != null)
        {
            var rt = sc.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ColScore, 0f);
            Style(sc.GetComponent<TMP_Text>(), font, size, color, TextAlignmentOptions.MidlineRight);
        }
    }

    // ================================================================ login
    private static void SkinLogin(Transform root, Kit k)
    {
        SetBackground(root.Find("BackgroundImage"), k);
        FixOverlay(root.Find("DarkOverlay"), PanelOverlay);
        SkinTitle(root.Find("Title"));

        Transform status = root.Find("StatusText");
        if (status != null)
            Style(status.GetComponent<TextMeshProUGUI>(), k.Font, FontStatus, Cream, TextAlignmentOptions.Center);

        // --- pannello di accesso ---
        Transform login = root.Find("LoginPanel");
        if (login != null)
        {
            Stretch(login);   // era ridotto a un rettangolo di 162x-16: degenere
            Transform box = EnsureBox(login, "LoginBox", k, 940f, new RectOffset(50, 50, 60, 50), 22f);

            SkinInput(FindDeep(login, "EmailInput"), box, k, 0);
            SkinInput(FindDeep(login, "PasswordInput"), box, k, 1);

            SkinBoxButton(FindDeep(login, "LoginButton"), box, k, true, 2);
            SkinBoxButton(FindDeep(login, "RegisterButton"), box, k, false, 3);
            // Accesso come ospite rimosso: senza account non c'e' classifica ne'
            // progressione salvata, quindi era una scorciatoia verso meta' gioco.
            Transform guest = FindDeep(login, "GuestButton");
            if (guest != null)
            {
                Undo.DestroyObjectImmediate(guest.gameObject);
                Debug.Log("[Oakheart] GuestButton rimosso dal pannello di accesso.");
            }

            // Recupero password. PlayFab la manda con la propria infrastruttura,
            // quindi funziona anche senza il server SMTP del titolo.
            Transform forgot = FindDeep(login, "ForgotButton");
            if (forgot == null)
                forgot = NewButton("ForgotButton", box, "Password dimenticata?");
            SkinLinkButton(forgot, box, k, 4);

            var lg = Object.FindAnyObjectByType<LoginUI>(FindObjectsInactive.Include);
            var fbtn = forgot.GetComponent<Button>();
            if (lg != null && fbtn != null)
            {
                if (fbtn.onClick.GetPersistentEventCount() == 0)
                {
                    UnityEventTools.AddPersistentListener(fbtn.onClick,
                        new UnityAction(lg.OnForgotPasswordPressed));
                    Debug.Log("[Oakheart] ForgotButton collegato a LoginUI.OnForgotPasswordPressed().");
                }
                var so = new SerializedObject(lg);
                var pf = so.FindProperty("forgotButton");
                if (pf != null) { pf.objectReferenceValue = fbtn; so.ApplyModifiedProperties(); }
            }

            RebuildBox(box);
        }

        // --- pannello di registrazione ---
        Transform reg = root.Find("RegisterPanel");
        if (reg != null)
        {
            Stretch(reg);
            Transform box = EnsureBox(reg, "RegisterBox", k, 940f, new RectOffset(50, 50, 60, 50), 22f);

            SkinInput(FindDeep(reg, "RegEmail"), box, k, 0);
            SkinInput(FindDeep(reg, "RegPassword"), box, k, 1);
            SkinInput(FindDeep(reg, "RegUsername"), box, k, 2);

            SkinBoxButton(FindDeep(reg, "ConfirmRegisterButton"), box, k, true, 3);

            // Mancava del tutto un modo per tornare indietro: chi apriva la
            // registrazione per sbaglio doveva chiudere l'app. LoginUI ha gia'
            // il metodo pubblico ShowLogin(), bastava un bottone che lo chiami.
            Transform back = FindDeep(reg, "BackToLoginButton");
            if (back == null)
            {
                back = NewButton("BackToLoginButton", box, "TORNA AL LOGIN");
            }
            SkinBoxButton(back, box, k, false, 4);

            var loginUI = Object.FindAnyObjectByType<LoginUI>(FindObjectsInactive.Include);
            var btn = back.GetComponent<Button>();
            if (loginUI != null && btn != null && btn.onClick.GetPersistentEventCount() == 0)
            {
                UnityEventTools.AddPersistentListener(btn.onClick,
                    new UnityAction(loginUI.ShowLogin));
                Debug.Log("[Oakheart] BackToLoginButton collegato a LoginUI.ShowLogin().");
            }

            RebuildBox(box);
        }
    }

    // ================================================================ prefab riga classifica
    /// <summary>
    /// Il prefab della riga aveva i tre testi ad anchoredPosition y=733 dentro una
    /// riga alta 50: con la classifica vuota non si vedeva, con dati veri sarebbero
    /// finiti fuori dal pannello. Qui vengono rifatti con le stesse colonne
    /// dell'intestazione.
    /// </summary>
    private static void SkinRowItemPrefab(Kit k)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowItemPath);
        if (prefab == null) return;

        GameObject contents = PrefabUtility.LoadPrefabContents(RowItemPath);

        var rrt = contents.GetComponent<RectTransform>();
        if (rrt != null)
        {
            rrt.anchorMin = new Vector2(0f, 0.5f);
            rrt.anchorMax = new Vector2(1f, 0.5f);
            rrt.pivot     = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.sizeDelta = new Vector2(0f, RowH);
        }

        // Sfondo trasparente pronto: LeaderboardUI ci scrive sopra il colore di
        // evidenziazione per la riga del giocatore, senza doverlo creare a runtime.
        var bg = contents.GetComponent<Image>();
        if (bg == null) bg = contents.AddComponent<Image>();
        bg.sprite = null;
        bg.color = new Color(0f, 0f, 0f, 0f);
        bg.raycastTarget = false;

        LayoutColumns(contents.transform, "RowPos", "RowName", "RowScore", k.Font, FontRow, DarkWood, k.Sunken);

        PrefabUtility.SaveAsPrefabAsset(contents, RowItemPath);
        PrefabUtility.UnloadPrefabContents(contents);
        Debug.Log("[Oakheart] RowItem.prefab rifatto: colonne allineate all'intestazione.");
    }

    // ================================================================ helper
    private static Kit LoadKit()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart", "Font non trovato:\n" + FontPath, "OK");
            return null;
        }
        var k = new Kit
        {
            Ornate  = Load("panels/panel_ornate_48x48"),
            Plate   = Load("panels/nameplate_44x14"),
            Sunken  = Load("panels/panel_sunken_24x24"),
            PNormal = Load("buttons/button_primary_normal_40x16"),
            PHover  = Load("buttons/button_primary_hover_40x16"),
            PPress  = Load("buttons/button_primary_pressed_40x16"),
            SNormal = Load("buttons/button_secondary_normal_40x16"),
            SHover  = Load("buttons/button_secondary_hover_40x16"),
            SPress  = Load("buttons/button_secondary_pressed_40x16"),
            Duotone = AssetDatabase.LoadAssetAtPath<Sprite>(DuotonePath),
            Font    = font
        };
        if (k.Ornate == null)
        {
            if (!Silent) EditorUtility.DisplayDialog("Oakheart",
                "Sprite Oakheart non trovati. Lancia prima\nTools > Oakheart > Configura sprite UI.", "OK");
            return null;
        }
        return k;
    }

    /// <summary>
    /// Il titolo usa Pirata One con un materiale TMP che ha un proprio colore di
    /// faccia e di contorno: cambiare tmp.color non lo tocca, per questo restava
    /// azzurro. Qui si crea (una volta sola) un materiale della palette.
    /// </summary>
    private static Material EnsureTitleMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TitleMatPath);
        if (mat == null)
        {
            var pirata = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PirataPath);
            if (pirata == null || pirata.material == null) return null;
            mat = new Material(pirata.material);
            AssetDatabase.CreateAsset(mat, TitleMatPath);
        }

        mat.SetColor(ShaderUtilities.ID_FaceColor, Gold);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, DarkWood);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void SkinTitle(Transform title)
    {
        if (title == null) return;
        var tmp = title.GetComponent<TMP_Text>();
        if (tmp == null) return;
        var mat = EnsureTitleMaterial();
        if (mat != null) tmp.fontSharedMaterial = mat;
        tmp.color = Color.white;   // il vertex color moltiplica: bianco = colore del materiale
    }

    private static Sprite Load(string rel)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + rel + ".png");
    }

    private static void SetColor(SerializedObject so, string field, Color c)
    {
        var p = so.FindProperty(field);
        if (p != null) p.colorValue = c;
    }

    private static void SetBackground(Transform t, Kit k)
    {
        if (t == null || k.Duotone == null) return;
        var img = t.GetComponent<Image>();
        if (img == null) return;
        img.sprite = k.Duotone;
        img.color = Color.white;
        img.preserveAspect = false;
    }

    /// <summary>
    /// Diversi overlay avevano sizeDelta negativi enormi, che li riducevano a un
    /// rettangolo di pochi pixel invece che a tutto schermo.
    /// </summary>
    private static void FixOverlay(Transform t, float alpha, bool blockRaycast = false)
    {
        if (t == null) return;
        Stretch(t);
        var img = t.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = null;
            img.color = new Color(0f, 0f, 0f, alpha);
            img.raycastTarget = blockRaycast;   // un velo che intercetta i tocchi blocca i bottoni
        }
    }

    private static Transform EnsureBox(Transform parent, string name, Kit k,
                                       float width, RectOffset padding, float spacing)
    {
        Transform box = FindDeep(parent, name);
        if (box == null) box = NewImage(name, parent);
        if (box.parent != parent) box.SetParent(parent, false);

        var rt = box.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        SetSliced(box.gameObject, k.Ornate, Color.white);

        var vlg = Comp<VerticalLayoutGroup>(box.gameObject);
        vlg.padding = padding;
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var fit = Comp<ContentSizeFitter>(box.gameObject);
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        return box;
    }

    private static void RebuildBox(Transform box)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(box.GetComponent<RectTransform>());
    }

    private static void AttachPlate(Transform box, string plateName, Transform title,
                                    Kit k, float width, float fontSize)
    {
        Transform plate = FindDeep(box, plateName);
        if (plate == null) plate = NewImage(plateName, box);
        if (plate.parent != box) plate.SetParent(box, false);

        Comp<LayoutElement>(plate.gameObject).ignoreLayout = true;
        var rt = plate.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 92f);
        SetSliced(plate.gameObject, k.Plate, Color.white);
        NoRaycast(plate.gameObject);

        if (title != null)
        {
            title.SetParent(plate, false);
            Stretch(title);
            Style(title.GetComponent<TextMeshProUGUI>(), k.Font, fontSize, Cream, TextAlignmentOptions.Center);
        }
    }

    private static void MoveIntoBox(Transform t, Transform box, float w, float h, int index)
    {
        if (t.parent != box) t.SetParent(box, false);
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, h);
        Comp<LayoutElement>(t.gameObject).ignoreLayout = false;
        t.SetSiblingIndex(index);
    }

    private static void SkinMenuButton(Transform t, Kit k, bool primary,
                                       float fontSize, float w, float h)
    {
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        SkinButton(t.gameObject,
                   primary ? k.PNormal : k.SNormal,
                   primary ? k.PHover  : k.SHover,
                   primary ? k.PPress  : k.SPress);
        LabelOf(t, k.Font, fontSize);
    }

    private static void SkinBoxButton(Transform t, Transform box, Kit k, bool primary, int index,
                                      float w = 780f, float h = 110f)
    {
        if (t == null) return;
        MoveIntoBox(t, box, w, h, index);
        SkinButton(t.gameObject,
                   primary ? k.PNormal : k.SNormal,
                   primary ? k.PHover  : k.SHover,
                   primary ? k.PPress  : k.SPress);
        LabelOf(t, k.Font, FontButton);
    }

    /// <summary>
    /// Un collegamento, non un bottone: nessuna cornice, testo sottolineato.
    /// L'area cliccabile resta un rettangolo invisibile, perche' senza un Graphic
    /// con raycastTarget il tocco non verrebbe intercettato; il colore di risposta
    /// alla pressione viene applicato al testo invece che al rettangolo.
    /// </summary>
    private static void SkinLinkButton(Transform t, Transform box, Kit k, int index,
                                       float w = 780f, float h = 110f)
    {
        if (t == null) return;
        // Stessa misura dei bottoni sopra: cambia l'aspetto, non il ritmo del pannello.
        MoveIntoBox(t, box, w, h, index);

        var img = Comp<Image>(t.gameObject);
        img.sprite = null;
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        var lbl = t.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null)
        {
            Stretch(lbl.transform);
            Style(lbl, k.Font, FontLink, LinkColor, TextAlignmentOptions.Center);
            lbl.fontStyle = FontStyles.Underline;
        }

        var btn = t.GetComponent<Button>();
        if (btn != null && lbl != null)
        {
            btn.targetGraphic = lbl;
            btn.transition = Selectable.Transition.ColorTint;
            var c = btn.colors;
            c.normalColor      = Color.white;                      // moltiplica: lascia LinkColor
            c.highlightedColor = new Color(1f, 1f, 1f, 0.75f);
            c.pressedColor     = new Color(1f, 1f, 1f, 0.50f);
            c.disabledColor    = new Color(1f, 1f, 1f, 0.35f);
            c.fadeDuration     = 0.08f;
            btn.colors = c;
        }
    }

    private static void SkinInput(Transform t, Transform box, Kit k, int index)
    {
        if (t == null) return;
        MoveIntoBox(t, box, 780f, 120f, index);
        SetSliced(t.gameObject, k.Sunken, Color.white);

        var inp = t.GetComponent<TMP_InputField>();
        if (inp == null) return;

        if (inp.textComponent != null)
        {
            Style(inp.textComponent, k.Font, FontInput, DarkWood, TextAlignmentOptions.MidlineLeft);
            inp.textComponent.margin = new Vector4(18f, 0f, 18f, 0f);   // non incollato al bordo
        }

        var ph = inp.placeholder as TMP_Text;
        if (ph != null)
        {
            Style(ph, k.Font, FontInput,
                  new Color(DarkWood.r, DarkWood.g, DarkWood.b, PlaceholderAlpha),
                  TextAlignmentOptions.MidlineLeft);
            ph.margin = new Vector4(18f, 0f, 18f, 0f);
        }

        inp.customCaretColor = true;
        inp.caretColor = DarkWood;
        inp.selectionColor = new Color(Wood.r, Wood.g, Wood.b, 0.45f);
    }

    private static Transform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Transform NewButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                                typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);

        var txt = new GameObject("Text (TMP)", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        Stretch(txt.transform);
        return go.transform;
    }

    private static T Comp<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
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

    private static void NoRaycast(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    private static void SkinButton(GameObject go, Sprite normal, Sprite hover, Sprite pressed,
                                   Sprite disabled = null)
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
            disabledSprite    = disabled != null ? disabled : normal
        };
    }

    private static void LabelOf(Transform btn, TMP_FontAsset font, float size)
    {
        var t = btn.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        Stretch(t.transform);
        Style(t, font, size, DarkWood, TextAlignmentOptions.Center);
        t.margin = new Vector4(10f, 0f, 10f, 6f);
    }

    // Accetta TMP_Text e non TextMeshProUGUI perche' TMP_InputField.textComponent
    // e' dichiarato come TMP_Text: il tipo piu' generale li copre entrambi.
    private static void Style(TMP_Text tmp, TMP_FontAsset font,
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
