using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PageThreeSceneSetup
{
    private const string ScenePath = "Assets/Scenes/StartScene.unity";
    private const string RoundedSpritePath = "Assets/Resources/UI/RoundedRect.png";
    private const string DiagramSpritePath = "Assets/IntroImages/intro_page_1.png";

    private static readonly Color32 AccentRed = new Color32(194, 28, 29, 255);
    private static readonly Color32 HeadingColor = new Color32(35, 35, 38, 255);
    private static readonly Color32 BodyColor = new Color32(83, 91, 105, 255);

    [InitializeOnLoadMethod]
    private static void ScheduleApplyWhenNeeded()
    {
        EditorApplication.delayCall += ApplyWhenNeeded;
    }

    private static void ApplyWhenNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        GameObject page = FindInScene(scene, "Trang 3");
        if (page != null && page.transform.Find("PageThreeContent") == null)
        {
            BuildAndSave(scene);
        }

        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Digital Twin/Rebuild operating principle page")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new System.InvalidOperationException("Exit Play Mode before rebuilding the operating principle page.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        BuildAndSave(scene);

        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void BuildAndSave(Scene scene)
    {
        GameObject page = FindInScene(scene, "Trang 3");
        if (page == null)
        {
            throw new System.InvalidOperationException("Could not find Trang 3 in StartScene.");
        }

        for (int i = page.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(page.transform.GetChild(i).gameObject);
        }

        Sprite roundedRectangle = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        Sprite diagram = AssetDatabase.LoadAssetAtPath<Sprite>(DiagramSpritePath);
        RectTransform content = CreateRect(page.transform, "PageThreeContent", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        CreateHeader(content);
        CreateBookmarkIcon(content);
        TextMeshProUGUI title = CreateText(content, "PageThreeTitle",
            "NGUY\u00CAN L\u00DD HO\u1EA0T \u0110\u1ED8NG", 44f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(121f, -112f),
            new Vector2(900f, 66f), TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = HeadingColor;
        title.fontWeight = FontWeight.Black;
        title.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform processCard = CreateCard(content, "ProcessCard", roundedRectangle,
            new Vector2(52f, -194f), new Vector2(1235f, 437f));
        CreateProcessContent(processCard, roundedRectangle);

        RectTransform notesCard = CreateCard(content, "NotesCard", roundedRectangle,
            new Vector2(52f, -643f), new Vector2(1235f, 277f));
        TextMeshProUGUI notes = CreateText(notesCard, "Notes",
            "\u2022  T\u1ED1c \u0111\u1ED9 \u0111\u1ED9ng c\u01A1 ph\u1EE5 thu\u1ED9c v\u00E0o t\u1EA7n s\u1ED1 xung ph\u00E1t t\u1EEB PLC.\n" +
            "\u2022  V\u1ECB tr\u00ED/g\u00F3c quay ph\u1EE5 thu\u1ED9c v\u00E0o t\u1ED5ng s\u1ED1 xung \u0111i\u1EC1u khi\u1EC3n \u0111\u01B0\u1EE3c ph\u00E1t ra.\n" +
            "\u2022  Encoder pha A/B l\u1EC7ch nhau 90 \u0111\u1ED9 gi\u00FAp x\u00E1c \u0111\u1ECBnh chi\u1EC1u quay v\u00E0 s\u1ED1 xung th\u1EF1c t\u1EBF.\n" +
            "\u2022  Bi\u00EAn d\u1EA1ng t\u0103ng t\u1ED1c - ch\u1EA1y \u0111\u1EC1u - gi\u1EA3m t\u1ED1c gi\u00FAp \u0111\u1ED9ng c\u01A1 v\u1EADn h\u00E0nh \u00EAm, h\u1EA1n ch\u1EBF\n" +
            "   qu\u00E1 t\u1EA3i ho\u1EB7c tr\u01B0\u1EE3t c\u01A1 kh\u00ED.",
            30f, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62f, -39f),
            new Vector2(1115f, 218f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        StyleBodyText(notes, 3f);

        CreateDiagram(content, diagram);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageThreeSceneSetup] Rebuilt the operating principle page.");
    }

    private static void CreateHeader(RectTransform content)
    {
        RectTransform header = CreatePanel(content, "PageThreeHeader", null, AccentRed,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 50f));
        TextMeshProUGUI left = CreateText(header, "PracticeLabel", "B\u00E0i th\u1EF1c h\u00E0nh 1", 27f,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f),
            new Vector2(650f, 46f), TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
        left.color = Color.white;
        TextMeshProUGUI right = CreateText(header, "PracticeTitle",
            "\u0110\u1EA5u n\u1ED1i h\u1EC7 th\u1ED1ng \u0111i\u1EC1u khi\u1EC3n \u0111\u1ED9ng c\u01A1 servo", 25f,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f),
            new Vector2(900f, 46f), TextAlignmentOptions.Right, FontStyles.Bold | FontStyles.Italic);
        right.color = Color.white;
    }

    private static void CreateProcessContent(RectTransform card, Sprite roundedRectangle)
    {
        TextMeshProUGUI intro = CreateText(card, "Intro",
            "H\u1EC7 th\u1ED1ng \u0111\u01B0\u1EE3c x\u00E2y d\u1EF1ng theo c\u1EA5u tr\u00FAc \u0111i\u1EC1u khi\u1EC3n v\u00F2ng k\u00EDn.",
            30f, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -37f),
            new Vector2(1125f, 45f), TextAlignmentOptions.Left, FontStyles.Normal);
        StyleBodyText(intro, 0f);

        CreateProcessStep(card, roundedRectangle, 1, -88f,
            "Ng\u01B0\u1EDDi v\u1EADn h\u00E0nh nh\u1EADp l\u1EC7nh tr\u00EAn HMI", 45f);
        CreateProcessStep(card, roundedRectangle, 2, -140f,
            "PLC x\u1EED l\u00FD l\u1EC7nh v\u00E0 ph\u00E1t xung t\u1ED1c \u0111\u1ED9 cao \u0111\u1EBFn Servo Driver.", 45f);
        CreateProcessStep(card, roundedRectangle, 3, -192f,
            "Driver khu\u1EBFch \u0111\u1EA1i v\u00E0 bi\u1EBFn \u0111\u1ED5i t\u00EDn hi\u1EC7u \u0111i\u1EC1u khi\u1EC3n th\u00E0nh \u0111i\u1EC7n \u00E1p ba pha c\u1EA5p cho\n" +
            "\u0111\u1ED9ng c\u01A1 BLDC Servo.", 82f);
        CreateProcessStep(card, roundedRectangle, 4, -280f,
            "Encoder g\u1EAFn v\u1EDBi tr\u1EE5c \u0111\u1ED9ng c\u01A1 t\u1EA1o xung ph\u1EA3n h\u1ED3i v\u1EC1 PLC\n" +
            "\u2192 H\u1EC7 th\u1ED1ng x\u00E1c \u0111\u1ECBnh \u0111\u01B0\u1EE3c chi\u1EC1u quay, t\u1ED1c \u0111\u1ED9 v\u00E0 v\u1ECB tr\u00ED th\u1EF1c t\u1EBF.\n" +
            "\u2192 Gi\u00E1m sai l\u1EC7ch so v\u1EDBi gi\u00E1 tr\u1ECB \u0111\u1EB7t.", 125f);
    }

    private static void CreateProcessStep(RectTransform card, Sprite roundedRectangle, int number, float top,
        string value, float textHeight)
    {
        RectTransform numberBox = CreatePanel(card, "StepNumber" + number, roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, top),
            new Vector2(42f, 42f));
        Outline outline = numberBox.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(218, 221, 224, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
        TextMeshProUGUI numberText = CreateText(numberBox, "Number", number.ToString(), 23f,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Normal);
        numberText.color = BodyColor;

        TextMeshProUGUI text = CreateText(card, "StepText" + number, value, 30f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, top + 1f),
            new Vector2(1085f, textHeight), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        StyleBodyText(text, 1f);
    }

    private static void CreateDiagram(RectTransform content, Sprite diagram)
    {
        GameObject imageObject = new GameObject("ClosedLoopDiagram", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(content, false);
        imageObject.layer = 5;
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(1307f, -194f);
        rect.sizeDelta = new Vector2(565f, 300f);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = diagram;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static RectTransform CreateCard(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Shadow), typeof(Outline));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = false;
        Shadow shadow = gameObject.GetComponent<Shadow>();
        shadow.effectColor = new Color32(30, 35, 42, 22);
        shadow.effectDistance = new Vector2(0f, -2f);
        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color32(218, 221, 224, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
        return rect;
    }

    private static void CreateBookmarkIcon(RectTransform parent)
    {
        Color color = new Color32(211, 28, 31, 255);
        RectTransform icon = CreateRect(parent, "PageThreeTitleIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(70f, -134f), new Vector2(28f, 38f));
        CreatePanel(icon, "Top", null, color, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(0f, 17f), new Vector2(27f, 3f));
        CreatePanel(icon, "Left", null, color, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(-12f, 1f), new Vector2(3f, 34f));
        CreatePanel(icon, "Right", null, color, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(12f, 1f), new Vector2(3f, 34f));
        RectTransform leftTip = CreatePanel(icon, "LeftTip", null, color, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(-6f, -12f), new Vector2(14.5f, 3f));
        leftTip.localEulerAngles = new Vector3(0f, 0f, 34f);
        RectTransform rightTip = CreatePanel(icon, "RightTip", null, color, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, new Vector2(6f, -12f), new Vector2(14.5f, 3f));
        rightTip.localEulerAngles = new Vector3(0f, 0f, -34f);
    }

    private static void StyleBodyText(TextMeshProUGUI text, float lineSpacing)
    {
        text.color = BodyColor;
        text.lineSpacing = lineSpacing;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Sprite sprite, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size,
        TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
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
