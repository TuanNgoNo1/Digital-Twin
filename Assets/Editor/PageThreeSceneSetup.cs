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
    private const string DiagramTexturePath = "Assets/Resources/UI/ClosedLoopServoDiagram.png";
    private const string LayoutMarker = "ReferenceLayoutV8";

    private static readonly Color32 AccentRed = new Color32(194, 28, 29, 255);
    private static readonly Color32 Blue = new Color32(35, 83, 190, 255);
    private static readonly Color32 SignalRed = new Color32(230, 0, 0, 255);
    private static readonly Color32 FeedbackGreen = new Color32(0, 132, 34, 255);

    private static readonly string[] StepTitles =
    {
        "Ng\u01B0\u1EDDi v\u1EADn h\u00E0nh nh\u1EADp l\u1EC7nh tr\u00EAn HMI",
        "PLC x\u1EED l\u00FD l\u1EC7nh v\u00E0 ph\u00E1t xung t\u1ED1c \u0111\u1ED9 cao \u0111\u1EBFn Servo Driver",
        "Driver khu\u1EBFch \u0111\u1EA1i v\u00E0 bi\u1EBFn \u0111\u1ED5i t\u00EDn hi\u1EC7u",
        "Encoder g\u1EAFn v\u1EDBi tr\u1EE5c \u0111\u1ED9ng c\u01A1 t\u1EA1o xung ph\u1EA3n h\u1ED3i v\u1EC1 PLC"
    };

    private static readonly string[] StepDescriptions =
    {
        "C\u00E1c l\u1EC7nh \u0111i\u1EC1u khi\u1EC3n \u0111\u01B0\u1EE3c nh\u1EADp v\u00E0 g\u1EEDi t\u1EDBi PLC th\u00F4ng qua\nm\u00E0n h\u00ECnh HMI.",
        "PLC x\u1EED l\u00FD t\u00EDn hi\u1EC7u \u0111i\u1EC1u khi\u1EC3n v\u00E0 ph\u00E1t xung t\u1ED1c \u0111\u1ED9 cao (pulse) \u0111\u1EBFn\nServo Driver.",
        "Driver nh\u1EADn t\u00EDn hi\u1EC7u t\u1EEB PLC, khu\u1EBFch \u0111\u1EA1i v\u00E0 bi\u1EBFn \u0111\u1ED5i th\u00E0nh \u0111i\u1EC7n \u00E1p\nba pha c\u1EA5p cho \u0111\u1ED9ng c\u01A1 BLDC Servo.",
        "\u2192 H\u1EC7 th\u1ED1ng x\u00E1c \u0111\u1ECBnh \u0111\u01B0\u1EE3c chi\u1EC1u quay, t\u1ED1c \u0111\u1ED9 v\u00E0 v\u1ECB tr\u00ED th\u1EF1c t\u1EBF.\n" +
        "\u2192 Gi\u1EA3m sai l\u1EC7ch so v\u1EDBi gi\u00E1 tr\u1ECB \u0111\u1EB7t."
    };

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

        Transform content = FindInScene(scene, "Trang 3")?.transform.Find("PageThreeContent");
        if (content == null || content.Find(LayoutMarker) == null)
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
            throw new System.InvalidOperationException(
                "Exit Play Mode before rebuilding the operating principle page.");
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
        Texture2D diagramTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DiagramTexturePath);
        RectTransform content = CreateRect(page.transform, "PageThreeContent", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateRect(content, LayoutMarker, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

        PageTwoSceneSetup.CreateCircuitBackground(content);
        PageTwoSceneSetup.CreateTopHeader(content);
        Transform sharedHeader = content.Find("PageTwoHeader");
        if (sharedHeader != null)
        {
            sharedHeader.name = "PageThreeHeader";
        }

        PageTwoSceneSetup.CreateBookmarkIcon(content);
        Transform sharedBookmark = content.Find("PageTwoTitleIcon");
        if (sharedBookmark != null)
        {
            sharedBookmark.name = "PageThreeTitleIcon";
        }

        TextMeshProUGUI title = CreateText(content, "PageThreeTitle",
            "NGUY\u00CAN L\u00DD HO\u1EA0T \u0110\u1ED8NG", 44f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(140f, -101f), new Vector2(900f, 72f),
            TextAlignmentOptions.Left, FontStyles.Bold);
        title.color = AccentRed;
        title.fontWeight = FontWeight.Black;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        Outline titleWeight = title.gameObject.AddComponent<Outline>();
        titleWeight.effectColor = AccentRed;
        titleWeight.effectDistance = new Vector2(1.5f, -1.5f);
        titleWeight.useGraphicAlpha = true;

        Transform pageTwoContent = FindInScene(scene, "Trang 2")?.transform.Find("PageTwoContent");
        float[] cardTops = { -196f, -363f, -530f, -697f };
        for (int i = 0; i < StepTitles.Length; i++)
        {
            CreateStepCard(content, roundedRectangle, pageTwoContent, i, cardTops[i]);
        }

        CreateDiagramImage(content, roundedRectangle, diagramTexture);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PageThreeSceneSetup] Rebuilt the operating principle page.");
    }

    private static void CreateStepCard(RectTransform parent, Sprite roundedRectangle, Transform pageTwoContent,
        int stepIndex, float top)
    {
        RectTransform card = CreateCard(parent, "PrincipleStep_" + (stepIndex + 1), roundedRectangle,
            new Vector2(46f, top), new Vector2(990f, 154f));

        CreateNumberBadge(card, roundedRectangle, stepIndex + 1);

        TextMeshProUGUI heading = CreateText(card, "Heading", StepTitles[stepIndex], 29f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(72f, -18f), new Vector2(826f, 48f),
            TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        heading.color = Color.black;
        heading.fontWeight = FontWeight.Black;
        heading.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI body = CreateText(card, "Description", StepDescriptions[stepIndex], 26f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(72f, -67f), new Vector2(816f, 74f),
            TextAlignmentOptions.TopLeft, FontStyles.Normal);
        body.color = Color.black;
        body.fontWeight = FontWeight.Medium;
        body.lineSpacing = 1f;
        body.textWrappingMode = TextWrappingModes.Normal;

        CloneStepIcon(card, roundedRectangle, pageTwoContent, stepIndex);
    }

    private static void CreateNumberBadge(RectTransform card, Sprite roundedRectangle, int number)
    {
        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        RectTransform badge = CreatePanel(card, "StepNumber", circle != null ? circle : roundedRectangle,
            new Color32(246, 249, 255, 255), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(30f, -37f), new Vector2(38f, 38f));
        Image badgeImage = badge.GetComponent<Image>();
        badgeImage.type = Image.Type.Simple;
        Outline badgeOutline = badge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = Blue;
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        badgeOutline.useGraphicAlpha = false;

        TextMeshProUGUI label = CreateText(badge, "Number", number.ToString(), 18f,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Bold);
        label.color = Blue;
        label.fontWeight = FontWeight.Black;
    }

    private static void CloneStepIcon(RectTransform card, Sprite roundedRectangle, Transform pageTwoContent,
        int stepIndex)
    {
        string relativePath;
        switch (stepIndex)
        {
            case 0:
                relativePath = "PartButtonList/PartButton_1/PartIcon";
                break;
            case 1:
                relativePath = "DetailOverlayRoot/DetailModalCard/DetailIconPlate/ModalPartIcon_0";
                break;
            case 2:
                relativePath = "PartButtonList/PartButton_2/PartIcon";
                break;
            default:
                relativePath = "PartButtonList/PartButton_3/PartIcon";
                break;
        }

        Transform source = pageTwoContent?.Find(relativePath);
        if (source != null)
        {
            GameObject icon = Object.Instantiate(source.gameObject, card, false);
            icon.name = "StepIcon";
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1f, 0.5f);
            iconRect.anchorMax = new Vector2(1f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-54f, 0f);
            iconRect.sizeDelta = new Vector2(60f, 60f);
            iconRect.localScale = Vector3.one;
            icon.SetActive(true);
            return;
        }

        RectTransform fallback = CreatePanel(card, "StepIcon", roundedRectangle, AccentRed,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-54f, 0f), new Vector2(42f, 42f));
        CreatePanel(fallback, "Core", roundedRectangle, Color.white, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-7f, -7f));
    }

    private static void CreateDiagramImage(RectTransform parent, Sprite roundedRectangle,
        Texture2D diagramTexture)
    {
        // The source screenshot contains an opaque warm-white page around the actual diagram.
        // Crop and mask the card/title independently so that only the component is composited
        // over this page's circuit background.
        RectTransform cardMask = CreatePanel(parent, "ClosedLoopDiagramCardImage", roundedRectangle,
            Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(1067f, -208f), new Vector2(802f, 426f));
        cardMask.GetComponent<Image>().raycastTarget = false;
        Mask cardClip = cardMask.gameObject.AddComponent<Mask>();
        cardClip.showMaskGraphic = true;
        CreateCroppedDiagramImage(cardMask, "CardPixels", diagramTexture,
            new Rect(0.0206f, 0.0764f, 0.9551f, 0.8269f));

        RectTransform titleMask = CreatePanel(parent, "ClosedLoopDiagramTitleImage", roundedRectangle,
            Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(1222f, -189f), new Vector2(458f, 37f));
        titleMask.GetComponent<Image>().raycastTarget = false;
        Mask titleClip = titleMask.gameObject.AddComponent<Mask>();
        titleClip.showMaskGraphic = false;
        CreateCroppedDiagramImage(titleMask, "TitlePixels", diagramTexture,
            new Rect(0.2041f, 0.8707f, 0.5449f, 0.0703f));
    }

    private static void CreateCroppedDiagramImage(RectTransform parent, string name,
        Texture2D diagramTexture, Rect uvRect)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        imageObject.transform.SetParent(parent, false);
        imageObject.layer = 5;
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = diagramTexture;
        image.uvRect = uvRect;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void CreateDiagramCard(RectTransform parent, Sprite roundedRectangle)
    {
        RectTransform card = CreateCard(parent, "ClosedLoopDiagramCard", roundedRectangle,
            new Vector2(1072f, -196f), new Vector2(798f, 427f));

        RectTransform diagram = CreateRect(card, "ClosedLoopDiagram", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        CreateDiagramArrow(diagram, "CommandArrow", new Vector2(190f, 187f), new Vector2(316f, 187f),
            SignalRed, 8f, 17f);
        CreateDiagramArrow(diagram, "PowerArrow", new Vector2(390f, 88f), new Vector2(390f, 128f),
            Blue, 7f, 16f);
        CreateDiagramArrow(diagram, "MotorArrowTop", new Vector2(464f, 158f), new Vector2(574f, 158f),
            Blue, 7f, 17f);
        CreateDiagramArrow(diagram, "MotorArrowBottom", new Vector2(464f, 218f), new Vector2(574f, 218f),
            Blue, 7f, 17f);

        CreateDashedDiagramLine(diagram, "FeedbackTwoDown", new Vector2(389f, 246f),
            new Vector2(389f, 310f), FeedbackGreen, 4f);
        CreateDashedDiagramLine(diagram, "FeedbackTwoAcross", new Vector2(389f, 310f),
            new Vector2(107f, 310f), FeedbackGreen, 4f);
        CreateDashedDiagramLine(diagram, "FeedbackTwoUp", new Vector2(107f, 310f),
            new Vector2(107f, 246f), FeedbackGreen, 4f);
        CreateDiagramArrowHead(diagram, "FeedbackTwoArrow", new Vector2(107f, 310f),
            new Vector2(107f, 246f), FeedbackGreen, 13f, 4f);

        CreateDashedDiagramLine(diagram, "FeedbackOneDown", new Vector2(749f, 214f),
            new Vector2(749f, 310f), FeedbackGreen, 4f);
        CreateDashedDiagramLine(diagram, "FeedbackOneAcross", new Vector2(749f, 310f),
            new Vector2(419f, 310f), FeedbackGreen, 4f);
        CreateDashedDiagramLine(diagram, "FeedbackOneUp", new Vector2(419f, 310f),
            new Vector2(419f, 246f), FeedbackGreen, 4f);
        CreateDiagramArrowHead(diagram, "FeedbackOneArrow", new Vector2(419f, 310f),
            new Vector2(419f, 246f), FeedbackGreen, 13f, 4f);

        CreateDiagramBlock(diagram, roundedRectangle, "PlcBlock", new Vector2(28f, 128f),
            new Vector2(162f, 118f), "PLC\n(CONTROLLER)", 17f);
        CreateDiagramBlock(diagram, roundedRectangle, "ServoPackBlock", new Vector2(316f, 128f),
            new Vector2(148f, 118f), "SERVO\nPACK\n(AMPLIFIER)", 19f);
        CreateDiagramBlock(diagram, roundedRectangle, "ServoMotorBlock", new Vector2(574f, 128f),
            new Vector2(145f, 112f), "SERVO\nMOTOR", 20f);

        CreateDiagramLine(diagram, "MotorTopFlange", new Vector2(566f, 120f), new Vector2(727f, 120f),
            Blue, 3f);
        CreateDiagramLine(diagram, "MotorBottomFlange", new Vector2(566f, 248f), new Vector2(727f, 248f),
            Blue, 3f);
        CreateDiagramLine(diagram, "MotorShaftTop", new Vector2(534f, 163f), new Vector2(574f, 163f),
            Blue, 3f);
        CreateDiagramLine(diagram, "MotorShaftMiddle", new Vector2(526f, 184f), new Vector2(574f, 184f),
            Blue, 3f);
        CreateDiagramLine(diagram, "MotorShaftBottom", new Vector2(534f, 205f), new Vector2(574f, 205f),
            Blue, 3f);
        CreateDiagramLine(diagram, "MotorToEncoder", new Vector2(719f, 184f), new Vector2(724f, 184f),
            Blue, 3f);

        Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        RectTransform encoder = CreatePanel(diagram, "Encoder", circle != null ? circle : roundedRectangle,
            Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(754f, -184f), new Vector2(60f, 60f));
        encoder.GetComponent<Image>().type = Image.Type.Simple;
        Outline encoderOutline = encoder.gameObject.AddComponent<Outline>();
        encoderOutline.effectColor = Blue;
        encoderOutline.effectDistance = new Vector2(2f, -2f);
        encoderOutline.useGraphicAlpha = false;
        TextMeshProUGUI encoderLabel = CreateText(encoder, "Label", "ENCODER", 10f, Vector2.zero,
            Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Bold);
        encoderLabel.fontWeight = FontWeight.Black;

        CreateDiagramLabel(diagram, "PowerLabel", "POWER", 18f, new Vector2(335f, 52f),
            new Vector2(110f, 30f), Color.black);
        CreateDiagramLabel(diagram, "CommandLabel", "COMMAND\nSIGNAL", 17f, new Vector2(196f, 96f),
            new Vector2(112f, 58f), Color.black);
        CreateDiagramLabel(diagram, "FeedbackTwoLabel", "FEEDBACK 2\n(STATUS)", 16f,
            new Vector2(142f, 260f), new Vector2(190f, 46f), FeedbackGreen);
        CreateDiagramLabel(diagram, "FeedbackOneLabel", "FEEDBACK 1\n(POSITION/SPEED)", 16f,
            new Vector2(511f, 260f), new Vector2(220f, 46f), FeedbackGreen);
        CreateDiagramLabel(diagram, "ClosedLoopLabel", "CLOSED-LOOP SERVO SYSTEM", 18f,
            new Vector2(218f, 365f), new Vector2(365f, 32f), Color.black);

        RectTransform badge = CreatePanel(card, "DiagramBadge", roundedRectangle,
            new Color32(244, 247, 255, 255), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 42f));
        Outline badgeOutline = badge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = Blue;
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        badgeOutline.useGraphicAlpha = false;

        TextMeshProUGUI badgeText = CreateText(badge, "Label",
            "S\u01A1 \u0111\u1ED3 h\u1EC7 th\u1ED1ng Servo theo c\u1EA5u tr\u00FAc v\u00F2ng k\u00EDn", 19f,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Center, FontStyles.Bold);
        badgeText.color = Blue;
        badgeText.fontWeight = FontWeight.Black;
        badgeText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void CreateDiagramBlock(RectTransform parent, Sprite roundedRectangle, string name,
        Vector2 position, Vector2 size, string label, float fontSize)
    {
        RectTransform block = CreatePanel(parent, name, roundedRectangle, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(position.x, -position.y), size);
        Outline outline = block.gameObject.AddComponent<Outline>();
        outline.effectColor = Blue;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;
        TextMeshProUGUI text = CreateText(block, "Label", label, fontSize, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, -12f),
            TextAlignmentOptions.Center, FontStyles.Bold);
        text.fontWeight = FontWeight.Black;
        text.lineSpacing = -10f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static TextMeshProUGUI CreateDiagramLabel(RectTransform parent, string name, string value,
        float fontSize, Vector2 position, Vector2 size, Color color)
    {
        TextMeshProUGUI label = CreateText(parent, name, value, fontSize,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(position.x, -position.y), size, TextAlignmentOptions.Center, FontStyles.Bold);
        label.color = color;
        label.fontWeight = FontWeight.Black;
        label.lineSpacing = -8f;
        return label;
    }

    private static void CreateDiagramArrow(RectTransform parent, string name, Vector2 start, Vector2 end,
        Color color, float width, float headLength)
    {
        CreateDiagramLine(parent, name + "Line", start, end, color, width);
        CreateDiagramArrowHead(parent, name + "Head", start, end, color, headLength, width);
    }

    private static void CreateDiagramArrowHead(RectTransform parent, string name, Vector2 start, Vector2 end,
        Color color, float headLength, float width)
    {
        Vector2 localStart = new Vector2(start.x, -start.y);
        Vector2 localEnd = new Vector2(end.x, -end.y);
        Vector2 direction = (localEnd - localStart).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 back = localEnd - direction * headLength;
        CreateUiLine(parent, name + "A", back + perpendicular * headLength * 0.58f, localEnd, color, width);
        CreateUiLine(parent, name + "B", back - perpendicular * headLength * 0.58f, localEnd, color, width);
    }

    private static void CreateDashedDiagramLine(RectTransform parent, string name, Vector2 start, Vector2 end,
        Color color, float width)
    {
        float length = Vector2.Distance(start, end);
        if (length <= 0.01f)
        {
            return;
        }

        Vector2 direction = (end - start) / length;
        const float dashLength = 9f;
        const float gapLength = 7f;
        int dashIndex = 0;
        for (float distance = 0f; distance < length; distance += dashLength + gapLength)
        {
            Vector2 dashStart = start + direction * distance;
            Vector2 dashEnd = start + direction * Mathf.Min(distance + dashLength, length);
            CreateDiagramLine(parent, name + dashIndex, dashStart, dashEnd, color, width);
            dashIndex++;
        }
    }

    private static void CreateDiagramLine(RectTransform parent, string name, Vector2 start, Vector2 end,
        Color color, float width)
    {
        CreateUiLine(parent, name, new Vector2(start.x, -start.y), new Vector2(end.x, -end.y), color, width);
    }

    private static void CreateUiLine(RectTransform parent, string name, Vector2 start, Vector2 end,
        Color color, float width)
    {
        RectTransform line = CreatePanel(parent, name, null, color,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            (start + end) * 0.5f, new Vector2(Vector2.Distance(start, end), width));
        Vector2 direction = end - start;
        line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    private static RectTransform CreateCard(RectTransform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size)
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
        shadow.effectColor = new Color32(28, 34, 40, 22);
        shadow.effectDistance = new Vector2(0f, -3f);
        shadow.useGraphicAlpha = false;

        Outline outline = gameObject.GetComponent<Outline>();
        outline.effectColor = new Color32(225, 227, 230, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
        return rect;
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
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
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
