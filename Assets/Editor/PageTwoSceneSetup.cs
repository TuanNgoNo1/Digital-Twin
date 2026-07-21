using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PageTwoSceneSetup
{
    private static readonly string[] Labels =
    {
        "PLC Mitsubishi FX3U",
        "HMI Mitsubishi GOT1000",
        "Động cơ BLDC Servo",
        "Encoder",
        "Aptomat",
        "Dây cắm",
        "Bảng cắm dây"
    };

    static PageTwoSceneSetup()
    {
        EditorApplication.delayCall += RunWhenStartSceneIsOpen;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RunWhenStartSceneIsOpen;
        }
    }

    private static void RunWhenStartSceneIsOpen()
    {
        const string scenePath = "Assets/Scenes/StartScene.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool closeAfterSetup = !scene.isLoaded;
        if (closeAfterSetup)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }
        if (FindInScene(scene, "PageTwoContent") == null)
        {
            BuildAndSave(scene);
        }
        else
        {
            ConfigureExistingAssets(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void Run()
    {
        const string scenePath = "Assets/Scenes/StartScene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        BuildAndSave(scene);
    }

    private static void BuildAndSave(Scene scene)
    {
        const string scenePath = "Assets/Scenes/StartScene.unity";
        GameObject page = FindInScene(scene, "Trang 2");
        if (page == null)
        {
            throw new System.InvalidOperationException("Không tìm thấy Trang 2 trong StartScene.");
        }

        Transform oldContent = page.transform.Find("PageTwoContent");
        if (oldContent != null)
        {
            Object.DestroyImmediate(oldContent.gameObject);
        }

        RectTransform content = CreateRect(page.transform, "PageTwoContent", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        content.gameObject.layer = 5;

        CreateText(content, "PageTwoTitle", "Các thành phần chính của mô hình", 48f,
            new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1100f, 80f), TextAlignmentOptions.Center, FontStyles.Normal);

        RectTransform buttonList = CreateRect(content, "PartButtonList", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, -18f), new Vector2(460f, 720f));
        Button[] buttons = new Button[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            buttons[i] = CreateButton(buttonList, "PartButton_" + i, Labels[i], new Vector2(0.5f, 1f),
                new Vector2(0f, -58f - i * 105f), new Vector2(430f, 82f), Labels[i].Length > 21 ? 29f : 34f);
        }

        TextMeshProUGUI selectedLabel = CreateText(content, "SelectedPartLabel", string.Empty, 42f,
            new Vector2(0f, 0.5f), new Vector2(145f, 170f), new Vector2(700f, 120f), TextAlignmentOptions.Left, FontStyles.Bold);
        selectedLabel.color = new Color(0.08f, 0.34f, 0.58f, 1f);
        selectedLabel.gameObject.SetActive(false);

        TextMeshProUGUI descriptionText = CreateText(content, "PartDescriptionText", string.Empty, 36f,
            new Vector2(0f, 0.5f), new Vector2(145f, 92f), new Vector2(700f, 360f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
        descriptionText.lineSpacing = 7f;
        descriptionText.gameObject.SetActive(false);

        Button listButton = CreateButton(content, "ShowPartListButton", "‹  Danh sách", new Vector2(0f, 1f),
            new Vector2(48f, -138f), new Vector2(190f, 54f), 27f);
        listButton.gameObject.SetActive(false);

        GameObject borderObject = new GameObject("ModelViewportBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        borderObject.transform.SetParent(content, false);
        borderObject.layer = 5;
        RectTransform border = borderObject.GetComponent<RectTransform>();
        border.anchorMin = new Vector2(0.49f, 0.14f);
        border.anchorMax = new Vector2(0.91f, 0.82f);
        border.offsetMin = Vector2.zero;
        border.offsetMax = Vector2.zero;
        Image borderImage = borderObject.GetComponent<Image>();
        borderImage.color = new Color(1f, 1f, 1f, 0f);
        borderImage.raycastTarget = false;
        Outline outline = borderObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.1f, 0.13f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        PageTwoPartsController controller = page.GetComponent<PageTwoPartsController>();
        if (controller == null)
        {
            controller = page.AddComponent<PageTwoPartsController>();
        }
        SerializedObject serializedController = new SerializedObject(controller);
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
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageTwoSceneSetup] Đã tạo giao diện TextMeshPro của Trang 2 trong hierarchy.");
    }

    private static void ConfigureExistingAssets(Scene scene)
    {
        GameObject page = FindInScene(scene, "Trang 2");
        PageTwoPartsController controller = page != null ? page.GetComponent<PageTwoPartsController>() : null;
        if (controller == null)
        {
            return;
        }
        SerializedObject serializedController = new SerializedObject(controller);
        Transform content = page.transform.Find("PageTwoContent");
        RectTransform buttonList = content != null ? content.Find("PartButtonList") as RectTransform : null;
        TextMeshProUGUI descriptionText = content != null ? content.Find("PartDescriptionText")?.GetComponent<TextMeshProUGUI>() : null;
        if (content != null && descriptionText == null)
        {
            descriptionText = CreateText(content as RectTransform, "PartDescriptionText", string.Empty, 36f,
                new Vector2(0f, 0.5f), new Vector2(145f, 92f), new Vector2(700f, 360f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
            descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
            descriptionText.lineSpacing = 7f;
            descriptionText.gameObject.SetActive(false);
        }
        if (descriptionText != null)
        {
            descriptionText.fontSize = 36f;
        }
        serializedController.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        if (buttonList != null)
        {
            Button[] buttons = new Button[Labels.Length];
            for (int i = 0; i < Labels.Length; i++)
            {
                Transform existing = buttonList.Find("PartButton_" + i);
                buttons[i] = existing != null
                    ? existing.GetComponent<Button>()
                    : CreateButton(buttonList, "PartButton_" + i, Labels[i], new Vector2(0.5f, 1f),
                        new Vector2(0f, -58f - i * 96f), new Vector2(430f, 78f), Labels[i].Length > 21 ? 29f : 34f);

                RectTransform rect = buttons[i].transform as RectTransform;
                rect.anchoredPosition = new Vector2(0f, -58f - i * 96f);
                rect.sizeDelta = new Vector2(430f, 78f);
                TextMeshProUGUI label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = Labels[i];
                }
            }

            SerializedProperty buttonArray = serializedController.FindProperty("partButtons");
            buttonArray.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttonArray.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
        }
        AssignWireAssets(serializedController);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void AssignWireAssets(SerializedObject controller)
    {
        controller.FindProperty("jack35Prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jack 3.5mm.fbx");

        string[] wirePaths =
        {
            "Assets/Materials/Red_wire.mat",
            "Assets/Materials/Yellow_wire.mat",
            "Assets/Materials/Black_wire.mat"
        };
        string[] jackPaths =
        {
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Red.mat",
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Yellow.mat",
            "Assets/Materials/WirePlugOverlay/WirePlugOverlay_Black.mat"
        };
        AssignMaterialArray(controller.FindProperty("wireMaterials"), wirePaths);
        AssignMaterialArray(controller.FindProperty("jackBodyMaterials"), jackPaths);
    }

    private static void AssignMaterialArray(SerializedProperty property, string[] paths)
    {
        property.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
        }
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

    private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize,
        Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.black;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);
        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.1f, 0.13f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        TextMeshProUGUI text = CreateText(rect, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero,
            size - new Vector2(24f, 12f), TextAlignmentOptions.Center, FontStyles.Normal);
        text.raycastTarget = false;
        return gameObject.GetComponent<Button>();
    }
}
