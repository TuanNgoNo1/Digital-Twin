using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PLCController_v2 : MonoBehaviour
{
    public static PLCController_v2 Instance { get; private set; }

    [Serializable]
    public class MotorTelemetry
    {
        public string runId;
        public string lessonId;
        public string userId;
        public string timestamp;
        public string action;
        public bool running;
        public float speedRpm;
        public int count;
        public float rotations;
        public float angle;
        public string direction = "forward";
        public bool backendSynced = true;
        public string backendStatus = "UNKNOWN";
    }

    [Serializable]
    private class ControlCommand
    {
        public string action;
        public string runId;
        public string lessonId;
        public string userId;
        public float speed;
        public float rotations;
        public float angle;
        public string direction;
        public string timestamp;
    }

    [Header("Pi Gateway")]
    public string piBaseUrl = "http://10.38.100.27:5000";
    public string controlEndpoint = "/control";
    public string telemetryEndpoint = "/telemetry";
    public float pollInterval = 0.5f;
    public int timeoutSeconds = 3;
    public bool pollTelemetryOnStart = true;

    [Header("Demo Session")]
    public string lessonId = "TH1";
    public string userId = "demo-user";
    public string runId;

    [Header("Fallback khi Pi offline")]
    public bool optimisticLocalTelemetry = false;
    public float fallbackSpeedRpm = 100f;

    [Header("Motor ảo")]
    public RotateSubmarineBlades rotateBlades;
    public VirtualMotorController virtualMotor;
    public Transform visualMotorRotor;
    public bool syncMotorModel = true;

    [Header("HMI demo fallback")]
    public bool showRuntimeHmi = false;
    public bool runtimeHmiVisible = false;
    public int runtimeHmiWidth = 260;

    [Header("Canvas HMI")]
    public bool createCanvasHmi = true;
    public Vector2 canvasHmiSize = new Vector2(300f, 250f);
    [Tooltip("Vi tri goc tren-trai cua bang HMI (pixel, tinh tu goc tren-trai man hinh).")]
    public Vector2 canvasHmiAnchoredPosition = new Vector2(16f, -16f);
    [Tooltip("Ty le thu nho bang HMI de khong che vung noi day.")]
    public float canvasHmiScale = 0.5f;
    [Header("Nhan day (chu mau)")]
    public bool showWireLabels = true;
    [Tooltip("Tam cua 2 dong nhan, tinh tu goc tren-trai man hinh (pixel).")]
    public Vector2 wireLabelsCenter = new Vector2(917f, -120f);

    [Header("Tương thích script cũ")]
    [Tooltip("URL cũ dạng http://pi:5000/control. Nếu còn được gán trong Inspector, script sẽ tự suy ra piBaseUrl.")]
    public string url = "http://10.38.100.27:5000/control";

    public event Action<MotorTelemetry> OnTelemetryUpdated;
    public event Action<string> OnConnectionStatusChanged;

    public MotorTelemetry LatestTelemetry { get; private set; } = new MotorTelemetry();
    public bool IsPiOnline { get; private set; }

    private Coroutine pollingJob;
    private string lastStatus = "";
    private GameObject canvasHmiRoot;
    private TextMeshProUGUI hmiAngleText;
    private TextMeshProUGUI hmiRotText;
    private TextMeshProUGUI hmiSpeedText;
    private TextMeshProUGUI hmiSpeedSetText;
    private TextMeshProUGUI hmiStatusText;
    private TMP_InputField hmiRotInput;
    private TMP_InputField hmiAngleInput;
    private float hmiTargetSpeed = 100f;
    private bool initialized;
    private float visualDegreesPerSecond;
    private bool visualDirectionForward = true;
    private string visualSyncStatus = "Visual: waiting";

    private void Awake()
    {
        if (!isActiveAndEnabled)
            return;

        InitializeController();
    }

    private void OnEnable()
    {
        InitializeController();
    }

    private void InitializeController()
    {
        if (initialized)
            return;

        initialized = true;
        Instance = this;

#if UNITY_EDITOR
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif

        if (string.IsNullOrWhiteSpace(runId))
            runId = $"TH1-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        if (!string.IsNullOrWhiteSpace(url) && url.EndsWith(controlEndpoint, StringComparison.OrdinalIgnoreCase))
            piBaseUrl = url.Substring(0, url.Length - controlEndpoint.Length);

        if (rotateBlades == null)
            rotateBlades = FindBestRotateBlades();

        if (virtualMotor == null)
            virtualMotor = FindObjectOfType<VirtualMotorController>();

        if (visualMotorRotor == null)
            visualMotorRotor = FindLikelyRotor();

        if (virtualMotor != null && virtualMotor.motorRotor == null && visualMotorRotor != null)
            virtualMotor.motorRotor = visualMotorRotor;

        LatestTelemetry.runId = runId;
        LatestTelemetry.lessonId = lessonId;
        LatestTelemetry.userId = userId;
        LatestTelemetry.speedRpm = hmiTargetSpeed;
        LatestTelemetry.direction = "forward";

        if (createCanvasHmi)
            CreateCanvasHmi();
    }

    private void Start()
    {
        if (pollTelemetryOnStart)
            StartTelemetryPolling();
    }

    private void Update()
    {
        if (!syncMotorModel || !LatestTelemetry.running || visualMotorRotor == null || visualDegreesPerSecond <= 0f)
            return;

        bool virtualMotorOwnsRotor = virtualMotor != null && virtualMotor.isActiveAndEnabled && virtualMotor.motorRotor == visualMotorRotor;
        bool bladesOwnRotor = rotateBlades != null
            && rotateBlades.isActiveAndEnabled
            && rotateBlades.rotatableObjects != null
            && rotateBlades.rotatableObjects.Contains(visualMotorRotor.gameObject);

        if (virtualMotorOwnsRotor || bladesOwnRotor)
            return;

        float direction = visualDirectionForward ? 1f : -1f;
        visualMotorRotor.Rotate(Vector3.forward, visualDegreesPerSecond * direction * Time.deltaTime, Space.Self);
    }

    public void StartTelemetryPolling()
    {
        if (pollingJob != null)
            StopCoroutine(pollingJob);

        pollingJob = StartCoroutine(PollTelemetryRoutine());
    }

    public void StopTelemetryPolling()
    {
        if (pollingJob == null)
            return;

        StopCoroutine(pollingJob);
        pollingJob = null;
    }

    public void TurnOn()
    {
        float speed = LatestTelemetry.speedRpm > 0f ? LatestTelemetry.speedRpm : hmiTargetSpeed;
        SendControl("ON", speed: speed);
    }

    public void TurnOff()
    {
        SendControl("OFF");
    }

    public void SetSpeed(float rpm)
    {
        LatestTelemetry.speedRpm = Mathf.Max(0f, rpm);
        SendControl("SET_SPEED", speed: LatestTelemetry.speedRpm);
    }

    public void SetTargetRotations(float rotations)
    {
        LatestTelemetry.rotations = Mathf.Max(0f, rotations);
        SendControl("SET_ROTATIONS", rotations: LatestTelemetry.rotations);
    }

    public void SetTargetAngle(float angle)
    {
        LatestTelemetry.angle = Mathf.Max(0f, angle);
        SendControl("SET_ANGLE", angle: LatestTelemetry.angle);
    }

    public void SetDirectionForward()
    {
        LatestTelemetry.direction = "forward";
        SendControl("SET_DIRECTION", direction: "forward");
    }

    public void SetDirectionReverse()
    {
        LatestTelemetry.direction = "reverse";
        SendControl("SET_DIRECTION", direction: "reverse");
    }

    public void SendControl(string action)
    {
        SendControl(action, LatestTelemetry.speedRpm, LatestTelemetry.rotations, LatestTelemetry.angle, LatestTelemetry.direction);
    }

    public void SetRuntimeHmiVisible(bool visible)
    {
        runtimeHmiVisible = visible;

        if (createCanvasHmi && canvasHmiRoot == null)
            CreateCanvasHmi();

        if (canvasHmiRoot != null)
            canvasHmiRoot.SetActive(visible);
    }

    private void SendControl(string action, float speed = -1f, float rotations = -1f, float angle = -1f, string direction = "")
    {
        if (speed < 0f) speed = LatestTelemetry.speedRpm > 0f ? LatestTelemetry.speedRpm : fallbackSpeedRpm;
        if (rotations < 0f) rotations = LatestTelemetry.rotations;
        if (angle < 0f) angle = LatestTelemetry.angle;
        if (string.IsNullOrWhiteSpace(direction)) direction = LatestTelemetry.direction;

        var command = new ControlCommand
        {
            action = action,
            runId = runId,
            lessonId = lessonId,
            userId = userId,
            speed = speed,
            rotations = rotations,
            angle = angle,
            direction = direction,
            timestamp = DateTimeOffset.UtcNow.ToString("o")
        };

        StartCoroutine(PostControlRoutine(command));

        if (optimisticLocalTelemetry)
            ApplyOptimisticTelemetry(command);
    }

    private IEnumerator PostControlRoutine(ControlCommand command)
    {
        string jsonData = JsonUtility.ToJson(command);

        using (UnityWebRequest request = new UnityWebRequest(BuildUrl(controlEndpoint), "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException ex)
            {
                SetConnectionStatus(false, "HTTP BLOCKED: enable Allow downloads over HTTP = Always Allowed");
                Debug.LogError($"[PLCController_v2] HTTP request blocked by Unity Player Settings: {ex.Message}");
                yield break;
            }

            yield return operation;

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetConnectionStatus(false, $"PI OFFLINE: {request.error}");
                Debug.LogError($"[PLCController_v2] Control {command.action} failed: {request.error}");
            }
            else
            {
                SetConnectionStatus(true, $"PI OK: {command.action}");
                Debug.Log($"[PLCController_v2] Control {command.action}: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator PollTelemetryRoutine()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BuildUrl(telemetryEndpoint)))
            {
                request.timeout = timeoutSeconds;
                UnityWebRequestAsyncOperation operation = null;
                bool requestStarted = false;
                try
                {
                    operation = request.SendWebRequest();
                    requestStarted = true;
                }
                catch (InvalidOperationException ex)
                {
                    SetConnectionStatus(false, "HTTP BLOCKED: enable Allow downloads over HTTP = Always Allowed");
                    Debug.LogError($"[PLCController_v2] HTTP telemetry blocked by Unity Player Settings: {ex.Message}");
                }

                if (!requestStarted)
                {
                    yield return new WaitForSeconds(pollInterval);
                    continue;
                }

                yield return operation;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        MotorTelemetry telemetry = JsonUtility.FromJson<MotorTelemetry>(request.downloadHandler.text);
                        if (telemetry != null)
                        {
                            ApplyTelemetry(telemetry, true);
                            SetConnectionStatus(true, telemetry.backendSynced ? "PI ONLINE / BACKEND SYNCED" : "PI ONLINE / BACKEND NOT SYNCED");
                        }
                    }
                    catch (Exception ex)
                    {
                        SetConnectionStatus(false, $"TELEMETRY DATA ERR: {ex.Message}");
                    }
                }
                else
                {
                    SetConnectionStatus(false, $"PI OFFLINE: {request.error}");
                    if (optimisticLocalTelemetry)
                        PublishTelemetry();
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void ApplyTelemetry(MotorTelemetry telemetry, bool fromPi)
    {
        if (string.IsNullOrWhiteSpace(telemetry.runId)) telemetry.runId = runId;
        if (string.IsNullOrWhiteSpace(telemetry.lessonId)) telemetry.lessonId = lessonId;
        if (string.IsNullOrWhiteSpace(telemetry.userId)) telemetry.userId = userId;
        if (string.IsNullOrWhiteSpace(telemetry.direction)) telemetry.direction = LatestTelemetry.direction;

        LatestTelemetry = telemetry;
        if (fromPi)
            IsPiOnline = true;

        SyncMotorFromTelemetry();
        PublishTelemetry();
    }

    private void ApplyOptimisticTelemetry(ControlCommand command)
    {
        LatestTelemetry.action = command.action;
        LatestTelemetry.timestamp = command.timestamp;
        LatestTelemetry.speedRpm = command.speed;
        LatestTelemetry.direction = command.direction;
        LatestTelemetry.rotations = command.rotations;
        LatestTelemetry.angle = command.angle;
        LatestTelemetry.backendSynced = false;
        LatestTelemetry.backendStatus = IsPiOnline ? "PENDING" : "LOCAL_FALLBACK";

        if (command.action == "ON")
            LatestTelemetry.running = true;
        else if (command.action == "OFF")
            LatestTelemetry.running = false;

        SyncMotorFromTelemetry();
        PublishTelemetry();
    }

    private void SyncMotorFromTelemetry()
    {
        if (!syncMotorModel)
            return;

        if (rotateBlades == null)
            rotateBlades = FindBestRotateBlades();

        if (virtualMotor == null)
            virtualMotor = FindObjectOfType<VirtualMotorController>();

        if (rotateBlades == null && virtualMotor == null)
        {
            Debug.LogWarning("[PLCController_v2] Khong tim thay motor ao de sync telemetry.");
            return;
        }

        float rpm = Mathf.Max(0f, LatestTelemetry.speedRpm);
        // Dong bo 1:1 voi motor that: RPM -> deg/s = RPM * 6 (khong scale, khong clamp)
        visualDegreesPerSecond = LatestTelemetry.running ? rpm * 6f : 0f;
        float visualRpm = rpm;
        bool isForward = !LatestTelemetry.direction.Equals("reverse", StringComparison.OrdinalIgnoreCase);
        visualDirectionForward = isForward;

        if (visualMotorRotor == null)
            visualMotorRotor = FindLikelyRotor();

        if (rotateBlades != null)
        {
            rotateBlades.soVongCanQuay = float.PositiveInfinity;
            rotateBlades.rotationSpeed = visualDegreesPerSecond;
            rotateBlades.SetRotationDirection(isForward);
            if (rotateBlades.GetIsRotating() != LatestTelemetry.running)
                rotateBlades.RotateObject(LatestTelemetry.running);
        }

        if (virtualMotor != null)
        {
            if (Mathf.Abs(virtualMotor.targetSpeed - visualRpm) > 0.1f)
                virtualMotor.SetSpeed(visualRpm);

            if (virtualMotor.isForward != isForward)
            {
                if (isForward) virtualMotor.SetForward();
                else virtualMotor.SetReverse();
            }

            if (LatestTelemetry.running && !virtualMotor.isRunning)
                virtualMotor.StartMotor();
            else if (!LatestTelemetry.running && virtualMotor.isRunning)
                virtualMotor.Stop();
        }

        string targetName = visualMotorRotor != null ? visualMotorRotor.name : "none";
        bool bladesRotating = rotateBlades != null && rotateBlades.GetIsRotating();
        bool virtualRotating = virtualMotor != null && virtualMotor.isRunning;
        visualSyncStatus = $"Visual: {(LatestTelemetry.running ? "RUN" : "STOP")} {visualDegreesPerSecond:F0} deg/s -> {targetName}"
            + $" (blades:{bladesRotating}, vm:{virtualRotating})";
    }

    private RotateSubmarineBlades FindBestRotateBlades()
    {
#if UNITY_2023_1_OR_NEWER
        RotateSubmarineBlades[] candidates = FindObjectsByType<RotateSubmarineBlades>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        RotateSubmarineBlades[] candidates = FindObjectsOfType<RotateSubmarineBlades>(true);
#endif
        RotateSubmarineBlades fallback = null;
        foreach (RotateSubmarineBlades candidate in candidates)
        {
            if (candidate == null)
                continue;

            if (fallback == null)
                fallback = candidate;

            if (candidate.rotatableObjects != null && candidate.rotatableObjects.Count > 0)
                return candidate;
        }

        return fallback;
    }

    private Transform FindLikelyRotor()
    {
        GameObject exact = GameObject.Find("Rotor");
        if (exact != null)
            return exact.transform;

#if UNITY_2023_1_OR_NEWER
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Transform[] transforms = FindObjectsOfType<Transform>(true);
#endif
        foreach (Transform candidate in transforms)
        {
            if (candidate == null)
                continue;

            string candidateName = candidate.name.ToLowerInvariant();
            if (candidateName.Contains("rotor") || candidateName.Contains("shaft") || candidateName.Contains("gear"))
                return candidate;
        }

        return null;
    }

    private void PublishTelemetry()
    {
        UpdateCanvasHmi();
        OnTelemetryUpdated?.Invoke(LatestTelemetry);
    }

    private void SetConnectionStatus(bool online, string status)
    {
        IsPiOnline = online;
        if (lastStatus == status)
            return;

        lastStatus = status;
        UpdateCanvasHmi();
        OnConnectionStatusChanged?.Invoke(status);
        Debug.Log($"[PLCController_v2] {status}");
    }

    private string BuildUrl(string endpoint)
    {
        string baseUrl = piBaseUrl.TrimEnd('/');
        string suffix = endpoint.StartsWith("/") ? endpoint : "/" + endpoint;
        return baseUrl + suffix;
    }

    private void CreateCanvasHmi()
    {
        if (canvasHmiRoot != null)
            return;

        canvasHmiRoot = new GameObject("Runtime_Pi_HMI_Canvas");
        Canvas canvas = canvasHmiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasHmiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        canvasHmiRoot.AddComponent<GraphicRaycaster>();

        Color green = new Color(0.18f, 0.55f, 0.20f, 1f);
        Color red = new Color(0.72f, 0.12f, 0.12f, 1f);
        Color blueBtn = new Color(0.16f, 0.34f, 0.72f, 1f);
        Color redBtn = new Color(0.82f, 0.14f, 0.14f, 1f);

        GameObject panel = new GameObject("HMI_Panel");
        panel.transform.SetParent(canvasHmiRoot.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = canvasHmiAnchoredPosition;
        panelRect.sizeDelta = new Vector2(600f, 300f);
        panel.transform.localScale = Vector3.one * canvasHmiScale;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        Transform gp = MakeSubPanel(panel.transform, "Green", new Vector2(0f, 0f), new Vector2(410f, 300f), green);
        Transform rp = MakeSubPanel(panel.transform, "Red", new Vector2(410f, 0f), new Vector2(190f, 300f), red);

        // ----- Panel trai (xanh) -----
        CreateText(gp, "L1", "Đặt vị trí", new Vector2(8f, -12f), new Vector2(104f, 26f), 15, true);
        CreateText(gp, "U1", "Vòng", new Vector2(116f, -12f), new Vector2(48f, 26f), 14, false);
        hmiRotInput = CreateInputField(gp, "RotInput", "0", new Vector2(166f, -14f), new Vector2(72f, 30f), 15);
        CreateButton(gp, "SetRot", "SET", new Vector2(246f, -14f), new Vector2(64f, 32f), blueBtn).onClick.AddListener(() =>
        { if (float.TryParse(hmiRotInput.text, out float v)) SetTargetRotations(v); });

        CreateText(gp, "L2", "Đặt vị trí", new Vector2(8f, -52f), new Vector2(104f, 26f), 15, true);
        CreateText(gp, "U2", "Độ", new Vector2(116f, -52f), new Vector2(48f, 26f), 14, false);
        hmiAngleInput = CreateInputField(gp, "AngleInput", "0", new Vector2(166f, -54f), new Vector2(72f, 30f), 15);
        CreateButton(gp, "SetAngle", "SET", new Vector2(246f, -54f), new Vector2(64f, 32f), blueBtn).onClick.AddListener(() =>
        { if (float.TryParse(hmiAngleInput.text, out float v)) SetTargetAngle(v); });

        CreateText(gp, "L3", "Đặt tốc độ:", new Vector2(8f, -92f), new Vector2(104f, 26f), 15, true);
        CreateText(gp, "U3", "Vòng/phút", new Vector2(116f, -92f), new Vector2(80f, 26f), 13, false);
        hmiSpeedSetText = CreateText(gp, "SpeedSet", "100", new Vector2(300f, -92f), new Vector2(60f, 26f), 16, true);
        CreateButton(gp, "Plus", "+", new Vector2(166f, -124f), new Vector2(56f, 30f), blueBtn).onClick.AddListener(() =>
        { hmiTargetSpeed = Mathf.Clamp(hmiTargetSpeed + 10f, 0f, 3000f); SetSpeed(hmiTargetSpeed); if (hmiSpeedSetText != null) hmiSpeedSetText.text = hmiTargetSpeed.ToString("F0"); });
        CreateButton(gp, "Minus", "-", new Vector2(228f, -124f), new Vector2(56f, 30f), redBtn).onClick.AddListener(() =>
        { hmiTargetSpeed = Mathf.Clamp(hmiTargetSpeed - 10f, 0f, 3000f); SetSpeed(hmiTargetSpeed); if (hmiSpeedSetText != null) hmiSpeedSetText.text = hmiTargetSpeed.ToString("F0"); });

        hmiAngleText = CreateText(gp, "St1", "Vị trí (độ): 0", new Vector2(8f, -172f), new Vector2(230f, 24f), 15, false);
        hmiRotText = CreateText(gp, "St2", "Đã quay: 0.00", new Vector2(8f, -198f), new Vector2(230f, 24f), 15, false);
        hmiSpeedText = CreateText(gp, "St3", "Tốc độ RPM: 0", new Vector2(8f, -224f), new Vector2(230f, 24f), 15, false);
        CreateButton(gp, "RstStatus", "RST", new Vector2(250f, -200f), new Vector2(70f, 44f), redBtn).onClick.AddListener(() => SendControl("RESET_COUNTER"));
        hmiStatusText = CreateText(gp, "PiStatus", "PI: ...", new Vector2(8f, -262f), new Vector2(394f, 22f), 12, false);

        // ----- Panel phai (do) -----
        CreateButton(rp, "Fwd", "Thuận", new Vector2(12f, -12f), new Vector2(166f, 44f), blueBtn).onClick.AddListener(SetDirectionForward);
        CreateButton(rp, "Rev", "Ngược", new Vector2(12f, -62f), new Vector2(166f, 44f), blueBtn).onClick.AddListener(SetDirectionReverse);
        CreateButton(rp, "Start", "START", new Vector2(12f, -116f), new Vector2(166f, 42f), blueBtn).onClick.AddListener(TurnOn);
        CreateButton(rp, "Stop", "STOP", new Vector2(12f, -162f), new Vector2(166f, 42f), redBtn).onClick.AddListener(TurnOff);
        CreateButton(rp, "RstRight", "RST", new Vector2(12f, -208f), new Vector2(166f, 40f), redBtn).onClick.AddListener(() => SendControl("ERR_RESET"));

        if (showWireLabels)
        {
            CreateWireLabel(canvasHmiRoot.transform, "WireLabelYellow", "Dây Vàng: Y0-Pin11", new Color(1f, 0.78f, 0f), wireLabelsCenter);
            CreateWireLabel(canvasHmiRoot.transform, "WireLabelRed", "Dây Đỏ: X0-0B", new Color(0.86f, 0.12f, 0.12f), wireLabelsCenter + new Vector2(0f, -34f));
        }

        canvasHmiRoot.SetActive(runtimeHmiVisible);
        UpdateCanvasHmi();
    }

    private void CreateWireLabel(Transform parent, string name, string content, Color color, Vector2 anchoredPosition)
    {
        TextMeshProUGUI t = CreateText(parent, name, content, anchoredPosition, new Vector2(260f, 26f), 18, true);
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        t.rectTransform.anchoredPosition = anchoredPosition;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
    }

    private Transform MakeSubPanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go.transform;
    }

    private TMP_InputField CreateInputField(Transform parent, string name, string initial, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        go.AddComponent<Image>().color = Color.white;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(6f, 2f);
        tr.offsetMax = new Vector2(-6f, -2f);
        TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = Color.black;
        t.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = rect;
        input.textComponent = t;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.text = initial;
        return input;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string value, Vector2 anchoredPosition, Vector2 size, int fontSize, bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = new Color(0.08f, 0.1f, 0.12f, 1f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor = color * 0.85f;
        colors.selectedColor = color;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(go.transform, "Text", label, Vector2.zero, size, 20, true);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    private void UpdateCanvasHmi()
    {
        if (canvasHmiRoot == null)
            return;

        if (hmiAngleText != null) hmiAngleText.text = $"Vị trí (độ): {LatestTelemetry.angle:F0}";
        if (hmiRotText != null) hmiRotText.text = $"Đã quay: {LatestTelemetry.rotations:F2}";
        if (hmiSpeedText != null) hmiSpeedText.text = $"Tốc độ RPM: {LatestTelemetry.speedRpm:F0}";
        if (hmiStatusText != null)
            hmiStatusText.text = (IsPiOnline ? "PI ONLINE" : "PI OFFLINE/WAIT") + (LatestTelemetry.running ? " | RUN" : " | STOP");
    }

    private void OnGUI()
    {
        if (createCanvasHmi || !showRuntimeHmi || !runtimeHmiVisible)
            return;

        GUILayout.BeginArea(new Rect(16, 16, runtimeHmiWidth, 260), GUI.skin.box);
        GUILayout.Label("HMI Demo - Pi Gateway");
        GUILayout.Label(IsPiOnline ? "PI: ONLINE" : "PI: OFFLINE / FALLBACK");
        GUILayout.Label(LatestTelemetry.running ? "Motor: RUNNING" : "Motor: STOPPED");
        GUILayout.Label($"Speed: {LatestTelemetry.speedRpm:F0} RPM");
        GUILayout.Label($"Count: {LatestTelemetry.count}");
        GUILayout.Label($"Rotations: {LatestTelemetry.rotations:F2}");
        GUILayout.Label($"Angle: {LatestTelemetry.angle:F1}");
        GUILayout.Label($"Direction: {LatestTelemetry.direction}");
        GUILayout.Label(LatestTelemetry.backendSynced ? "Backend: synced" : $"Backend: {LatestTelemetry.backendStatus}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("ON", GUILayout.Height(36))) TurnOn();
        if (GUILayout.Button("OFF", GUILayout.Height(36))) TurnOff();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Forward")) SetDirectionForward();
        if (GUILayout.Button("Reverse")) SetDirectionReverse();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
