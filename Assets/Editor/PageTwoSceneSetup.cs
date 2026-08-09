using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PageTwoSceneSetup
{
    private const string ScenePath = "Assets/Scenes/StartScene.unity";
    private const string RoundedSpritePath = "Assets/Resources/UI/RoundedRect.png";

    private static readonly Color32 AccentRed = new Color32(194, 28, 29, 255);
    private static readonly Color32 HeadingColor = new Color32(35, 35, 38, 255);

    private static readonly string[] Labels =
    {
        "PLC Mitsubishi FX3U",
        "HMI Mitsubishi GOT1000",
        "\u0110\u1ED9ng c\u01A1 BLDC Servo",
        "Encoder",
        "Aptomat",
        "D\u00E2y c\u1EAFm",
        "B\u1EA3ng c\u1EAFm d\u00E2y"
    };

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

        Transform content = FindInScene(scene, "Trang 2")?.transform.Find("PageTwoContent");
        if (content == null || content.Find("PageTwoHeader") == null || content.Find("DetailPartIcon") == null ||
            content.Find("DetailViewportBorder") != null)
        {
            BuildAndSave(scene);
        }

        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Digital Twin/Rebuild components page")]
    public static void Run()
    {
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
        GameObject page = FindInScene(scene, "Trang 2");
        if (page == null)
        {
            throw new System.InvalidOperationException("Could not find Trang 2 in StartScene.");
        }

        Transform oldContent = page.transform.Find("PageTwoContent");
        if (oldContent != null)
        {
            Object.DestroyImmediate(oldContent.gameObject);
        }

        Sprite roundedRectangle = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        RectTransform content = CreateRect(page.transform, "PageTwoContent", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform header = CreatePanel(content, "PageTwoHeader", null, AccentRed,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 50f));
        TextMeshProUGUI headerLeft = CreateText(header, "PracticeLabel", "B\u00E0i th\u1EF1c h\u00E0nh 1", 27f,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f),
            new Vector2(650f, 46f), TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.Italic);
        headerLeft.color = Color.white;
        TextMeshProUGUI headerRight = CreateText(header, "PracticeTitle",
            "\u0110\u1EA5u n\u1ED1i h\u1EC7 th\u1ED1ng \u0111i\u1EC1u khi\u1EC3n \u0111\u1ED9ng c\u01A1 servo", 25f,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, 0f),
            new Vector2(900f, 46f), TextAlignmentOptions.Right, FontStyles.Bold | FontStyles.Italic);
        headerRight.color = Color.white;

        CreateBookmarkIcon(content);
        TextMeshProUGUI title = CreateText(content, "PageTwoTitle",
            "C\u00C1C TH\u00C0NH PH\u1EA6N CH\u00CDNH C\u1EE6A M\u00D4 H\u00CCNH", 48f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(118f, -103f),
            new Vector2(1250f, 74f), TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = HeadingColor;
        title.fontWeight = FontWeight.Black;
        title.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform buttonList = CreateRect(content, "PartButtonList", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(311f, -226f), new Vector2(400f, 660f));
        Button[] buttons = new Button[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            buttons[i] = CreateButton(buttonList, "PartButton_" + i, Labels[i], roundedRectangle,
                new Vector2(0.5f, 1f), new Vector2(0f, -i * 96f), new Vector2(400f, 84f), 31f);
        }

        TextMeshProUGUI selectedLabel = CreateText(content, "SelectedPartLabel", string.Empty, 34f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -76f),
            new Vector2(960f, 58f), TextAlignmentOptions.Left, FontStyles.Bold);
        selectedLabel.color = HeadingColor;
        selectedLabel.fontWeight = FontWeight.SemiBold;
        selectedLabel.textWrappingMode = TextWrappingModes.NoWrap;
        selectedLabel.gameObject.SetActive(false);

        TextMeshProUGUI descriptionText = CreateText(content, "PartDescriptionText", string.Empty, 38f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(58f, -145f),
            new Vector2(980f, 790f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        descriptionText.color = new Color32(70, 76, 86, 255);
        descriptionText.lineSpacing = 4f;
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.gameObject.SetActive(false);

        RectTransform detailIcon = CreateDetailPartIcon(content);
        detailIcon.gameObject.SetActive(false);
        RectTransform detailDivider = CreatePanel(content, "DetailDivider", null, new Color32(224, 226, 229, 255),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -122f),
            new Vector2(960f, 2f));
        detailDivider.gameObject.SetActive(false);

        Button listButton = CreateButton(content, "ShowPartListButton", "\u2039  Danh s\u00E1ch", roundedRectangle,
            new Vector2(0f, 1f), new Vector2(58f, -190f), new Vector2(220f, 58f), 27f);
        listButton.gameObject.SetActive(false);

        PageTwoPartsController controller = page.GetComponent<PageTwoPartsController>();
        if (controller == null)
        {
            controller = page.AddComponent<PageTwoPartsController>();
        }
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("accentColor").colorValue = AccentRed;
        serializedController.FindProperty("buttonList").objectReferenceValue = buttonList;
        serializedController.FindProperty("selectedLabel").objectReferenceValue = selectedLabel;
        serializedController.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        serializedController.FindProperty("listButton").objectReferenceValue = listButton;
        SerializedProperty buttonArray = serializedController.FindProperty("partButtons");
        buttonArray.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttonArray.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        }
        AssignWireAssets(serializedController);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageTwoSceneSetup] Rebuilt the components page.");
    }

    private static void CreateBookmarkIcon(RectTransform parent)
    {
        Color color = new Color32(211, 28, 31, 255);
        RectTransform icon = CreateRect(parent, "PageTwoTitleIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(70f, -134f), new Vector2(28f, 38f));
        CreatePanel(icon, "Top", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 17f), new Vector2(27f, 3f));
        CreatePanel(icon, "Left", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-12f, 1f), new Vector2(3f, 34f));
        CreatePanel(icon, "Right", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(12f, 1f), new Vector2(3f, 34f));
        RectTransform leftTip = CreatePanel(icon, "LeftTip", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-6f, -12f), new Vector2(14.5f, 3f));
        leftTip.localEulerAngles = new Vector3(0f, 0f, 34f);
        RectTransform rightTip = CreatePanel(icon, "RightTip", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(6f, -12f), new Vector2(14.5f, 3f));
        rightTip.localEulerAngles = new Vector3(0f, 0f, -34f);
    }

    private static RectTransform CreateDetailPartIcon(RectTransform parent)
    {
        RectTransform icon = CreateRect(parent, "DetailPartIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(52f, -89f), new Vector2(28f, 28f));
        Color color = new Color32(211, 28, 31, 255);
        CreatePanel(icon, "ChipBody", null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(15f, 15f));
        CreatePanel(icon, "ChipCore", null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7f, 7f));

        for (int i = -1; i <= 1; i++)
        {
            CreatePanel(icon, "PinTop" + i, null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(i * 5f, 10f), new Vector2(2f, 6f));
            CreatePanel(icon, "PinBottom" + i, null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(i * 5f, -10f), new Vector2(2f, 6f));
            CreatePanel(icon, "PinLeft" + i, null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-10f, i * 5f), new Vector2(6f, 2f));
            CreatePanel(icon, "PinRight" + i, null, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(10f, i * 5f), new Vector2(6f, 2f));
        }

        return icon;
    }

    private static void AssignWireAssets(SerializedObject controller)
    {
        controller.FindProperty("jack35Prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jack 3.5mm.fbx");
        AssignMaterialArray(controller.FindProperty("wireMaterials"), new[]
        {
            "Assets/Materials/Red_wire.mat",
            "Assets/Materials/Yellow_wire.mat",
            "Assets/Materials/Black_wire.mat"
        });
        AssignMaterialArray(controller.FindProperty("jackBodyMaterials"), new[]
        {
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Red.mat",
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Yellow.mat",
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Black.mat"
        });
    }

    private static void AssignMaterialArray(SerializedProperty property, string[] paths)
    {
        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
        }
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Sprite roundedRectangle,
        Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Shadow), typeof(Outline), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = gameObject.GetComponent<Image>();
        image.sprite = roundedRectangle;
        image.type = roundedRectangle != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        Shadow shadow = gameObject.GetComponent<Shadow>();
        shadow.effectColor = new Color32(28, 34, 40, 18);
        shadow.effectDistance = new Vector2(0f, -2f);
        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color32(218, 221, 224, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(252, 242, 242, 255);
        colors.pressedColor = new Color32(244, 222, 222, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(rect, "Label", label, fontSize, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, FontStyles.Normal);
        text.color = new Color32(58, 58, 62, 255);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return button;
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
