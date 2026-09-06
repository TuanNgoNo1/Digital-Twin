using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PageFourSceneSetup
{
    private const string ScenePath = "Assets/Scenes/StartScene.unity";
    private const string RoundedSpritePath = "Assets/Resources/UI/RoundedRect.png";
    private const string LayoutMarker = "ReferenceGuideLayoutV9";
    private static readonly Color32 AccentRed = new Color32(194, 28, 29, 255);

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

        GameObject page = FindInScene(scene, "Trang 4");
        Transform content = page != null ? page.transform.Find("PageFourContent") : null;
        if (page != null && (content == null || content.Find(LayoutMarker) == null))
        {
            BuildAndSave(scene);
        }

        if (closeAfterSetup)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Digital Twin/Rebuild practice guide page")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new System.InvalidOperationException("Exit Play Mode before rebuilding the practice guide page.");
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
        GameObject page = FindInScene(scene, "Trang 4");
        if (page == null)
        {
            throw new System.InvalidOperationException("Could not find Trang 4 in StartScene.");
        }

        Transform model = EnsureModel(scene, page.transform);
        Transform oldContent = page.transform.Find("PageFourContent");
        if (oldContent != null)
        {
            Object.DestroyImmediate(oldContent.gameObject);
        }

        Sprite roundedRectangle = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        RectTransform content = CreateRect(page.transform, "PageFourContent", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateRect(content, LayoutMarker, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

        PageTwoSceneSetup.CreateCircuitBackground(content);
        PageTwoSceneSetup.CreateTopHeader(content);
        Transform sharedHeader = content.Find("PageTwoHeader");
        if (sharedHeader != null)
        {
            sharedHeader.name = "PageFourHeader";
        }
        PageTwoSceneSetup.CreateBookmarkIcon(content);
        Transform sharedBookmark = content.Find("PageTwoTitleIcon");
        if (sharedBookmark != null)
        {
            sharedBookmark.name = "PageFourTitleIcon";
        }

        TextMeshProUGUI title = CreateText(content, "PageFourTitle",
            "H\u01AF\u1EDANG D\u00C2N TH\u1EF0C H\u00C0NH", 44f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -101f),
            new Vector2(1000f, 72f), TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = AccentRed;
        title.fontWeight = FontWeight.Black;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        Outline titleWeight = title.gameObject.AddComponent<Outline>();
        titleWeight.effectColor = AccentRed;
        titleWeight.effectDistance = new Vector2(1.5f, -1.5f);
        titleWeight.useGraphicAlpha = true;
        CreateRaisedTilde(title, roundedRectangle);

        RawImage previewImage = CreatePreviewCard(content, roundedRectangle);

        RectTransform noteCard = CreateCard(content, "PageFourNoteCard", roundedRectangle,
            new Vector2(388f, -860f), new Vector2(1060f, 84f));
        TextMeshProUGUI note = CreateText(noteCard, "PageFourNote",
            "Nh\u1EA5p l\u1EA7n l\u01B0\u1EE3t v\u00E0o hai l\u1ED7 c\u1EAFm theo h\u01B0\u1EDBng d\u1EABn. D\u00E2y n\u1ED1i s\u1EBD t\u1EF1 \u0111\u1ED9ng xu\u1EA5t hi\u1EC7n.",
            25f, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-56f, -18f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        note.color = Color.black;
        note.fontWeight = FontWeight.Bold;
        note.textWrappingMode = TextWrappingModes.NoWrap;
        note.enableAutoSizing = true;
        note.fontSizeMin = 20f;
        note.fontSizeMax = 25f;
        note.overflowMode = TextOverflowModes.Ellipsis;

        Button playButton = CreatePlayButton(content, roundedRectangle);
        RectTransform cursor = CreateHandCursor(content);
        Texture2D handIcons = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HandIcons.png");
        RawImage cursorImage = cursor.GetComponent<RawImage>();
        cursorImage.texture = handIcons;
        cursorImage.uvRect = new Rect(0f, 0f, 0.5f, 1f);
        cursorImage.color = Color.black;
        cursorImage.raycastTarget = false;

        PageFourWiringTutorialController controller = page.GetComponent<PageFourWiringTutorialController>();
        if (controller == null)
        {
            controller = page.AddComponent<PageFourWiringTutorialController>();
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("modelRoot").objectReferenceValue = model;
        serialized.FindProperty("modelPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d_Thay_Tien_1.fbx");
        serialized.FindProperty("previewImage").objectReferenceValue = previewImage;
        serialized.FindProperty("playButton").objectReferenceValue = playButton;
        serialized.FindProperty("cursorObject").objectReferenceValue = cursor;
        serialized.FindProperty("cursorImage").objectReferenceValue = cursorImage;
        serialized.FindProperty("handIconsTexture").objectReferenceValue = handIcons;
        serialized.FindProperty("connectedWirePrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/3D_Thay_Tien_Wires/Wires 1/Wire_Head_Yellow(Y0-Pin11).obj");
        serialized.FindProperty("jack35Prefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Jack 3.5mm.fbx");
        serialized.FindProperty("wireMaterial").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Red_wire.mat");
        serialized.FindProperty("jackBodyMaterial").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WirePlugOverlay/WirePlugOverlay_Red.mat");
        serialized.FindProperty("moveDuration").floatValue = 1.35f;
        serialized.FindProperty("cursorApproachDuration").floatValue = 1f;
        serialized.FindProperty("cursorTransferDuration").floatValue = 0.8f;
        serialized.FindProperty("cursorReleaseDuration").floatValue = 1f;
        serialized.FindProperty("socketALocal").vector3Value =
            new Vector3(-0.102497f, -0.025666f, 0.24773f);
        serialized.FindProperty("socketBLocal").vector3Value =
            new Vector3(-0.03568f, 0.116647f, 0.24773f);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageFourSceneSetup] Rebuilt the practice guide page.");
    }

    private static Transform EnsureModel(Scene scene, Transform page)
    {
        Transform model = page.Find("PageFourModel");
        if (model != null)
        {
            return model;
        }

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d_Thay_Tien_1.fbx");
        GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset, page) as GameObject;
        if (modelInstance == null)
        {
            throw new System.InvalidOperationException("Could not instantiate the page 4 model.");
        }

        modelInstance.name = "PageFourModel";
        Transform pageTwoModel = FindInScene(scene, "3d_Thay_Tien_1")?.transform;
        if (pageTwoModel != null)
        {
            modelInstance.transform.localPosition = pageTwoModel.localPosition;
            modelInstance.transform.localRotation = pageTwoModel.localRotation;
            modelInstance.transform.localScale = pageTwoModel.localScale;
        }
        return modelInstance.transform;
    }

    private static void CreateHeader(RectTransform content)
    {
        RectTransform header = CreatePanel(content, "PageFourHeader", null, AccentRed,
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

    private static RawImage CreatePreviewCard(RectTransform parent, Sprite roundedRectangle)
    {
        GameObject frameObject = new GameObject("PageFourPreviewCard", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Shadow), typeof(Outline), typeof(Mask));
        frameObject.transform.SetParent(parent, false);
        frameObject.layer = 5;
        RectTransform frame = frameObject.GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0f, 1f);
        frame.anchorMax = new Vector2(0f, 1f);
        frame.pivot = new Vector2(0f, 1f);
        frame.anchoredPosition = new Vector2(378f, -196f);
        frame.sizeDelta = new Vector2(1170f, 628f);

        Image frameImage = frameObject.GetComponent<Image>();
        frameImage.sprite = roundedRectangle;
        frameImage.type = roundedRectangle != null ? Image.Type.Sliced : Image.Type.Simple;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        Shadow shadow = frameObject.GetComponent<Shadow>();
        shadow.effectColor = new Color32(26, 33, 42, 34);
        shadow.effectDistance = new Vector2(0f, -7f);
        shadow.useGraphicAlpha = true;

        Outline outline = frameObject.GetComponent<Outline>();
        outline.effectColor = new Color32(222, 226, 231, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        Mask mask = frameObject.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject imageObject = new GameObject("PreviewImage", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(frame, false);
        imageObject.layer = 5;
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(3f, 3f);
        imageRect.offsetMax = new Vector2(-3f, -3f);
        RawImage previewImage = imageObject.GetComponent<RawImage>();
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;
        return previewImage;
    }

    private static Button CreatePlayButton(RectTransform parent, Sprite roundedRectangle)
    {
        GameObject gameObject = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(1504f, -902f);
        rect.sizeDelta = new Vector2(72f, 72f);
        Image image = gameObject.GetComponent<Image>();
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        image.sprite = circle != null ? circle : roundedRectangle;
        image.type = Image.Type.Simple;
        image.color = new Color32(72, 77, 82, 255);

        RectTransform face = CreatePanel(rect, "ButtonFace", circle != null ? circle : roundedRectangle,
            Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(-7f, -7f));
        Image faceImage = face.GetComponent<Image>();
        faceImage.type = Image.Type.Simple;
        Button button = gameObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(247, 248, 250, 255);
        colors.pressedColor = new Color32(232, 235, 239, 255);
        colors.selectedColor = colors.normalColor;
        button.targetGraphic = faceImage;
        button.colors = colors;
        CreatePlayIconLine(face, "PlayTop", new Vector2(-9f, 13f), new Vector2(13f, 0f));
        CreatePlayIconLine(face, "PlayBottom", new Vector2(13f, 0f), new Vector2(-9f, -13f));
        CreatePlayIconLine(face, "PlayBack", new Vector2(-9f, -13f), new Vector2(-9f, 13f));
        return button;
    }

    private static void CreatePlayIconLine(RectTransform parent, string name, Vector2 start, Vector2 end)
    {
        RectTransform line = CreatePanel(parent, name, null, Color.black,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            (start + end) * 0.5f, new Vector2(Vector2.Distance(start, end), 3.5f));
        Vector2 direction = end - start;
        line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private static RectTransform CreateHandCursor(RectTransform parent)
    {
        GameObject gameObject = new GameObject("CursorObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = 5;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(36f, 36f);
        return rect;
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
        Shadow shadow = gameObject.GetComponent<Shadow>();
        shadow.effectColor = new Color32(30, 35, 42, 20);
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
        RectTransform icon = CreateRect(parent, "PageFourTitleIcon", new Vector2(0f, 1f), new Vector2(0f, 1f),
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

    private static void CreateRaisedTilde(TextMeshProUGUI title, Sprite roundedRectangle)
    {
        title.ForceMeshUpdate();
        const int accentedCharacterIndex = 7;
        TMP_CharacterInfo character = title.textInfo.characterInfo[accentedCharacterIndex];
        Vector2 position = new Vector2(
            (character.bottomLeft.x + character.topRight.x) * 0.5f,
            character.topRight.y + 1.5f);

        RectTransform tilde = CreateRect(
            title.rectTransform,
            "PageFourTitleRaisedTilde",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f),
            position,
            new Vector2(26f, 11f));

        float[] strokeX = { -6.6f, -4.4f, -2.2f, 0f, 2.2f, 4.4f, 6.6f };
        float[] strokeY = { -0.35f, 0.55f, 0.75f, 0f, -0.75f, -0.55f, 0.35f };
        float[] strokeAngles = { 22f, 7f, -18f, -22f, -18f, 7f, 22f };
        for (int i = 0; i < strokeX.Length; i++)
        {
            RectTransform stroke = CreatePanel(
                tilde,
                "Stroke_" + (i + 1),
                roundedRectangle,
                AccentRed,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(strokeX[i], strokeY[i]),
                new Vector2(4.2f, 3.1f));
            stroke.localEulerAngles = new Vector3(0f, 0f, strokeAngles[i]);
        }
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }
}
