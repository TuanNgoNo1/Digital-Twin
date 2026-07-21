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
    private const int WireLayoutSlotCount = 6;
    private const string StartSceneName = "StartScene";
    private const string CompletionCheatCode = "6394";
    private static readonly string[] NavigationStepTitles = { "Bước 1", "Bước 2", "Bước 3", "Bước 4" };
    private static readonly string[] NavigationStepDescriptions =
    {
        "Đấu nối mạch điều khiển động cơ",
        "Đấu nối encoder",
        "Đấu nối mạch lực",
        "Vận hành"
    };

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
    public float wireDisplayLength = 0.1f;

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
    public Vector2 stepNavigationButtonSize = new Vector2(172f, 82f);
    public Vector2 stepNavigationMargin = new Vector2(24f, 24f);

    [Header("Camera responsive khong crop hai ben")]
    public bool preserveWideCameraFraming = true;
    public float cameraDesignAspect = 2.25f;
    public float cameraDesignVerticalFov = 60f;

    [Header("Heading tren bang socket - chi Buoc 1 den 3")]
    public bool createBoardStepHeading = true;

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
    private string cheatCodeBuffer = string.Empty;
    private GameObject stepResultPopupRoot;
    private GameObject stepNavigationRoot;
    private GameObject guideReturnRoot;
    private RectTransform stepNavigationPanelRect;
    private RectTransform guideReturnButtonRect;
    private readonly List<Button> stepNavigationButtons = new List<Button>();
    private readonly List<StepNavigationItem> stepNavigationItems = new List<StepNavigationItem>();
    private readonly List<SocketPoint> focusedStepSockets = new List<SocketPoint>();
    private TextMeshProUGUI popupIconText;
    private TextMeshProUGUI popupStatusText;
    private TextMeshProUGUI popupMessageText;
    private Image popupIconBackground;
    private Image popupAccent;
    private BoardStepHeading boardStepHeading;
    private static Sprite roundedRectangleSprite;
    private static Sprite socketLabelBackgroundSprite;
    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite playTriangleSprite;
    private static bool hasSavedProgress;
    private static int savedCurrentStepIndex;
    private static int savedVisibleStepIndex;
    private static int savedHighestUnlockedStepIndex;
    private static int savedCompletedWires;
    private static bool savedSystemUnlocked;
    private static readonly List<SavedWireConnection> savedWireConnections = new List<SavedWireConnection>();

    private sealed class StepNavigationItem
    {
        public Image Background;
        public Image Border;
        public Image Shadow;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;
        public readonly List<Graphic> IconGraphics = new List<Graphic>();
    }

    private sealed class SavedWireConnection
    {
        public int StepIndex;
        public string WireName;
        public string SocketA;
        public string SocketB;
    }

    public bool IsPopupVisible => popupVisible || Time.frameCount == popupClosedFrame;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        HandleCompletionCheatCode();
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
        CreateGuideReturnButton();
        EnsureResponsiveCameraFraming();
        EnsureBoardStepHeading();

        if (arrangeWireHeadsOnStart)
            ArrangeAllSteps();

        totalWires = stepRoots.Sum(root => GetStepWires(root).Count);
        currentStepIndex = 0;
        visibleStepIndex = 0;
        highestUnlockedStepIndex = 0;
        completedWires = 0;
        LockSystem();
        RestoreProgressIfNeeded();
        if (visibleStepIndex == HmiStepIndex && systemUnlocked)
        {
            ShowAllCompletedWires();
            OpenHmiScene();
        }
        else
        {
            ShowOnlyStep(visibleStepIndex);
        }

        UpdateStepNavigationBar();
        UpdateGuideReturnButton();

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

    private void EnsureBoardStepHeading()
    {
        if (!createBoardStepHeading)
            return;

        boardStepHeading = FindFirstObjectByType<BoardStepHeading>(FindObjectsInactive.Include);
        if (boardStepHeading == null)
        {
            GameObject headingObject = new GameObject("BoardStepHeading");
            boardStepHeading = headingObject.AddComponent<BoardStepHeading>();
        }
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

    private void HandleCompletionCheatCode()
    {
        if (!initialized || string.IsNullOrEmpty(Input.inputString))
            return;

        foreach (char inputChar in Input.inputString)
        {
            if (!char.IsDigit(inputChar))
                continue;

            cheatCodeBuffer += inputChar;
            if (cheatCodeBuffer.Length > CompletionCheatCode.Length)
                cheatCodeBuffer = cheatCodeBuffer.Substring(cheatCodeBuffer.Length - CompletionCheatCode.Length);

            if (cheatCodeBuffer == CompletionCheatCode)
            {
                CompleteAllWiringStepsWithCheat();
                cheatCodeBuffer = string.Empty;
                return;
            }
        }
    }

    private void CompleteAllWiringStepsWithCheat()
    {
        if (systemUnlocked)
            return;

        pendingStepCompletion = false;
        popupVisible = false;

        if (stepResultPopupRoot != null)
            stepResultPopupRoot.SetActive(false);

        currentStepIndex = stepRoots.Count;
        visibleStepIndex = HmiStepIndex;
        highestUnlockedStepIndex = HmiStepIndex;
        completedWires = totalWires;

        foreach (GameObject stepRoot in stepRoots)
        {
            if (stepRoot == null)
                continue;

            foreach (WireBody wire in GetStepWires(stepRoot))
            {
                if (wire == null)
                    continue;

                wire.isFullyConnected = true;
                wire.isCorrect = true;
            }
        }

        ShowAllCompletedWires();
        UnlockSystem();
        UpdateStepNavigationBar();
        UpdateGuideReturnButton();

        Debug.Log("[Circuit] Cheat code 6394: da hoan thien 3 buoc noi day va mo Buoc 4.");
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

        if (boardStepHeading != null)
            boardStepHeading.ShowStep(visibleStepIndex);

        UpdateStepSocketFocus(visibleStepIndex);
        UpdateGuideReturnButton();
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

        if (boardStepHeading != null)
            boardStepHeading.Hide();

        ClearStepSocketFocus();
        UpdateGuideReturnButton();

        Debug.Log("[Circuit] Da hien lai day ket noi cua ca ba buoc.");
    }

    private void UpdateStepSocketFocus(int stepIndex)
    {
        ClearStepSocketFocus();
        if (stepIndex < 0 || stepIndex >= stepRoots.Count)
            return;

        HashSet<string> socketIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WireBody wire in GetStepWires(stepRoots[stepIndex]))
        {
            if (!string.IsNullOrWhiteSpace(wire.correctSocketA))
                socketIds.Add(wire.correctSocketA.Trim());
            if (!string.IsNullOrWhiteSpace(wire.correctSocketB))
                socketIds.Add(wire.correctSocketB.Trim());
        }

        if (socketIds.Count == 0)
            return;

        foreach (SocketPoint socket in Resources.FindObjectsOfTypeAll<SocketPoint>())
        {
            if (socket == null ||
                !socket.gameObject.scene.IsValid() ||
                string.IsNullOrWhiteSpace(socket.socketID) ||
                !socketIds.Contains(socket.socketID.Trim()))
            {
                continue;
            }

            socket.SetGuideFocus(true);
            focusedStepSockets.Add(socket);
        }
    }

    private void ClearStepSocketFocus()
    {
        foreach (SocketPoint socket in focusedStepSockets)
        {
            if (socket != null)
                socket.SetGuideFocus(false);
        }

        focusedStepSockets.Clear();
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
            UpdateGuideReturnButton();
            Debug.Log("[Circuit] Dang xem Buoc 4: HMI.");
            return;
        }

        CloseHmiScene();
        visibleStepIndex = stepIndex;
        ShowOnlyStep(visibleStepIndex);
        UpdateStepNavigationBar();
        UpdateGuideReturnButton();
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
        bool overStepNavigation = stepNavigationRoot != null &&
            stepNavigationRoot.activeInHierarchy &&
            stepNavigationPanelRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                stepNavigationPanelRect,
                screenPosition,
                null);

        bool overGuideReturn = guideReturnRoot != null &&
            guideReturnRoot.activeInHierarchy &&
            guideReturnButtonRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                guideReturnButtonRect,
                screenPosition,
                null);

        return overStepNavigation || overGuideReturn;
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

        int slotCount = Mathf.Max(WireLayoutSlotCount, wires.Count);
        float firstY = layoutCenter.y + rowSpacing * (slotCount - 1) * 0.5f;
        float leftX = layoutCenter.x - wireDisplayLength * 0.5f;
        float rightX = layoutCenter.x + wireDisplayLength * 0.5f;

        for (int i = 0; i < wires.Count; i++)
        {
            WireBody wire = wires[i];
            float y = firstY - i * rowSpacing;

            PreparePlugForLayout(wire.plugA, new Vector3(leftX, y, layoutCenter.z), wire);
            PreparePlugForLayout(wire.plugB, new Vector3(rightX, y, layoutCenter.z), wire);
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
                if (parent == null)
                    continue;

                Transform existingBackground = parent.Find(backgroundName);
                if (existingBackground != null)
                {
                    Image existingImage = existingBackground.GetComponent<Image>();
                    if (existingImage != null)
                        ApplySocketLabelBackgroundStyle(existingImage);
                    continue;
                }

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
        ApplySocketLabelBackgroundStyle(image);
    }

    private void ApplySocketLabelBackgroundStyle(Image image)
    {
        image.color = socketLabelBackgroundColor;
        image.sprite = GetSocketLabelBackgroundSprite();
        image.type = Image.Type.Sliced;
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

        const float horizontalPadding = 8f;
        const float verticalPadding = 8f;
        const float buttonSpacing = 10f;
        Vector2 buttonSize = new Vector2(
            Mathf.Max(stepNavigationButtonSize.x, 172f),
            Mathf.Max(stepNavigationButtonSize.y, 82f));
        float panelWidth =
            horizontalPadding * 2f +
            buttonSize.x * NavigationStepCount +
            buttonSpacing * (NavigationStepCount - 1);
        float panelHeight = verticalPadding * 2f + buttonSize.y;

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
            Color.clear);
        panel.GetComponent<Image>().raycastTarget = false;
        stepNavigationPanelRect = panel.GetComponent<RectTransform>();
        stepNavigationPanelRect.anchorMin = Vector2.zero;
        stepNavigationPanelRect.anchorMax = Vector2.zero;
        stepNavigationPanelRect.pivot = Vector2.zero;
        stepNavigationPanelRect.anchoredPosition = stepNavigationMargin;
        stepNavigationPanelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        stepNavigationButtons.Clear();
        stepNavigationItems.Clear();

        for (int i = 0; i < NavigationStepCount; i++)
        {
            int stepIndex = i;

            Vector2 buttonPosition = new Vector2(
                horizontalPadding + i * (buttonSize.x + buttonSpacing),
                0f);

            GameObject shadowObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Shadow",
                new Color(0.06f, 0.09f, 0.16f, 0.16f));
            Image shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = GetRoundedRectangleSprite();
            shadowImage.type = Image.Type.Sliced;
            shadowImage.raycastTarget = false;
            SetLeftCenteredRect(
                shadowObject.GetComponent<RectTransform>(),
                buttonSize,
                buttonPosition + new Vector2(0f, -3f));

            GameObject borderObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Border",
                new Color(0.84f, 0.87f, 0.92f, 1f));
            Image borderImage = borderObject.GetComponent<Image>();
            borderImage.sprite = GetRoundedRectangleSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;
            SetLeftCenteredRect(
                borderObject.GetComponent<RectTransform>(),
                buttonSize + new Vector2(2f, 2f),
                buttonPosition + new Vector2(-1f, 0f));

            GameObject buttonObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Button",
                Color.white);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = GetRoundedRectangleSprite();
            buttonImage.type = Image.Type.Sliced;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetLeftCenteredRect(buttonRect, buttonSize, buttonPosition);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => ShowStepFromNavigation(stepIndex));
            stepNavigationButtons.Add(button);

            StepNavigationItem item = new StepNavigationItem
            {
                Background = buttonImage,
                Border = borderImage,
                Shadow = shadowImage
            };

            item.IconGraphics.AddRange(CreateStepNavigationIcon(buttonObject.transform, i));

            TextMeshProUGUI title = CreatePopupText(
                buttonObject.transform,
                "Title",
                NavigationStepTitles[i],
                15f,
                FontStyles.Bold,
                new Color(0.37f, 0.43f, 0.52f, 1f),
                TextAlignmentOptions.Center);
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 17f;
            SetCenteredRect(title.rectTransform, new Vector2(122f, 24f), new Vector2(20f, 22f));
            item.Title = title;

            TextMeshProUGUI description = CreatePopupText(
                buttonObject.transform,
                "Description",
                NavigationStepDescriptions[i],
                13f,
                FontStyles.Bold,
                new Color(0.48f, 0.55f, 0.64f, 1f),
                TextAlignmentOptions.Center);
            description.enableAutoSizing = true;
            description.enableWordWrapping = true;
            description.fontSizeMin = 11f;
            description.fontSizeMax = 14f;
            SetCenteredRect(description.rectTransform, new Vector2(160f, 40f), new Vector2(0f, -16f));
            item.Description = description;

            stepNavigationItems.Add(item);
        }
    }

    private void CreateGuideReturnButton()
    {
        if (guideReturnRoot != null)
            return;

        guideReturnRoot = new GameObject(
            "GuideReturn_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = guideReturnRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4950;

        CanvasScaler scaler = guideReturnRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject buttonObject = CreatePopupImage(
            guideReturnRoot.transform,
            "GuideReturnButton",
            Color.white);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = GetRoundedRectangleSprite();
        buttonImage.type = Image.Type.Sliced;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        guideReturnButtonRect = buttonRect;
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(24f, -24f);
        buttonRect.sizeDelta = new Vector2(210f, 58f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(ReturnToGuidePage);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.95f);
        colors.highlightedColor = new Color(0.94f, 0.97f, 1f, 1f);
        colors.pressedColor = new Color(0.86f, 0.92f, 0.99f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        TextMeshProUGUI text = CreatePopupText(
            buttonObject.transform,
            "Label",
            "V\u1ec1 h\u01b0\u1edbng d\u1eabn",
            22f,
            FontStyles.Bold,
            new Color(0.11f, 0.18f, 0.29f, 1f),
            TextAlignmentOptions.Center);
        text.enableAutoSizing = true;
        text.fontSizeMin = 15f;
        text.fontSizeMax = 22f;
        SetCenteredRect(text.rectTransform, new Vector2(190f, 42f), Vector2.zero);

        UpdateGuideReturnButton();
    }

    private void UpdateGuideReturnButton()
    {
        if (guideReturnRoot != null)
            guideReturnRoot.SetActive(visibleStepIndex >= 0 && visibleStepIndex < HmiStepIndex);
    }

    private void ReturnToGuidePage()
    {
        SaveProgressForGuideReturn();
        StartScreenController.OpenGuidePageOnStart = true;
        StartScreenController.ContinuePracticeFromGuide = false;
        SceneManager.LoadScene(StartSceneName);
    }

    private void SaveProgressForGuideReturn()
    {
        hasSavedProgress = true;
        savedCurrentStepIndex = currentStepIndex;
        savedVisibleStepIndex = visibleStepIndex;
        savedHighestUnlockedStepIndex = highestUnlockedStepIndex;
        savedCompletedWires = completedWires;
        savedSystemUnlocked = systemUnlocked;
        SaveWireConnections();
    }

    private void RestoreProgressIfNeeded()
    {
        if (!StartScreenController.ContinuePracticeFromGuide)
            return;

        StartScreenController.ContinuePracticeFromGuide = false;

        if (!hasSavedProgress)
            return;

        currentStepIndex = Mathf.Clamp(savedCurrentStepIndex, 0, stepRoots.Count);
        visibleStepIndex = Mathf.Clamp(savedVisibleStepIndex, 0, HmiStepIndex);
        highestUnlockedStepIndex = Mathf.Clamp(savedHighestUnlockedStepIndex, 0, HmiStepIndex);
        completedWires = savedCompletedWires;
        RestoreWireConnections();

        if (savedSystemUnlocked)
        {
            systemUnlocked = true;
            SetObjectAndParentsActive(hmiPanel, true);

            if (cameraStream != null)
                cameraStream.SetActive(true);

            if (plcControllerV2 != null)
                plcControllerV2.SetRuntimeHmiVisible(true);
        }

        Debug.Log($"[Circuit] Tiep tuc tu huong dan: Buoc dang lam {currentStepIndex + 1}, dang xem Buoc {visibleStepIndex + 1}.");
    }

    private void SaveWireConnections()
    {
        savedWireConnections.Clear();

        for (int stepIndex = 0; stepIndex < stepRoots.Count; stepIndex++)
        {
            GameObject stepRoot = stepRoots[stepIndex];
            if (stepRoot == null)
                continue;

            foreach (WireBody wire in GetStepWires(stepRoot))
            {
                if (wire == null)
                    continue;

                savedWireConnections.Add(new SavedWireConnection
                {
                    StepIndex = stepIndex,
                    WireName = wire.name,
                    SocketA = GetConnectedSocketId(wire.plugA),
                    SocketB = GetConnectedSocketId(wire.plugB)
                });
            }
        }
    }

    private void RestoreWireConnections()
    {
        if (savedWireConnections.Count == 0)
            return;

        for (int stepIndex = 0; stepIndex < stepRoots.Count; stepIndex++)
        {
            GameObject stepRoot = stepRoots[stepIndex];
            if (stepRoot == null)
                continue;

            Dictionary<string, SavedWireConnection> savedByWireName = savedWireConnections
                .Where(saved => saved.StepIndex == stepIndex)
                .GroupBy(saved => saved.WireName)
                .ToDictionary(group => group.Key, group => group.First());
            Dictionary<string, SocketPoint> socketsById = FindSocketsById(stepRoot);

            foreach (WireBody wire in GetStepWires(stepRoot))
            {
                if (wire == null || !savedByWireName.TryGetValue(wire.name, out SavedWireConnection saved))
                    continue;

                RestorePlugConnection(wire.plugA, saved.SocketA, socketsById);
                RestorePlugConnection(wire.plugB, saved.SocketB, socketsById);
                wire.RefreshConnectionState();
            }
        }
    }

    private static Dictionary<string, SocketPoint> FindSocketsById(GameObject stepRoot)
    {
        Dictionary<string, SocketPoint> socketsById = new Dictionary<string, SocketPoint>(StringComparer.OrdinalIgnoreCase);
        if (stepRoot == null)
            return socketsById;

        foreach (SocketPoint socket in stepRoot.GetComponentsInChildren<SocketPoint>(true))
        {
            if (socket == null || string.IsNullOrWhiteSpace(socket.socketID))
                continue;

            if (!socketsById.ContainsKey(socket.socketID))
                socketsById.Add(socket.socketID, socket);
        }

        return socketsById;
    }

    private static void RestorePlugConnection(
        WirePlug plug,
        string socketId,
        Dictionary<string, SocketPoint> socketsById)
    {
        if (plug == null)
            return;

        if (plug.connectedSocket != null)
            plug.connectedSocket.isOccupied = false;

        plug.connectedSocket = null;
        plug.isSnapped = false;

        if (string.IsNullOrWhiteSpace(socketId) || !socketsById.TryGetValue(socketId, out SocketPoint socket))
            return;

        plug.connectedSocket = socket;
        plug.isSnapped = true;
        socket.isOccupied = true;
        plug.transform.position = socket.transform.position;
        plug.transform.rotation = socket.transform.rotation;
    }

    private static string GetConnectedSocketId(WirePlug plug)
    {
        if (plug == null || !plug.isSnapped || plug.connectedSocket == null)
            return string.Empty;

        return plug.connectedSocket.socketID;
    }

    private void UpdateStepNavigationBar()
    {
        if (stepNavigationButtons.Count != NavigationStepCount ||
            stepNavigationItems.Count != NavigationStepCount)
            return;

        Color selectedBackground = new Color(0.04f, 0.39f, 0.92f, 1f);
        Color normalBackground = new Color(1f, 1f, 1f, 1f);
        Color lockedBackground = new Color(0.96f, 0.97f, 0.99f, 1f);
        Color selectedText = Color.white;
        Color normalTitle = new Color(0.36f, 0.42f, 0.51f, 1f);
        Color normalDescription = new Color(0.5f, 0.57f, 0.66f, 1f);
        Color lockedText = new Color(0.58f, 0.64f, 0.72f, 1f);
        Color normalBorder = new Color(0.84f, 0.87f, 0.92f, 1f);
        Color selectedBorder = new Color(0.04f, 0.39f, 0.92f, 1f);
        Color lockedBorder = new Color(0.88f, 0.9f, 0.94f, 1f);
        Color shadowColor = new Color(0.06f, 0.09f, 0.16f, 0.16f);

        for (int i = 0; i < stepNavigationButtons.Count; i++)
        {
            Button button = stepNavigationButtons[i];
            StepNavigationItem item = stepNavigationItems[i];
            bool isUnlocked = i <= highestUnlockedStepIndex;
            bool isSelected = i == visibleStepIndex;
            Color backgroundColor = !isUnlocked
                ? lockedBackground
                : isSelected
                    ? selectedBackground
                    : normalBackground;

            button.interactable = isUnlocked;
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = isSelected
                ? LightenColor(selectedBackground, 0.08f)
                : new Color(0.94f, 0.97f, 1f, 1f);
            colors.pressedColor = isSelected
                ? DarkenColor(selectedBackground, 0.12f)
                : new Color(0.89f, 0.93f, 0.98f, 1f);
            colors.selectedColor = backgroundColor;
            colors.disabledColor = lockedBackground;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (button.targetGraphic != null)
                button.targetGraphic.color = backgroundColor;

            if (item.Background != null)
                item.Background.color = backgroundColor;

            if (item.Border != null)
                item.Border.color = !isUnlocked
                    ? lockedBorder
                    : isSelected
                        ? selectedBorder
                        : normalBorder;

            if (item.Shadow != null)
                item.Shadow.color = isSelected
                    ? new Color(0.04f, 0.25f, 0.55f, 0.24f)
                    : shadowColor;

            Color titleColor = !isUnlocked
                ? lockedText
                : isSelected
                    ? selectedText
                    : normalTitle;
            Color descriptionColor = !isUnlocked
                ? lockedText
                : isSelected
                    ? selectedText
                    : normalDescription;

            if (item.Title != null)
                item.Title.color = titleColor;

            if (item.Description != null)
                item.Description.color = descriptionColor;

            foreach (Graphic iconGraphic in item.IconGraphics)
            {
                if (iconGraphic != null)
                    iconGraphic.color = titleColor;
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

    private static List<Graphic> CreateStepNavigationIcon(Transform parent, int stepIndex)
    {
        GameObject iconRoot = new GameObject("Icon", typeof(RectTransform));
        iconRoot.transform.SetParent(parent, false);
        SetCenteredRect(iconRoot.GetComponent<RectTransform>(), new Vector2(22f, 22f), new Vector2(-42f, 16f));

        List<Graphic> graphics = new List<Graphic>();
        switch (stepIndex)
        {
            case 0:
                graphics.Add(CreateIconImage(iconRoot.transform, "CableLine", new Vector2(16f, 2.4f), new Vector2(0f, -1f), -28f, GetRoundedRectangleSprite()));
                graphics.Add(CreateIconImage(iconRoot.transform, "PlugA", new Vector2(6.5f, 7f), new Vector2(-6f, 3.2f), -28f, GetRoundedRectangleSprite()));
                graphics.Add(CreateIconImage(iconRoot.transform, "PlugB", new Vector2(6.5f, 7f), new Vector2(6f, -5.2f), -28f, GetRoundedRectangleSprite()));
                break;
            case 1:
                graphics.Add(CreateIconImage(iconRoot.transform, "PowerRing", new Vector2(18f, 18f), Vector2.zero, 0f, GetRingSprite()));
                graphics.Add(CreateIconImage(iconRoot.transform, "PowerLine", new Vector2(3f, 10f), new Vector2(0f, 4.5f), 0f, GetRoundedRectangleSprite()));
                break;
            case 2:
                graphics.Add(CreateIconImage(iconRoot.transform, "SearchRing", new Vector2(14f, 14f), new Vector2(-2f, 2f), 0f, GetRingSprite()));
                graphics.Add(CreateIconImage(iconRoot.transform, "SearchHandle", new Vector2(9f, 3f), new Vector2(5f, -5f), -45f, GetRoundedRectangleSprite()));
                break;
            default:
                graphics.Add(CreateIconImage(iconRoot.transform, "RunRing", new Vector2(18f, 18f), Vector2.zero, 0f, GetRingSprite()));
                graphics.Add(CreateIconImage(iconRoot.transform, "RunPlay", new Vector2(9f, 11f), new Vector2(1.4f, 0f), 0f, GetPlayTriangleSprite()));
                break;
        }

        return graphics;
    }

    private static Image CreateIconImage(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 position,
        float rotationZ = 0f,
        Sprite sprite = null)
    {
        GameObject imageObject = CreatePopupImage(parent, objectName, Color.white);
        Image image = imageObject.GetComponent<Image>();
        image.raycastTarget = false;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
        }

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, size, position);
        rect.localEulerAngles = new Vector3(0f, 0f, rotationZ);
        return image;
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

    private static void SetLeftCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Sprite GetRoundedRectangleSprite()
    {
        if (roundedRectangleSprite != null)
            return roundedRectangleSprite;

        roundedRectangleSprite = CreateRoundedSprite("RuntimeRoundedRectangle", 64, 12);
        return roundedRectangleSprite;
    }

    private static Sprite GetSocketLabelBackgroundSprite()
    {
        if (socketLabelBackgroundSprite != null)
            return socketLabelBackgroundSprite;

        socketLabelBackgroundSprite = CreateRoundedSprite("RuntimeSocketLabelBackground", 64, 12);
        return socketLabelBackgroundSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        circleSprite = CreateRoundedSprite("RuntimeCircle", 32, 16);
        return circleSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite != null)
            return ringSprite;

        ringSprite = CreateRingSprite("RuntimeIconRing", 64, 25f, 18f);
        return ringSprite;
    }

    private static Sprite GetPlayTriangleSprite()
    {
        if (playTriangleSprite != null)
            return playTriangleSprite;

        playTriangleSprite = CreatePlayTriangleSprite("RuntimePlayTriangle", 64);
        return playTriangleSprite;
    }

    private static Sprite CreateRingSprite(string textureName, int size, float outerRadius, float innerRadius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            name = textureName,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);
        float center = size * 0.5f;
        float outerSqr = outerRadius * outerRadius;
        float innerSqr = innerRadius * innerRadius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float distanceSqr = dx * dx + dy * dy;
                pixels[y * size + x] = distanceSqr <= outerSqr && distanceSqr >= innerSqr
                    ? Color.white
                    : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreatePlayTriangleSprite(string textureName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            name = textureName,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 leftTop = new Vector2(0.26f, 0.18f);
        Vector2 leftBottom = new Vector2(0.26f, 0.82f);
        Vector2 rightCenter = new Vector2(0.82f, 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                pixels[y * size + x] = IsPointInTriangle(point, leftTop, leftBottom, rightCenter)
                    ? Color.white
                    : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = TriangleSign(point, a, b);
        float d2 = TriangleSign(point, b, c);
        float d3 = TriangleSign(point, c, a);

        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float TriangleSign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) -
            (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static Sprite CreateRoundedSprite(string textureName, int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            name = textureName,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x + 0.5f, y + 0.5f, size, radius);
                pixels[y * size + x] = inside ? Color.white : clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static bool IsInsideRoundedRect(float x, float y, int size, int radius)
    {
        float left = radius;
        float right = size - radius;
        float bottom = radius;
        float top = size - radius;

        if ((x >= left && x <= right) || (y >= bottom && y <= top))
            return true;

        float centerX = x < left ? left : right;
        float centerY = y < bottom ? bottom : top;
        float dx = x - centerX;
        float dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
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
