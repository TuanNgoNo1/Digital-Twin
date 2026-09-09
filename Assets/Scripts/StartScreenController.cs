using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    public static bool OpenGuidePageOnStart;
    public static bool ContinuePracticeFromGuide;

    [Header("Legacy panels")]
    [SerializeField] private GameObject practicePanel;
    [SerializeField] private GameObject introductionPanel;
    [SerializeField] private GameObject guidePanel;

    [Header("Page navigation")]
    [SerializeField] private string practiceSceneName = "Sy_scene";
    [SerializeField] private GameObject[] pages;
    [SerializeField] private RectTransform background;

    private const int PageCount = 4;

    private int currentPageIndex;
    private Button previousButton;
    private Button nextButton;
    private TextMeshProUGUI previousButtonLabel;
    private TextMeshProUGUI previousButtonArrow;
    private TextMeshProUGUI nextButtonLabel;
    private TextMeshProUGUI nextButtonArrow;
    private Outline nextButtonOutline;
    private Shadow previousButtonShadow;
    private Shadow nextButtonShadow;
    private Sprite defaultNavigationSprite;
    private Image backgroundImage;
    private Camera sceneCamera;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackground;
    private Font mainTitleSystemFont;
    private TMP_FontAsset mainTitleFontAsset;
    private Material mainTitleBoldMaterial;
    private readonly Material[] sectionTitleBoldMaterials = new Material[3];
    private static Sprite navigationPillSprite;

    private void Awake()
    {
        ResolveReferences();
        ApplyExtraBoldMainTitle();
        ApplyExtraBoldSectionTitles();
        BuildNavigationButtons();
        ShowPage(OpenGuidePageOnStart ? PageCount - 1 : 0);
        OpenGuidePageOnStart = false;
    }

    private void OnDestroy()
    {
        if (mainTitleBoldMaterial != null)
        {
            Destroy(mainTitleBoldMaterial);
        }
        if (mainTitleFontAsset != null)
        {
            Destroy(mainTitleFontAsset);
        }
        if (mainTitleSystemFont != null)
        {
            Destroy(mainTitleSystemFont);
        }
        foreach (Material material in sectionTitleBoldMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    private void ApplyExtraBoldMainTitle()
    {
        if (pages == null || pages.Length == 0 || pages[0] == null)
        {
            return;
        }

        TextMeshProUGUI[] pageTexts = pages[0].GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI mainTitle = null;
        foreach (TextMeshProUGUI pageText in pageTexts)
        {
            if (pageText.name == "MainTitle")
            {
                mainTitle = pageText;
                break;
            }
        }

        if (mainTitle == null || mainTitle.fontSharedMaterial == null)
        {
            return;
        }

        mainTitle.text = mainTitle.text
            .Replace("\u1EA4", "\u00C2<voffset=0.11em>\u0301</voffset>")
            .Replace("\u1ED0", "\u00D4<voffset=0.11em>\u0301</voffset>")
            .Replace("\u1EC0", "\u00CA<voffset=0.11em>\u0300</voffset>")
            .Replace("\u1EC2", "\u00CA<voffset=0.14em>\u0309</voffset>");
        mainTitleSystemFont = Font.CreateDynamicFontFromOSFont("Arial Black", 64);
        if (mainTitleSystemFont != null)
        {
            mainTitleFontAsset = TMP_FontAsset.CreateFontAsset(mainTitleSystemFont);
            if (mainTitleFontAsset != null)
            {
                mainTitle.font = mainTitleFontAsset;
            }
        }

        mainTitle.fontStyle = FontStyles.Bold;
        mainTitle.fontWeight = FontWeight.Black;
        mainTitleBoldMaterial = new Material(mainTitle.fontSharedMaterial)
        {
            name = "MainTitle ExtraBold Material"
        };

        if (mainTitleBoldMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            mainTitleBoldMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
        }

        mainTitle.fontSharedMaterial = mainTitleBoldMaterial;
        mainTitle.SetMaterialDirty();
    }

    private void ApplyExtraBoldSectionTitles()
    {
        if (pages == null)
        {
            return;
        }

        string[] titleNames = { "PageTwoTitle", "PageThreeTitle", "PageFourTitle" };
        for (int index = 0; index < titleNames.Length; index++)
        {
            int pageIndex = index + 1;
            if (pageIndex >= pages.Length || pages[pageIndex] == null)
            {
                continue;
            }

            TextMeshProUGUI title = null;
            foreach (TextMeshProUGUI text in
                     pages[pageIndex].GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == titleNames[index])
                {
                    title = text;
                    break;
                }
            }

            if (title == null || title.fontSharedMaterial == null)
            {
                continue;
            }

            title.fontStyle = FontStyles.Bold;
            title.fontWeight = FontWeight.Black;
            Material boldMaterial = new Material(title.fontSharedMaterial)
            {
                name = title.name + " ExtraBold Material"
            };
            if (boldMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
            {
                boldMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
            }

            sectionTitleBoldMaterials[index] = boldMaterial;
            title.fontSharedMaterial = boldMaterial;
            title.SetMaterialDirty();

            Outline outline = title.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }
    }

    public void ShowPractice()
    {
        SceneManager.LoadScene(practiceSceneName);
    }

    public void LoadPracticeScene()
    {
        SceneManager.LoadScene(practiceSceneName);
    }

    public void ShowIntroduction()
    {
        ShowLegacyPanel(introductionPanel);
    }

    public void ShowGuide()
    {
        ShowLegacyPanel(guidePanel);
    }

    public void PreviousPage()
    {
        if (currentPageIndex == 1 && pages.Length > 1 && pages[1] != null)
        {
            PageTwoPartsController pageTwo = pages[1].GetComponent<PageTwoPartsController>();
            if (pageTwo != null && pageTwo.TryShowPartList())
            {
                return;
            }
        }

        if (currentPageIndex <= 0)
        {
            return;
        }

        ShowPage(currentPageIndex - 1);
    }

    public void NextPage()
    {
        if (currentPageIndex >= pages.Length - 1)
        {
            ContinuePracticeFromGuide = true;
            SceneManager.LoadScene(practiceSceneName);
            return;
        }

        ShowPage(currentPageIndex + 1);
    }

    private void ResolveReferences()
    {
        if (pages == null || pages.Length == 0)
        {
            pages = new GameObject[PageCount];

            for (int i = 0; i < PageCount; i++)
            {
                GameObject page = GameObject.Find($"Trang {i + 1}");
                pages[i] = page;
            }
        }

        if (background == null)
        {
            GameObject backgroundObject = GameObject.Find("Background");
            background = backgroundObject != null ? backgroundObject.GetComponent<RectTransform>() : null;
        }

        backgroundImage = background != null ? background.GetComponent<Image>() : null;
        sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            GameObject cameraObject = GameObject.Find("Camera");
            sceneCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : FindFirstObjectByType<Camera>();
        }
        if (sceneCamera != null)
        {
            originalCameraClearFlags = sceneCamera.clearFlags;
            originalCameraBackground = sceneCamera.backgroundColor;
        }
    }

    private void BuildNavigationButtons()
    {
        RectTransform parent = transform as RectTransform;
        if (parent == null)
        {
            return;
        }

        previousButton = CreateNavigationButton(parent, "PreviousButton", "\u2190", new Vector2(0f, 0f), new Vector2(44f, 22f));
        nextButton = CreateNavigationButton(parent, "NextButton", "\u2192", new Vector2(1f, 0f), new Vector2(-44f, 22f));
        defaultNavigationSprite = nextButton.GetComponent<Image>().sprite;

        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        ConfigurePreviousButtonHoverEvents();
        ConfigureNextButtonHoverEvents();
    }

    private Button CreateNavigationButton(RectTransform parent, string objectName, string arrow, Vector2 anchor, Vector2 position)
    {
        GameObject existing = parent.Find(objectName)?.gameObject;
        if (existing != null)
        {
            existing.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
            }
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.layer = parent.gameObject.layer;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(70f, 70f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        Sprite roundedRectangle = Resources.Load<Sprite>("UI/RoundedRect");
        if (roundedRectangle == null)
        {
            roundedRectangle = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
        if (roundedRectangle != null)
        {
            image.sprite = roundedRectangle;
            image.type = Image.Type.Sliced;
        }

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color32(218, 222, 226, 255);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(246, 247, 248, 255);
        colors.pressedColor = new Color32(232, 235, 238, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(245, 245, 245, 180);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreateButtonText(buttonObject.transform, "Arrow", arrow, 34f, Vector2.zero, FontStyles.Normal);

        return button;
    }

    private void CreateButtonText(Transform parent, string objectName, string value, float fontSize, Vector2 position, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        textObject.layer = parent.gameObject.layer;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(105, 117, 132, 255);
        text.raycastTarget = false;
    }

    private void AddPageLabels()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            GameObject page = pages[i];
            if (i <= 1 || i == 3 || page == null || page.transform.Find("PageNumberText") != null)
            {
                continue;
            }

            GameObject textObject = new GameObject("PageNumberText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(page.transform, false);
            textObject.layer = page.layer;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600f, 160f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = $"Trang {i + 1}";
            text.fontSize = 72f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.black;
            text.raycastTarget = false;
        }
    }

    private void ShowPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pages.Length - 1));

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(currentPageIndex > 0);
        }

        ApplyPreviousButtonStyle(currentPageIndex > 0);
        ApplyNextButtonStyle(true);

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            // Trang 3 (index 3) ẩn background; trang 1 và trang 2 đều hiện background giống nhau
            color.a = currentPageIndex == 3 ? 0f : 1f;
            backgroundImage.color = color;
        }
        if (sceneCamera != null)
        {
            bool usesThreeDimensionalPreview = currentPageIndex == 3;
            sceneCamera.clearFlags = usesThreeDimensionalPreview ? CameraClearFlags.SolidColor : originalCameraClearFlags;
            sceneCamera.backgroundColor = usesThreeDimensionalPreview
                ? new Color32(247, 247, 247, 255)
                : originalCameraBackground;
        }
    }

    private void ApplyNextButtonStyle(bool isFirstPage)
    {
        if (nextButton == null)
        {
            return;
        }

        RectTransform rect = nextButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = isFirstPage
                ? new Vector2(currentPageIndex == 1 || currentPageIndex == 2 ? -70f : -50f, 16f)
                : new Vector2(-44f, 22f);
            rect.sizeDelta = isFirstPage ? new Vector2(208f, 72f) : new Vector2(70f, 70f);
        }

        Image image = nextButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = isFirstPage ? new Color32(202, 20, 23, 255) : Color.white;
            defaultNavigationSprite ??= image.sprite;
            image.sprite = isFirstPage ? GetNavigationPillSprite() : defaultNavigationSprite;
            image.type = Image.Type.Sliced;
        }
        nextButton.transition = isFirstPage ? Selectable.Transition.None : Selectable.Transition.ColorTint;

        ColorBlock colors = nextButton.colors;
        if (isFirstPage)
        {
            colors.normalColor = new Color32(202, 20, 23, 255);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color32(250, 246, 246, 255);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color32(202, 20, 23, 150);
        }
        else
        {
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(246, 247, 248, 255);
            colors.pressedColor = new Color32(232, 235, 238, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(245, 245, 245, 180);
        }
        nextButton.colors = colors;

        nextButtonOutline ??= nextButton.GetComponent<Outline>();
        if (nextButtonOutline != null)
        {
            nextButtonOutline.enabled = true;
            nextButtonOutline.effectColor = isFirstPage
                ? new Color32(202, 20, 23, 255)
                : new Color32(218, 222, 226, 255);
            nextButtonOutline.effectDistance = isFirstPage ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            nextButtonOutline.useGraphicAlpha = false;
        }

        if (nextButtonShadow == null)
        {
            nextButtonShadow = nextButton.gameObject.AddComponent<Shadow>();
        }
        nextButtonShadow.enabled = isFirstPage;
        nextButtonShadow.effectColor = new Color32(32, 20, 20, 30);
        nextButtonShadow.effectDistance = new Vector2(0f, -4f);
        nextButtonShadow.useGraphicAlpha = true;

        nextButtonLabel ??= nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (nextButtonLabel != null)
        {
            RectTransform labelRect = nextButtonLabel.rectTransform;
            nextButtonLabel.text = isFirstPage ? "Ti\u1EBFp theo" : "\u2192";
            nextButtonLabel.fontSize = isFirstPage ? 29f : 34f;
            nextButtonLabel.fontStyle = FontStyles.Normal;
            nextButtonLabel.color = isFirstPage ? Color.white : new Color32(105, 117, 132, 255);
            nextButtonLabel.alignment = TextAlignmentOptions.Center;

            if (isFirstPage)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(-20f, 0f);
                labelRect.sizeDelta = new Vector2(120f, 72f);
            }
            else
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;
            }
        }

        EnsureNextButtonArrow();
        nextButtonArrow.gameObject.SetActive(isFirstPage);
    }

    private void ConfigureNextButtonHoverEvents()
    {
        EventTrigger trigger = nextButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetNextButtonHover(true));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetNextButtonHover(false));
        trigger.triggers.Add(exit);
    }

    private void SetNextButtonHover(bool isHovered)
    {
        nextButtonLabel ??= nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (nextButtonLabel != null)
        {
            nextButtonLabel.color = isHovered ? new Color32(202, 20, 23, 255) : Color.white;
        }

        if (nextButtonArrow != null)
        {
            nextButtonArrow.color = isHovered ? new Color32(202, 20, 23, 255) : Color.white;
        }

        Image image = nextButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = isHovered ? Color.white : new Color32(202, 20, 23, 255);
        }
        if (nextButtonShadow != null)
        {
            nextButtonShadow.enabled = !isHovered;
        }

        nextButtonOutline ??= nextButton.GetComponent<Outline>();
        if (nextButtonOutline != null)
        {
            nextButtonOutline.enabled = true;
            nextButtonOutline.effectColor = new Color32(202, 20, 23, 255);
        }
    }

    private void ApplyPreviousButtonStyle(bool isPageTwo)
    {
        if (previousButton == null)
        {
            return;
        }

        RectTransform rect = previousButton.transform as RectTransform;
        Image image = previousButton.GetComponent<Image>();
        TextMeshProUGUI label = previousButton.GetComponentInChildren<TextMeshProUGUI>();
        Outline outline = previousButton.GetComponent<Outline>();
        if (previousButtonShadow == null)
        {
            previousButtonShadow = previousButton.gameObject.AddComponent<Shadow>();
        }

        if (rect != null)
        {
            rect.anchoredPosition = isPageTwo ? new Vector2(48f, 16f) : new Vector2(44f, 22f);
            rect.sizeDelta = isPageTwo ? new Vector2(208f, 72f) : new Vector2(70f, 70f);
        }
        if (image != null)
        {
            image.sprite = isPageTwo ? GetNavigationPillSprite() : defaultNavigationSprite;
            image.type = Image.Type.Sliced;
            image.color = isPageTwo ? new Color32(202, 20, 23, 255) : Color.white;
        }
        previousButton.transition = isPageTwo ? Selectable.Transition.None : Selectable.Transition.ColorTint;
        previousButtonLabel = label;
        if (previousButtonLabel != null)
        {
            previousButtonLabel.text = isPageTwo ? "Quay l\u1EA1i" : "\u2190";
            previousButtonLabel.fontSize = isPageTwo ? 29f : 34f;
            previousButtonLabel.fontStyle = FontStyles.Normal;
            previousButtonLabel.color = isPageTwo ? Color.white : new Color32(105, 117, 132, 255);
            RectTransform labelRect = previousButtonLabel.rectTransform;
            if (isPageTwo)
            {
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = new Vector2(20f, 0f);
                labelRect.sizeDelta = new Vector2(120f, 72f);
            }
            else
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;
            }
        }
        EnsurePreviousButtonArrow();
        previousButtonArrow.gameObject.SetActive(isPageTwo);
        if (outline != null)
        {
            outline.effectColor = isPageTwo ? new Color32(202, 20, 23, 255) : new Color32(218, 222, 226, 255);
            outline.effectDistance = isPageTwo ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
        }
        previousButtonShadow.enabled = isPageTwo;
        previousButtonShadow.effectColor = new Color32(32, 20, 20, 30);
        previousButtonShadow.effectDistance = new Vector2(0f, -4f);
        previousButtonShadow.useGraphicAlpha = true;

        ColorBlock colors = previousButton.colors;
        colors.normalColor = isPageTwo ? new Color32(202, 20, 23, 255) : Color.white;
        colors.highlightedColor = isPageTwo ? Color.white : new Color32(246, 247, 248, 255);
        colors.pressedColor = isPageTwo ? new Color32(250, 246, 246, 255) : new Color32(232, 235, 238, 255);
        colors.selectedColor = colors.normalColor;
        previousButton.colors = colors;
    }

    private void ConfigurePreviousButtonHoverEvents()
    {
        EventTrigger trigger = previousButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => SetPreviousButtonHover(true));
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => SetPreviousButtonHover(false));
        trigger.triggers.Add(exit);
    }

    private void SetPreviousButtonHover(bool isHovered)
    {
        if (currentPageIndex <= 0)
        {
            return;
        }
        Color color = isHovered ? new Color32(202, 20, 23, 255) : Color.white;
        if (previousButtonLabel != null)
        {
            previousButtonLabel.color = color;
        }
        if (previousButtonArrow != null)
        {
            previousButtonArrow.color = color;
        }
        Image image = previousButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = isHovered ? Color.white : new Color32(202, 20, 23, 255);
        }
        if (previousButtonShadow != null)
        {
            previousButtonShadow.enabled = !isHovered;
        }
        Outline outline = previousButton.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;
            outline.effectColor = new Color32(202, 20, 23, 255);
        }
    }

    private void EnsurePreviousButtonArrow()
    {
        if (previousButtonArrow != null)
        {
            return;
        }
        GameObject arrowObject = new GameObject("PillArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        arrowObject.transform.SetParent(previousButton.transform, false);
        arrowObject.layer = previousButton.gameObject.layer;
        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-64f, 3f);
        rect.sizeDelta = new Vector2(36f, 72f);
        previousButtonArrow = arrowObject.GetComponent<TextMeshProUGUI>();
        previousButtonArrow.font = TMP_Settings.defaultFontAsset;
        previousButtonArrow.text = "\u2190";
        previousButtonArrow.fontSize = 35f;
        previousButtonArrow.fontStyle = FontStyles.Bold;
        previousButtonArrow.alignment = TextAlignmentOptions.Center;
        previousButtonArrow.color = Color.white;
        previousButtonArrow.raycastTarget = false;
    }

    private void EnsureNextButtonArrow()
    {
        if (nextButtonArrow != null)
        {
            return;
        }

        GameObject arrowObject = new GameObject("PillArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        arrowObject.transform.SetParent(nextButton.transform, false);
        arrowObject.layer = nextButton.gameObject.layer;

        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(64f, 3f);
        rect.sizeDelta = new Vector2(36f, 72f);

        nextButtonArrow = arrowObject.GetComponent<TextMeshProUGUI>();
        nextButtonArrow.font = TMP_Settings.defaultFontAsset;
        nextButtonArrow.text = "\u2192";
        nextButtonArrow.fontSize = 35f;
        nextButtonArrow.fontStyle = FontStyles.Bold;
        nextButtonArrow.alignment = TextAlignmentOptions.Center;
        nextButtonArrow.color = Color.white;
        nextButtonArrow.raycastTarget = false;
    }

    private static Sprite GetNavigationPillSprite()
    {
        if (navigationPillSprite != null)
        {
            return navigationPillSprite;
        }

        const int width = 132;
        const int height = 72;
        const float radius = 25f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "NavigationPillTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = x + 0.5f;
                float sampleY = y + 0.5f;
                float centerX = Mathf.Clamp(sampleX, radius, width - radius);
                float centerY = Mathf.Clamp(sampleY, radius, height - radius);
                float distance = Vector2.Distance(new Vector2(sampleX, sampleY), new Vector2(centerX, centerY));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        navigationPillSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        navigationPillSprite.name = "NavigationPillSprite";
        navigationPillSprite.hideFlags = HideFlags.HideAndDontSave;
        return navigationPillSprite;
    }

    private void ShowLegacyPanel(GameObject selectedPanel)
    {
        if (practicePanel != null)
        {
            practicePanel.SetActive(practicePanel == selectedPanel);
        }

        if (introductionPanel != null)
        {
            introductionPanel.SetActive(introductionPanel == selectedPanel);
        }

        if (guidePanel != null)
        {
            guidePanel.SetActive(guidePanel == selectedPanel);
        }
    }
}
