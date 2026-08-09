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

    private static readonly Color32 AccentRed = new Color32(194, 28, 29, 255);
    private static readonly Color32 BodyText = new Color32(75, 86, 105, 255);

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
        if (content != null && (content.Find("PracticeBadge") == null || usesFontCheckbox || needsBolderTitle))
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

        Image background = FindInScene(scene, "Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color32(247, 247, 247, 255);
        }

        RectTransform content = CreateRect(page.transform, "PageOneContent", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreatePanel(content, "BadgeShadow", roundedRectangle, new Color32(96, 15, 16, 45),
            new Vector2(0.5f, 1f), new Vector2(0f, -197f), new Vector2(295f, 60f), false);
        RectTransform badge = CreatePanel(content, "PracticeBadge", roundedRectangle, AccentRed,
            new Vector2(0.5f, 1f), new Vector2(0f, -193f), new Vector2(295f, 60f), false);
        TextMeshProUGUI badgeText = CreateText(badge, "Label", "B\u00E0i th\u1EF1c h\u00E0nh 1", 30f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, FontStyles.Bold);
        badgeText.color = Color.white;

        TextMeshProUGUI title = CreateText(content, "MainTitle",
            "\u0110\u1EA4U N\u1ED0I H\u1EC6 TH\u1ED0NG \u0110I\u1EC0U KHI\u1EC2N \u0110\u1ED8NG C\u01A0 SERVO",
            48f, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -283f), new Vector2(1520f, 82f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        title.color = AccentRed;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.fontWeight = FontWeight.Black;
        Outline titleWeight = title.gameObject.AddComponent<Outline>();
        titleWeight.effectColor = AccentRed;
        titleWeight.effectDistance = new Vector2(0.8f, -0.8f);
        titleWeight.useGraphicAlpha = true;

        CreatePanel(content, "GoalCardShadow", roundedRectangle, new Color32(32, 40, 48, 24),
            new Vector2(0.5f, 1f), new Vector2(0f, -461f), new Vector2(1154f, 315f), false);
        RectTransform card = CreatePanel(content, "GoalCard", roundedRectangle, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -455f), new Vector2(1154f, 315f), true);

        CreateCheckIcon(card, roundedRectangle);

        TextMeshProUGUI header = CreateText(card, "GoalHeader", "M\u1EE5c ti\u00EAu b\u00E0i th\u1EF1c h\u00E0nh", 36f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -32f), new Vector2(760f, 50f),
            TextAlignmentOptions.Left, FontStyles.Bold);
        header.color = new Color32(18, 20, 24, 255);

        RectTransform divider = CreatePanel(card, "Divider", null, new Color32(220, 223, 226, 255),
            new Vector2(0f, 1f), new Vector2(44f, -92f), new Vector2(1066f, 2f), false);
        divider.pivot = new Vector2(0f, 1f);

        string objectives =
            "\u2022  Nh\u1EADn bi\u1EBFt c\u00E1c th\u00E0nh ph\u1EA7n c\u1EE7a h\u1EC7 th\u1ED1ng servo v\u00F2ng k\u00EDn.\n" +
            "\u2022  Hi\u1EC3u vai tr\u00F2 c\u1EE7a PLC, HMI, servo driver, \u0111\u1ED9ng c\u01A1 BLDC v\u00E0 encoder.\n" +
            "\u2022  Th\u1EF1c hi\u1EC7n \u0111\u1EA5u n\u1ED1i m\u1EA1ch \u0111i\u1EC1u khi\u1EC3n, m\u1EA1ch ph\u1EA3n h\u1ED3i v\u00E0 m\u1EA1ch \u0111\u1ED9ng l\u1EF1c.\n" +
            "\u2022  Ki\u1EC3m tra v\u00E0 \u0111\u00E1nh gi\u00E1 ho\u1EA1t \u0111\u1ED9ng c\u1EE7a h\u1EC7 th\u1ED1ng sau khi ho\u00E0n th\u00E0nh \u0111\u1EA5u n\u1ED1i.";
        TextMeshProUGUI body = CreateText(card, "Objectives", objectives, 31f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(62f, -116f), new Vector2(1048f, 175f),
            TextAlignmentOptions.TopLeft, FontStyles.Normal);
        body.color = BodyText;
        body.lineSpacing = 7f;
        body.textWrappingMode = TextWrappingModes.Normal;

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

    private static void CreateCheckIcon(RectTransform parent, Sprite roundedRectangle)
    {
        RectTransform icon = CreateRect(parent, "GoalIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(57f, -52f), new Vector2(26f, 26f));
        CreatePanel(icon, "Box", roundedRectangle, new Color32(220, 31, 31, 255),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f), false);
        CreatePanel(icon, "BoxFill", roundedRectangle, Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f), false);

        RectTransform shortStroke = CreatePanel(icon, "CheckShort", null, new Color32(220, 31, 31, 255),
            new Vector2(0.5f, 0.5f), new Vector2(-4f, -1f), new Vector2(10f, 3f), false);
        shortStroke.localEulerAngles = new Vector3(0f, 0f, -45f);
        RectTransform longStroke = CreatePanel(icon, "CheckLong", null, new Color32(220, 31, 31, 255),
            new Vector2(0.5f, 0.5f), new Vector2(4f, 1f), new Vector2(15f, 3f), false);
        longStroke.localEulerAngles = new Vector3(0f, 0f, 45f);
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
