using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircuitManager : MonoBehaviour
{
    private const int NavigationStepCount = 4;
    private const int HmiStepIndex = 3;
    private const int WireLayoutSlotCount = 6;
    private const string StartSceneName = "StartScene";
    private const string CompletionCheatCode = "6394";
    private static readonly string[] PracticalNavigationLabels =
    {
        "1. \u0110\u1EA5u n\u1ED1i m\u1EA1ch \u0111i\u1EC1u khi\u1EC3n \u0111\u1ED9ng c\u01A1",
        "2. \u0110\u1EA5u n\u1ED1i encoder",
        "\u0110\u1EA5u n\u1ED1i m\u1EA1ch l\u1EF1c",
        "4. V\u1EADn h\u00E0nh"
    };
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

    [Header("Bo cuc thuc hanh dang the")]
    public bool createPracticalWorkspaceLayout = true;
    public float practicalCameraVerticalFov = 68f;
    public Vector2 practicalCameraOffset = new Vector2(0.044f, -0.062f);

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
    private GameObject practicalWorkspaceRoot;
    private GameObject practicalWorkspaceMaskRoot;
    private RectTransform stepNavigationPanelRect;
    private RectTransform guideReturnButtonRect;
    private readonly List<Button> stepNavigationButtons = new List<Button>();
    private readonly List<StepNavigationItem> stepNavigationItems = new List<StepNavigationItem>();
    private readonly List<SocketPoint> focusedStepSockets = new List<SocketPoint>();
    private TextMeshProUGUI popupIconText;
    private TextMeshProUGUI popupStatusText;
    private TextMeshProUGUI popupMessageText;
    private TextMeshProUGUI popupButtonText;
    private TextMeshProUGUI practicalInstructionText;
    private TextMeshProUGUI practicalWireGuideText;
    private readonly List<GameObject> practicalWireRows = new List<GameObject>();
    private readonly List<TextMeshProUGUI> practicalWireRowNumbers = new List<TextMeshProUGUI>();
    private readonly List<Image> practicalWireRowBackgrounds = new List<Image>();
    private readonly List<Image> popupCheckGraphics = new List<Image>();
    private Image popupIconBackground;
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
        MoveCoveredSocketLabelsAwayFromWires();
        EnsureSocketLabelBackgrounds();
        CreateStepResultPopup();
        CreateStepNavigationBar();
        CreateGuideReturnButton();
        EnsureResponsiveCameraFraming();
        CreatePracticalWorkspaceLayout();
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

        framing.designAspect = createPracticalWorkspaceLayout
            ? 16f / 9f
            : cameraDesignAspect;
        framing.designVerticalFov = createPracticalWorkspaceLayout
            ? practicalCameraVerticalFov
            : cameraDesignVerticalFov;

        if (createPracticalWorkspaceLayout)
        {
            mainCamera.transform.position +=
                mainCamera.transform.right * practicalCameraOffset.x +
                mainCamera.transform.up * practicalCameraOffset.y;
        }

        framing.ApplyFraming();
    }

    private void EnsureBoardStepHeading()
    {
        if (createPracticalWorkspaceLayout)
        {
            BoardStepHeading existingHeading = FindFirstObjectByType<BoardStepHeading>(FindObjectsInactive.Include);
            if (existingHeading != null)
                existingHeading.gameObject.SetActive(false);
            return;
        }

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
            SaveWireConnections();
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
        UpdatePracticalWorkspaceLayout();
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
        UpdatePracticalWorkspaceLayout();

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

            SaveWireConnections();
            visibleStepIndex = HmiStepIndex;
            ShowAllCompletedWires();
            OpenHmiScene();
            UpdateStepNavigationBar();
            UpdateGuideReturnButton();
            Debug.Log("[Circuit] Dang xem Buoc 4: HMI.");
            return;
        }

        bool returningFromHmi = visibleStepIndex == HmiStepIndex;
        CloseHmiScene();
        visibleStepIndex = stepIndex;
        if (returningFromHmi)
            RestoreWireConnections();
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

        if (createPracticalWorkspaceLayout && ArrangeStepInWireCard(wires))
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

    private bool ArrangeStepInWireCard(List<WireBody> wires)
    {
        Camera mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (mainCamera == null || Screen.width <= 0 || Screen.height <= 0)
            return false;

        Plane wirePlane = new Plane(Vector3.forward, new Vector3(0f, 0f, layoutCenter.z));
        const float rowCenterX = 0.892f;
        const float plugHalfSpacing = 0.033f;
        const float firstRowFromTop = 0.288f;
        const float rowSpacingFromTop = 0.103f;

        for (int i = 0; i < wires.Count; i++)
        {
            float rowY = 1f - firstRowFromTop - i * rowSpacingFromTop;
            Vector3 leftScreen = new Vector3(
                Screen.width * (rowCenterX - plugHalfSpacing),
                Screen.height * rowY,
                0f);
            Vector3 rightScreen = new Vector3(
                Screen.width * (rowCenterX + plugHalfSpacing),
                Screen.height * rowY,
                0f);

            if (!TryScreenPointOnWirePlane(mainCamera, wirePlane, leftScreen, out Vector3 leftPosition) ||
                !TryScreenPointOnWirePlane(mainCamera, wirePlane, rightScreen, out Vector3 rightPosition))
            {
                return false;
            }

            PreparePlugForLayout(wires[i].plugA, leftPosition, wires[i]);
            PreparePlugForLayout(wires[i].plugB, rightPosition, wires[i]);
        }

        return true;
    }

    private static bool TryScreenPointOnWirePlane(
        Camera camera,
        Plane plane,
        Vector3 screenPoint,
        out Vector3 worldPoint)
    {
        Ray ray = camera.ScreenPointToRay(screenPoint);
        if (plane.Raycast(ray, out float distance))
        {
            worldPoint = ray.GetPoint(distance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
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
        string wireNumbers = string.Join(", ", wrongWires.Select(GetWireDisplayName));

        Color errorColor = new Color(0.78f, 0.08f, 0.1f, 1f);
        popupIconBackground.color = errorColor;
        popupIconText.text = "!";
        popupIconText.color = errorColor;
        popupIconText.gameObject.SetActive(true);
        foreach (Image checkGraphic in popupCheckGraphics)
            checkGraphic.gameObject.SetActive(false);
        popupStatusText.color = new Color(0.24f, 0.27f, 0.34f, 1f);
        popupStatusText.fontStyle = FontStyles.Normal;
        popupStatusText.text =
            "Gi\u1EAFc c\u1EAFm ch\u01B0a \u0111\u00FAng. Vui l\u00F2ng c\u1EAFm l\u1EA1i theo h\u01B0\u1EDBng d\u1EABn.";
        popupMessageText.color = errorColor;
        popupMessageText.text = "D\u00E2y c\u1EAFm sai: " + wireNumbers;
        popupButtonText.text = "Th\u1EED l\u1EA1i";

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
        ShowPopup();
    }

    private void ShowPopup()
    {
        if (pendingStepCompletion)
        {
            Color successColor = new Color(0.08f, 0.67f, 0.04f, 1f);
            popupIconBackground.color = successColor;
            popupIconText.gameObject.SetActive(false);
            foreach (Image checkGraphic in popupCheckGraphics)
            {
                checkGraphic.color = successColor;
                checkGraphic.gameObject.SetActive(true);
            }
            popupStatusText.color = new Color(0.24f, 0.27f, 0.34f, 1f);
            popupStatusText.fontStyle = FontStyles.Normal;
            popupStatusText.text =
                $"B\u1EA1n \u0111\u00E3 ho\u00E0n th\u00E0nh b\u01B0\u1EDBc {currentStepIndex + 1}.";
            popupMessageText.text = string.Empty;
            popupButtonText.text = "Ti\u1EBFp t\u1EE5c";
        }

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

    private void MoveCoveredSocketLabelsAwayFromWires()
    {
        Dictionary<string, SocketPoint> socketsById = new Dictionary<string, SocketPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (SocketPoint socket in Resources.FindObjectsOfTypeAll<SocketPoint>())
        {
            if (socket == null ||
                !socket.gameObject.scene.IsValid() ||
                string.IsNullOrWhiteSpace(socket.socketID) ||
                socketsById.ContainsKey(socket.socketID.Trim()))
            {
                continue;
            }

            socketsById.Add(socket.socketID.Trim(), socket);
        }

        int movedCount = 0;
        int stepCount = Mathf.Min(stepRoots.Count, guideRoots.Count);
        for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
        {
            GameObject stepRoot = stepRoots[stepIndex];
            GameObject guideRoot = guideRoots[stepIndex];
            if (stepRoot == null || guideRoot == null)
                continue;

            TextMeshProUGUI[] labels = guideRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (WireBody wire in GetStepWires(stepRoot))
            {
                movedCount += MoveCoveredEndpointLabel(
                    labels,
                    socketsById,
                    wire,
                    "A",
                    wire.correctSocketA,
                    wire.correctSocketB);
                movedCount += MoveCoveredEndpointLabel(
                    labels,
                    socketsById,
                    wire,
                    "B",
                    wire.correctSocketB,
                    wire.correctSocketA);
            }
        }

        Debug.Log($"[Circuit] Da doi ben {movedCount} nhan socket bi day che.");
    }

    private static int MoveCoveredEndpointLabel(
        IEnumerable<TextMeshProUGUI> labels,
        IReadOnlyDictionary<string, SocketPoint> socketsById,
        WireBody wire,
        string endpoint,
        string socketId,
        string otherSocketId)
    {
        if (wire == null ||
            string.IsNullOrWhiteSpace(socketId) ||
            string.IsNullOrWhiteSpace(otherSocketId) ||
            !socketsById.TryGetValue(socketId.Trim(), out SocketPoint socket) ||
            !socketsById.TryGetValue(otherSocketId.Trim(), out SocketPoint otherSocket))
        {
            return 0;
        }

        string labelPrefix = $"Label_{wire.name}_{endpoint}_";
        TextMeshProUGUI label = labels.FirstOrDefault(candidate =>
            candidate != null && candidate.name.StartsWith(labelPrefix, StringComparison.Ordinal));
        if (label == null)
            return 0;

        Vector2 socketPosition = socket.transform.position;
        Vector2 labelOffset = (Vector2)label.transform.position - socketPosition;
        Vector2 connectionDirection = (Vector2)otherSocket.transform.position - socketPosition;
        float magnitudes = labelOffset.magnitude * connectionDirection.magnitude;
        if (magnitudes <= Mathf.Epsilon || Vector2.Dot(labelOffset, connectionDirection) / magnitudes <= 0.2f)
            return 0;

        Vector2 oppositePosition = socketPosition - labelOffset;
        Vector3 currentPosition = label.transform.position;
        label.transform.position = new Vector3(oppositePosition.x, oppositePosition.y, currentPosition.z);
        return 1;
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

        Vector2 compactPopupSize = createPracticalWorkspaceLayout
            ? new Vector2(900f, 320f)
            : stepResultPopupSize;

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
            new Color(0.02f, 0.03f, 0.05f, 0.32f));
        StretchRect(dimBackground.GetComponent<RectTransform>());

        GameObject shadow = CreatePopupImage(
            stepResultPopupRoot.transform,
            "CardShadow",
            new Color(0f, 0f, 0f, 0.18f));
        Image shadowImage = shadow.GetComponent<Image>();
        shadowImage.sprite = GetRoundedRectangleSprite();
        shadowImage.type = Image.Type.Sliced;
        SetCenteredRect(shadow.GetComponent<RectTransform>(), compactPopupSize, new Vector2(7f, -8f));

        GameObject card = CreatePopupImage(
            stepResultPopupRoot.transform,
            "Card",
            Color.white);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = GetRoundedRectangleSprite();
        cardImage.type = Image.Type.Sliced;
        SetCenteredRect(card.GetComponent<RectTransform>(), compactPopupSize, Vector2.zero);

        GameObject accentObject = CreatePopupImage(
            card.transform,
            "TopAccent",
            new Color(1f, 0.69f, 0.08f, 1f));
        accentObject.SetActive(false);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 8f);

        GameObject iconObject = CreatePopupImage(
            card.transform,
            "StatusIcon",
            new Color(0.78f, 0.08f, 0.1f, 1f));
        popupIconBackground = iconObject.GetComponent<Image>();
        popupIconBackground.sprite = GetRingSprite();
        popupIconBackground.type = Image.Type.Simple;
        popupIconBackground.raycastTarget = false;
        SetCenteredRect(iconObject.GetComponent<RectTransform>(), new Vector2(70f, 70f), new Vector2(0f, 92f));
        popupIconText = CreatePopupText(
            iconObject.transform,
            "IconText",
            "!",
            42f,
            FontStyles.Bold,
            new Color(0.78f, 0.08f, 0.1f, 1f),
            TextAlignmentOptions.Center);
        StretchRect(popupIconText.rectTransform);

        popupCheckGraphics.Clear();
        popupCheckGraphics.Add(CreateIconImage(
            iconObject.transform,
            "CheckShort",
            new Vector2(24f, 7f),
            new Vector2(-10f, -3f),
            -45f,
            GetRoundedRectangleSprite()));
        popupCheckGraphics.Add(CreateIconImage(
            iconObject.transform,
            "CheckLong",
            new Vector2(39f, 7f),
            new Vector2(8f, 1f),
            45f,
            GetRoundedRectangleSprite()));
        foreach (Image checkGraphic in popupCheckGraphics)
        {
            checkGraphic.color = new Color(0.08f, 0.67f, 0.04f, 1f);
            checkGraphic.gameObject.SetActive(false);
        }

        TextMeshProUGUI title = CreatePopupText(
            card.transform,
            "Title",
            "THÔNG BÁO KẾT QUẢ",
            27f,
            FontStyles.Bold,
            new Color(0.09f, 0.13f, 0.21f, 1f),
            TextAlignmentOptions.Left);
        SetCenteredRect(title.rectTransform, new Vector2(460f, 54f), new Vector2(55f, 160f));
        title.gameObject.SetActive(false);

        GameObject divider = CreatePopupImage(
            card.transform,
            "Divider",
            new Color(0.84f, 0.87f, 0.92f, 1f));
        SetCenteredRect(divider.GetComponent<RectTransform>(), new Vector2(540f, 2f), new Vector2(0f, 120f));
        divider.SetActive(false);

        popupStatusText = CreatePopupText(
            card.transform,
            "Status",
            string.Empty,
            27f,
            FontStyles.Normal,
            new Color(0.24f, 0.27f, 0.34f, 1f),
            TextAlignmentOptions.Center);
        popupStatusText.enableAutoSizing = true;
        popupStatusText.fontSizeMin = 20f;
        popupStatusText.fontSizeMax = 27f;
        popupStatusText.enableWordWrapping = false;
        SetCenteredRect(popupStatusText.rectTransform, new Vector2(820f, 52f), new Vector2(0f, 22f));

        popupMessageText = CreatePopupText(
            card.transform,
            "Message",
            string.Empty,
            24f,
            FontStyles.Normal,
            new Color(0.78f, 0.08f, 0.1f, 1f),
            TextAlignmentOptions.Center);
        popupMessageText.enableAutoSizing = true;
        popupMessageText.fontSizeMin = 18f;
        popupMessageText.fontSizeMax = 24f;
        popupMessageText.enableWordWrapping = false;
        popupMessageText.overflowMode = TextOverflowModes.Overflow;
        SetCenteredRect(popupMessageText.rectTransform, new Vector2(800f, 44f), new Vector2(0f, -28f));

        GameObject buttonObject = CreatePopupImage(
            card.transform,
            "OK_Button",
            new Color(0.74f, 0.07f, 0.09f, 1f));
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = GetRoundedRectangleSprite();
        buttonImage.type = Image.Type.Sliced;
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), new Vector2(160f, 58f), new Vector2(0f, -108f));

        Button okButton = buttonObject.AddComponent<Button>();
        ColorBlock buttonColors = okButton.colors;
        buttonColors.normalColor = new Color(0.74f, 0.07f, 0.09f, 1f);
        buttonColors.highlightedColor = new Color(0.84f, 0.1f, 0.12f, 1f);
        buttonColors.pressedColor = new Color(0.62f, 0.04f, 0.06f, 1f);
        buttonColors.selectedColor = buttonColors.normalColor;
        okButton.colors = buttonColors;
        okButton.onClick.AddListener(HandlePopupOk);

        popupButtonText = CreatePopupText(
            buttonObject.transform,
            "Text",
            "Th\u1EED l\u1EA1i",
            23f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(popupButtonText.rectTransform);

        stepResultPopupRoot.SetActive(false);
    }

    private void CreatePracticalWorkspaceLayout()
    {
        if (!createPracticalWorkspaceLayout || practicalWorkspaceRoot != null)
            return;

        Camera mainCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (mainCamera == null)
            return;

        HideLegacyPracticalPanels();

        practicalWorkspaceRoot = new GameObject(
            "PracticalWorkspace_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas workspaceCanvas = practicalWorkspaceRoot.GetComponent<Canvas>();
        workspaceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        workspaceCanvas.worldCamera = mainCamera;
        workspaceCanvas.planeDistance = Mathf.Max(mainCamera.nearClipPlane + 0.45f, 0.75f);
        workspaceCanvas.sortingOrder = 100;

        CanvasScaler workspaceScaler = practicalWorkspaceRoot.GetComponent<CanvasScaler>();
        workspaceScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        workspaceScaler.referenceResolution = new Vector2(1920f, 1080f);
        workspaceScaler.matchWidthOrHeight = 0.5f;

        Color pageBackground = new Color(0.965f, 0.965f, 0.965f, 1f);
        Color cardBorder = new Color(0.84f, 0.85f, 0.87f, 1f);
        Color cardShadow = new Color(0.12f, 0.14f, 0.17f, 0.12f);
        Color textColor = new Color(0.25f, 0.28f, 0.33f, 1f);
        Color red = new Color(0.82f, 0.12f, 0.15f, 1f);

        GameObject page = CreatePopupImage(
            practicalWorkspaceRoot.transform,
            "PageBackground",
            pageBackground);
        page.GetComponent<Image>().raycastTarget = false;
        StretchRect(page.GetComponent<RectTransform>());

        CreateWorkspaceCard(
            practicalWorkspaceRoot.transform,
            "ModelPanel",
            new Vector2(525f, 144f),
            new Vector2(942f, 804f),
            cardBorder,
            cardShadow);

        GameObject guideCard = CreateWorkspaceCard(
            practicalWorkspaceRoot.transform,
            "GuideCard",
            new Vector2(52f, 144f),
            new Vector2(442f, 544f),
            cardBorder,
            cardShadow);

        CreateDocumentIcon(guideCard.transform, new Vector2(44f, 43f), red);
        TextMeshProUGUI guideHeading = CreatePopupText(
            guideCard.transform,
            "Heading",
            "H\u01B0\u1EDBng d\u1EABn",
            36f,
            FontStyles.Bold,
            new Color(0.16f, 0.17f, 0.19f, 1f),
            TextAlignmentOptions.MidlineLeft);
        SetTopLeftRect(guideHeading.rectTransform, new Vector2(300f, 58f), new Vector2(86f, 31f));

        GameObject guideDivider = CreatePopupImage(
            guideCard.transform,
            "Divider",
            new Color(0.87f, 0.88f, 0.9f, 1f));
        guideDivider.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(guideDivider.GetComponent<RectTransform>(), new Vector2(354f, 2f), new Vector2(44f, 94f));

        practicalInstructionText = CreatePopupText(
            guideCard.transform,
            "Instruction",
            string.Empty,
            29f,
            FontStyles.Normal,
            new Color(0.34f, 0.38f, 0.44f, 1f),
            TextAlignmentOptions.TopLeft);
        practicalInstructionText.enableWordWrapping = true;
        practicalInstructionText.lineSpacing = 5f;
        SetTopLeftRect(
            practicalInstructionText.rectTransform,
            new Vector2(354f, 130f),
            new Vector2(44f, 122f));

        practicalWireGuideText = CreatePopupText(
            guideCard.transform,
            "WireGuide",
            string.Empty,
            27f,
            FontStyles.Normal,
            textColor,
            TextAlignmentOptions.TopLeft);
        practicalWireGuideText.enableWordWrapping = false;
        practicalWireGuideText.overflowMode = TextOverflowModes.Overflow;
        practicalWireGuideText.lineSpacing = 10f;
        SetTopLeftRect(
            practicalWireGuideText.rectTransform,
            new Vector2(342f, 250f),
            new Vector2(58f, 283f));

        GameObject wireCard = CreateWorkspaceCard(
            practicalWorkspaceRoot.transform,
            "WireCard",
            new Vector2(1492f, 144f),
            new Vector2(381f, 804f),
            cardBorder,
            cardShadow);

        CreateWireIcon(wireCard.transform, new Vector2(44f, 47f), red);
        TextMeshProUGUI wireHeading = CreatePopupText(
            wireCard.transform,
            "Heading",
            "B\u1ED9 d\u00E2y",
            36f,
            FontStyles.Bold,
            new Color(0.16f, 0.17f, 0.19f, 1f),
            TextAlignmentOptions.MidlineLeft);
        SetTopLeftRect(wireHeading.rectTransform, new Vector2(230f, 58f), new Vector2(86f, 31f));

        GameObject wireDivider = CreatePopupImage(
            wireCard.transform,
            "Divider",
            new Color(0.87f, 0.88f, 0.9f, 1f));
        wireDivider.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(wireDivider.GetComponent<RectTransform>(), new Vector2(292f, 2f), new Vector2(44f, 94f));

        practicalWireRows.Clear();
        practicalWireRowNumbers.Clear();
        practicalWireRowBackgrounds.Clear();
        for (int i = 0; i < WireLayoutSlotCount; i++)
        {
            GameObject row = CreatePopupImage(
                wireCard.transform,
                $"WireRow_{i + 1}",
                new Color(0.99f, 0.98f, 0.98f, 0.92f));
            Image rowImage = row.GetComponent<Image>();
            rowImage.sprite = GetRoundedRectangleSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.raycastTarget = false;
            SetTopLeftRect(
                row.GetComponent<RectTransform>(),
                new Vector2(292f, 99f),
                new Vector2(44f, 117f + i * 112f));

            GameObject rowBorder = CreatePopupImage(
                row.transform,
                "Border",
                new Color(0.86f, 0.87f, 0.89f, 1f));
            Image rowBorderImage = rowBorder.GetComponent<Image>();
            rowBorderImage.sprite = GetRoundedRectangleSprite();
            rowBorderImage.type = Image.Type.Sliced;
            rowBorderImage.raycastTarget = false;
            StretchRect(rowBorder.GetComponent<RectTransform>());
            rowBorder.transform.SetAsFirstSibling();

            GameObject rowSurface = CreatePopupImage(
                row.transform,
                "Surface",
                rowImage.color);
            Image rowSurfaceImage = rowSurface.GetComponent<Image>();
            rowSurfaceImage.sprite = GetRoundedRectangleSprite();
            rowSurfaceImage.type = Image.Type.Sliced;
            rowSurfaceImage.raycastTarget = false;
            SetTopLeftRect(
                rowSurface.GetComponent<RectTransform>(),
                new Vector2(288f, 95f),
                new Vector2(2f, 2f));

            TextMeshProUGUI number = CreatePopupText(
                row.transform,
                "Number",
                (i + 1).ToString(),
                31f,
                FontStyles.Normal,
                new Color(0.4f, 0.43f, 0.48f, 1f),
                TextAlignmentOptions.Center);
            SetTopLeftRect(number.rectTransform, new Vector2(52f, 99f), new Vector2(10f, 0f));

            practicalWireRows.Add(row);
            practicalWireRowNumbers.Add(number);
            practicalWireRowBackgrounds.Add(rowSurfaceImage);
        }

        CreatePracticalWorkspaceMasks(pageBackground);
        UpdatePracticalWorkspaceLayout();
    }

    private GameObject CreateWorkspaceCard(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Color borderColor,
        Color shadowColor)
    {
        GameObject shadow = CreatePopupImage(parent, objectName + "_Shadow", shadowColor);
        Image shadowImage = shadow.GetComponent<Image>();
        shadowImage.sprite = GetRoundedRectangleSprite();
        shadowImage.type = Image.Type.Sliced;
        shadowImage.raycastTarget = false;
        SetTopLeftRect(shadow.GetComponent<RectTransform>(), size, position + new Vector2(4f, 5f));

        GameObject border = CreatePopupImage(parent, objectName + "_Border", borderColor);
        Image borderImage = border.GetComponent<Image>();
        borderImage.sprite = GetRoundedRectangleSprite();
        borderImage.type = Image.Type.Sliced;
        borderImage.raycastTarget = false;
        SetTopLeftRect(border.GetComponent<RectTransform>(), size + new Vector2(4f, 4f), position - new Vector2(2f, 2f));

        GameObject card = CreatePopupImage(parent, objectName, Color.white);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = GetRoundedRectangleSprite();
        cardImage.type = Image.Type.Sliced;
        cardImage.raycastTarget = false;
        SetTopLeftRect(card.GetComponent<RectTransform>(), size, position);
        return card;
    }

    private void CreatePracticalWorkspaceMasks(Color backgroundColor)
    {
        practicalWorkspaceMaskRoot = new GameObject(
            "PracticalWorkspaceMasks_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas maskCanvas = practicalWorkspaceMaskRoot.GetComponent<Canvas>();
        maskCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        maskCanvas.sortingOrder = 4800;

        CanvasScaler scaler = practicalWorkspaceMaskRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject topMask = CreatePopupImage(practicalWorkspaceMaskRoot.transform, "TopMask", backgroundColor);
        topMask.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(topMask.GetComponent<RectTransform>(), new Vector2(1920f, 144f), Vector2.zero);

        GameObject bottomMask = CreatePopupImage(practicalWorkspaceMaskRoot.transform, "BottomMask", backgroundColor);
        bottomMask.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(bottomMask.GetComponent<RectTransform>(), new Vector2(1920f, 132f), new Vector2(0f, 948f));
    }

    private void HideLegacyPracticalPanels()
    {
        foreach (string panelName in new[] { "bd-Photoroom", "bhd (1)" })
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel != null)
                panel.SetActive(false);
        }

        foreach (GameObject guideRoot in guideRoots)
        {
            if (guideRoot == null)
                continue;

            Transform instructionPanel = guideRoot.transform.Find("InstructionPanel");
            if (instructionPanel != null)
                instructionPanel.gameObject.SetActive(false);
        }

        foreach (GameObject stepRoot in stepRoots)
        {
            if (stepRoot == null)
                continue;

            foreach (Transform child in stepRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals("StepUI", StringComparison.OrdinalIgnoreCase))
                    child.gameObject.SetActive(false);
            }

            foreach (TextMeshProUGUI label in stepRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name.StartsWith("D\u00E2y ", StringComparison.OrdinalIgnoreCase))
                    label.gameObject.SetActive(false);
            }
        }
    }

    private static void CreateDocumentIcon(Transform parent, Vector2 position, Color color)
    {
        GameObject outline = CreatePopupImage(parent, "GuideIcon", color);
        outline.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(outline.GetComponent<RectTransform>(), new Vector2(25f, 31f), position);

        GameObject paper = CreatePopupImage(outline.transform, "Paper", Color.white);
        paper.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(paper.GetComponent<RectTransform>(), new Vector2(19f, 25f), new Vector2(3f, 3f));

        for (int i = 0; i < 2; i++)
        {
            GameObject line = CreatePopupImage(outline.transform, $"Line_{i + 1}", color);
            line.GetComponent<Image>().raycastTarget = false;
            SetTopLeftRect(line.GetComponent<RectTransform>(), new Vector2(11f, 2f), new Vector2(7f, 11f + i * 6f));
        }
    }

    private static void CreateWireIcon(Transform parent, Vector2 position, Color color)
    {
        GameObject line = CreatePopupImage(parent, "WireIcon_Line", color);
        line.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(line.GetComponent<RectTransform>(), new Vector2(27f, 3f), position + new Vector2(0f, 12f));

        for (int i = 0; i < 2; i++)
        {
            GameObject plug = CreatePopupImage(parent, $"WireIcon_Plug_{i + 1}", color);
            plug.GetComponent<Image>().raycastTarget = false;
            SetTopLeftRect(
                plug.GetComponent<RectTransform>(),
                new Vector2(7f, 11f),
                position + new Vector2(i == 0 ? -2f : 22f, 8f));
        }
    }

    private void UpdatePracticalWorkspaceLayout()
    {
        bool showWorkspace = visibleStepIndex >= 0 && visibleStepIndex < HmiStepIndex;
        if (practicalWorkspaceRoot != null)
            practicalWorkspaceRoot.SetActive(showWorkspace);
        if (practicalWorkspaceMaskRoot != null)
            practicalWorkspaceMaskRoot.SetActive(showWorkspace);

        if (!showWorkspace || practicalInstructionText == null || practicalWireGuideText == null)
            return;

        practicalInstructionText.text =
            "K\u00E9o th\u1EA3 c\u00E1c \u0111\u1EA7u d\u00E2y n\u1ED1i\n" +
            "v\u00E0o c\u00E1c gi\u1EAFc c\u1EAFm tr\u00EAn\n" +
            "b\u1EA3ng \u0111i\u1EC1u khi\u1EC3n:";

        List<WireBody> wires = visibleStepIndex < stepRoots.Count
            ? GetStepWires(stepRoots[visibleStepIndex])
            : new List<WireBody>();

        practicalWireGuideText.text = string.Join(
            "\n",
            wires.Select(wire =>
                $"<color=#{ColorUtility.ToHtmlStringRGB(GetPracticalGuideTextColor(wire))}>" +
                $"\u2022 {GetWireDisplayName(wire)}: {wire.correctSocketA} \u2192 {wire.correctSocketB}</color>"));

        for (int i = 0; i < practicalWireRows.Count; i++)
        {
            bool hasWire = i < wires.Count;
            practicalWireRows[i].SetActive(hasWire);
            if (!hasWire)
                continue;

            practicalWireRowNumbers[i].text = GetWireNumber(wires[i]).ToString();
            practicalWireRowBackgrounds[i].color = GetWireRowColor(wires[i]);
        }
    }

    private static int GetWireNumber(WireBody wire)
    {
        if (wire == null)
            return 0;

        string source = wire.name;
        int markerIndex = source.IndexOf("Wire_", StringComparison.OrdinalIgnoreCase);
        int start = markerIndex >= 0 ? markerIndex + 5 : 0;
        int end = start;
        while (end < source.Length && char.IsDigit(source[end]))
            end++;

        return end > start && int.TryParse(source.Substring(start, end - start), out int number)
            ? number
            : 0;
    }

    private static Color GetWireTextColor(WireBody wire)
    {
        WireColor color = wire != null && wire.plugA != null ? wire.plugA.wireColor : WireColor.Any;
        switch (color)
        {
            case WireColor.Red:
                return new Color(0.82f, 0.12f, 0.15f, 1f);
            case WireColor.Yellow:
                return new Color(0.86f, 0.58f, 0.03f, 1f);
            case WireColor.Green:
                return new Color(0.13f, 0.55f, 0.32f, 1f);
            case WireColor.Blue:
                return new Color(0.12f, 0.38f, 0.72f, 1f);
            default:
                return new Color(0.2f, 0.23f, 0.28f, 1f);
        }
    }

    private Color GetPracticalGuideTextColor(WireBody wire)
    {
        int wireNumber = GetWireNumber(wire);
        if (visibleStepIndex == 0 && wireNumber >= 1 && wireNumber <= 2)
            return new Color(0.82f, 0.12f, 0.15f, 1f);

        return GetWireTextColor(wire);
    }

    private static Color GetWireRowColor(WireBody wire)
    {
        WireColor color = wire != null && wire.plugA != null ? wire.plugA.wireColor : WireColor.Any;
        switch (color)
        {
            case WireColor.Red:
                return new Color(1f, 0.965f, 0.97f, 0.94f);
            case WireColor.Yellow:
                return new Color(1f, 0.99f, 0.94f, 0.94f);
            case WireColor.Green:
                return new Color(0.95f, 0.99f, 0.96f, 0.94f);
            case WireColor.Blue:
                return new Color(0.95f, 0.98f, 1f, 0.94f);
            default:
                return new Color(0.97f, 0.975f, 0.98f, 0.94f);
        }
    }

    private void CreateStepNavigationBar()
    {
        if (!createStepNavigationBar || stepNavigationRoot != null)
            return;

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
        SetTopLeftRect(
            stepNavigationPanelRect,
            new Vector2(1823f, 69f),
            new Vector2(50f, 52f));

        GameObject connector = CreatePopupImage(
            panel.transform,
            "StepConnector",
            new Color(0.72f, 0.76f, 0.81f, 1f));
        connector.GetComponent<Image>().raycastTarget = false;
        SetTopLeftRect(
            connector.GetComponent<RectTransform>(),
            new Vector2(1133f, 3f),
            new Vector2(475f, 33f));

        stepNavigationButtons.Clear();
        stepNavigationItems.Clear();

        float[] buttonX = { 0f, 662f, 1142f, 1608f };
        float[] buttonWidth = { 475f, 293f, 279f, 215f };

        for (int i = 0; i < NavigationStepCount; i++)
        {
            int stepIndex = i;
            Vector2 buttonSize = new Vector2(buttonWidth[i], 69f);
            Vector2 buttonPosition = new Vector2(buttonX[i], 0f);

            GameObject shadowObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Shadow",
                new Color(0.12f, 0.15f, 0.2f, 0.12f));
            Image shadowImage = shadowObject.GetComponent<Image>();
            shadowImage.sprite = GetRoundedRectangleSprite();
            shadowImage.type = Image.Type.Sliced;
            shadowImage.raycastTarget = false;
            SetTopLeftRect(
                shadowObject.GetComponent<RectTransform>(),
                buttonSize,
                buttonPosition + new Vector2(0f, 4f));

            GameObject borderObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Border",
                new Color(0.84f, 0.87f, 0.92f, 1f));
            Image borderImage = borderObject.GetComponent<Image>();
            borderImage.sprite = GetRoundedRectangleSprite();
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;
            SetTopLeftRect(
                borderObject.GetComponent<RectTransform>(),
                buttonSize + new Vector2(4f, 4f),
                buttonPosition + new Vector2(-2f, -2f));

            GameObject buttonObject = CreatePopupImage(
                panel.transform,
                $"Step_{i + 1}_Button",
                Color.white);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = GetRoundedRectangleSprite();
            buttonImage.type = Image.Type.Sliced;

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetTopLeftRect(buttonRect, buttonSize, buttonPosition);

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

            TextMeshProUGUI title = CreatePopupText(
                buttonObject.transform,
                "Label",
                PracticalNavigationLabels[i],
                25f,
                FontStyles.Normal,
                new Color(0.37f, 0.43f, 0.52f, 1f),
                TextAlignmentOptions.Center);
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 25f;
            title.enableWordWrapping = false;
            SetTopLeftRect(
                title.rectTransform,
                new Vector2(buttonSize.x - 24f, buttonSize.y),
                new Vector2(12f, 0f));
            item.Title = title;

            EventTrigger hoverTrigger = buttonObject.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            pointerEnter.callback.AddListener(_ =>
            {
                if (!button.interactable)
                    return;

                borderImage.color = new Color(0.92f, 0.16f, 0.19f, 1f);
                shadowImage.color = new Color(0.5f, 0.08f, 0.1f, 0.22f);
                title.color = new Color(0.82f, 0.12f, 0.15f, 1f);
            });
            hoverTrigger.triggers.Add(pointerEnter);

            EventTrigger.Entry pointerExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            pointerExit.callback.AddListener(_ => UpdateStepNavigationBar());
            hoverTrigger.triggers.Add(pointerExit);

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
        buttonRect.anchoredPosition = new Vector2(52f, -992f);
        buttonRect.sizeDelta = new Vector2(237f, 71f);

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

        TextMeshProUGUI buttonText = CreatePopupText(
            buttonObject.transform,
            "Label",
            "\u2190  H\u01B0\u1EDBng d\u1EABn",
            23f,
            FontStyles.Normal,
            new Color(0.38f, 0.43f, 0.5f, 1f),
            TextAlignmentOptions.Center);
        buttonText.enableAutoSizing = true;
        buttonText.fontSizeMin = 17f;
        buttonText.fontSizeMax = 23f;
        SetCenteredRect(buttonText.rectTransform, new Vector2(213f, 55f), Vector2.zero);

        UpdateGuideReturnButton();
    }

    private void UpdateGuideReturnButton()
    {
        if (guideReturnRoot == null)
            return;

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

        Dictionary<string, SocketPoint> socketsById = FindSocketsById();
        for (int stepIndex = 0; stepIndex < stepRoots.Count; stepIndex++)
        {
            GameObject stepRoot = stepRoots[stepIndex];
            if (stepRoot == null)
                continue;

            Dictionary<string, SavedWireConnection> savedByWireName = savedWireConnections
                .Where(saved => saved.StepIndex == stepIndex)
                .GroupBy(saved => saved.WireName)
                .ToDictionary(group => group.Key, group => group.First());

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

    private static Dictionary<string, SocketPoint> FindSocketsById()
    {
        Dictionary<string, SocketPoint> socketsById = new Dictionary<string, SocketPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (SocketPoint socket in Resources.FindObjectsOfTypeAll<SocketPoint>())
        {
            if (socket == null ||
                !socket.gameObject.scene.IsValid() ||
                string.IsNullOrWhiteSpace(socket.socketID))
            {
                continue;
            }

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

        Color selectedBackground = Color.white;
        Color normalBackground = new Color(1f, 1f, 1f, 1f);
        Color lockedBackground = new Color(0.99f, 0.99f, 0.99f, 1f);
        Color selectedText = new Color(0.82f, 0.12f, 0.15f, 1f);
        Color normalTitle = new Color(0.38f, 0.41f, 0.46f, 1f);
        Color normalDescription = new Color(0.5f, 0.57f, 0.66f, 1f);
        Color lockedText = new Color(0.48f, 0.51f, 0.56f, 1f);
        Color normalBorder = new Color(0.84f, 0.86f, 0.89f, 1f);
        Color selectedBorder = new Color(0.9f, 0.12f, 0.16f, 1f);
        Color lockedBorder = new Color(0.88f, 0.89f, 0.91f, 1f);
        Color shadowColor = new Color(0.12f, 0.15f, 0.2f, 0.1f);

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
                ? new Color(1f, 0.91f, 0.92f, 1f)
                : new Color(1f, 0.94f, 0.95f, 1f);
            colors.pressedColor = isSelected
                ? new Color(1f, 0.93f, 0.93f, 1f)
                : new Color(0.93f, 0.95f, 0.97f, 1f);
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
                    ? new Color(0.5f, 0.08f, 0.1f, 0.14f)
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

    private static void SetTopLeftRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
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
