using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PageOneSceneSetup
{
    private const string ScenePath = "Assets/Scenes/StartScene.unity";
    private const string RoundedSpritePath = "Assets/Resources/UI/RoundedRect.png";

    private static readonly Color32 AccentRed = new Color32(194, 29, 31, 255);
    private static readonly Color32 AccentBlue = new Color32(23, 61, 151, 255);
    private static readonly Color32 PageBackground = new Color32(249, 250, 251, 255);
    private static readonly Color32 CircuitLine = new Color32(213, 217, 221, 150);
    private static readonly Color32 BodyText = new Color32(0, 0, 0, 255);

    [InitializeOnLoadMethod]
    private static void ScheduleApplyWhenNeeded()
    {
        EditorApplication.playModeStateChanged -= ApplyAfterPlayMode;
        EditorApplication.playModeStateChanged += ApplyAfterPlayMode;
        EditorApplication.delayCall += ApplyWhenNeeded;
    }

    private static void ApplyAfterPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            ApplyWhenNeeded();
        }
    }

    private static void ApplyWhenNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= ApplyAfterPlayMode;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        Transform content = FindInScene(scene, "Trang 1")?.transform.Find("PageOneContent");
        Transform goalIcon = content != null ? content.Find("GoalCard/GoalIcon") : null;
        TextMeshProUGUI mainTitle = content != null ? content.Find("MainTitle")?.GetComponent<TextMeshProUGUI>() : null;
        bool usesFontCheckbox = goalIcon != null && goalIcon.GetComponent<TextMeshProUGUI>() != null;
        bool needsBolderTitle = mainTitle != null &&
            (mainTitle.fontWeight != FontWeight.Black || mainTitle.GetComponent<Outline>() == null);
        bool needsReferenceLayout = content != null &&
            (content.Find("CircuitBackground") == null || content.Find("Subtitle") == null ||
             content.Find("GoalCard/ObjectiveRowsBold") == null);
        if (content != null && (content.Find("PracticeBadge") == null || usesFontCheckbox ||
                                needsBolderTitle || needsReferenceLayout))
        {
            BuildAndSave(scene);
        }

        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Digital Twin/Rebuild introduction page")]
    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildAndSave(scene);
    }

    private static void BuildAndSave(Scene scene)
    {
        GameObject page = FindInScene(scene, "Trang 1");
        if (page == null)
        {
            throw new System.InvalidOperationException("Could not find Trang 1 in StartScene.");
        }

        Sprite roundedRectangle = LoadOrCreateRoundedSprite();
        Transform oldContent = page.transform.Find("PageOneContent");
        if (oldContent != null)
        {
            Object.DestroyImmediate(oldContent.gameObject);
        }

        RectTransform content = CreateRect(page.transform, "PageOneContent", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateCircuitBackground(content);

        RectTransform badge = CreatePanel(content, "PracticeBadge", roundedRectangle, new Color32(234, 239, 255, 255),
            new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(304f, 70f), false);
        TextMeshProUGUI badgeText = CreateText(badge, "Label", "B\u00C0I TH\u1EF0C H\u00C0NH 1", 28f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, FontStyles.Bold);
        badgeText.color = AccentBlue;

        TextMeshProUGUI title = CreateText(content, "MainTitle",
            "\u0110\u00C2<voffset=0.11em>\u0301</voffset>U N\u00D4<voffset=0.11em>\u0301</voffset>I " +
            "H\u1EC6 TH\u00D4<voffset=0.11em>\u0301</voffset>NG " +
            "\u0110I\u00CA<voffset=0.11em>\u0300</voffset>U KHI\u00CA<voffset=0.14em>\u0309</voffset>N " +
            "\u0110\u1ED8NG C\u01A0 SERVO",
            54f, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(1600f, 80f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        title.color = AccentRed;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.fontWeight = FontWeight.Black;
        Outline titleWeight = title.gameObject.AddComponent<Outline>();
        titleWeight.effectColor = AccentRed;
        titleWeight.effectDistance = new Vector2(0.9f, -0.9f);
        titleWeight.useGraphicAlpha = true;

        TextMeshProUGUI subtitle = CreateText(content, "Subtitle",
            "Th\u1EF1c h\u00E0nh \u0111\u1EA5u n\u1ED1i v\u00E0 ki\u1EC3m tra h\u1EC7 th\u1ED1ng \u0111i\u1EC1u khi\u1EC3n \u0111\u1ED9ng c\u01A1 Servo\n" +
            "s\u1EED d\u1EE5ng PLC, HMI, Servo Driver, \u0111\u1ED9ng c\u01A1 BLDC v\u00E0 Encoder.",
            31f, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -312f), new Vector2(1180f, 100f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        subtitle.color = Color.black;
        subtitle.fontWeight = FontWeight.Bold;
        subtitle.lineSpacing = 4f;

        CreatePanel(content, "GoalCardShadowFar", roundedRectangle, new Color32(38, 45, 52, 7),
            new Vector2(0.5f, 1f), new Vector2(0f, -494f), new Vector2(1260f, 415f), false);
        CreatePanel(content, "GoalCardShadowSoft", roundedRectangle, new Color32(38, 45, 52, 9),
            new Vector2(0.5f, 1f), new Vector2(0f, -489f), new Vector2(1236f, 393f), false);
        CreatePanel(content, "GoalCardShadow", roundedRectangle, new Color32(38, 45, 52, 11),
            new Vector2(0.5f, 1f), new Vector2(0f, -486f), new Vector2(1218f, 381f), false);
        RectTransform card = CreatePanel(content, "GoalCard", roundedRectangle, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -483f), new Vector2(1208f, 371f), false);

        CreateTargetIcon(card, roundedRectangle);

        TextMeshProUGUI header = CreateText(card, "GoalHeader", "M\u1EE5c ti\u00EAu b\u00E0i th\u1EF1c h\u00E0nh", 36f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, -39f), new Vector2(800f, 58f),
            TextAlignmentOptions.Left, FontStyles.Bold);
        header.color = Color.black;
        header.fontWeight = FontWeight.Bold;

        RectTransform divider = CreatePanel(card, "Divider", null, new Color32(220, 220, 220, 255),
            new Vector2(0f, 1f), new Vector2(42f, -114f), new Vector2(1124f, 1f), false);
        divider.pivot = new Vector2(0f, 1f);

        CreateObjectiveRows(card);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageOneSceneSetup] Rebuilt the introduction page.");
    }

    private static Sprite LoadOrCreateRoundedSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        return sprite != null
            ? sprite
            : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Sprite sprite, Color color,
        Vector2 anchor, Vector2 position, Vector2 size, bool addOutline)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        if (addOutline)
        {
            Outline outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(218, 221, 224, 255);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }
        return rect;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void CreateCircuitBackground(RectTransform parent)
    {
        RectTransform background = CreateRect(parent, "CircuitBackground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = PageBackground;
        backgroundImage.raycastTarget = false;

        RectTransform traces = CreateRect(background, "CircuitTraces", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Vector2[][] paths =
        {
            new[] { new Vector2(0f, 0f), new Vector2(455f, 0f), new Vector2(601f, 27f), new Vector2(626f, 66f) },
            new[] { new Vector2(0f, 87f), new Vector2(451f, 161f), new Vector2(482f, 111f), new Vector2(568f, 121f) },
            new[] { new Vector2(0f, 135f), new Vector2(540f, 225f), new Vector2(567f, 175f), new Vector2(717f, 195f) },
            new[] { new Vector2(0f, 190f), new Vector2(557f, 286f), new Vector2(580f, 327f), new Vector2(746f, 351f) },
            new[] { new Vector2(1192f, 10f), new Vector2(1632f, 83f), new Vector2(1655f, 115f), new Vector2(1752f, 131f), new Vector2(1786f, 92f), new Vector2(1920f, 110f) },
            new[] { new Vector2(1417f, 98f), new Vector2(1630f, 136f), new Vector2(1648f, 163f), new Vector2(1920f, 209f) },
            new[] { new Vector2(1518f, 154f), new Vector2(1629f, 174f), new Vector2(1648f, 204f), new Vector2(1920f, 249f) },
            new[] { new Vector2(1607f, 203f), new Vector2(1642f, 210f), new Vector2(1662f, 241f), new Vector2(1920f, 285f) },
            new[] { new Vector2(1354f, 248f), new Vector2(1643f, 296f), new Vector2(1669f, 252f), new Vector2(1920f, 295f) },
            new[] { new Vector2(1436f, 331f), new Vector2(1519f, 345f), new Vector2(1559f, 303f), new Vector2(1920f, 364f) },
            new[] { new Vector2(1590f, 361f), new Vector2(1681f, 377f), new Vector2(1704f, 350f), new Vector2(1920f, 386f) },
            new[] { new Vector2(0f, 603f), new Vector2(511f, 694f), new Vector2(535f, 735f), new Vector2(737f, 769f) },
            new[] { new Vector2(0f, 650f), new Vector2(486f, 735f), new Vector2(513f, 783f), new Vector2(712f, 817f) },
            new[] { new Vector2(0f, 700f), new Vector2(418f, 772f), new Vector2(442f, 811f), new Vector2(629f, 842f) },
            new[] { new Vector2(0f, 776f), new Vector2(340f, 833f), new Vector2(380f, 806f), new Vector2(568f, 836f) },
            new[] { new Vector2(0f, 827f), new Vector2(483f, 908f), new Vector2(506f, 951f), new Vector2(706f, 984f) },
            new[] { new Vector2(0f, 965f), new Vector2(338f, 1022f), new Vector2(381f, 972f), new Vector2(600f, 1009f) },
            new[] { new Vector2(1236f, 919f), new Vector2(1421f, 950f), new Vector2(1438f, 981f), new Vector2(1572f, 1004f) },
            new[] { new Vector2(1441f, 877f), new Vector2(1533f, 892f), new Vector2(1563f, 955f), new Vector2(1728f, 982f), new Vector2(1770f, 927f), new Vector2(1920f, 952f) },
            new[] { new Vector2(1604f, 875f), new Vector2(1680f, 888f), new Vector2(1704f, 850f), new Vector2(1920f, 887f) },
            new[] { new Vector2(1310f, 962f), new Vector2(1418f, 980f), new Vector2(1433f, 1011f), new Vector2(1684f, 1054f) }
        };

        foreach (Vector2[] path in paths)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                CreateCircuitLine(traces, path[i], path[i + 1], 1.25f, CircuitLine);
            }
        }

        Vector2[] dots =
        {
            new Vector2(1192f, 10f), new Vector2(1518f, 154f), new Vector2(1607f, 203f),
            new Vector2(1354f, 248f), new Vector2(1436f, 331f), new Vector2(1590f, 361f),
            new Vector2(1236f, 919f), new Vector2(1441f, 877f), new Vector2(1604f, 875f),
            new Vector2(1310f, 962f), new Vector2(1640f, 866f)
        };
        foreach (Vector2 dot in dots)
        {
            RectTransform point = CreatePanel(traces, "CircuitDot", null, new Color32(203, 207, 211, 165),
                new Vector2(0f, 1f), new Vector2(dot.x, -dot.y), new Vector2(7f, 7f), false);
            point.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private static void CreateCircuitLine(RectTransform parent, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        RectTransform line = CreatePanel(parent, "CircuitLine", null, color,
            new Vector2(0f, 1f), new Vector2(start.x, -start.y), new Vector2(delta.magnitude, thickness), false);
        line.pivot = new Vector2(0f, 0.5f);
        line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void CreateTargetIcon(RectTransform parent, Sprite roundedRectangle)
    {
        RectTransform icon = CreatePanel(parent, "GoalIcon", roundedRectangle, new Color32(255, 240, 240, 255),
            new Vector2(0f, 1f), new Vector2(42f, -27f), new Vector2(64f, 64f), false);

        CreatePanel(icon, "OuterRing", roundedRectangle, AccentRed,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f), false);
        CreatePanel(icon, "OuterRingFill", roundedRectangle, new Color32(255, 240, 240, 255),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(33f, 33f), false);
        CreatePanel(icon, "InnerRing", roundedRectangle, AccentRed,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(23f, 23f), false);
        CreatePanel(icon, "InnerRingFill", roundedRectangle, new Color32(255, 240, 240, 255),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f), false);
        CreatePanel(icon, "Center", roundedRectangle, AccentRed,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), false);
    }

    private static void CreateObjectiveRows(RectTransform card)
    {
        RectTransform rows = CreateRect(card, "ObjectiveRowsBold", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(46f, -139f), new Vector2(1116f, 202f));
        rows.pivot = new Vector2(0f, 1f);

        string[] numbers = { "01.", "02.", "03.", "04." };
        string[] values =
        {
            "Nh\u1EADn bi\u1EBFt c\u00E1c th\u00E0nh ph\u1EA7n c\u1EE7a h\u1EC7 th\u1ED1ng servo v\u00F2ng k\u00EDn.",
            "Hi\u1EC3u vai tr\u00F2 c\u1EE7a PLC, HMI, servo driver, \u0111\u1ED9ng c\u01A1 BLDC v\u00E0 encoder.",
            "Th\u1EF1c hi\u1EC7n \u0111\u1EA5u n\u1ED1i m\u1EA1ch \u0111i\u1EC1u khi\u1EC3n, m\u1EA1ch ph\u1EA3n h\u1ED3i v\u00E0 m\u1EA1ch \u0111\u1ED9ng l\u1EF1c.",
            "Ki\u1EC3m tra v\u00E0 \u0111\u00E1nh gi\u00E1 ho\u1EA1t \u0111\u1ED9ng c\u1EE7a h\u1EC7 th\u1ED1ng sau khi ho\u00E0n th\u00E0nh \u0111\u1EA5u n\u1ED1i."
        };

        for (int i = 0; i < values.Length; i++)
        {
            float y = -i * 54f;
            TextMeshProUGUI number = CreateText(rows, $"Number{i + 1}", numbers[i], 28f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(54f, 44f),
                TextAlignmentOptions.Left, FontStyles.Bold);
            number.color = AccentBlue;

            TextMeshProUGUI value = CreateText(rows, $"Objective{i + 1}", values[i], 28f,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(55f, y), new Vector2(1061f, 44f),
                TextAlignmentOptions.Left, FontStyles.Bold);
            value.color = BodyText;
            value.fontWeight = FontWeight.Bold;
            value.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, TextAlignmentOptions alignment,
        FontStyles fontStyle)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.black;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }
}
