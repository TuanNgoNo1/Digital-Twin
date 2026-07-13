using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardStepHeading : MonoBehaviour
{
    private const string MainTitle = "Đấu nối mạch điện điều khiển động cơ Servo";

    private static readonly string[] StepTitles =
    {
        "Đấu nối mạch điều khiển động cơ",
        "Đấu nối encoder",
        "Đấu nối mạch lực động cơ"
    };

    [SerializeField] private float headingWidth = 760f;
    [SerializeField] private float headingHeight = 112f;
    [SerializeField] private float widthRelativeToBoard = 0.96f;
    [SerializeField] private float verticalGap = 0.018f;
    [SerializeField] private float cameraDepthOffset = -0.12f;
    [SerializeField] private float targetViewportTop = 0.99f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform headingRect;
    private TextMeshProUGUI stepTitleText;
    private Sprite roundedSprite;
    private Renderer boardRenderer;
    private Renderer boardFrameRenderer;
    private bool isVisible;
    private bool missingBoardWarningLogged;

    private void Awake()
    {
        BuildUi();
    }

    public void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= StepTitles.Length)
        {
            Hide();
            return;
        }

        if (headingRect == null)
            BuildUi();

        stepTitleText.text = StepTitles[stepIndex];
        isVisible = true;
        headingRect.gameObject.SetActive(true);
        PositionAsWorldObject();
    }

    public void Hide()
    {
        isVisible = false;
        if (headingRect != null)
            headingRect.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (isVisible)
            PositionAsWorldObject();
    }

    private void BuildUi()
    {
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "BoardStepHeading_Canvas",
            typeof(RectTransform),
            typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;

        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(headingWidth, headingHeight);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);

        GameObject frame = CreateImage(
            canvasObject.transform,
            "HeadingFrame",
            Color.white,
            true);
        headingRect = frame.GetComponent<RectTransform>();
        StretchRect(headingRect);

        Mask frameMask = frame.AddComponent<Mask>();
        frameMask.showMaskGraphic = true;

        GameObject redBand = CreateImage(
            frame.transform,
            "MainTitleBand",
            new Color(0.91f, 0.12f, 0.09f, 1f));
        RectTransform redRect = redBand.GetComponent<RectTransform>();
        redRect.anchorMin = new Vector2(0f, 1f);
        redRect.anchorMax = new Vector2(1f, 1f);
        redRect.pivot = new Vector2(0.5f, 1f);
        redRect.anchoredPosition = Vector2.zero;
        redRect.sizeDelta = new Vector2(0f, 60f);

        TextMeshProUGUI mainTitle = CreateText(
            redBand.transform,
            "MainTitle",
            MainTitle,
            30f,
            FontStyles.Bold,
            Color.white);
        mainTitle.enableAutoSizing = true;
        mainTitle.fontSizeMin = 18f;
        mainTitle.fontSizeMax = 30f;
        mainTitle.margin = new Vector4(16f, 2f, 16f, 2f);
        StretchRect(mainTitle.rectTransform);

        GameObject whiteBand = CreateImage(
            frame.transform,
            "StepTitleBand",
            new Color(0.985f, 0.985f, 0.985f, 1f));
        RectTransform whiteRect = whiteBand.GetComponent<RectTransform>();
        whiteRect.anchorMin = new Vector2(0f, 0f);
        whiteRect.anchorMax = new Vector2(1f, 0f);
        whiteRect.pivot = new Vector2(0.5f, 0f);
        whiteRect.anchoredPosition = Vector2.zero;
        whiteRect.sizeDelta = new Vector2(0f, headingHeight - 60f);

        stepTitleText = CreateText(
            whiteBand.transform,
            "StepTitle",
            StepTitles[0],
            24f,
            FontStyles.Bold,
            new Color(0.08f, 0.08f, 0.08f, 1f));
        stepTitleText.enableAutoSizing = true;
        stepTitleText.fontSizeMin = 16f;
        stepTitleText.fontSizeMax = 24f;
        stepTitleText.margin = new Vector4(14f, 2f, 14f, 2f);
        StretchRect(stepTitleText.rectTransform);

        headingRect.gameObject.SetActive(false);
        PositionAsWorldObject();
    }

    private void PositionAsWorldObject()
    {
        if (canvasRect == null || headingRect == null)
            return;

        if (boardRenderer == null)
        {
            GameObject board = GameObject.Find("Board");
            boardRenderer = board != null ? board.GetComponent<Renderer>() : null;

            GameObject boardFrame = GameObject.Find("Board_Frame");
            boardFrameRenderer = boardFrame != null ? boardFrame.GetComponent<Renderer>() : null;
        }

        if (boardRenderer == null)
        {
            if (!missingBoardWarningLogged)
            {
                missingBoardWarningLogged = true;
                Debug.LogWarning("[BoardStepHeading] Khong tim thay Board de dat heading World Space.");
            }
            return;
        }

        missingBoardWarningLogged = false;
        Bounds boardBounds = boardRenderer.bounds;
        float worldWidth = boardBounds.size.x * widthRelativeToBoard;
        float worldScale = worldWidth / headingWidth;
        float worldHeight = headingHeight * worldScale;
        float topEdge = boardFrameRenderer != null
            ? boardFrameRenderer.bounds.max.y
            : boardBounds.max.y;

        Camera camera = Camera.main;
        if (canvas != null)
            canvas.worldCamera = camera;

        Transform canvasTransform = canvasRect.transform;
        canvasTransform.localScale = Vector3.one * worldScale;
        canvasTransform.rotation = camera != null
            ? camera.transform.rotation
            : boardRenderer.transform.rotation;
        canvasTransform.position = new Vector3(
            boardBounds.center.x,
            topEdge + verticalGap + worldHeight * 0.5f,
            boardBounds.center.z - cameraDepthOffset);

        AlignTopToViewport(camera, canvasTransform);
    }

    private void AlignTopToViewport(Camera camera, Transform canvasTransform)
    {
        if (camera == null || canvasRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);

        float currentTop = float.NegativeInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(corners[i]);
            if (viewportPoint.z <= 0f)
                return;

            currentTop = Mathf.Max(currentTop, viewportPoint.y);
        }

        Vector3 centerViewportPoint = camera.WorldToViewportPoint(canvasTransform.position);
        centerViewportPoint.y += targetViewportTop - currentTop;
        canvasTransform.position = camera.ViewportToWorldPoint(centerViewportPoint);
    }

    private GameObject CreateImage(
        Transform parent,
        string objectName,
        Color color,
        bool useRoundedSprite = false)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        if (useRoundedSprite)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
        }
        image.raycastTarget = false;
        return imageObject;
    }

    private Sprite GetRoundedSprite()
    {
        if (roundedSprite != null)
            return roundedSprite;

        const int textureSize = 64;
        const float cornerRadius = 16f;
        const float halfSize = (textureSize - 1) * 0.5f;
        float innerExtent = halfSize - cornerRadius;

        Texture2D texture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false);
        texture.name = "Runtime_BoardHeading_RoundedRect";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float cornerX = Mathf.Max(Mathf.Abs(x - halfSize) - innerExtent, 0f);
                float cornerY = Mathf.Max(Mathf.Abs(y - halfSize) - innerExtent, 0f);
                float distanceFromEdge = Mathf.Sqrt(cornerX * cornerX + cornerY * cornerY) - cornerRadius;
                float alpha = Mathf.Clamp01(0.5f - distanceFromEdge);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        roundedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(20f, 20f, 20f, 20f));
        roundedSprite.name = "Runtime_BoardHeading_RoundedRect";
        roundedSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedSprite;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

}
