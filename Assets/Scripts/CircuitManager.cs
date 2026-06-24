using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircuitManager : MonoBehaviour
{
    private const int NavigationStepCount = 4;
    private const int HmiStepIndex = 3;

    public static CircuitManager Instance;

    [Header("Ba buoc noi day")]
    public List<GameObject> stepRoots = new List<GameObject>();
    public List<GameObject> guideRoots = new List<GameObject>();
    public int currentStepIndex;

    [Header("Bo tri hai hang wire head")]
    public bool arrangeWireHeadsOnStart = false;
    public Vector3 layoutCenter = new Vector3(256.14f, 0.08f, -47.286f);
    public float columnSpacing = 0.055f;
    public float rowSpacing = 0.07f;

    [Header("Object legacy khong tham gia gameplay")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Nen trang cho socket label")]
    public bool createSocketLabelBackgrounds = true;
    public Color socketLabelBackgroundColor = new Color(1f, 1f, 1f, 0.92f);
    public Vector2 socketLabelBackgroundPadding = new Vector2(14f, 8f);

    [Header("Popup ket qua tung buoc")]
    public bool createStepResultPopup = true;
    public Vector2 stepResultPopupSize = new Vector2(620f, 440f);

    [Header("Thanh xem lai bon buoc")]
    public bool createStepNavigationBar = true;
    public Vector2 stepNavigationButtonSize = new Vector2(118f, 44f);
    public Vector2 stepNavigationMargin = new Vector2(24f, 24f);

    [Header("Camera responsive khong crop hai ben")]
    public bool preserveWideCameraFraming = true;
    public float cameraDesignAspect = 2.25f;
    public float cameraDesignVerticalFov = 60f;

    [Header("HMI chi mo sau khi xong ca ba buoc")]
    public string hmiSceneName = "HMI_scene";
    public GameObject hmiPanel;
    public GameObject cameraStream;

    [Header("Thong tin runtime")]
    public int totalWires = 14;
    public int completedWires;

    private PLCController_v2 plcControllerV2;
    private bool initialized;
    private bool systemUnlocked;
    private bool popupVisible;
    private bool pendingStepCompletion;
    private int popupClosedFrame = -1;
    private int visibleStepIndex;
    private int highestUnlockedStepIndex;
    private bool hmiSceneLoading;
    private GameObject stepResultPopupRoot;
    private GameObject stepNavigationRoot;
    private RectTransform stepNavigationPanelRect;
    private readonly List<Button> stepNavigationButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> stepNavigationLabels = new List<TextMeshProUGUI>();
    private TextMeshProUGUI popupIconText;
    private TextMeshProUGUI popupStatusText;
    private TextMeshProUGUI popupMessageText;
    private Image popupIconBackground;
    private Image popupAccent;

    public bool IsPopupVisible => popupVisible || Time.frameCount == popupClosedFrame;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        if (initialized)
            return;

        initialized = true;
        plcControllerV2 = PLCController_v2.Instance != null
            ? PLCController_v2.Instance
            : FindObjectOfType<PLCController_v2>();

        AutoFindStepRoots();
        AutoFindGuideRoots();
        if (stepRoots.Count != 3)
        {
            Debug.LogError("[Circuit] Khong the bat dau vi chua du ba nhom day.");
            return;
        }

        DisableLegacyObjects();
        EnsureSocketLabelBackgrounds();
        CreateStepResultPopup();
        CreateStepNavigationBar();
        EnsureResponsiveCameraFraming();

        if (arrangeWireHeadsOnStart)
            ArrangeAllSteps();

        totalWires = stepRoots.Sum(root => GetStepWires(root).Count);
        currentStepIndex = 0;
        visibleStepIndex = 0;
        highestUnlockedStepIndex = 0;
        completedWires = 0;
        LockSystem();
        ShowOnlyStep(currentStepIndex);
        UpdateStepNavigationBar();

        Debug.Log($"[Circuit] Bat dau Buoc 1: {GetStepWires(stepRoots[0]).Count} day. Tong cong {totalWires} day.");
        EvaluateCircuit();
    }

    private void EnsureResponsiveCameraFraming()
    {
        if (!preserveWideCameraFraming)
            return;

        Camera mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (mainCamera == null)
        {
            Debug.LogWarning("[Circuit] Khong tim thay camera de chong crop ngang.");
            return;
        }

        ResponsiveCameraFraming framing = mainCamera.GetComponent<ResponsiveCameraFraming>();
        if (framing == null)
            framing = mainCamera.gameObject.AddComponent<ResponsiveCameraFraming>();

        framing.designAspect = cameraDesignAspect;
        framing.designVerticalFov = cameraDesignVerticalFov;
        framing.ApplyFraming();
    }

    public void OnWireConnectedCorrectly(WireBody wire)
    {
        EvaluateCircuit();
    }

    public void EvaluateCircuit()
    {
        if (!initialized || systemUnlocked || popupVisible ||
            currentStepIndex < 0 || currentStepIndex >= stepRoots.Count)
            return;

        List<WireBody> currentWires = GetStepWires(stepRoots[currentStepIndex]);
        foreach (WireBody wire in currentWires)
            wire.RefreshConnectionState();

        int correctInCurrentStep = currentWires.Count(wire => wire.isCorrect);
        completedWires = CountCompletedPreviousSteps() + correctInCurrentStep;

        Debug.Log($"[Circuit] Buoc {currentStepIndex + 1}: {correctInCurrentStep}/{currentWires.Count} day dung. Tong: {completedWires}/{totalWires}.");

        bool allConnected = currentWires.Count > 0 && currentWires.All(wire => wire.isFullyConnected);
        if (!allConnected)
            return;

        List<WireBody> wrongWires = currentWires.Where(wire => !wire.isCorrect).ToList();
        if (wrongWires.Count > 0)
        {
            ShowWrongWiresPopup(wrongWires);
            return;
        }

        ShowStepCompletedPopup();
    }

    private void CompleteCurrentStep()
    {
        int completedStepNumber = currentStepIndex + 1;
        stepRoots[currentStepIndex].SetActive(false);
        if (currentStepIndex < guideRoots.Count && guideRoots[currentStepIndex] != null)
            guideRoots[currentStepIndex].SetActive(false);
        Debug.Log($"<color=green>✓ HOAN THANH BUOC {completedStepNumber}</color>");

        highestUnlockedStepIndex = Mathf.Min(completedStepNumber, HmiStepIndex);
        currentStepIndex++;
        if (currentStepIndex >= stepRoots.Count)
        {
            completedWires = totalWires;
            visibleStepIndex = HmiStepIndex;
            ShowAllCompletedWires();
            UnlockSystem();
            UpdateStepNavigationBar();
            return;
        }

        visibleStepIndex = currentStepIndex;
        ShowOnlyStep(currentStepIndex);
        UpdateStepNavigationBar();
        Debug.Log($"[Circuit] Chuyen sang Buoc {currentStepIndex + 1}: {GetStepWires(stepRoots[currentStepIndex]).Count} day.");
    }

    private void ShowOnlyStep(int visibleStepIndex)
    {
        for (int i = 0; i < stepRoots.Count; i++)
        {
            if (stepRoots[i] != null)
            {
                stepRoots[i].SetActive(i == visibleStepIndex);
                SetStepInteractionEnabled(
                    stepRoots[i],
                    i == currentStepIndex && currentStepIndex < stepRoots.Count && !systemUnlocked);
            }

            if (i < guideRoots.Count && guideRoots[i] != null)
                guideRoots[i].SetActive(i == visibleStepIndex);
        }
    }

    private void ShowAllCompletedWires()
    {
        foreach (GameObject stepRoot in stepRoots)
        {
            if (stepRoot == null)
                continue;

            stepRoot.SetActive(true);
            SetStepInteractionEnabled(stepRoot, false);
            HideStepPresentationObjects(stepRoot);
        }

        foreach (GameObject guideRoot in guideRoots)
        {
            if (guideRoot != null)
                guideRoot.SetActive(false);
        }

        Debug.Log("[Circuit] Da hien lai day ket noi cua ca ba buoc.");
    }

    private static void SetStepInteractionEnabled(GameObject stepRoot, bool enabled)
    {
        if (stepRoot == null)
            return;

        foreach (WirePlug plug in stepRoot.GetComponentsInChildren<WirePlug>(true))
        {
            if (plug != null)
                plug.enabled = enabled;
        }
    }

    private void ShowStepFromNavigation(int stepIndex)
    {
        if (popupVisible ||
            stepIndex < 0 ||
            stepIndex >= NavigationStepCount ||
            stepIndex > highestUnlockedStepIndex)
        {
            return;
        }

        if (stepIndex == HmiStepIndex)
        {
            if (!systemUnlocked)
                return;

            visibleStepIndex = HmiStepIndex;
            ShowAllCompletedWires();
            OpenHmiScene();
            UpdateStepNavigationBar();
            Debug.Log("[Circuit] Dang xem Buoc 4: HMI.");
            return;
        }

        CloseHmiScene();
        visibleStepIndex = stepIndex;
        ShowOnlyStep(visibleStepIndex);
        UpdateStepNavigationBar();
        Debug.Log($"[Circuit] Dang xem lai Buoc {stepIndex + 1}.");
    }

    private void OpenHmiScene()
    {
        if (plcControllerV2 == null)
        {
            plcControllerV2 = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindObjectOfType<PLCController_v2>();
        }

        if (plcControllerV2 != null)
            plcControllerV2.SetRuntimeHmiVisible(true);

        Scene hmiScene = SceneManager.GetSceneByName(hmiSceneName);
        if (hmiScene.isLoaded || hmiSceneLoading)
            return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            hmiSceneName,
            LoadSceneMode.Additive);
        if (loadOperation == null)
        {
            Debug.LogError($"[Circuit] Khong the mo scene HMI: {hmiSceneName}.");
            return;
        }

        hmiSceneLoading = true;
        loadOperation.completed += _ =>
        {
            hmiSceneLoading = false;
            if (visibleStepIndex != HmiStepIndex)
                CloseHmiScene();
        };
    }

    private void CloseHmiScene()
    {
        if (plcControllerV2 != null)
            plcControllerV2.SetRuntimeHmiVisible(false);

        Scene hmiScene = SceneManager.GetSceneByName(hmiSceneName);
        if (hmiScene.isLoaded)
            SceneManager.UnloadSceneAsync(hmiScene);

        hmiSceneLoading = false;
    }

    public bool IsPointerOverStepNavigation(Vector2 screenPosition)
    {
        return stepNavigationRoot != null &&
            stepNavigationRoot.activeInHierarchy &&
            stepNavigationPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                stepNavigationPanelRect,
                screenPosition,
                null);
    }

    private static void HideStepPresentationObjects(GameObject stepRoot)
    {
        HashSet<GameObject> presentationObjects = new HashSet<GameObject>();

        foreach (Transform child in stepRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name.Equals("StepUI", StringComparison.OrdinalIgnoreCase))
                presentationObjects.Add(child.gameObject);
        }

        foreach (Canvas canvas in stepRoot.GetComponentsInChildren<Canvas>(true))
            presentationObjects.Add(canvas.gameObject);

        foreach (TextMeshProUGUI text in stepRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            presentationObjects.Add(text.gameObject);

        foreach (SpriteRenderer background in stepRoot.GetComponentsInChildren<SpriteRenderer>(true))
            presentationObjects.Add(background.gameObject);

        foreach (GameObject presentationObject in presentationObjects)
        {
            if (presentationObject != null && presentationObject != stepRoot)
                presentationObject.SetActive(false);
        }
    }

    private void ArrangeAllSteps()
    {
        foreach (GameObject root in stepRoots)
            ArrangeStep(root);
    }

    private void ArrangeStep(GameObject root)
    {
        List<WireBody> wires = GetStepWires(root);
        if (wires.Count == 0)
            return;

        float firstX = layoutCenter.x - columnSpacing * (wires.Count - 1) * 0.5f;
        float topY = layoutCenter.y + rowSpacing * 0.5f;
        float bottomY = layoutCenter.y - rowSpacing * 0.5f;

        for (int i = 0; i < wires.Count; i++)
        {
            WireBody wire = wires[i];
            float x = firstX + i * columnSpacing;

            PreparePlugForLayout(wire.plugA, new Vector3(x, topY, layoutCenter.z), wire);
            PreparePlugForLayout(wire.plugB, new Vector3(x, bottomY, layoutCenter.z), wire);
        }
    }

    private static void PreparePlugForLayout(WirePlug plug, Vector3 position, WireBody parentWire)
    {
        if (plug == null)
            return;

        if (plug.connectedSocket != null)
            plug.connectedSocket.isOccupied = false;

        plug.connectedSocket = null;
        plug.isSnapped = false;
        plug.parentWire = parentWire;
        plug.transform.position = position;
    }

    private List<WireBody> GetStepWires(GameObject root)
    {
        if (root == null)
            return new List<WireBody>();

        return root.GetComponentsInChildren<WireBody>(true)
            .Where(wire => wire != null &&
                !string.IsNullOrWhiteSpace(wire.correctSocketA) &&
                !string.IsNullOrWhiteSpace(wire.correctSocketB))
            .OrderBy(wire => wire.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int CountCompletedPreviousSteps()
    {
        int count = 0;
        for (int i = 0; i < currentStepIndex && i < stepRoots.Count; i++)
            count += GetStepWires(stepRoots[i]).Count;
        return count;
    }

    private void ShowWrongWiresPopup(List<WireBody> wrongWires)
    {
        if (stepResultPopupRoot == null)
        {
            Debug.LogWarning($"[Circuit] Cac day sai: {string.Join(", ", wrongWires.Select(GetWireDisplayName))}.");
            return;
        }

        pendingStepCompletion = false;
        popupIconBackground.color = new Color(1f, 0.69f, 0.08f, 1f);
        popupAccent.color = new Color(1f, 0.69f, 0.08f, 1f);
        popupIconText.text = "!";
        popupStatusText.color = new Color(0.82f, 0.42f, 0.02f, 1f);

        string wireNumbers = string.Join(", ", wrongWires.Select(GetWireDisplayName));
        popupStatusText.text = $"{wireNumbers} chưa được cắm đúng";

        string expectedConnections = string.Join(
            "\n",
            wrongWires.Select(wire =>
                $"{GetWireDisplayName(wire)}: {wire.correctSocketA} - {wire.correctSocketB}"));

        popupMessageText.text =
            "Vui lòng kiểm tra và cắm lại theo hướng dẫn:\n\n" +
            expectedConnections;

        ShowPopup();
        Debug.LogWarning($"[Circuit] Buoc {currentStepIndex + 1} co day sai: {wireNumbers}.");
    }

    private void ShowStepCompletedPopup()
    {
        if (stepResultPopupRoot == null)
        {
            CompleteCurrentStep();
            return;
        }

        pendingStepCompletion = true;
        popupIconBackground.color = new Color(0.12f, 0.68f, 0.38f, 1f);
        popupAccent.color = new Color(0.12f, 0.68f, 0.38f, 1f);
        popupIconText.text = "OK";
        popupStatusText.color = new Color(0.06f, 0.5f, 0.25f, 1f);
        popupStatusText.text = $"Đã hoàn thành Bước {currentStepIndex + 1}";

        string stepName = GetStepDisplayName(currentStepIndex);
        bool isFinalStep = currentStepIndex == stepRoots.Count - 1;
        popupMessageText.text = isFinalStep
            ? $"Bạn đã nối đúng toàn bộ dây của {stepName}.\n\nNhấn OK để mở màn hình HMI."
            : $"Bạn đã nối đúng toàn bộ dây của {stepName}.\n\nNhấn OK để chuyển sang Bước {currentStepIndex + 2}.";

        ShowPopup();
    }

    private void ShowPopup()
    {
        popupVisible = true;
        stepResultPopupRoot.SetActive(true);
    }

    private void HandlePopupOk()
    {
        bool shouldCompleteStep = pendingStepCompletion;
        pendingStepCompletion = false;
        popupVisible = false;
        popupClosedFrame = Time.frameCount;

        if (stepResultPopupRoot != null)
            stepResultPopupRoot.SetActive(false);

        if (shouldCompleteStep)
            CompleteCurrentStep();
    }

    private static string GetWireDisplayName(WireBody wire)
    {
        if (wire == null)
            return "Dây ?";

        string source = wire.name;
        int markerIndex = source.IndexOf("Wire_", StringComparison.OrdinalIgnoreCase);
        int digitIndex = markerIndex >= 0 ? markerIndex + 5 : 0;
        int digitEnd = digitIndex;

        while (digitEnd < source.Length && char.IsDigit(source[digitEnd]))
            digitEnd++;

        if (digitEnd > digitIndex &&
            int.TryParse(source.Substring(digitIndex, digitEnd - digitIndex), out int wireNumber))
        {
            return $"Dây {wireNumber}";
        }

        return $"Dây {source}";
    }

    private static string GetStepDisplayName(int stepIndex)
    {
        switch (stepIndex)
        {
            case 0:
                return "mạch điều khiển động cơ";
            case 1:
                return "mạch phản hồi";
            case 2:
                return "mạch lực";
            default:
                return $"Bước {stepIndex + 1}";
        }
    }

    private void AutoFindStepRoots()
    {
        stepRoots.RemoveAll(root => root == null);
        if (stepRoots.Count >= 3)
            return;

        Transform storage = GameObject.Find("WireHeads_Storage")?.transform;
        if (storage == null)
        {
            Debug.LogError("[Circuit] Khong tim thay WireHeads_Storage.");
            return;
        }

        string[] expectedNames = { "Buoc1_MachDieuKhien", "Buoc_2", "Buoc_3" };
        stepRoots.Clear();
        foreach (string expectedName in expectedNames)
        {
            Transform child = storage.Find(expectedName);
            if (child != null)
                stepRoots.Add(child.gameObject);
        }

        if (stepRoots.Count != 3)
            Debug.LogError($"[Circuit] Can 3 step root, hien tim thay {stepRoots.Count}.");
    }

    private void AutoFindGuideRoots()
    {
        guideRoots.RemoveAll(root => root == null);
        if (guideRoots.Count >= 3)
            return;

        Transform storage = GameObject.Find("WiringGuides_Storage")?.transform;
        if (storage == null)
        {
            Debug.LogWarning("[Circuit] Khong tim thay WiringGuides_Storage.");
            return;
        }

        string[] expectedNames = { "Buoc_1", "Buoc_2", "Buoc_3" };
        guideRoots.Clear();
        foreach (string expectedName in expectedNames)
        {
            Transform child = storage.Find(expectedName);
            if (child != null)
                guideRoots.Add(child.gameObject);
        }
    }

    private void DisableLegacyObjects()
    {
        foreach (GameObject legacyObject in objectsToDisable)
        {
            if (legacyObject != null)
                legacyObject.SetActive(false);
        }
    }

    private void EnsureSocketLabelBackgrounds()
    {
        if (!createSocketLabelBackgrounds)
            return;

        int createdCount = 0;
        foreach (GameObject guideRoot in guideRoots)
        {
            if (guideRoot == null)
                continue;

            TextMeshProUGUI[] labels = guideRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Where(label => label.name.StartsWith("Label_", StringComparison.Ordinal))
                .ToArray();

            foreach (TextMeshProUGUI label in labels)
            {
                Transform parent = label.transform.parent;
                string backgroundName = "Background_" + label.name;
                if (parent == null || parent.Find(backgroundName) != null)
                    continue;

                CreateSocketLabelBackground(label, parent, backgroundName);
                createdCount++;
            }
        }

        Debug.Log($"[Circuit] Da tao {createdCount} nen trang cho socket label. Vi tri label duoc giu nguyen.");
    }

    private void CreateSocketLabelBackground(TextMeshProUGUI label, Transform parent, string backgroundName)
    {
        GameObject background = new GameObject(
            backgroundName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform source = label.rectTransform;
        RectTransform rect = background.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.SetSiblingIndex(label.transform.GetSiblingIndex());
        rect.anchorMin = source.anchorMin;
        rect.anchorMax = source.anchorMax;
        rect.pivot = source.pivot;
        rect.anchoredPosition3D = source.anchoredPosition3D;
        rect.localRotation = source.localRotation;
        rect.localScale = source.localScale;

        Vector2 preferredSize = label.GetPreferredValues(label.text);
        rect.sizeDelta = preferredSize + socketLabelBackgroundPadding;

        Canvas labelCanvas = label.GetComponent<Canvas>();
        Canvas backgroundCanvas = background.GetComponent<Canvas>();
        backgroundCanvas.renderMode = RenderMode.WorldSpace;
        backgroundCanvas.overrideSorting = true;
        backgroundCanvas.sortingOrder = labelCanvas != null ? labelCanvas.sortingOrder - 1 : 99;

        Image image = background.GetComponent<Image>();
        image.color = socketLabelBackgroundColor;
        image.raycastTarget = false;
    }

    private void CreateStepResultPopup()
    {
        if (!createStepResultPopup || stepResultPopupRoot != null)
            return;

        stepResultPopupRoot = new GameObject(
            "StepResultPopup_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = stepResultPopupRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = stepResultPopupRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dimBackground = CreatePopupImage(
            stepResultPopupRoot.transform,
            "DimBackground",
            new Color(0.02f, 0.04f, 0.08f, 0.68f));
        StretchRect(dimBackground.GetComponent<RectTransform>());

        GameObject shadow = CreatePopupImage(
            stepResultPopupRoot.transform,
            "CardShadow",
            new Color(0f, 0f, 0f, 0.32f));
        SetCenteredRect(shadow.GetComponent<RectTransform>(), stepResultPopupSize, new Vector2(10f, -12f));

        GameObject card = CreatePopupImage(
            stepResultPopupRoot.transform,
            "Card",
            new Color(0.985f, 0.99f, 1f, 1f));
        SetCenteredRect(card.GetComponent<RectTransform>(), stepResultPopupSize, Vector2.zero);

        GameObject accentObject = CreatePopupImage(
            card.transform,
            "TopAccent",
            new Color(1f, 0.69f, 0.08f, 1f));
        popupAccent = accentObject.GetComponent<Image>();
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 8f);

        GameObject iconObject = CreatePopupImage(
            card.transform,
            "StatusIcon",
            new Color(1f, 0.69f, 0.08f, 1f));
        popupIconBackground = iconObject.GetComponent<Image>();
        SetCenteredRect(iconObject.GetComponent<RectTransform>(), new Vector2(54f, 54f), new Vector2(-245f, 160f));
        popupIconText = CreatePopupText(
            iconObject.transform,
            "IconText",
            "!",
            25f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(popupIconText.rectTransform);

        TextMeshProUGUI title = CreatePopupText(
            card.transform,
            "Title",
            "THÔNG BÁO KẾT QUẢ",
            27f,
            FontStyles.Bold,
            new Color(0.09f, 0.13f, 0.21f, 1f),
            TextAlignmentOptions.Left);
        SetCenteredRect(title.rectTransform, new Vector2(460f, 54f), new Vector2(55f, 160f));

        GameObject divider = CreatePopupImage(
            card.transform,
            "Divider",
            new Color(0.84f, 0.87f, 0.92f, 1f));
        SetCenteredRect(divider.GetComponent<RectTransform>(), new Vector2(540f, 2f), new Vector2(0f, 120f));

        popupStatusText = CreatePopupText(
            card.transform,
            "Status",
            string.Empty,
            25f,
            FontStyles.Bold,
            new Color(0.82f, 0.42f, 0.02f, 1f),
            TextAlignmentOptions.Center);
        popupStatusText.enableAutoSizing = true;
        popupStatusText.fontSizeMin = 18f;
        popupStatusText.fontSizeMax = 25f;
        SetCenteredRect(popupStatusText.rectTransform, new Vector2(540f, 70f), new Vector2(0f, 75f));

        popupMessageText = CreatePopupText(
            card.transform,
            "Message",
            string.Empty,
            21f,
            FontStyles.Normal,
            new Color(0.16f, 0.2f, 0.29f, 1f),
            TextAlignmentOptions.Center);
        popupMessageText.enableAutoSizing = true;
        popupMessageText.fontSizeMin = 16f;
        popupMessageText.fontSizeMax = 21f;
        popupMessageText.overflowMode = TextOverflowModes.Overflow;
        SetCenteredRect(popupMessageText.rectTransform, new Vector2(540f, 190f), new Vector2(0f, -30f));

        GameObject buttonObject = CreatePopupImage(
            card.transform,
            "OK_Button",
            new Color(0.04f, 0.39f, 0.92f, 1f));
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), new Vector2(190f, 54f), new Vector2(0f, -170f));

        Button okButton = buttonObject.AddComponent<Button>();
        ColorBlock buttonColors = okButton.colors;
        buttonColors.normalColor = new Color(0.04f, 0.39f, 0.92f, 1f);
        buttonColors.highlightedColor = new Color(0.08f, 0.5f, 1f, 1f);
        buttonColors.pressedColor = new Color(0.03f, 0.28f, 0.72f, 1f);
        buttonColors.selectedColor = buttonColors.normalColor;
        okButton.colors = buttonColors;
        okButton.onClick.AddListener(HandlePopupOk);

        TextMeshProUGUI buttonText = CreatePopupText(
            buttonObject.transform,
            "Text",
            "OK",
            22f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(buttonText.rectTransform);

        stepResultPopupRoot.SetActive(false);
    }

    private void CreateStepNavigationBar()
    {
        if (!createStepNavigationBar || stepNavigationRoot != null)
            return;

        const float horizontalPadding = 10f;
        const float verticalPadding = 10f;
        const float buttonSpacing = 8f;
        float panelWidth =
            horizontalPadding * 2f +
            stepNavigationButtonSize.x * NavigationStepCount +
            buttonSpacing * (NavigationStepCount - 1);
        float panelHeight = verticalPadding * 2f + stepNavigationButtonSize.y;

        stepNavigationRoot = new GameObject(
            "StepNavigation_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = stepNavigationRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4900;

        CanvasScaler scaler = stepNavigationRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreatePopupImage(
            stepNavigationRoot.transform,
            "StepNavigationBar",
            new Color(0.045f, 0.065f, 0.1f, 0.94f));
        stepNavigationPanelRect = panel.GetComponent<RectTransform>();
        stepNavigationPanelRect.anchorMin = Vector2.zero;
        stepNavigationPanelRect.anchorMax = Vector2.zero;
        stepNavigationPanelRect.pivot = Vector2.zero;
        stepNavigationPanelRect.anchoredPosition = stepNavigationMargin;
        stepNavigationPanelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        stepNavigationButtons.Clear();
        stepNavigationLabels.Clear();

        for (int i = 0; i < NavigationStepCount; i++)
        {
            int stepIndex = i;
            GameObject buttonObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Button",
                Color.white);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(
                horizontalPadding + i * (stepNavigationButtonSize.x + buttonSpacing),
                0f);
            buttonRect.sizeDelta = stepNavigationButtonSize;

            Button button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => ShowStepFromNavigation(stepIndex));
            stepNavigationButtons.Add(button);

            TextMeshProUGUI label = CreatePopupText(
                buttonObject.transform,
                "Text",
                $"B\u01B0\u1EDBc {i + 1}",
                19f,
                FontStyles.Bold,
                Color.white,
                TextAlignmentOptions.Center);
            StretchRect(label.rectTransform);
            stepNavigationLabels.Add(label);
        }
    }

    private void UpdateStepNavigationBar()
    {
        if (stepNavigationButtons.Count != NavigationStepCount)
            return;

        Color lockedColor = new Color(0.22f, 0.25f, 0.31f, 1f);
        Color unlockedColor = new Color(0.12f, 0.31f, 0.52f, 1f);
        Color completedColor = new Color(0.08f, 0.5f, 0.31f, 1f);
        Color selectedColor = new Color(0.04f, 0.46f, 0.84f, 1f);

        for (int i = 0; i < stepNavigationButtons.Count; i++)
        {
            Button button = stepNavigationButtons[i];
            bool isUnlocked = i <= highestUnlockedStepIndex;
            bool isCompleted = i < currentStepIndex;
            bool isSelected = i == visibleStepIndex;
            Color baseColor = !isUnlocked
                ? lockedColor
                : isSelected
                    ? selectedColor
                    : isCompleted
                        ? completedColor
                        : unlockedColor;

            button.interactable = isUnlocked;
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = LightenColor(baseColor, 0.1f);
            colors.pressedColor = DarkenColor(baseColor, 0.18f);
            colors.selectedColor = baseColor;
            colors.disabledColor = lockedColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (button.targetGraphic != null)
                button.targetGraphic.color = baseColor;

            if (i < stepNavigationLabels.Count)
            {
                stepNavigationLabels[i].color = isUnlocked
                    ? Color.white
                    : new Color(0.62f, 0.66f, 0.72f, 1f);
            }
        }
    }

    private static Color LightenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount),
            Mathf.Clamp01(color.b + amount),
            color.a);
    }

    private static Color DarkenColor(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a);
    }

    private static GameObject CreatePopupImage(Transform parent, string objectName, Color color)
    {
        GameObject gameObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        gameObject.transform.SetParent(parent, false);

        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return gameObject;
    }

    private static TextMeshProUGUI CreatePopupText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
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

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void LockSystem()
    {
        systemUnlocked = false;

        if (hmiPanel != null)
            hmiPanel.SetActive(false);

        if (cameraStream != null)
            cameraStream.SetActive(false);

        if (plcControllerV2 != null)
            plcControllerV2.SetRuntimeHmiVisible(false);

        Scene hmiScene = SceneManager.GetSceneByName(hmiSceneName);
        if (hmiScene.isLoaded)
            SceneManager.UnloadSceneAsync(hmiScene);
    }

    private void UnlockSystem()
    {
        if (systemUnlocked)
            return;

        systemUnlocked = true;
        SetObjectAndParentsActive(hmiPanel, true);

        if (cameraStream != null)
            cameraStream.SetActive(true);

        OpenHmiScene();

        Debug.Log($"<color=green>✓ HOAN THANH TOAN BO {totalWires} DAY. DA MO HMI.</color>");
    }

    private static void SetObjectAndParentsActive(GameObject target, bool active)
    {
        if (target == null)
            return;

        Transform current = target.transform;
        while (current != null)
        {
            current.gameObject.SetActive(active);
            current = current.parent;
        }
    }
}
