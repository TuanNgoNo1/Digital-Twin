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
    public float fallbackSpeedRpm = 10000f;

    [Header("Motor ảo")]
    public RotateSubmarineBlades rotateBlades;
    public VirtualMotorController virtualMotor;
    public Transform visualMotorRotor;
    public bool syncMotorModel = true;
    public float visualMotorSpeedScale = 0.6f;
    public float visualMotorMinDegreesPerSecond = 120f;
    public float visualMotorMaxDegreesPerSecond = 1440f;

    [Header("HMI demo fallback")]
    public bool showRuntimeHmi = false;
    public bool runtimeHmiVisible = false;
    public int runtimeHmiWidth = 260;

    [Header("Canvas HMI")]
    public bool createCanvasHmi = true;
    public Vector2 canvasHmiSize = new Vector2(430f, 390f);

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
    private TextMeshProUGUI canvasStatusText;
    private TextMeshProUGUI canvasMotorText;
    private TextMeshProUGUI canvasPiText;
    private TextMeshProUGUI canvasTelemetryText;
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
        LatestTelemetry.speedRpm = fallbackSpeedRpm;
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

    private bool lessonFinished;

    public void TurnOn()
    {
        if (lessonFinished) return;
        SendControl("ON", speed: fallbackSpeedRpm);
    }

    public void TurnOff()
    {
        if (lessonFinished) return;
        SendControl("OFF");
    }

    public void FinishLesson()
    {
        if (lessonFinished) return;
        lessonFinished = true;
        SendControl("OFF");
        string dataJson = JsonUtility.ToJson(new FinishData
        {
            running = LatestTelemetry.running,
            speedRpm = LatestTelemetry.speedRpm,
            count = LatestTelemetry.count,
            direction = LatestTelemetry.direction
        });
        PDTwinBridge.Submit(10f, dataJson);
        Debug.Log("[PLCController_v2] Lesson finished, score submitted.");
    }

    [System.Serializable]
    private class FinishData
    {
        public bool running;
        public float speedRpm;
        public int count;
        public string direction;
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
        visualDegreesPerSecond = LatestTelemetry.running
            ? Mathf.Clamp(rpm * visualMotorSpeedScale, visualMotorMinDegreesPerSecond, visualMotorMaxDegreesPerSecond)
            : 0f;
        float visualRpm = visualDegreesPerSecond / 6f;
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

        GameObject panel = new GameObject("HMI_Panel");
        panel.transform.SetParent(canvasHmiRoot.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = canvasHmiSize;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.92f, 0.94f, 0.96f, 0.97f);

        CreateText(panel.transform, "Title", "HMI - PLC MOTOR", new Vector2(16f, -12f), new Vector2(328f, 26f), 18, true);
        canvasPiText = CreateText(panel.transform, "PiStatus", "PI: UNKNOWN", new Vector2(16f, -42f), new Vector2(328f, 24f), 13, false);
        canvasMotorText = CreateText(panel.transform, "MotorStatus", "Motor: STOPPED", new Vector2(16f, -68f), new Vector2(328f, 24f), 14, false);
        canvasStatusText = CreateText(panel.transform, "LastStatus", "Gateway ready", new Vector2(16f, -94f), new Vector2(398f, 34f), 12, false);
        canvasTelemetryText = CreateText(panel.transform, "Telemetry", "Telemetry: waiting", new Vector2(16f, -130f), new Vector2(398f, 120f), 12, false);

        Button onButton = CreateButton(panel.transform, "ON_Button", "ON", new Vector2(16f, -278f), new Vector2(185f, 42f), new Color(0.08f, 0.58f, 0.22f, 1f));
        Button offButton = CreateButton(panel.transform, "OFF_Button", "OFF", new Vector2(229f, -278f), new Vector2(185f, 42f), new Color(0.76f, 0.1f, 0.1f, 1f));

        onButton.onClick.AddListener(TurnOn);
        offButton.onClick.AddListener(TurnOff);

        Button finishButton = CreateButton(panel.transform, "Finish_Button", "FINISH", new Vector2(16f, -330f), new Vector2(398f, 42f), new Color(0.1f, 0.3f, 0.7f, 1f));
        finishButton.onClick.AddListener(FinishLesson);

        canvasHmiRoot.SetActive(runtimeHmiVisible);
        UpdateCanvasHmi();
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

        if (canvasPiText != null)
            canvasPiText.text = $"PI: {(IsPiOnline ? "ONLINE" : "OFFLINE / WAITING")}  ({piBaseUrl})";

        if (canvasMotorText != null)
            canvasMotorText.text = LatestTelemetry.running ? "Motor: RUNNING" : "Motor: STOPPED";

        if (canvasStatusText != null)
            canvasStatusText.text = string.IsNullOrWhiteSpace(lastStatus) ? $"Gateway: {piBaseUrl}" : lastStatus;

        if (canvasTelemetryText != null)
        {
            canvasTelemetryText.text =
                $"Speed: {LatestTelemetry.speedRpm:F0} RPM   Count: {LatestTelemetry.count}\n" +
                $"Rotations: {LatestTelemetry.rotations:F2}   Angle: {LatestTelemetry.angle:F1} deg\n" +
                $"Direction: {LatestTelemetry.direction}   Action: {LatestTelemetry.action}\n" +
                $"Backend: {(LatestTelemetry.backendSynced ? "SYNCED" : LatestTelemetry.backendStatus)}\n" +
                visualSyncStatus;
        }
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
