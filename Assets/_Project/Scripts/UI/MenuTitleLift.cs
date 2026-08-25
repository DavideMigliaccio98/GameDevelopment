using TMPro;
using UnityEngine;

/// <summary>
/// Tiene il titolo del gioco visibile sopra un pannello a schermo intero.
///
/// Il problema si e' presentato due volte identico, prima con la selezione
/// livelli e poi col profilo: quei pannelli occupano tutto lo schermo e si
/// portano dietro un fondale proprio, quindi coprono il titolo. Non sparisce,
/// ci finisce sotto.
///
/// Servono due cose. La prima e' disegnarlo dopo il pannello. La seconda e'
/// alzarlo, perche' il riquadro del pannello e' centrato in verticale mentre il
/// titolo e' ancorato in alto: su uno schermo alto e stretto i due non si
/// toccano, su uno piu' tozzo il riquadro sale e ci finisce addosso. Una quota
/// fissa funzionerebbe su un formato solo.
///
/// Quindi la quota non si sceglie, si misura: si guarda dove comincia davvero
/// la targhetta del pannello e si alza il titolo quel tanto che serve, non un
/// pixel di piu'. Dove lo spazio c'e' gia', il titolo non si muove affatto.
///
/// Lo stato e' statico perche' i pannelli si aprono uno alla volta: due aperti
/// insieme non esistono in questo gioco, e tenere lo stato qui evita che ogni
/// pannello si porti dietro la propria copia della stessa logica.
/// </summary>
public static class MenuTitleLift
{
    private const string TitleName = "Title";

    private static RectTransform title;
    private static int originalIndex = -1;
    private static Vector2 originalPos;
    private static bool raised;

    /// <summary>
    /// Porta il titolo davanti al pannello e, se serve, lo alza sopra la targhetta.
    /// </summary>
    /// <param name="panelRoot">Il pannello che si sta aprendo.</param>
    /// <param name="plateNames">Nomi da cercare per la targhetta, in ordine di preferenza.</param>
    public static void Raise(Transform panelRoot, string[] plateNames, float gap, float topMargin,
                             bool log = false)
    {
        if (panelRoot == null) return;

        if (title == null)
        {
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            title = canvas.transform.Find(TitleName) as RectTransform;
            if (title == null) return;
        }

        if (originalIndex < 0)
        {
            originalIndex = title.GetSiblingIndex();
            originalPos = title.anchoredPosition;
        }

        title.SetAsLastSibling();
        title.anchoredPosition = originalPos;
        raised = true;

        RectTransform plate = FindPlate(panelRoot, plateNames);
        if (plate == null) return;

        float scale = title.lossyScale.y;
        if (scale <= 0.0001f) return;

        float textBottom, textTop;
        Measure(out textBottom, out textTop);

        var plateCorners = new Vector3[4];
        plate.GetWorldCorners(plateCorners);
        float plateTop = plateCorners[1].y;                 // angolo in alto a sinistra

        float needed = (plateTop + gap * scale) - textBottom;
        if (needed <= 0f)
        {
            if (log) Debug.Log("[MenuTitleLift] Spazio gia' sufficiente: titolo non spostato.");
            return;
        }

        // La scritta non deve uscire dal bordo alto dello schermo.
        float room = float.MaxValue;
        RectTransform parent = title.parent as RectTransform;
        if (parent != null)
        {
            var parentCorners = new Vector3[4];
            parent.GetWorldCorners(parentCorners);
            room = (parentCorners[1].y - topMargin * scale) - textTop;
        }

        float applied = Mathf.Min(needed, room);
        if (applied <= 0f)
        {
            if (log)
                Debug.LogWarning($"[MenuTitleLift] Non c'e' spazio: servivano {needed / scale:F0}, "
                                 + $"disponibili {room / scale:F0}.");
            return;
        }

        title.anchoredPosition = originalPos + new Vector2(0f, applied / scale);

        if (log)
            Debug.Log($"[MenuTitleLift] Titolo alzato di {applied / scale:F0} "
                      + $"(servivano {needed / scale:F0}, disponibili {room / scale:F0}).");
    }

    /// <summary>
    /// Rimette il titolo dov'era, sia come posizione sia come ordine di disegno.
    /// </summary>
    public static void Restore()
    {
        if (title == null || !raised) return;
        if (originalIndex >= 0) title.SetSiblingIndex(originalIndex);
        title.anchoredPosition = originalPos;
        raised = false;
    }

    /// <summary>
    /// I confini verticali della SCRITTA, non del riquadro che la contiene.
    ///
    /// La differenza non e' un dettaglio: il riquadro del titolo e' 800x250
    /// mentre la scritta ne occupa una sessantina in mezzo. Misurando il
    /// riquadro si chiede uno spostamento enorme, si sbatte contro il limite in
    /// alto e ci si ferma a meta', col testo ancora addosso alla targhetta.
    /// </summary>
    private static void Measure(out float bottom, out float top)
    {
        TMP_Text label = title.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.ForceMeshUpdate();
            Bounds b = label.textBounds;
            if (b.size.y > 0.0001f)
            {
                bottom = label.transform.TransformPoint(new Vector3(0f, b.min.y, 0f)).y;
                top = label.transform.TransformPoint(new Vector3(0f, b.max.y, 0f)).y;
                return;
            }
        }

        var corners = new Vector3[4];
        title.GetWorldCorners(corners);
        bottom = corners[0].y;
        top = corners[1].y;
    }

    private static RectTransform FindPlate(Transform root, string[] names)
    {
        if (names == null) return null;
        for (int i = 0; i < names.Length; i++)
        {
            Transform t = FindDeep(root, names[i]);
            RectTransform rt = t as RectTransform;
            if (rt != null) return rt;
        }
        return null;
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
