using TMPro;
using UnityEngine;
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
    private Image backgroundImage;
    private Camera sceneCamera;
    private CameraClearFlags originalCameraClearFlags;
    private Color originalCameraBackground;

    private void Awake()
    {
        ResolveReferences();
        BuildNavigationButtons();
        AddPageLabels();
        ShowPage(OpenGuidePageOnStart ? PageCount - 1 : 0);
        OpenGuidePageOnStart = false;
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
        if (sceneCamera != null)
        {
            originalCameraClearFlags = sceneCamera.clearFlags;
            originalCameraBackground = sceneCamera.backgroundColor;
        }
    }

    private void BuildNavigationButtons()
    {
        RectTransform parent = background != null ? background : transform as RectTransform;
        if (parent == null)
        {
            return;
        }

        previousButton = CreateNavigationButton(parent, "PreviousButton", "\u2190", "Tr\u01b0\u1edbc", new Vector2(0f, 0f), new Vector2(40f, 40f));
        nextButton = CreateNavigationButton(parent, "NextButton", "\u2192", "Sau", new Vector2(1f, 0f), new Vector2(-40f, 40f));

        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
    }

    private Button CreateNavigationButton(RectTransform parent, string objectName, string arrow, string label, Vector2 anchor, Vector2 position)
    {
        GameObject existing = parent.Find(objectName)?.gameObject;
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.layer = parent.gameObject.layer;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(130f, 110f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.82f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.86f, 0.95f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;

        CreateButtonText(buttonObject.transform, "Arrow", arrow, 48f, new Vector2(0f, 18f), FontStyles.Bold);
        CreateButtonText(buttonObject.transform, "Label", label, 26f, new Vector2(0f, -28f), FontStyles.Bold);

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
        text.color = Color.black;
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
            previousButton.interactable = currentPageIndex > 0;
        }

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = currentPageIndex == 1 || currentPageIndex == 3 ? 0f : 1f;
            backgroundImage.color = color;
        }
        if (sceneCamera != null)
        {
            bool usesThreeDimensionalPreview = currentPageIndex == 1 || currentPageIndex == 3;
            sceneCamera.clearFlags = usesThreeDimensionalPreview ? CameraClearFlags.SolidColor : originalCameraClearFlags;
            sceneCamera.backgroundColor = usesThreeDimensionalPreview ? Color.white : originalCameraBackground;
        }
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
