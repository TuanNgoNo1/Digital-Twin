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
    private static readonly Color32 PageBackground = new Color32(249, 250, 251, 255);
    private static readonly Color32 CircuitLine = new Color32(213, 217, 221, 150);

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
        Transform bookmarkIcon = content?.Find("PageTwoTitleIcon");
        // Rebuild nếu thiếu component hoặc icon bookmark vẫn dùng design cũ (chưa dùng ảnh Image)
        bool oldBookmarkDesign = bookmarkIcon != null && bookmarkIcon.Find("Image") == null;
        if (content == null || content.Find("ReferenceLayoutV12") == null || content.Find("DetailOverlayRoot") == null ||
            content.Find("DetailViewportBorder") != null || content.Find("CircuitBackground") == null ||
            content.Find("ModelViewport") == null || oldBookmarkDesign)
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
        CreateRect(content, "ReferenceLayoutV12", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

        CreateCircuitBackground(content);
        CreateTopHeader(content);
        CreateBookmarkIcon(content);
        TextMeshProUGUI title = CreateText(content, "PageTwoTitle",
            "C\u00C1C TH\u00C0NH PH\u1EA6N CH\u00CDNH C\u1EE6A M\u00D4 H\u00CCNH", 44f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -101f),
            new Vector2(1150f, 72f), TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = AccentRed;
        title.fontWeight = FontWeight.Black;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        Outline titleWeight = title.gameObject.AddComponent<Outline>();
        titleWeight.effectColor = AccentRed;
        titleWeight.effectDistance = new Vector2(1.5f, -1.5f);
        titleWeight.useGraphicAlpha = true;

        RectTransform buttonList = CreateRect(content, "PartButtonList", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(182f, -200f), new Vector2(500f, 690f));
        Button[] buttons = new Button[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            buttons[i] = CreateButton(buttonList, "PartButton_" + i, Labels[i], roundedRectangle,
                new Vector2(0.5f, 1f), new Vector2(0f, -i * 101f), new Vector2(500f, 86f), 27f, i);
        }

        CreateOverviewFrame(content, roundedRectangle);

        CreateDetailModal(content, roundedRectangle, buttons,
            out TextMeshProUGUI selectedLabel, out TextMeshProUGUI descriptionText, out Button listButton);

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

    internal static void CreateTopHeader(RectTransform parent)
    {
        RectTransform header = CreatePanel(parent, "PageTwoHeader", null, new Color32(249, 250, 251, 248),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
            new Vector2(0f, 50f));

        TextMeshProUGUI leftText = CreateText(header, "PracticeLabel", "Bài thực hành 1", 21f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -2f),
            new Vector2(430f, 44f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        leftText.color = Color.black;
        leftText.fontWeight = FontWeight.Black;
        Outline leftWeight = leftText.gameObject.AddComponent<Outline>();
        leftWeight.effectColor = Color.black;
        leftWeight.effectDistance = new Vector2(0.45f, -0.45f);
        leftWeight.useGraphicAlpha = true;

        TextMeshProUGUI rightText = CreateText(header, "PracticeTitle",
            "Đấu nối hệ thống điều khiển động cơ servo", 21f,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -2f),
            new Vector2(760f, 44f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        rightText.color = Color.black;
        rightText.fontWeight = FontWeight.Black;
        Outline rightWeight = rightText.gameObject.AddComponent<Outline>();
        rightWeight.effectColor = Color.black;
        rightWeight.effectDistance = new Vector2(0.45f, -0.45f);
        rightWeight.useGraphicAlpha = true;

        CreatePanel(header, "BottomDivider", null, new Color32(218, 220, 223, 255),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(0f, 1f));
    }

    private static void CreateDetailModal(RectTransform parent, Sprite roundedRectangle, Button[] partButtons,
        out TextMeshProUGUI selectedLabel, out TextMeshProUGUI descriptionText, out Button closeButton)
    {
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circle == null)
        {
            circle = roundedRectangle;
        }

        RectTransform overlayRoot = CreateRect(parent, "DetailOverlayRoot", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Canvas overlayCanvas = overlayRoot.gameObject.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 100;
        overlayRoot.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform backdrop = CreatePanel(overlayRoot, "DimBackdrop", null, new Color32(0, 0, 0, 118),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.raycastTarget = true;
        Button backdropButton = backdrop.gameObject.AddComponent<Button>();
        backdropButton.targetGraphic = backdropImage;
        backdropButton.transition = Selectable.Transition.None;

        RectTransform card = CreatePanel(overlayRoot, "DetailModalCard", roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(260f, -250f), new Vector2(1320f, 580f));
        card.GetComponent<Image>().raycastTarget = true;
        Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color32(20, 25, 31, 48);
        cardShadow.effectDistance = new Vector2(0f, -6f);
        cardShadow.useGraphicAlpha = false;

        RectTransform iconPlate = CreatePanel(card, "DetailIconPlate", roundedRectangle,
            new Color32(255, 241, 241, 255), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(50f, -35f), new Vector2(72f, 72f));
        for (int i = 0; i < partButtons.Length; i++)
        {
            if (i == 0)
            {
                RectTransform chipIcon = CreateRect(iconPlate, "ModalPartIcon_0",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
                Color chipRed = new Color32(211, 28, 31, 255);
                CreateStrokeRectangle(chipIcon, "ChipBody", roundedRectangle, chipRed, Vector2.zero,
                    new Vector2(32f, 32f), 2.7f);
                CreateStrokeRectangle(chipIcon, "ChipCore", roundedRectangle, chipRed, Vector2.zero,
                    new Vector2(19f, 19f), 2.3f);
                for (int pin = -1; pin <= 1; pin++)
                {
                    float offset = pin * 10f;
                    CreateRoundedLine(chipIcon, "PinTop" + pin, new Vector2(offset, 16f),
                        new Vector2(offset, 23f), 2.5f, roundedRectangle, chipRed);
                    CreateRoundedLine(chipIcon, "PinBottom" + pin, new Vector2(offset, -16f),
                        new Vector2(offset, -23f), 2.5f, roundedRectangle, chipRed);
                    CreateRoundedLine(chipIcon, "PinLeft" + pin, new Vector2(-16f, offset),
                        new Vector2(-23f, offset), 2.5f, roundedRectangle, chipRed);
                    CreateRoundedLine(chipIcon, "PinRight" + pin, new Vector2(16f, offset),
                        new Vector2(23f, offset), 2.5f, roundedRectangle, chipRed);
                }
                chipIcon.gameObject.SetActive(false);
                continue;
            }

            Transform sourceIcon = partButtons[i] != null ? partButtons[i].transform.Find("PartIcon") : null;
            if (sourceIcon == null)
            {
                continue;
            }

            GameObject iconClone = Object.Instantiate(sourceIcon.gameObject, iconPlate, false);
            iconClone.name = "ModalPartIcon_" + i;
            RectTransform iconRect = iconClone.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(60f, 60f);
            iconRect.localScale = Vector3.one;
            iconClone.SetActive(false);
        }

        selectedLabel = CreateText(card, "SelectedPartLabel", string.Empty, 34f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(150f, -35f), new Vector2(600f, 72f),
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        selectedLabel.color = Color.black;
        selectedLabel.fontWeight = FontWeight.Black;
        selectedLabel.textWrappingMode = TextWrappingModes.NoWrap;
        Outline selectedLabelWeight = selectedLabel.gameObject.AddComponent<Outline>();
        selectedLabelWeight.effectColor = Color.black;
        selectedLabelWeight.effectDistance = new Vector2(0.45f, -0.45f);
        selectedLabelWeight.useGraphicAlpha = true;

        CreatePanel(card, "HeaderDivider", null, new Color32(219, 221, 224, 255),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50f, -128f), new Vector2(730f, 1f));

        RectTransform descriptionPanel = CreatePanel(card, "DescriptionPanel", roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(50f, -148f), new Vector2(730f, 250f));
        Outline descriptionOutline = descriptionPanel.gameObject.AddComponent<Outline>();
        descriptionOutline.effectColor = new Color32(224, 226, 229, 255);
        descriptionOutline.effectDistance = new Vector2(1f, -1f);
        descriptionOutline.useGraphicAlpha = false;

        descriptionText = CreateText(descriptionPanel, "PartDescriptionText", string.Empty, 30f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -14f), new Vector2(682f, 220f),
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        descriptionText.color = Color.black;
        descriptionText.fontWeight = FontWeight.Bold;
        descriptionText.lineSpacing = 5f;
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.richText = true;

        RectTransform previewFrame = CreatePanel(card, "ModalPreviewFrame", roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(815f, -36f), new Vector2(455f, 455f));
        Outline previewOutline = previewFrame.gameObject.AddComponent<Outline>();
        previewOutline.effectColor = new Color32(224, 226, 229, 255);
        previewOutline.effectDistance = new Vector2(1f, -1f);
        previewOutline.useGraphicAlpha = false;
        Shadow previewShadow = previewFrame.gameObject.AddComponent<Shadow>();
        previewShadow.effectColor = new Color32(28, 34, 40, 18);
        previewShadow.effectDistance = new Vector2(0f, -2f);
        previewShadow.useGraphicAlpha = false;

        GameObject modalPreviewObject = new GameObject("ModalPreview", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        modalPreviewObject.transform.SetParent(previewFrame, false);
        modalPreviewObject.layer = 5;
        RectTransform modalPreviewRect = modalPreviewObject.GetComponent<RectTransform>();
        modalPreviewRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalPreviewRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalPreviewRect.pivot = new Vector2(0.5f, 0.5f);
        modalPreviewRect.anchoredPosition = Vector2.zero;
        modalPreviewRect.sizeDelta = new Vector2(420f, 340f);
        RawImage modalPreview = modalPreviewObject.GetComponent<RawImage>();
        modalPreview.color = Color.white;
        modalPreview.raycastTarget = false;

        Button previousButton = CreateButton(card, "PreviousPartButton", string.Empty, roundedRectangle,
            new Vector2(0f, 1f), new Vector2(55f, -510f), new Vector2(430f, 48f), 21f);
        ConfigureModalNavigationButton(previousButton, TextAlignmentOptions.MidlineLeft);

        Button nextButton = CreateButton(card, "NextPartButton", string.Empty, roundedRectangle,
            new Vector2(0f, 1f), new Vector2(835f, -510f), new Vector2(430f, 48f), 21f);
        ConfigureModalNavigationButton(nextButton, TextAlignmentOptions.MidlineRight);

        RectTransform dots = CreateRect(card, "PageDots", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(660f, -533f), new Vector2(180f, 20f));
        for (int i = 0; i < Labels.Length; i++)
        {
            CreatePanel(dots, "Dot_" + i, roundedRectangle, new Color32(201, 202, 204, 255),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-48f + i * 16f, 0f), new Vector2(9f, 9f));
        }

        RectTransform closeRect = CreatePanel(overlayRoot, "ShowPartListButton", circle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(1630f, -286f), new Vector2(72f, 72f));
        Image closeImage = closeRect.GetComponent<Image>();
        closeImage.raycastTarget = true;
        Shadow closeShadow = closeRect.gameObject.AddComponent<Shadow>();
        closeShadow.effectColor = new Color32(20, 25, 31, 48);
        closeShadow.effectDistance = new Vector2(0f, -4f);
        closeShadow.useGraphicAlpha = false;
        closeButton = closeRect.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        ColorBlock closeColors = closeButton.colors;
        closeColors.normalColor = Color.white;
        closeColors.highlightedColor = new Color32(246, 246, 247, 255);
        closeColors.pressedColor = new Color32(229, 231, 234, 255);
        closeButton.colors = closeColors;

        TextMeshProUGUI closeGlyph = CreateText(closeRect, "CloseGlyph", "\u00D7", 42f,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Bold);
        closeGlyph.color = Color.black;
        closeGlyph.fontWeight = FontWeight.Black;

        overlayRoot.gameObject.SetActive(false);
    }

    private static void ConfigureModalNavigationButton(Button button, TextAlignmentOptions alignment)
    {
        Image image = button.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        button.GetComponent<Shadow>().enabled = false;
        button.GetComponent<Outline>().enabled = false;

        TextMeshProUGUI label = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.color = AccentRed;
            label.fontSize = 21f;
            label.fontWeight = FontWeight.Black;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Outline labelWeight = label.gameObject.AddComponent<Outline>();
            labelWeight.effectColor = AccentRed;
            labelWeight.effectDistance = new Vector2(0.45f, -0.45f);
            labelWeight.useGraphicAlpha = true;
        }
    }

    internal static void CreateBookmarkIcon(RectTransform parent)
    {
        RectTransform icon = CreateRect(parent, "PageTwoTitleIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(80f, -133f), new Vector2(64f, 64f));

        string iconPath = "Assets/Resources/UI/BookmarkIcon.png";

        // Đảm bảo Unity đã import file ảnh
        if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(iconPath))
        {
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
        }

        // Thiết lập Texture type = Sprite
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite ||
                                 importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (iconSprite == null)
        {
            Debug.LogWarning("[PageTwoSceneSetup] Could not load BookmarkIcon.png as Sprite at: " + iconPath);
        }

        Image img = CreatePanel(icon, "Image", iconSprite, Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
            .GetComponent<Image>();
        if (img != null)
        {
            img.preserveAspect = true;
        }
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
        Vector2 anchor, Vector2 position, Vector2 size, float fontSize, int partIndex = -1)
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

        TextMeshProUGUI text = CreateText(rect, "Label", label, fontSize,
            partIndex >= 0 ? new Vector2(0f, 0.5f) : Vector2.zero,
            partIndex >= 0 ? new Vector2(0f, 0.5f) : Vector2.one,
            partIndex >= 0 ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
            partIndex >= 0 ? new Vector2(103f, 0f) : Vector2.zero,
            partIndex >= 0 ? new Vector2(300f, 70f) : Vector2.zero,
            partIndex >= 0 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center,
            partIndex >= 0 ? FontStyles.Bold : FontStyles.Normal);
        text.color = Color.black;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (partIndex >= 0)
        {
            if (partIndex == 1)
            {
                text.fontSize = 24f;
            }
            text.fontWeight = FontWeight.Black;
            Outline textWeight = text.gameObject.AddComponent<Outline>();
            textWeight.effectColor = Color.black;
            textWeight.effectDistance = new Vector2(0.45f, -0.45f);
            textWeight.useGraphicAlpha = true;
            RectTransform numberCircle = CreatePanel(rect, "NumberCircle", roundedRectangle, new Color32(246, 249, 255, 255),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(47f, 0f), new Vector2(36f, 36f));
            Outline numberOutline = numberCircle.gameObject.AddComponent<Outline>();
            numberOutline.effectColor = new Color32(35, 83, 190, 255);
            numberOutline.effectDistance = new Vector2(1f, -1f);
            numberOutline.useGraphicAlpha = false;
            TextMeshProUGUI number = CreateText(numberCircle, "Number", (partIndex + 1).ToString(), 18f,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center, FontStyles.Bold);
            number.color = new Color32(35, 83, 190, 255);
            number.fontWeight = FontWeight.Black;
            Outline numberWeight = number.gameObject.AddComponent<Outline>();
            numberWeight.effectColor = new Color32(35, 83, 190, 255);
            numberWeight.effectDistance = new Vector2(0.35f, -0.35f);
            numberWeight.useGraphicAlpha = true;
            CreatePartIcon(rect, partIndex, roundedRectangle);
        }
        return button;
    }

    private static void CreatePartIcon(RectTransform parent, int partIndex, Sprite roundedRectangle)
    {
        RectTransform icon = CreateRect(parent, "PartIcon", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-43f, 0f), new Vector2(60f, 60f));
        Color red = new Color32(211, 28, 31, 255);
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circle == null)
        {
            circle = roundedRectangle;
        }

        if (partIndex == 0 || partIndex == 4)
        {
            string prefix = partIndex == 0 ? "Plc" : "Breaker";
            CreateStrokeRectangle(icon, prefix + "Body", roundedRectangle, red, Vector2.zero,
                new Vector2(32f, 48f), 2.7f);
            CreateStrokeRectangle(icon, prefix + "Screen", roundedRectangle, red, new Vector2(0f, 7f),
                new Vector2(13f, 21f), 2.3f);
            CreateRoundedLine(icon, prefix + "Divider", new Vector2(-11f, -10f), new Vector2(11f, -10f),
                2.2f, roundedRectangle, red);
            for (int i = 0; i < 3; i++)
            {
                CreateCircle(icon, prefix + "Terminal" + i, circle, red,
                    new Vector2(-8f + i * 8f, -17f), 4.2f);
            }
            return;
        }

        if (partIndex == 1)
        {
            CreateStrokeRectangle(icon, "HmiBody", roundedRectangle, red, new Vector2(0f, 4f),
                new Vector2(48f, 34f), 2.7f);
            CreateStrokeRectangle(icon, "HmiScreen", roundedRectangle, red, new Vector2(0f, 6f),
                new Vector2(36f, 22f), 2f);
            CreateRoundedLine(icon, "HmiBase", new Vector2(-17f, -17f), new Vector2(17f, -17f),
                2.7f, roundedRectangle, red);

            CreateRoundedLine(icon, "IndexFinger", new Vector2(2f, 5f), new Vector2(2f, -12f),
                5.3f, roundedRectangle, red);
            CreateRoundedLine(icon, "MiddleFinger", new Vector2(7f, -4f), new Vector2(7f, -12f),
                5.1f, roundedRectangle, red);
            CreateRoundedLine(icon, "RingFinger", new Vector2(12f, -5f), new Vector2(12f, -13f),
                5f, roundedRectangle, red);
            CreateRoundedLine(icon, "LittleFinger", new Vector2(17f, -7f), new Vector2(17f, -14f),
                4.8f, roundedRectangle, red);
            CreateRoundedLine(icon, "Thumb", new Vector2(1f, -8f), new Vector2(-7f, -4f),
                5.3f, roundedRectangle, red);
            CreatePanel(icon, "Palm", roundedRectangle, red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(9f, -13f), new Vector2(20f, 13f));
            return;
        }

        if (partIndex == 2)
        {
            CreateStrokeRectangle(icon, "MotorBody", roundedRectangle, red, Vector2.zero,
                new Vector2(36f, 30f), 2.7f);
            CreateRoundedLine(icon, "LeftCap", new Vector2(-21f, -10f), new Vector2(-21f, 10f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "RightCap", new Vector2(21f, -10f), new Vector2(21f, 10f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "LeftShaft", new Vector2(-29f, 0f), new Vector2(-22f, 0f),
                4f, roundedRectangle, red);
            CreateRoundedLine(icon, "RightShaft", new Vector2(22f, 0f), new Vector2(29f, 0f),
                4f, roundedRectangle, red);
            for (int i = -2; i <= 2; i++)
            {
                CreateRoundedLine(icon, "Fin" + i, new Vector2(-13f, i * 4f), new Vector2(13f, i * 4f),
                    2f, roundedRectangle, red);
            }
            CreateRoundedLine(icon, "HandleTop", new Vector2(-7f, 18f), new Vector2(7f, 18f),
                2.3f, roundedRectangle, red);
            CreateRoundedLine(icon, "HandleLeft", new Vector2(-7f, 15f), new Vector2(-7f, 18f),
                2.3f, roundedRectangle, red);
            CreateRoundedLine(icon, "HandleRight", new Vector2(7f, 15f), new Vector2(7f, 18f),
                2.3f, roundedRectangle, red);
            CreateRoundedLine(icon, "LeftFoot", new Vector2(-15f, -18f), new Vector2(-6f, -18f),
                2.8f, roundedRectangle, red);
            CreateRoundedLine(icon, "RightFoot", new Vector2(6f, -18f), new Vector2(15f, -18f),
                2.8f, roundedRectangle, red);
            return;
        }

        if (partIndex == 3)
        {
            CreateRoundedLine(icon, "Horizontal", new Vector2(-27f, 0f), new Vector2(27f, 0f),
                2.3f, roundedRectangle, red);
            CreateRoundedLine(icon, "Vertical", new Vector2(0f, -27f), new Vector2(0f, 27f),
                2.3f, roundedRectangle, red);
            CreateCircleOutline(icon, "OuterRing", circle, red, Vector2.zero, 43f, 2.6f);
            CreateCircleOutline(icon, "InnerRing", circle, red, Vector2.zero, 19f, 2.4f);
            CreateCircle(icon, "Center", circle, red, Vector2.zero, 7f);
            return;
        }

        if (partIndex == 5)
        {
            CreateRoundedLine(icon, "CableLeft", new Vector2(-14f, 12f), new Vector2(-14f, -6f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "CableCurveLeft", new Vector2(-14f, -6f), new Vector2(-9f, -14f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "CableBottom", new Vector2(-9f, -14f), new Vector2(3f, -14f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "CableCurveRight", new Vector2(3f, -14f), new Vector2(10f, -6f),
                3.4f, roundedRectangle, red);
            CreateRoundedLine(icon, "CableRight", new Vector2(10f, -6f), new Vector2(10f, 12f),
                3.4f, roundedRectangle, red);
            CreatePanel(icon, "LeftPlug", roundedRectangle, red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-14f, 17f), new Vector2(10f, 12f));
            CreatePanel(icon, "RightPlug", roundedRectangle, red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(10f, 17f), new Vector2(10f, 12f));
            CreateRoundedLine(icon, "LeftPinA", new Vector2(-17f, 23f), new Vector2(-17f, 27f),
                2f, roundedRectangle, red);
            CreateRoundedLine(icon, "LeftPinB", new Vector2(-11f, 23f), new Vector2(-11f, 27f),
                2f, roundedRectangle, red);
            CreateRoundedLine(icon, "RightPinA", new Vector2(7f, 23f), new Vector2(7f, 27f),
                2f, roundedRectangle, red);
            CreateRoundedLine(icon, "RightPinB", new Vector2(13f, 23f), new Vector2(13f, 27f),
                2f, roundedRectangle, red);
            return;
        }

        CreateStrokeRectangle(icon, "TerminalBoard", roundedRectangle, red, new Vector2(0f, -7f),
            new Vector2(52f, 23f), 2.7f);
        for (int i = 0; i < 5; i++)
        {
            float x = -20f + i * 10f;
            CreateCircleOutline(icon, "Socket" + i, circle, red, new Vector2(x, -8f), 7f, 1.7f);
            CreateRoundedLine(icon, "Pin" + i, new Vector2(x, 5f), new Vector2(x, 18f),
                2.3f, roundedRectangle, red);
            CreateCircle(icon, "PinHead" + i, circle, red, new Vector2(x, 19f), 4.5f);
            CreateRoundedLine(icon, "PinCollar" + i, new Vector2(x - 3f, 7f), new Vector2(x + 3f, 7f),
                2.1f, roundedRectangle, red);
        }
    }

    private static RectTransform CreateStrokeRectangle(RectTransform parent, string name, Sprite roundedRectangle,
        Color color, Vector2 position, Vector2 size, float thickness)
    {
        RectTransform root = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, size);
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        float inset = thickness * 0.5f;
        CreateRoundedLine(root, "Top", new Vector2(-halfWidth + inset, halfHeight - inset),
            new Vector2(halfWidth - inset, halfHeight - inset), thickness, roundedRectangle, color);
        CreateRoundedLine(root, "Bottom", new Vector2(-halfWidth + inset, -halfHeight + inset),
            new Vector2(halfWidth - inset, -halfHeight + inset), thickness, roundedRectangle, color);
        CreateRoundedLine(root, "Left", new Vector2(-halfWidth + inset, -halfHeight + inset),
            new Vector2(-halfWidth + inset, halfHeight - inset), thickness, roundedRectangle, color);
        CreateRoundedLine(root, "Right", new Vector2(halfWidth - inset, -halfHeight + inset),
            new Vector2(halfWidth - inset, halfHeight - inset), thickness, roundedRectangle, color);
        return root;
    }

    private static void CreateCircle(RectTransform parent, string name, Sprite circle, Color color,
        Vector2 position, float diameter)
    {
        CreatePanel(parent, name, circle, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, new Vector2(diameter, diameter));
    }

    private static void CreateCircleOutline(RectTransform parent, string name, Sprite circle, Color color,
        Vector2 position, float diameter, float thickness)
    {
        RectTransform root = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, new Vector2(diameter, diameter));
        CreatePanel(root, "Outer", circle, color, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreatePanel(root, "Inner", circle, Color.white, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-thickness * 2f, -thickness * 2f));
    }

    private static void CreateRoundedLine(RectTransform parent, string name, Vector2 start, Vector2 end,
        float thickness, Sprite roundedRectangle, Color color)
    {
        Vector2 delta = end - start;
        RectTransform line = CreatePanel(parent, name, roundedRectangle, color,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            (start + end) * 0.5f, new Vector2(delta.magnitude, thickness));
        line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void CreateOverviewFrame(RectTransform parent, Sprite roundedRectangle)
    {
        RectTransform viewport = CreatePanel(parent, "ModelViewport", roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(865f, -198f),
            new Vector2(866f, 700f));
        Image viewportMaskImage = viewport.GetComponent<Image>();
        viewportMaskImage.type = roundedRectangle != null ? Image.Type.Sliced : Image.Type.Simple;
        viewportMaskImage.raycastTarget = false;
        Mask viewportMask = viewport.gameObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject previewObject = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        previewObject.transform.SetParent(viewport, false);
        previewObject.layer = 5;
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = Vector2.zero;
        previewRect.sizeDelta = Vector2.zero;
        RawImage previewImage = previewObject.GetComponent<RawImage>();
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;

        RectTransform frame = CreatePanel(parent, "OverviewFrame", roundedRectangle, new Color32(255, 255, 255, 0),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(865f, -198f),
            new Vector2(866f, 700f));
        Shadow shadow = frame.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(30, 36, 42, 20);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = false;
        Outline outline = frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(225, 227, 230, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
        // Unity UI Outline duplicates the whole rounded sprite. Keep that copy behind
        // the RenderTexture viewport so it acts as a border instead of a gray cover.
        frame.SetSiblingIndex(viewport.GetSiblingIndex());

        RectTransform badge = CreatePanel(parent, "OverviewBadge", roundedRectangle, new Color32(244, 247, 255, 255),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1298f, -198f),
            new Vector2(228f, 40f));
        Outline badgeOutline = badge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = new Color32(35, 83, 190, 255);
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        badgeOutline.useGraphicAlpha = false;
        TextMeshProUGUI badgeText = CreateText(badge, "Label", "M\u00F4 h\u00ECnh t\u1ED5ng quan", 21f,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Bold);
        badgeText.color = new Color32(35, 83, 190, 255);
        badgeText.fontWeight = FontWeight.Black;
        Outline badgeWeight = badgeText.gameObject.AddComponent<Outline>();
        badgeWeight.effectColor = new Color32(35, 83, 190, 255);
        badgeWeight.effectDistance = new Vector2(0.3f, -0.3f);
        badgeWeight.useGraphicAlpha = true;
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

    internal static void CreateCircuitBackground(RectTransform parent)
    {
        RectTransform background = CreateRect(parent.transform, "CircuitBackground", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = PageBackground;
        backgroundImage.raycastTarget = false;

        RectTransform traces = CreateRect(background, "CircuitTraces", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

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
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(dot.x, -dot.y), new Vector2(7f, 7f));
            point.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private static void CreateCircuitLine(RectTransform parent, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        RectTransform line = CreatePanel(parent, "CircuitLine", null, color,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(start.x, -start.y), new Vector2(delta.magnitude, thickness));
        line.pivot = new Vector2(0f, 0.5f);
        line.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
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
