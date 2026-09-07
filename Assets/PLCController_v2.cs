using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PLCController_v2 : MonoBehaviour
{
    public const string DefaultPiBaseUrl = "http://103.238.69.131:8080/plc";
    public const string DefaultControlUrl = DefaultPiBaseUrl + "/control";

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
        public float setSpeedRpm;
        public float pulseFrequency;
        public int count;
        public float rotations;
        public float angle;
        public int encoderCount;
        public float rotationsExact;
        public float pulsesPerSample;
        public float speedRawD164;
        public string motionMode = "";
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
        public string mode;
        public string direction;
        public string timestamp;
    }

    [Header("Pi Gateway")]
    public string piBaseUrl = DefaultPiBaseUrl;
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
    [Tooltip("Object man HMI that trong Hierarchy. Keo RectTransform cua object nay de di chuyen toan bo HMI.")]
    public GameObject hmiScreenObject;
    [Tooltip("Ty le thu nho bang HMI de khong che vung noi day.")]
    public float canvasHmiScale = 0.5f;

    [Header("HMI Branding")]
    public Sprite institutionLogo;
    public string institutionName = "Học viện Công nghệ Bưu chính Viễn thông";

    [Header("Control Step Camera Layout")]
    public bool enableControlCameraLayout = true;
    public float controlHmiCameraFov = 30f;
    public float controlHmiDistanceScale = 1.35f;
    public float controlHmiMinDistance = 0.45f;
    public float controlPipCameraFov = 38f;
    public float controlPipDistanceScale = 1.7f;
    public float motorPipCameraFov = 24f;
    public float motorPipDistanceScale = 1.05f;
    public Vector2 motorPipSize = new Vector2(300f, 170f);
    public Vector2 motorPipOffset = new Vector2(-18f, 18f);
    public float wiringPipCameraFov = 42f;
    public float wiringPipDistanceScale = 1.08f;
    public Vector2 wiringPipSize = new Vector2(320f, 180f);
    public Vector2 wiringPipOffset = new Vector2(18f, 18f);
    public bool hideHmiInPipViews = true;

    [Header("Nhan day (chu mau)")]
    public bool showWireLabels = true;
    [Tooltip("Tam cua 2 dong nhan, tinh tu goc tren-trai man hinh (pixel).")]
    public Vector2 wireLabelsCenter = new Vector2(917f, -120f);

    [Header("Tương thích script cũ")]
    [Tooltip("URL cũ dạng https://domain/plc/control. Nếu còn được gán trong Inspector, script sẽ tự suy ra piBaseUrl.")]
    public string url = DefaultControlUrl;

    public event Action<MotorTelemetry> OnTelemetryUpdated;
    public event Action<string> OnConnectionStatusChanged;

    public MotorTelemetry LatestTelemetry { get; private set; } = new MotorTelemetry();
    public bool IsPiOnline { get; private set; }

    private struct BehaviourEnabledState
    {
        public MonoBehaviour behaviour;
        public bool enabled;
    }

    private struct LayerState
    {
        public GameObject gameObject;
        public int layer;
    }

    private Coroutine pollingJob;
    private string lastStatus = "";
    private GameObject canvasHmiRoot;
    private RectTransform canvasHmiPanelRect;
    private TextMeshProUGUI hmiAngleText;
    private TextMeshProUGUI hmiRotText;
    private TextMeshProUGUI hmiSpeedText;
    private TextMeshProUGUI hmiSpeedSetText;
    private TextMeshProUGUI hmiStatusText;
    private TextMeshProUGUI hmiStatusValueText;
    private TMP_InputField hmiRotInput;
    private TMP_InputField hmiAngleInput;
    private TMP_InputField hmiSpeedInput;
    private Button hmiForwardButton;
    private Button hmiReverseButton;
    private readonly Color hmiDirectionNormalColor = Color.white;
    private readonly Color hmiDirectionSelectedColor = new Color(0.90f, 0.94f, 1f, 1f);
    private float hmiTargetSpeed = 0f;
    private float hmiTargetRotations;
    private float hmiTargetAngle;
    private string selectedMotionMode = "";
    private bool hasQueuedRunCommand;
    private bool initialized;
    private float visualDegreesPerSecond;
    private bool visualDirectionForward = true;
    private string visualSyncStatus = "Visual: waiting";
    private Camera controlMainCamera;
    private Camera motorPipCamera;
    private Camera wiringPipCamera;
    private Camera controlBackgroundBlurCamera;
    private GameObject controlCameraOverlayRoot;
    private GameObject controlBackgroundBlurRoot;
    private RawImage motorPipImage;
    private RawImage wiringPipImage;
    private RawImage controlBackgroundBlurImage;
    private RenderTexture motorPipTexture;
    private RenderTexture wiringPipTexture;
    private RenderTexture controlBackgroundBlurTexture;
    private bool controlCameraLayoutActive;
    private bool mainCameraStateSaved;
    private Vector3 savedMainCameraPosition;
    private Quaternion savedMainCameraRotation;
    private float savedMainCameraFov;
    private float savedMainCameraNearClip;
    private bool savedMainCameraOrthographic;
    private float savedMainCameraOrthographicSize;
    private Rect savedMainCameraRect;
    private RenderTexture savedMainCameraTargetTexture;
    private float nextPipCameraRefreshTime;
    private readonly List<BehaviourEnabledState> disabledMainCameraBehaviours = new List<BehaviourEnabledState>();
    private readonly List<LayerState> hmiLayerStates = new List<LayerState>();

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

    private void OnDisable()
    {
        DeactivateControlCameraLayout();
    }

    private void OnDestroy()
    {
        ReleaseControlCameraTextures();
        ReleaseControlBackgroundTexture();
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

        NormalizeGatewayUrls();

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
        LatestTelemetry.speedRpm = 0f;
        LatestTelemetry.setSpeedRpm = hmiTargetSpeed;
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
        if (controlCameraLayoutActive && Time.unscaledTime >= nextPipCameraRefreshTime)
        {
            nextPipCameraRefreshTime = Time.unscaledTime + 0.5f;
            Vector3 viewForward = mainCameraStateSaved
                ? savedMainCameraRotation * Vector3.forward
                : (controlMainCamera != null ? controlMainCamera.transform.forward : Vector3.forward);
            ConfigurePipCameras(viewForward);
        }

        if (!syncMotorModel || !LatestTelemetry.running || visualMotorRotor == null || visualDegreesPerSecond <= 0f)
            return;

        bool virtualMotorOwnsRotor = virtualMotor != null && virtualMotor.isActiveAndEnabled && virtualMotor.motorRotor == visualMotorRotor;
        bool bladesOwnRotor = rotateBlades != null
            && rotateBlades.isActiveAndEnabled
            && rotateBlades.rotatableObjects != null
            && rotateBlades.rotatableObjects.Contains(visualMotorRotor.gameObject);

        if (virtualMotorOwnsRotor || bladesOwnRotor)
            return;

        // Rotor_Main model faces opposite the real motor convention, so invert
        // visual forward/reverse direction in the fallback rotation path too.
        float direction = visualDirectionForward ? -1f : 1f;
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
        if (!HasValidQueuedRunCommand())
        {
            Debug.LogWarning("[PLCController_v2] START ignored: chua SET so vong/goc hop le.");
            ShowHmiStatusMessage("Chua SET vong/goc", new Color(0.9f, 0.42f, 0.05f, 1f));
            return;
        }

        float speed = hmiTargetSpeed > 0f
            ? Mathf.Clamp(hmiTargetSpeed, 1f, 100f)
            : Mathf.Max(0f, LatestTelemetry.setSpeedRpm);
        SendControl("ON", speed: speed, rotations: hmiTargetRotations, angle: hmiTargetAngle, mode: selectedMotionMode);
    }

    public void TurnOff()
    {
        SendControl("OFF");
    }

    public void SetSpeed(float rpm)
    {
        float previousSpeed = hmiTargetSpeed;
        hmiTargetSpeed = Mathf.Clamp(rpm, 1f, 100f);
        LatestTelemetry.setSpeedRpm = hmiTargetSpeed;
        if (hmiSpeedInput != null)
            hmiSpeedInput.text = hmiTargetSpeed.ToString("F0");

        int pulseDelta = Mathf.RoundToInt(hmiTargetSpeed - Mathf.Max(0f, previousSpeed));
        if (pulseDelta > 0)
            SendControl("SPEED_UP", speed: pulseDelta);
        else if (pulseDelta < 0)
            SendControl("SPEED_DOWN", speed: -pulseDelta);
        else
            PublishTelemetry();
    }

    // Tang/giam toc bang xung M15/M16 (giong nut +/- tren HMI cung).
    // pulses = so lan xung gui xuong moi lan bam.
    public void SpeedUp(int pulses = 1)
    {
        hmiTargetSpeed = Mathf.Clamp(hmiTargetSpeed + Mathf.Max(1, pulses), 1f, 100f);
        LatestTelemetry.setSpeedRpm = hmiTargetSpeed;
        if (hmiSpeedInput != null)
            hmiSpeedInput.text = hmiTargetSpeed.ToString("F0");
        SendControl("SPEED_UP", speed: Mathf.Max(1, pulses));
    }

    public void SpeedDown(int pulses = 1)
    {
        hmiTargetSpeed = Mathf.Clamp(hmiTargetSpeed - Mathf.Max(1, pulses), 1f, 100f);
        LatestTelemetry.setSpeedRpm = hmiTargetSpeed;
        if (hmiSpeedInput != null)
            hmiSpeedInput.text = hmiTargetSpeed.ToString("F0");
        SendControl("SPEED_DOWN", speed: Mathf.Max(1, pulses));
    }

    public void SetTargetRotations(float rotations)
    {
        float value = Mathf.Max(0f, rotations);
        if (value <= 0f)
        {
            Debug.LogWarning("[PLCController_v2] SET_ROTATIONS ignored: gia tri phai lon hon 0.");
            ShowHmiStatusMessage("Gia tri vong khong hop le", new Color(0.9f, 0.42f, 0.05f, 1f));
            return;
        }

        hmiTargetRotations = value;
        hmiTargetAngle = 0f;
        selectedMotionMode = "rotations";
        hasQueuedRunCommand = true;
        LatestTelemetry.motionMode = selectedMotionMode;
        SendControl("SET_ROTATIONS", rotations: hmiTargetRotations, angle: 0f, mode: selectedMotionMode);
    }

    public void SetTargetAngle(float angle)
    {
        float value = Mathf.Max(0f, angle);
        if (value <= 0f)
        {
            Debug.LogWarning("[PLCController_v2] SET_ANGLE ignored: gia tri phai lon hon 0.");
            ShowHmiStatusMessage("Gia tri goc khong hop le", new Color(0.9f, 0.42f, 0.05f, 1f));
            return;
        }

        hmiTargetAngle = value;
        hmiTargetRotations = 0f;
        selectedMotionMode = "angle";
        hasQueuedRunCommand = true;
        LatestTelemetry.motionMode = selectedMotionMode;
        SendControl("SET_ANGLE", rotations: 0f, angle: hmiTargetAngle, mode: selectedMotionMode);
    }

    private bool HasValidQueuedRunCommand()
    {
        if (!hasQueuedRunCommand)
            return false;

        if (selectedMotionMode == "rotations")
            return hmiTargetRotations > 0f;

        if (selectedMotionMode == "angle")
            return hmiTargetAngle > 0f;

        return false;
    }

    private void ShowHmiStatusMessage(string message, Color color)
    {
        TextMeshProUGUI target = hmiStatusValueText != null
            ? hmiStatusValueText
            : hmiStatusText;
        if (target == null)
            return;

        target.text = hmiStatusValueText != null
            ? message
            : $"Trang thai: {message}";
        target.color = color;
    }

    private void RefreshHmiInputCacheBeforeStart()
    {
        if (hmiSpeedInput != null && float.TryParse(hmiSpeedInput.text, out float speed))
            hmiTargetSpeed = speed > 0f ? Mathf.Clamp(speed, 1f, 100f) : 0f;

        if (selectedMotionMode == "rotations")
        {
            if (hmiRotInput != null && float.TryParse(hmiRotInput.text, out float rotations))
            {
                hmiTargetRotations = Mathf.Max(0f, rotations);
                hmiTargetAngle = 0f;
            }
            return;
        }

        if (selectedMotionMode == "angle")
        {
            if (hmiAngleInput != null && float.TryParse(hmiAngleInput.text, out float angle))
            {
                hmiTargetAngle = Mathf.Max(0f, angle);
                hmiTargetRotations = 0f;
            }
            return;
        }

        if (hmiRotInput != null && float.TryParse(hmiRotInput.text, out float rotValue) && rotValue > 0f)
        {
            hmiTargetRotations = Mathf.Max(0f, rotValue);
            hmiTargetAngle = 0f;
            selectedMotionMode = "rotations";
            LatestTelemetry.motionMode = selectedMotionMode;
        }
        else if (hmiAngleInput != null && float.TryParse(hmiAngleInput.text, out float angleValue) && angleValue > 0f)
        {
            hmiTargetAngle = Mathf.Max(0f, angleValue);
            hmiTargetRotations = 0f;
            selectedMotionMode = "angle";
            LatestTelemetry.motionMode = selectedMotionMode;
        }
    }

    public void SetDirectionForward()
    {
        LatestTelemetry.direction = "forward";
        UpdateDirectionButtons();
        SendControl("SET_DIRECTION", direction: "forward");
    }

    public void SetDirectionReverse()
    {
        LatestTelemetry.direction = "reverse";
        UpdateDirectionButtons();
        SendControl("SET_DIRECTION", direction: "reverse");
    }

    private void ResetHmiInputFields()
    {
        hmiTargetSpeed = 0f;
        hmiTargetRotations = 0f;
        hmiTargetAngle = 0f;
        selectedMotionMode = "";
        hasQueuedRunCommand = false;
        LatestTelemetry.setSpeedRpm = 0f;
        LatestTelemetry.motionMode = "";

        if (hmiRotInput != null)
            hmiRotInput.text = "0";
        if (hmiAngleInput != null)
            hmiAngleInput.text = "0";
        if (hmiSpeedInput != null)
            hmiSpeedInput.text = "0";
        if (hmiSpeedSetText != null)
            hmiSpeedSetText.text = "0";
    }

    private void ResetHmiInputsAndPlc()
    {
        ResetHmiInputFields();
        SendControl("ERR_RESET", speed: 0f, rotations: 0f, angle: 0f, mode: "");
    }

    private void ResetHmiData()
    {
        ResetHmiInputFields();
        LatestTelemetry.running = false;
        LatestTelemetry.speedRpm = 0f;
        LatestTelemetry.setSpeedRpm = 0f;
        LatestTelemetry.pulseFrequency = 0f;
        LatestTelemetry.count = 0;
        LatestTelemetry.rotations = 0f;
        LatestTelemetry.angle = 0f;
        LatestTelemetry.encoderCount = 0;
        LatestTelemetry.rotationsExact = 0f;
        LatestTelemetry.pulsesPerSample = 0f;
        LatestTelemetry.speedRawD164 = 0f;
        UpdateCanvasHmi();
        SendControl(
            "RESET_COUNTER",
            speed: 0f,
            rotations: 0f,
            angle: 0f,
            direction: LatestTelemetry.direction,
            mode: "");
    }

    private void UpdateDirectionButtons()
    {
        bool isReverse = LatestTelemetry.direction.Equals("reverse", StringComparison.OrdinalIgnoreCase);
        ApplyButtonColor(hmiForwardButton, isReverse ? hmiDirectionNormalColor : hmiDirectionSelectedColor);
        ApplyButtonColor(hmiReverseButton, isReverse ? hmiDirectionSelectedColor : hmiDirectionNormalColor);
    }

    private void ApplyButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.12f;
        colors.pressedColor = color * 0.85f;
        colors.selectedColor = color;
        button.colors = colors;
    }

    public void SendControl(string action)
    {
        SendControl(action, hmiTargetSpeed, hmiTargetRotations, hmiTargetAngle, LatestTelemetry.direction, selectedMotionMode);
    }

    public void SetRuntimeHmiVisible(bool visible)
    {
        runtimeHmiVisible = visible;

        if (createCanvasHmi && canvasHmiRoot == null)
            CreateCanvasHmi();

        if (visible)
        {
            if (canvasHmiRoot != null)
                canvasHmiRoot.SetActive(true);

            ResetHmiInputFields();
            UpdateDirectionButtons();
            ActivateControlCameraLayout();
        }
        else
        {
            DeactivateControlCameraLayout();

            if (canvasHmiRoot != null)
                canvasHmiRoot.SetActive(false);
        }
    }

    private void SendControl(string action, float speed = -1f, float rotations = -1f, float angle = -1f, string direction = "", string mode = "")
    {
        if (speed < 0f) speed = hmiTargetSpeed > 0f ? hmiTargetSpeed : 0f;
        if (rotations < 0f) rotations = hmiTargetRotations;
        if (angle < 0f) angle = hmiTargetAngle;
        if (string.IsNullOrWhiteSpace(direction)) direction = LatestTelemetry.direction;
        if (string.IsNullOrWhiteSpace(mode)) mode = selectedMotionMode;

        var command = new ControlCommand
        {
            action = action,
            runId = runId,
            lessonId = lessonId,
            userId = userId,
            speed = speed,
            rotations = rotations,
            angle = angle,
            mode = mode,
            direction = direction,
            timestamp = DateTimeOffset.UtcNow.ToString("o")
        };

        StartCoroutine(PostControlRoutine(command));

        if (ShouldApplyLocalTelemetryImmediately(command))
            ApplyOptimisticTelemetry(command);
        else if (optimisticLocalTelemetry)
            ApplyOptimisticTelemetry(command);
    }

    private bool ShouldApplyLocalTelemetryImmediately(ControlCommand command)
    {
        if (command == null)
            return false;

        string action = command.action ?? "";
        return action.Equals("ON", StringComparison.OrdinalIgnoreCase)
            || action.Equals("OFF", StringComparison.OrdinalIgnoreCase)
            || action.Equals("SET_DIRECTION", StringComparison.OrdinalIgnoreCase);
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
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");
            request.timeout = GetRequestTimeoutSeconds(command);

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
                string responseText = request.downloadHandler.text;
                Debug.Log($"[PLCController_v2] Control {command.action}: {responseText}");
                ApplyTelemetryFromControlResponse(responseText);
            }
        }
    }

    private int GetRequestTimeoutSeconds(ControlCommand command)
    {
        int baseTimeout = Mathf.Max(1, timeoutSeconds);
        if (command == null || string.IsNullOrWhiteSpace(command.action))
            return baseTimeout;

        bool isSpeedPulse = command.action.Equals("SPEED_UP", StringComparison.OrdinalIgnoreCase)
            || command.action.Equals("SPEED_DOWN", StringComparison.OrdinalIgnoreCase);
        if (!isSpeedPulse)
            return baseTimeout;

        int pulseCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(command.speed)));
        return Mathf.Max(baseTimeout, Mathf.CeilToInt(2f + pulseCount * 0.18f));
    }

    private void ApplyTelemetryFromControlResponse(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return;

        bool looksLikeTelemetry =
            responseText.IndexOf("\"running\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
            responseText.IndexOf("\"speedRpm\"", StringComparison.OrdinalIgnoreCase) >= 0;

        if (!looksLikeTelemetry)
            return;

        try
        {
            MotorTelemetry telemetry = JsonUtility.FromJson<MotorTelemetry>(responseText);
            if (telemetry != null)
                ApplyTelemetry(telemetry, true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PLCController_v2] Khong doc duoc telemetry tu control response: {ex.Message}");
        }
    }

    private IEnumerator PollTelemetryRoutine()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BuildUrl(telemetryEndpoint)))
            {
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("ngrok-skip-browser-warning", "true");
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
        if (fromPi)
        {
            telemetry.setSpeedRpm = telemetry.setSpeedRpm > 0f
                ? Mathf.Clamp(telemetry.setSpeedRpm, 1f, 100f)
                : hmiTargetSpeed;
        }
        else if (telemetry.setSpeedRpm <= 0f)
            telemetry.setSpeedRpm = hmiTargetSpeed;
        if (!string.IsNullOrWhiteSpace(telemetry.motionMode))
            selectedMotionMode = telemetry.motionMode;
        else
            telemetry.motionMode = selectedMotionMode;

        LatestTelemetry = telemetry;
        if (fromPi)
            IsPiOnline = true;

        UpdateDirectionButtons();
        SyncMotorFromTelemetry();
        PublishTelemetry();
    }

    private void ApplyOptimisticTelemetry(ControlCommand command)
    {
        LatestTelemetry.action = command.action;
        LatestTelemetry.timestamp = command.timestamp;
        LatestTelemetry.setSpeedRpm = hmiTargetSpeed > 0f ? hmiTargetSpeed : Mathf.Max(0f, command.speed);
        LatestTelemetry.direction = command.direction;
        LatestTelemetry.motionMode = command.mode;
        LatestTelemetry.backendSynced = false;
        LatestTelemetry.backendStatus = IsPiOnline ? "PENDING" : "LOCAL_FALLBACK";

        if (command.rotations > 0f)
            LatestTelemetry.rotations = command.rotations;
        if (command.angle > 0f)
            LatestTelemetry.angle = command.angle;

        if (command.action.Equals("ON", StringComparison.OrdinalIgnoreCase))
            LatestTelemetry.running = true;
        else if (command.action.Equals("OFF", StringComparison.OrdinalIgnoreCase))
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

        // speedRpm la RPM phan hoi thuc te. Neu mau toc do ve 0 trong luc PLC van RUN,
        // dung toc do dat/tan so xung lam fallback de model 3D khong bi dung khung.
        float feedbackRpm = Mathf.Abs(LatestTelemetry.speedRpm);
        float fallbackRpm = LatestTelemetry.setSpeedRpm > 0f
            ? LatestTelemetry.setSpeedRpm
            : Mathf.Max(0f, LatestTelemetry.pulseFrequency / 5000f * 60f);
        bool feedbackLooksTooLow = LatestTelemetry.running
            && fallbackRpm > 0f
            && feedbackRpm < fallbackRpm * 0.5f;
        float realRpm = feedbackRpm > 0.5f && !feedbackLooksTooLow
            ? feedbackRpm
            : (LatestTelemetry.running ? fallbackRpm : 0f);
        visualDegreesPerSecond = LatestTelemetry.running ? realRpm * 6f : 0f; // 1 rpm = 6 deg/s
        float visualRpm = realRpm;
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

    private float GetDisplayRpm()
    {
        float feedbackRpm = Mathf.Abs(LatestTelemetry.speedRpm);
        float setRpm = Mathf.Max(0f, LatestTelemetry.setSpeedRpm);
        if (LatestTelemetry.running && setRpm > 0f && feedbackRpm < setRpm * 0.5f)
            return setRpm;

        return feedbackRpm;
    }

    private void ActivateControlCameraLayout()
    {
        if (!enableControlCameraLayout)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[PLCController_v2] Khong tim thay MainCamera de zoom vao HMI.");
            return;
        }

        controlMainCamera = mainCamera;
        SaveMainCameraState(mainCamera);
        DisableMainCameraFramingBehaviours(mainCamera);
        AssignWorldCanvasCamera(mainCamera);

        Vector3 viewForward = savedMainCameraRotation * Vector3.forward;
        Transform hmiTarget = hmiScreenObject != null ? hmiScreenObject.transform : (canvasHmiRoot != null ? canvasHmiRoot.transform : null);
        bool hmiUsesScreenSpace = IsCanvasHmiScreenSpace();
        mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        mainCamera.orthographic = false;

        if (hmiUsesScreenSpace)
        {
            if (TryCalculateWiringOverviewBounds(out Bounds overviewBounds))
                FrameCameraAtBoundsTight(mainCamera, overviewBounds, viewForward, wiringPipCameraFov, 0.9f, 0.16f);
            else if (TryCalculateWiringPipBounds(out Bounds wiringBounds))
                FrameCameraAtBoundsTight(mainCamera, wiringBounds, viewForward, wiringPipCameraFov, 0.85f, 0.16f);
        }
        else if (hmiTarget != null)
        {
            FrameCameraAtTarget(mainCamera, hmiTarget, viewForward, controlHmiCameraFov, controlHmiDistanceScale, controlHmiMinDistance);
        }

        if (hmiUsesScreenSpace)
            ActivateControlBackgroundBlur(mainCamera);

        EnsureControlCameraOverlay();
        MoveHmiToUiLayerForPip();
        ConfigurePipCameras(viewForward);

        if (controlCameraOverlayRoot != null)
            controlCameraOverlayRoot.SetActive(true);

        controlCameraLayoutActive = true;
        nextPipCameraRefreshTime = 0f;
    }

    private bool IsCanvasHmiScreenSpace()
    {
        if (canvasHmiRoot == null)
            return false;

        Canvas canvas = canvasHmiRoot.GetComponent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.WorldSpace;
    }

    private void DeactivateControlCameraLayout()
    {
        DeactivateControlBackgroundBlur();

        if (motorPipCamera != null)
            motorPipCamera.gameObject.SetActive(false);
        if (wiringPipCamera != null)
            wiringPipCamera.gameObject.SetActive(false);
        if (controlCameraOverlayRoot != null)
            controlCameraOverlayRoot.SetActive(false);

        RestoreHmiLayers();

        if (mainCameraStateSaved && controlMainCamera != null)
        {
            controlMainCamera.transform.SetPositionAndRotation(savedMainCameraPosition, savedMainCameraRotation);
            controlMainCamera.fieldOfView = savedMainCameraFov;
            controlMainCamera.nearClipPlane = savedMainCameraNearClip;
            controlMainCamera.orthographic = savedMainCameraOrthographic;
            controlMainCamera.orthographicSize = savedMainCameraOrthographicSize;
            controlMainCamera.rect = savedMainCameraRect;
            controlMainCamera.targetTexture = savedMainCameraTargetTexture;
        }

        RestoreMainCameraFramingBehaviours();
        controlCameraLayoutActive = false;
        mainCameraStateSaved = false;
        controlMainCamera = null;
    }

    private void SaveMainCameraState(Camera mainCamera)
    {
        if (mainCameraStateSaved || mainCamera == null)
            return;

        savedMainCameraPosition = mainCamera.transform.position;
        savedMainCameraRotation = mainCamera.transform.rotation;
        savedMainCameraFov = mainCamera.fieldOfView;
        savedMainCameraNearClip = mainCamera.nearClipPlane;
        savedMainCameraOrthographic = mainCamera.orthographic;
        savedMainCameraOrthographicSize = mainCamera.orthographicSize;
        savedMainCameraRect = mainCamera.rect;
        savedMainCameraTargetTexture = mainCamera.targetTexture;
        mainCameraStateSaved = true;
    }

    private void DisableMainCameraFramingBehaviours(Camera mainCamera)
    {
        if (mainCamera == null || disabledMainCameraBehaviours.Count > 0)
            return;

        MonoBehaviour[] behaviours = mainCamera.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.enabled)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName != "LockedCameraController" && typeName != "ResponsiveCameraFraming")
                continue;

            disabledMainCameraBehaviours.Add(new BehaviourEnabledState
            {
                behaviour = behaviour,
                enabled = behaviour.enabled
            });
            behaviour.enabled = false;
        }
    }

    private void RestoreMainCameraFramingBehaviours()
    {
        foreach (BehaviourEnabledState state in disabledMainCameraBehaviours)
        {
            if (state.behaviour != null)
                state.behaviour.enabled = state.enabled;
        }

        disabledMainCameraBehaviours.Clear();
    }

    private void AssignWorldCanvasCamera(Camera camera)
    {
        if (camera == null || canvasHmiRoot == null)
            return;

        Canvas[] canvases = canvasHmiRoot.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                canvas.worldCamera = camera;
        }
    }

    private void ActivateControlBackgroundBlur(Camera sourceCamera)
    {
        if (sourceCamera == null)
            return;

        EnsureControlBackgroundBlurRoot();

        int width = Mathf.Max(256, Screen.width / 6);
        int height = Mathf.Max(144, Screen.height / 6);
        if (controlBackgroundBlurTexture == null ||
            controlBackgroundBlurTexture.width != width ||
            controlBackgroundBlurTexture.height != height)
        {
            ReleaseControlBackgroundTexture();
            controlBackgroundBlurTexture = new RenderTexture(
                width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "ControlBackgroundBlurTexture",
                filterMode = FilterMode.Bilinear,
                antiAliasing = 1,
                useMipMap = false
            };
            controlBackgroundBlurTexture.Create();
        }

        if (controlBackgroundBlurImage != null)
            controlBackgroundBlurImage.texture = controlBackgroundBlurTexture;

        Camera blurCamera = EnsurePipCamera(
            ref controlBackgroundBlurCamera,
            "Runtime_Control_Background_Blur_Camera");
        CopyPipCameraSettings(sourceCamera, blurCamera);
        blurCamera.transform.SetPositionAndRotation(
            sourceCamera.transform.position,
            sourceCamera.transform.rotation);
        blurCamera.orthographic = sourceCamera.orthographic;
        blurCamera.orthographicSize = sourceCamera.orthographicSize;
        blurCamera.fieldOfView = sourceCamera.fieldOfView;
        blurCamera.rect = new Rect(0f, 0f, 1f, 1f);
        blurCamera.targetTexture = controlBackgroundBlurTexture;
        blurCamera.enabled = true;
        blurCamera.gameObject.SetActive(true);

        sourceCamera.targetTexture = savedMainCameraTargetTexture;
        if (controlBackgroundBlurRoot != null)
            controlBackgroundBlurRoot.SetActive(true);
    }

    private void EnsureControlBackgroundBlurRoot()
    {
        if (controlBackgroundBlurRoot != null)
            return;

        controlBackgroundBlurRoot = new GameObject("Runtime_Control_Background_Blur");
        Canvas canvas = controlBackgroundBlurRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = controlBackgroundBlurRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        GameObject background = new GameObject("BlurredCameraBackground");
        background.transform.SetParent(controlBackgroundBlurRoot.transform, false);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        controlBackgroundBlurImage = background.AddComponent<RawImage>();
        controlBackgroundBlurImage.raycastTarget = false;

        GameObject veil = new GameObject("CoolLightVeil");
        veil.transform.SetParent(controlBackgroundBlurRoot.transform, false);
        RectTransform veilRect = veil.AddComponent<RectTransform>();
        veilRect.anchorMin = Vector2.zero;
        veilRect.anchorMax = Vector2.one;
        veilRect.offsetMin = Vector2.zero;
        veilRect.offsetMax = Vector2.zero;
        Image veilImage = veil.AddComponent<Image>();
        veilImage.color = new Color(0.86f, 0.92f, 0.98f, 0.30f);
        veilImage.raycastTarget = false;

        controlBackgroundBlurRoot.SetActive(false);
    }

    private void DeactivateControlBackgroundBlur()
    {
        if (controlMainCamera != null)
            controlMainCamera.targetTexture = savedMainCameraTargetTexture;

        if (controlBackgroundBlurCamera != null)
        {
            controlBackgroundBlurCamera.enabled = false;
            controlBackgroundBlurCamera.gameObject.SetActive(false);
        }

        if (controlBackgroundBlurRoot != null)
            controlBackgroundBlurRoot.SetActive(false);
    }

    private void ReleaseControlBackgroundTexture()
    {
        if (controlBackgroundBlurImage != null)
            controlBackgroundBlurImage.texture = null;

        if (controlBackgroundBlurCamera != null &&
            controlBackgroundBlurCamera.targetTexture == controlBackgroundBlurTexture)
        {
            controlBackgroundBlurCamera.targetTexture = null;
        }

        if (controlBackgroundBlurTexture == null)
            return;

        controlBackgroundBlurTexture.Release();
        Destroy(controlBackgroundBlurTexture);
        controlBackgroundBlurTexture = null;
    }

    private void EnsureControlCameraOverlay()
    {
        if (controlCameraOverlayRoot != null)
            return;

        controlCameraOverlayRoot = new GameObject("Runtime_Control_Camera_Overlay");
        Canvas canvas = controlCameraOverlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500;

        CanvasScaler scaler = controlCameraOverlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        controlCameraOverlayRoot.AddComponent<GraphicRaycaster>();

        motorPipImage = CreatePipPanel(
            controlCameraOverlayRoot.transform,
            "MotorPipPanel",
            "MOTOR \u1EA2O",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            motorPipOffset,
            motorPipSize);

        if (!IsCanvasHmiScreenSpace())
        {
            wiringPipImage = CreatePipPanel(
                controlCameraOverlayRoot.transform,
                "WiringPipPanel",
                "DAY NOI",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                wiringPipOffset,
                wiringPipSize);
        }

        controlCameraOverlayRoot.SetActive(false);
    }

    private RawImage CreatePipPanel(Transform parent, string name, string title, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorMin;
        panelRect.anchorMax = anchorMax;
        panelRect.pivot = pivot;
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panel.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.92f);
        background.raycastTarget = false;
        AddShadow(panel, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -5f));

        TextMeshProUGUI label = CreateText(panel.transform, "Label", title, new Vector2(8f, -4f), new Vector2(size.x - 16f, 22f), 13, true);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(0.08f, 0.1f, 0.12f, 1f);

        GameObject view = new GameObject("View");
        view.transform.SetParent(panel.transform, false);
        RectTransform viewRect = view.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(6f, 6f);
        viewRect.offsetMax = new Vector2(-6f, -30f);

        RawImage rawImage = view.AddComponent<RawImage>();
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        return rawImage;
    }

    private void ConfigurePipCameras(Vector3 baseViewForward)
    {
        Camera sourceCamera = controlMainCamera != null ? controlMainCamera : Camera.main;
        if (sourceCamera == null)
            return;

        RenderTexture motorTexture = EnsureRenderTexture(ref motorPipTexture, motorPipSize, "MotorPipTexture");
        if (motorPipImage != null)
            motorPipImage.texture = motorTexture;

        RenderTexture wiringTexture = null;
        if (wiringPipImage != null)
        {
            wiringTexture = EnsureRenderTexture(ref wiringPipTexture, wiringPipSize, "WiringPipTexture");
            wiringPipImage.texture = wiringTexture;
        }

        Camera motorCamera = EnsurePipCamera(ref motorPipCamera, "Runtime_Motor_PIP_Camera");
        CopyPipCameraSettings(sourceCamera, motorCamera);
        motorCamera.targetTexture = motorTexture;
        motorCamera.enabled = true;
        motorCamera.gameObject.SetActive(true);

        Transform motorTarget = FindMotorViewTarget();
        if (motorTarget != null)
            FrameCameraAtTarget(motorCamera, motorTarget, GetMotorPipViewForward(baseViewForward), motorPipCameraFov, motorPipDistanceScale, 0.18f, false);
        else
            motorCamera.transform.SetPositionAndRotation(savedMainCameraPosition, savedMainCameraRotation);

        if (wiringPipImage != null)
        {
            Camera wiringCamera = EnsurePipCamera(ref wiringPipCamera, "Runtime_Wiring_PIP_Camera");
            CopyPipCameraSettings(sourceCamera, wiringCamera);
            wiringCamera.targetTexture = wiringTexture;
            float effectiveWiringFov = Mathf.Clamp(wiringPipCameraFov, 28f, 38f);
            if (TryCalculateWiringOverviewBounds(out Bounds overviewBounds))
                FrameCameraAtBoundsTight(wiringCamera, overviewBounds, baseViewForward, effectiveWiringFov, 0.9f, 0.16f);
            else if (TryCalculateWiringPipBounds(out Bounds wiringBounds))
                FrameCameraAtBoundsTight(wiringCamera, wiringBounds, baseViewForward, effectiveWiringFov, 0.78f, 0.16f);
            else
            {
                wiringCamera.fieldOfView = effectiveWiringFov;
                wiringCamera.transform.SetPositionAndRotation(savedMainCameraPosition, savedMainCameraRotation);
            }
            wiringCamera.enabled = true;
            wiringCamera.gameObject.SetActive(true);
        }
        else if (wiringPipCamera != null)
        {
            wiringPipCamera.enabled = false;
            wiringPipCamera.gameObject.SetActive(false);
        }
    }

    private Camera EnsurePipCamera(ref Camera camera, string name)
    {
        if (camera != null)
            return camera;

        GameObject cameraObject = new GameObject(name);
        cameraObject.transform.SetParent(transform, false);
        camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        return camera;
    }

    private void CopyPipCameraSettings(Camera source, Camera target)
    {
        if (target == null)
            return;

        target.clearFlags = source != null ? source.clearFlags : CameraClearFlags.Skybox;
        target.backgroundColor = source != null ? source.backgroundColor : Color.black;
        target.cullingMask = GetPipCullingMask(source);
        target.nearClipPlane = 0.02f;
        target.farClipPlane = source != null ? source.farClipPlane : 1000f;
        target.orthographic = false;
        target.fieldOfView = controlPipCameraFov;
        target.depth = source != null ? source.depth + 1f : 1f;
        target.allowHDR = source == null || source.allowHDR;
        target.allowMSAA = source == null || source.allowMSAA;
    }

    private int GetPipCullingMask(Camera source)
    {
        int mask = source != null ? source.cullingMask : ~0;
        if (!hideHmiInPipViews)
            return mask;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            mask &= ~(1 << uiLayer);

        return mask;
    }

    private RenderTexture EnsureRenderTexture(ref RenderTexture texture, Vector2 size, string name)
    {
        int width = Mathf.Max(128, Mathf.RoundToInt(size.x * 2f));
        int height = Mathf.Max(72, Mathf.RoundToInt(size.y * 2f));

        if (texture != null && texture.width == width && texture.height == height)
            return texture;

        ReleaseRenderTexture(ref texture);
        texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        texture.name = name;
        texture.Create();
        return texture;
    }

    private void ReleaseControlCameraTextures()
    {
        if (motorPipImage != null)
            motorPipImage.texture = null;
        if (wiringPipImage != null)
            wiringPipImage.texture = null;

        ReleaseRenderTexture(ref motorPipTexture);
        ReleaseRenderTexture(ref wiringPipTexture);
    }

    private void ReleaseRenderTexture(ref RenderTexture texture)
    {
        if (texture == null)
            return;

        texture.Release();
        if (Application.isPlaying)
            Destroy(texture);
        else
            DestroyImmediate(texture);
        texture = null;
    }

    private void MoveHmiToUiLayerForPip()
    {
        if (!hideHmiInPipViews || canvasHmiRoot == null || hmiLayerStates.Count > 0)
            return;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            return;

        Transform[] transforms = canvasHmiRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform item in transforms)
        {
            if (item == null)
                continue;

            hmiLayerStates.Add(new LayerState
            {
                gameObject = item.gameObject,
                layer = item.gameObject.layer
            });
            item.gameObject.layer = uiLayer;
        }
    }

    private void RestoreHmiLayers()
    {
        foreach (LayerState state in hmiLayerStates)
        {
            if (state.gameObject != null)
                state.gameObject.layer = state.layer;
        }

        hmiLayerStates.Clear();
    }

    private Transform FindMotorViewTarget()
    {
        if (visualMotorRotor == null)
            visualMotorRotor = FindLikelyRotor();

        if (virtualMotor != null)
            return virtualMotor.transform;

        if (rotateBlades != null)
            return rotateBlades.transform;

        return visualMotorRotor;
    }

    private Vector3 GetMotorPipViewForward(Vector3 baseViewForward)
    {
        Vector3 right = mainCameraStateSaved ? savedMainCameraRotation * Vector3.right : Vector3.right;
        Vector3 up = mainCameraStateSaved ? savedMainCameraRotation * Vector3.up : Vector3.up;
        Vector3 forward = baseViewForward + right * 0.55f - up * 0.08f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : baseViewForward;
    }

    private void FrameCameraAtTarget(Camera camera, Transform target, Vector3 viewForward, float fov, float distanceScale, float minDistance, bool includeInactiveBounds = true)
    {
        if (camera == null || target == null)
            return;

        Bounds bounds = CalculateTargetBounds(target, includeInactiveBounds);
        FrameCameraAtBounds(camera, bounds, viewForward, fov, distanceScale, minDistance);
    }

    private void FrameCameraAtBounds(Camera camera, Bounds bounds, Vector3 viewForward, float fov, float distanceScale, float minDistance)
    {
        if (camera == null)
            return;

        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
        float safeFov = Mathf.Clamp(fov, 12f, 75f);
        float fitDistance = radius / Mathf.Tan(safeFov * 0.5f * Mathf.Deg2Rad);
        float distance = Mathf.Max(minDistance, fitDistance * Mathf.Max(0.8f, distanceScale));
        Vector3 forward = viewForward.sqrMagnitude > 0.0001f ? viewForward.normalized : camera.transform.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        Vector3 up = mainCameraStateSaved ? savedMainCameraRotation * Vector3.up : Vector3.up;
        if (up.sqrMagnitude <= 0.0001f || Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.98f)
            up = Vector3.up;

        camera.orthographic = false;
        camera.fieldOfView = safeFov;
        camera.nearClipPlane = Mathf.Min(0.02f, Mathf.Max(0.005f, distance * 0.08f));
        camera.transform.SetPositionAndRotation(center - forward * distance, Quaternion.LookRotation(forward, up));
    }

    private void FrameCameraAtBoundsTight(Camera camera, Bounds bounds, Vector3 viewForward, float fov, float distanceScale, float minDistance)
    {
        if (camera == null)
            return;

        Vector3 center = bounds.center;
        float safeFov = Mathf.Clamp(fov, 12f, 75f);
        float aspect = 16f / 9f;
        if (camera.targetTexture != null && camera.targetTexture.height > 0)
            aspect = Mathf.Max(0.1f, (float)camera.targetTexture.width / camera.targetTexture.height);
        else if (Screen.height > 0)
            aspect = Mathf.Max(0.1f, (float)Screen.width / Screen.height);

        float halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect, 0.04f);
        float fitDistance = halfHeight / Mathf.Tan(safeFov * 0.5f * Mathf.Deg2Rad);
        float depthPadding = Mathf.Max(0.05f, bounds.extents.z * 0.35f);
        float distance = Mathf.Max(minDistance, fitDistance * Mathf.Max(0.45f, distanceScale) + depthPadding);
        Vector3 forward = viewForward.sqrMagnitude > 0.0001f ? viewForward.normalized : camera.transform.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        Vector3 up = mainCameraStateSaved ? savedMainCameraRotation * Vector3.up : Vector3.up;
        if (up.sqrMagnitude <= 0.0001f || Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.98f)
            up = Vector3.up;

        camera.orthographic = false;
        camera.fieldOfView = safeFov;
        camera.nearClipPlane = Mathf.Min(0.02f, Mathf.Max(0.005f, distance * 0.08f));
        camera.transform.SetPositionAndRotation(center - forward * distance, Quaternion.LookRotation(forward, up));
    }

    private bool TryCalculateWiringPipBounds(out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

#if UNITY_2023_1_OR_NEWER
        LineRenderer[] lines = FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        LineRenderer[] lines = FindObjectsOfType<LineRenderer>();
#endif
        foreach (LineRenderer line in lines)
        {
            if (line == null || !line.gameObject.activeInHierarchy || IsUnderRuntimeHmi(line.transform))
                continue;

            EncapsulateBounds(ref bounds, ref hasBounds, line.bounds);
        }

#if UNITY_2023_1_OR_NEWER
        TextMeshProUGUI[] labels = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        TextMeshProUGUI[] labels = FindObjectsOfType<TextMeshProUGUI>();
#endif
        Vector3[] corners = new Vector3[4];
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null || !label.gameObject.activeInHierarchy || IsUnderRuntimeHmi(label.transform))
                continue;

            string labelName = label.name;
            if (!labelName.StartsWith("Label_", StringComparison.OrdinalIgnoreCase)
                && !labelName.Contains("Socket"))
                continue;

            RectTransform rectTransform = label.rectTransform;
            rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                EncapsulatePoint(ref bounds, ref hasBounds, corners[i]);
        }

        if (!hasBounds)
            return false;

        bounds.Expand(Mathf.Max(0.04f, bounds.extents.magnitude * 0.18f));
        return true;
    }

    private bool TryCalculateWiringOverviewBounds(out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer anchor = FindWiringOverviewAnchorRenderer();
        if (anchor == null)
            return false;

        Vector3 anchorCenter = anchor.bounds.center;
        bool hasBounds = false;

#if UNITY_2023_1_OR_NEWER
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Renderer[] renderers = FindObjectsOfType<Renderer>();
#endif
        foreach (Renderer renderer in renderers)
        {
            if (!IsWiringOverviewRenderer(renderer))
                continue;

            Bounds rendererBounds = renderer.bounds;
            float distanceFromAnchor = Vector3.Distance(rendererBounds.center, anchorCenter);
            if (renderer != anchor && distanceFromAnchor > 1.6f)
                continue;

            EncapsulateBounds(ref bounds, ref hasBounds, rendererBounds);
        }

        if (!hasBounds)
            return false;

        bounds.Expand(Mathf.Max(0.08f, bounds.extents.magnitude * 0.12f));
        return true;
    }

    private Renderer FindWiringOverviewAnchorRenderer()
    {
#if UNITY_2023_1_OR_NEWER
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Renderer[] renderers = FindObjectsOfType<Renderer>();
#endif
        Renderer fallback = null;
        float fallbackSize = 0f;

        foreach (Renderer renderer in renderers)
        {
            if (!IsWiringOverviewRenderer(renderer))
                continue;

            string path = GetHierarchyPath(renderer.transform);
            if (ContainsIgnoreCase(path, "Board_Frame") || ContainsIgnoreCase(path, "Board_Table"))
                return renderer;

            if (ContainsIgnoreCase(path, "/Board") || string.Equals(renderer.name, "Board", StringComparison.OrdinalIgnoreCase))
                return renderer;

            float size = renderer.bounds.size.magnitude;
            if (size > fallbackSize)
            {
                fallback = renderer;
                fallbackSize = size;
            }
        }

        return fallback;
    }

    private bool IsWiringOverviewRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.gameObject.activeInHierarchy || IsUnderRuntimeHmi(renderer.transform))
            return false;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0 && renderer.gameObject.layer == uiLayer)
            return false;

        string path = GetHierarchyPath(renderer.transform);
        if (ContainsIgnoreCase(path, "Runtime_")
            || ContainsIgnoreCase(path, "PIP_Camera")
            || ContainsIgnoreCase(path, "PipPanel")
            || ContainsIgnoreCase(path, "WireHeads_Storage"))
            return false;

        Bounds rendererBounds = renderer.bounds;
        if (rendererBounds.size.sqrMagnitude <= 0.0001f)
            return false;

        return true;
    }

    private string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "";

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source)
            && !string.IsNullOrEmpty(value)
            && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsUnderRuntimeHmi(Transform candidate)
    {
        return canvasHmiRoot != null && candidate != null && candidate.IsChildOf(canvasHmiRoot.transform);
    }

    private void EncapsulateBounds(ref Bounds bounds, ref bool hasBounds, Bounds value)
    {
        if (!hasBounds)
        {
            bounds = value;
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(value);
        }
    }

    private Bounds CalculateTargetBounds(Transform target, bool includeInactive)
    {
        Bounds bounds = new Bounds(target.position, Vector3.zero);
        bool hasBounds = false;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(includeInactive);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            if (!includeInactive && !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        RectTransform[] rectTransforms = target.GetComponentsInChildren<RectTransform>(includeInactive);
        Vector3[] corners = new Vector3[4];
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform == null)
                continue;
            if (!includeInactive && !rectTransform.gameObject.activeInHierarchy)
                continue;

            rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
                EncapsulatePoint(ref bounds, ref hasBounds, corners[i]);
        }

        if (!hasBounds && !includeInactive)
            return CalculateTargetBounds(target, true);

        if (!hasBounds)
            bounds = new Bounds(target.position, Vector3.one * 0.1f);

        if (bounds.extents.magnitude < 0.02f)
            bounds.Expand(0.08f);

        return bounds;
    }

    private void EncapsulatePoint(ref Bounds bounds, ref bool hasBounds, Vector3 point)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.zero);
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(point);
        }
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

    private void NormalizeGatewayUrls()
    {
        if (string.IsNullOrWhiteSpace(piBaseUrl) || IsLegacyGatewayUrl(piBaseUrl))
            piBaseUrl = DefaultPiBaseUrl;

        if (string.IsNullOrWhiteSpace(url) || IsLegacyGatewayUrl(url))
            url = DefaultControlUrl;

        bool hasCustomCompatibilityUrl =
            !string.IsNullOrWhiteSpace(url)
            && !url.Equals(DefaultControlUrl, StringComparison.OrdinalIgnoreCase)
            && url.EndsWith(controlEndpoint, StringComparison.OrdinalIgnoreCase);

        if (hasCustomCompatibilityUrl)
            piBaseUrl = url.Substring(0, url.Length - controlEndpoint.Length);
    }

    private static bool IsLegacyGatewayUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.IndexOf("10.38.100.27", StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf("10.38.100.214", StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf("192.168.137.67", StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf(
                "unacquiescent-quiana-excepable.ngrok-free.dev",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    private void CreateCanvasHmi()
    {
        if (canvasHmiRoot != null)
            return;

        bool usesSceneHmi = hmiScreenObject != null;
        canvasHmiRoot = usesSceneHmi
            ? hmiScreenObject.transform.parent.gameObject
            : new GameObject("Runtime_Pi_HMI_Canvas");

        Canvas canvas = canvasHmiRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasHmiRoot.AddComponent<Canvas>();
        if (!usesSceneHmi)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasHmiRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasHmiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        if (canvasHmiRoot.GetComponent<GraphicRaycaster>() == null)
            canvasHmiRoot.AddComponent<GraphicRaycaster>();

        GameObject panel = hmiScreenObject != null
            ? hmiScreenObject
            : new GameObject("HMI_Screen");
        if (panel.transform.parent != canvasHmiRoot.transform)
            panel.transform.SetParent(canvasHmiRoot.transform, false);

        Vector2 panelSize = new Vector2(590f, 516f);
        canvasHmiPanelRect = panel.GetComponent<RectTransform>();
        if (canvasHmiPanelRect == null)
            canvasHmiPanelRect = panel.AddComponent<RectTransform>();

        RectTransform rootRect = canvasHmiRoot.GetComponent<RectTransform>();
        bool screenSpaceHmi = canvas.renderMode != RenderMode.WorldSpace;
        if (rootRect != null && screenSpaceHmi)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            canvasHmiRoot.transform.localScale = Vector3.one;
        }
        else if (rootRect != null)
        {
            rootRect.sizeDelta = panelSize;
        }

        if (screenSpaceHmi)
        {
            canvasHmiPanelRect.anchorMin = Vector2.one;
            canvasHmiPanelRect.anchorMax = Vector2.one;
            canvasHmiPanelRect.pivot = Vector2.one;
            canvasHmiPanelRect.anchoredPosition = new Vector2(-30f, -82f);
            panel.transform.localScale = Vector3.one * 0.85f;
        }
        else
        {
            canvasHmiPanelRect.anchorMin = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchorMax = new Vector2(0f, 1f);
            canvasHmiPanelRect.pivot = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchoredPosition = usesSceneHmi
                ? Vector2.zero
                : canvasHmiAnchoredPosition;
            panel.transform.localScale = usesSceneHmi
                ? Vector3.one
                : Vector3.one * canvasHmiScale;
        }
        canvasHmiPanelRect.sizeDelta = panelSize;

        Color ink = new Color(0.07f, 0.075f, 0.085f, 1f);
        Color brandRed = new Color(0.76f, 0.055f, 0.075f, 1f);
        Color paleRed = new Color(1f, 0.945f, 0.95f, 1f);
        Color borderRed = new Color(0.86f, 0.16f, 0.18f, 1f);
        Color borderGold = new Color(0.72f, 0.56f, 0.02f, 1f);
        Color greenButton = new Color(0.15f, 0.72f, 0.015f, 1f);
        Color stopRed = new Color(0.75f, 0.055f, 0.075f, 1f);
        Color goldButton = new Color(0.73f, 0.59f, 0.01f, 1f);

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
            panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.98f);
        ApplyRoundedImage(panelImage);
        AddShadow(panel, new Color(0.04f, 0.09f, 0.16f, 0.22f), new Vector2(0f, -8f));

        Transform iconBadge = MakeSubPanel(
            panel.transform,
            "ControllerIconBadge",
            new Vector2(28f, -22f),
            new Vector2(56f, 56f),
            paleRed);
        CreateControllerDocumentIcon(iconBadge, brandRed);

        TextMeshProUGUI dashboardTitle = CreateText(
            panel.transform,
            "DashboardTitle",
            "B\u1ED9 \u0111i\u1EC1u khi\u1EC3n",
            new Vector2(96f, -24f),
            new Vector2(440f, 52f),
            30,
            true);
        dashboardTitle.color = ink;

        CreateDecorativeImage(
            panel.transform,
            "HeaderDivider",
            new Vector2(28f, -92f),
            new Vector2(534f, 2f),
            new Color(0.87f, 0.88f, 0.90f, 1f));

        Transform setupCard = MakeOutlinedSubPanel(
            panel.transform,
            "SetupCard",
            new Vector2(36f, -106f),
            new Vector2(365f, 392f),
            Color.white,
            borderRed);
        Transform controlCard = MakeOutlinedSubPanel(
            panel.transform,
            "ControlCard",
            new Vector2(416f, -106f),
            new Vector2(158f, 392f),
            Color.white,
            borderGold);

        TextMeshProUGUI setupTitle = CreateText(
            setupCard, "SetupTitle", "Thi\u1EBFt l\u1EADp",
            new Vector2(24f, -18f), new Vector2(220f, 32f), 23, true);
        setupTitle.color = ink;
        TextMeshProUGUI controlTitle = CreateText(
            controlCard, "ControlTitle", "\u0110i\u1EC1u khi\u1EC3n",
            new Vector2(18f, -18f), new Vector2(128f, 32f), 21, true);
        controlTitle.color = ink;

        CreateText(
            setupCard, "RotationLabel", "\u0110\u1EB7t v\u1ECB tr\u00ED (v\u00F2ng)",
            new Vector2(24f, -64f), new Vector2(310f, 32f), 21, true);
        hmiRotInput = CreateInputField(
            setupCard, "RotInput", "0",
            new Vector2(24f, -104f), new Vector2(230f, 50f), 22);
        CreateButton(
            setupCard, "SetRot", "SET",
            new Vector2(264f, -104f), new Vector2(77f, 50f), brandRed)
            .onClick.AddListener(() =>
            { if (float.TryParse(hmiRotInput.text, out float value)) SetTargetRotations(value); });

        CreateText(
            setupCard, "AngleLabel", "\u0110\u1EB7t v\u1ECB tr\u00ED (\u0111\u1ED9)",
            new Vector2(24f, -170f), new Vector2(310f, 32f), 21, true);
        hmiAngleInput = CreateInputField(
            setupCard, "AngleInput", "0",
            new Vector2(24f, -210f), new Vector2(230f, 50f), 22);
        CreateButton(
            setupCard, "SetAngle", "SET",
            new Vector2(264f, -210f), new Vector2(77f, 50f), brandRed)
            .onClick.AddListener(() =>
            { if (float.TryParse(hmiAngleInput.text, out float value)) SetTargetAngle(value); });

        CreateText(
            setupCard, "SpeedLabel", "\u0110\u1EB7t t\u1ED1c \u0111\u1ED9 (v\u00F2ng / ph\u00FAt)",
            new Vector2(24f, -276f), new Vector2(320f, 34f), 21, true);
        Button minusButton = CreateButton(
            setupCard, "Minus", "\u2212",
            new Vector2(24f, -320f), new Vector2(48f, 50f), Color.white);
        minusButton.onClick.AddListener(() => SpeedDown(1));
        SetButtonTextColor(minusButton, ink);
        AddBorder(minusButton.gameObject, new Color(0.82f, 0.84f, 0.87f, 1f), 1f);

        hmiSpeedInput = CreateInputField(
            setupCard, "SpeedInput", "0",
            new Vector2(80f, -320f), new Vector2(110f, 50f), 22);
        Button plusButton = CreateButton(
            setupCard, "Plus", "+",
            new Vector2(198f, -320f), new Vector2(48f, 50f), Color.white);
        plusButton.onClick.AddListener(() => SpeedUp(1));
        SetButtonTextColor(plusButton, ink);
        AddBorder(plusButton.gameObject, new Color(0.82f, 0.84f, 0.87f, 1f), 1f);
        CreateButton(
            setupCard, "SetSpeed", "SET",
            new Vector2(264f, -320f), new Vector2(77f, 50f), brandRed)
            .onClick.AddListener(() =>
            { if (float.TryParse(hmiSpeedInput.text, out float value)) SetSpeed(value); });

        hmiForwardButton = CreateButton(
            controlCard, "Fwd", "Thu\u1EADn",
            new Vector2(14f, -58f), new Vector2(130f, 44f), Color.white);
        hmiForwardButton.onClick.AddListener(SetDirectionForward);
        SetButtonTextColor(hmiForwardButton, ink);
        AddBorder(hmiForwardButton.gameObject, new Color(0.84f, 0.85f, 0.87f, 1f), 1f);

        hmiReverseButton = CreateButton(
            controlCard, "Rev", "Ng\u01B0\u1EE3c",
            new Vector2(14f, -108f), new Vector2(130f, 44f), Color.white);
        hmiReverseButton.onClick.AddListener(SetDirectionReverse);
        SetButtonTextColor(hmiReverseButton, ink);
        AddBorder(hmiReverseButton.gameObject, new Color(0.84f, 0.85f, 0.87f, 1f), 1f);

        CreateButton(
            controlCard, "Start", "START",
            new Vector2(14f, -158f), new Vector2(130f, 44f), greenButton)
            .onClick.AddListener(TurnOn);
        CreateButton(
            controlCard, "Stop", "STOP",
            new Vector2(14f, -208f), new Vector2(130f, 44f), stopRed)
            .onClick.AddListener(TurnOff);
        CreateButton(
            controlCard, "RstRight", "RST",
            new Vector2(14f, -258f), new Vector2(130f, 44f), goldButton)
            .onClick.AddListener(ResetHmiInputsAndPlc);
        Button resetDataButton = CreateButton(
            controlCard, "ResetData", "RESET\nD\u1EEE LI\u1EC6U",
            new Vector2(14f, -308f), new Vector2(130f, 60f), brandRed);
        resetDataButton.onClick.AddListener(ResetHmiData);
        TextMeshProUGUI resetDataLabel =
            resetDataButton.GetComponentInChildren<TextMeshProUGUI>();
        if (resetDataLabel != null)
        {
            resetDataLabel.fontSize = 17f;
            resetDataLabel.fontWeight = FontWeight.Black;
            resetDataLabel.fontStyle = FontStyles.Bold;
            resetDataLabel.lineSpacing = -8f;
        }

        Transform statusCard = MakeSubPanel(
            panel.transform,
            "StatusCard",
            new Vector2(218f, -530f),
            new Vector2(372f, 214f),
            Color.white);
        Transform statusPill = MakeSubPanel(
            statusCard,
            "StatusPill",
            new Vector2(22f, -20f),
            new Vector2(328f, 48f),
            paleRed);
        hmiStatusText = CreateText(
            statusPill, "RunStatusLabel", "Tr\u1EA1ng th\u00E1i:",
            new Vector2(22f, 0f), new Vector2(150f, 48f), 20, true);
        hmiStatusText.color = ink;
        hmiStatusValueText = CreateText(
            statusPill, "RunStatusValue", "M\u1EA5t k\u1EBFt n\u1ED1i",
            new Vector2(166f, 0f), new Vector2(140f, 48f), 19, true);
        hmiStatusValueText.alignment = TextAlignmentOptions.Center;
        hmiStatusValueText.color = new Color(0.88f, 0.12f, 0.12f, 1f);

        hmiAngleText = CreateText(
            statusCard, "St1", "V\u1ECB tr\u00ED (\u0111\u1ED9): <pos=90%>0",
            new Vector2(24f, -82f), new Vector2(324f, 30f), 20, true);
        hmiRotText = CreateText(
            statusCard, "St2", "\u0110\u00E3 quay: <pos=90%>0",
            new Vector2(24f, -122f), new Vector2(324f, 30f), 20, true);
        hmiSpeedText = CreateText(
            statusCard, "St3", "T\u1ED1c \u0111\u1ED9 RPM: <pos=90%>0",
            new Vector2(24f, -162f), new Vector2(324f, 30f), 20, true);

        ResetHmiInputFields();
        UpdateDirectionButtons();
        canvasHmiRoot.SetActive(runtimeHmiVisible);
        UpdateCanvasHmi();
    }

    private void CreateCanvasHmiInstitutional()
    {
        if (canvasHmiRoot != null)
            return;

        bool usesSceneHmi = hmiScreenObject != null;
        canvasHmiRoot = usesSceneHmi ? hmiScreenObject.transform.parent.gameObject : new GameObject("Runtime_Pi_HMI_Canvas");
        Canvas canvas = canvasHmiRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasHmiRoot.AddComponent<Canvas>();
        if (!usesSceneHmi)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasHmiRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasHmiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        if (canvasHmiRoot.GetComponent<GraphicRaycaster>() == null)
            canvasHmiRoot.AddComponent<GraphicRaycaster>();

        Color screenBg = new Color(0.92f, 0.99f, 0.98f, 1f);
        Color card = new Color(0.985f, 1f, 1f, 0.98f);
        Color statusBg = new Color(0.94f, 1f, 0.98f, 1f);
        Color titleColor = new Color(0.04f, 0.28f, 0.28f, 1f);
        Color blueBtn = new Color(0.09f, 0.55f, 0.95f, 1f);
        Color greenBtn = new Color(0.15f, 0.78f, 0.27f, 1f);
        Color redBtn = new Color(0.95f, 0.18f, 0.18f, 1f);
        Color orangeBtn = new Color(1f, 0.47f, 0.05f, 1f);

        GameObject panel = hmiScreenObject != null ? hmiScreenObject : new GameObject("HMI_Screen");
        if (panel.transform.parent != canvasHmiRoot.transform)
            panel.transform.SetParent(canvasHmiRoot.transform, false);

        canvasHmiPanelRect = panel.GetComponent<RectTransform>();
        if (canvasHmiPanelRect == null)
            canvasHmiPanelRect = panel.AddComponent<RectTransform>();
        Vector2 modernHmiSize = new Vector2(620f, 480f);
        RectTransform rootRect = canvasHmiRoot.GetComponent<RectTransform>();
        bool screenSpaceHmi = canvas.renderMode != RenderMode.WorldSpace;
        if (rootRect != null)
        {
            if (screenSpaceHmi)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                canvasHmiRoot.transform.localScale = Vector3.one;
            }
            else
            {
                rootRect.sizeDelta = modernHmiSize;
            }
        }
        if (screenSpaceHmi)
        {
            canvasHmiPanelRect.anchorMin = new Vector2(1f, 1f);
            canvasHmiPanelRect.anchorMax = new Vector2(1f, 1f);
            canvasHmiPanelRect.pivot = new Vector2(1f, 1f);
            canvasHmiPanelRect.anchoredPosition = new Vector2(-32f, -150f);
        }
        else
        {
            canvasHmiPanelRect.anchorMin = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchorMax = new Vector2(0f, 1f);
            canvasHmiPanelRect.pivot = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchoredPosition = usesSceneHmi ? Vector2.zero : canvasHmiAnchoredPosition;
        }
        canvasHmiPanelRect.sizeDelta = modernHmiSize;
        if (screenSpaceHmi)
            panel.transform.localScale = Vector3.one * 0.85f;
        else if (!usesSceneHmi)
            panel.transform.localScale = Vector3.one * canvasHmiScale;
        else
            panel.transform.localScale = Vector3.one;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
            panelImage = panel.AddComponent<Image>();
        panelImage.color = screenBg;
        AddShadow(panel, new Color(0.02f, 0.16f, 0.16f, 0.18f), new Vector2(0f, -6f));

        CreateInstitutionHeader(panel.transform, modernHmiSize, redBtn);

        TextMeshProUGUI title = CreateText(panel.transform, "HmiTitle", "GIAO DIỆN ĐIỀU KHIỂN", new Vector2(0f, -66f), new Vector2(620f, 32f), 22, true);
        title.alignment = TextAlignmentOptions.Center;
        title.color = titleColor;

        Transform gp = MakeSubPanel(panel.transform, "SetupCard", new Vector2(14f, -104f), new Vector2(374f, 342f), card);
        Transform rp = MakeSubPanel(panel.transform, "ControlCard", new Vector2(402f, -104f), new Vector2(204f, 342f), card);
        Transform statusPanel = MakeSubPanel(gp, "StatusPanel", new Vector2(14f, -244f), new Vector2(346f, 88f), statusBg);

        TextMeshProUGUI setupTitle = CreateText(gp, "SetupTitle", "Thiết lập", new Vector2(16f, -10f), new Vector2(140f, 26f), 19, true);
        setupTitle.color = titleColor;
        TextMeshProUGUI controlTitle = CreateText(rp, "ControlTitle", "Điều khiển", new Vector2(16f, -10f), new Vector2(170f, 26f), 19, true);
        controlTitle.color = titleColor;

        CreateText(gp, "L1", "Đặt vị trí", new Vector2(18f, -48f), new Vector2(94f, 26f), 14, true);
        CreateText(gp, "U1", "Vòng", new Vector2(118f, -48f), new Vector2(54f, 26f), 10, false);
        hmiRotInput = CreateInputField(gp, "RotInput", "0", new Vector2(186f, -45f), new Vector2(94f, 32f), 16);
        CreateButton(gp, "SetRot", "SET", new Vector2(294f, -45f), new Vector2(62f, 32f), blueBtn).onClick.AddListener(() =>
        { if (float.TryParse(hmiRotInput.text, out float v)) SetTargetRotations(v); });

        CreateText(gp, "L2", "Đặt vị trí", new Vector2(18f, -88f), new Vector2(94f, 26f), 14, true);
        CreateText(gp, "U2", "Độ", new Vector2(118f, -88f), new Vector2(54f, 26f), 10, false);
        hmiAngleInput = CreateInputField(gp, "AngleInput", "0", new Vector2(186f, -85f), new Vector2(94f, 32f), 16);
        CreateButton(gp, "SetAngle", "SET", new Vector2(294f, -85f), new Vector2(62f, 32f), blueBtn).onClick.AddListener(() =>
        { if (float.TryParse(hmiAngleInput.text, out float v)) SetTargetAngle(v); });

        CreateText(gp, "L3", "Đặt tốc độ:", new Vector2(18f, -128f), new Vector2(94f, 26f), 14, true);
        CreateText(gp, "U3", "Vòng/phút", new Vector2(118f, -128f), new Vector2(62f, 26f), 10, false);
        hmiSpeedInput = CreateInputField(gp, "SpeedInput", "0", new Vector2(186f, -125f), new Vector2(94f, 32f), 16);
        CreateButton(gp, "SetSpeed", "SET", new Vector2(294f, -125f), new Vector2(62f, 32f), blueBtn).onClick.AddListener(() =>
        { if (float.TryParse(hmiSpeedInput.text, out float v)) SetSpeed(v); });

        CreateButton(gp, "Plus", "+", new Vector2(94f, -168f), new Vector2(100f, 36f), blueBtn).onClick.AddListener(() => SpeedUp(1));
        CreateButton(gp, "Minus", "-", new Vector2(208f, -168f), new Vector2(100f, 36f), redBtn).onClick.AddListener(() => SpeedDown(1));
        CreateButton(gp, "RstStatus", "RST", new Vector2(128f, -212f), new Vector2(122f, 38f), redBtn).onClick.AddListener(() => SendControl("RESET_COUNTER"));

        hmiStatusText = CreateText(statusPanel, "RunStatus", "Trạng thái: Sẵn sàng", new Vector2(14f, -6f), new Vector2(318f, 20f), 15, true);
        hmiAngleText = CreateText(statusPanel, "St1", "Vị trí (độ): 0", new Vector2(14f, -28f), new Vector2(318f, 18f), 14, false);
        hmiRotText = CreateText(statusPanel, "St2", "Đã quay: 0", new Vector2(14f, -49f), new Vector2(318f, 18f), 14, false);
        hmiSpeedText = CreateText(statusPanel, "St3", "Tốc độ RPM: 0", new Vector2(14f, -70f), new Vector2(318f, 18f), 14, false);

        hmiForwardButton = CreateButton(rp, "Fwd", "Thuận", new Vector2(16f, -48f), new Vector2(172f, 42f), blueBtn);
        hmiForwardButton.onClick.AddListener(SetDirectionForward);
        hmiReverseButton = CreateButton(rp, "Rev", "Ngược", new Vector2(16f, -101f), new Vector2(172f, 42f), blueBtn);
        hmiReverseButton.onClick.AddListener(SetDirectionReverse);
        CreateButton(rp, "Start", "START", new Vector2(16f, -154f), new Vector2(172f, 42f), greenBtn).onClick.AddListener(TurnOn);
        CreateButton(rp, "Stop", "STOP", new Vector2(16f, -207f), new Vector2(172f, 42f), redBtn).onClick.AddListener(TurnOff);
        CreateButton(rp, "RstRight", "RST", new Vector2(16f, -260f), new Vector2(172f, 42f), orangeBtn).onClick.AddListener(ResetHmiInputsAndPlc);

        if (showWireLabels)
        {
            CreateWireLabel(canvasHmiRoot.transform, "WireLabelYellow", "Dây Vàng: Y0-Pin11", new Color(1f, 0.78f, 0f), wireLabelsCenter);
            CreateWireLabel(canvasHmiRoot.transform, "WireLabelRed", "Dây Đỏ: X0-0B", new Color(0.86f, 0.12f, 0.12f), wireLabelsCenter + new Vector2(0f, -34f));
        }

        ResetHmiInputFields();
        UpdateDirectionButtons();
        canvasHmiRoot.SetActive(runtimeHmiVisible);
        UpdateCanvasHmi();
    }

    private void CreateCanvasHmiLegacy()
    {
        if (canvasHmiRoot != null)
            return;

        bool usesSceneHmi = hmiScreenObject != null;
        canvasHmiRoot = usesSceneHmi ? hmiScreenObject.transform.parent.gameObject : new GameObject("Runtime_Pi_HMI_Canvas");
        Canvas canvas = canvasHmiRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasHmiRoot.AddComponent<Canvas>();
        if (!usesSceneHmi)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasHmiRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasHmiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        if (canvasHmiRoot.GetComponent<GraphicRaycaster>() == null)
            canvasHmiRoot.AddComponent<GraphicRaycaster>();

        Color green = new Color(0.18f, 0.55f, 0.20f, 1f);
        Color red = new Color(0.72f, 0.12f, 0.12f, 1f);
        Color blueBtn = new Color(0.16f, 0.34f, 0.72f, 1f);
        Color redBtn = new Color(0.82f, 0.14f, 0.14f, 1f);

        GameObject panel = hmiScreenObject != null ? hmiScreenObject : new GameObject("HMI_Screen");
        if (panel.transform.parent != canvasHmiRoot.transform)
            panel.transform.SetParent(canvasHmiRoot.transform, false);
        canvasHmiPanelRect = panel.GetComponent<RectTransform>();
        if (canvasHmiPanelRect == null)
            canvasHmiPanelRect = panel.AddComponent<RectTransform>();
        if (!usesSceneHmi)
        {
            canvasHmiPanelRect.anchorMin = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchorMax = new Vector2(0f, 1f);
            canvasHmiPanelRect.pivot = new Vector2(0f, 1f);
            canvasHmiPanelRect.anchoredPosition = canvasHmiAnchoredPosition;
            canvasHmiPanelRect.sizeDelta = new Vector2(600f, 300f);
            panel.transform.localScale = Vector3.one * canvasHmiScale;
        }
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
            panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.25f);

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
        CreateButton(gp, "Plus", "+", new Vector2(166f, -124f), new Vector2(56f, 30f), blueBtn).onClick.AddListener(() => SpeedUp(1));
        CreateButton(gp, "Minus", "-", new Vector2(228f, -124f), new Vector2(56f, 30f), redBtn).onClick.AddListener(() => SpeedDown(1));

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

    private void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.AddComponent<Shadow>();

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
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
        Image image = go.AddComponent<Image>();
        image.color = color;
        ApplyRoundedImage(image);
        go.AddComponent<RectMask2D>();
        AddShadow(go, new Color(0.02f, 0.16f, 0.16f, 0.12f), new Vector2(0f, -4f));
        return go.transform;
    }

    private Transform MakeOutlinedSubPanel(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color fill,
        Color border)
    {
        Transform panel = MakeSubPanel(parent, name, position, size, fill);
        AddBorder(panel.gameObject, border, 1f);
        return panel;
    }

    private Image CreateDecorativeImage(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void CreateControllerDocumentIcon(Transform parent, Color color)
    {
        CreateDecorativeImage(parent, "DocumentLeft", new Vector2(15f, -10f), new Vector2(2f, 36f), color);
        CreateDecorativeImage(parent, "DocumentTop", new Vector2(15f, -10f), new Vector2(24f, 2f), color);
        CreateDecorativeImage(parent, "DocumentRight", new Vector2(37f, -10f), new Vector2(2f, 36f), color);
        CreateDecorativeImage(parent, "DocumentBottom", new Vector2(15f, -44f), new Vector2(24f, 2f), color);
        CreateDecorativeImage(parent, "DocumentLine1", new Vector2(21f, -20f), new Vector2(12f, 2f), color);
        CreateDecorativeImage(parent, "DocumentLine2", new Vector2(21f, -27f), new Vector2(12f, 2f), color);
        CreateDecorativeImage(parent, "DocumentLine3", new Vector2(21f, -34f), new Vector2(9f, 2f), color);
    }

    private void ApplyRoundedImage(Image image)
    {
        if (image == null)
            return;

        Sprite rounded = Resources.Load<Sprite>("UI/RoundedRect");
        if (rounded == null)
            return;

        image.sprite = rounded;
        image.type = Image.Type.Sliced;
    }

    private void AddBorder(GameObject target, Color color, float width)
    {
        if (target == null)
            return;

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, -width);
        outline.useGraphicAlpha = true;
    }

    private void SetButtonTextColor(Button button, Color color)
    {
        if (button == null)
            return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.color = color;
    }

    private void StretchText(
        TextMeshProUGUI text,
        TextAlignmentOptions alignment,
        Color color)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.alignment = alignment;
        text.color = color;
    }

    private void CreateInstitutionHeader(Transform parent, Vector2 panelSize, Color brandRed)
    {
        GameObject logoBox = new GameObject("InstitutionLogo");
        logoBox.transform.SetParent(parent, false);
        RectTransform logoRect = logoBox.AddComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0f, 1f);
        logoRect.anchorMax = new Vector2(0f, 1f);
        logoRect.pivot = new Vector2(0f, 1f);
        logoRect.anchoredPosition = new Vector2(14f, -7f);
        logoRect.sizeDelta = new Vector2(58f, 58f);
        Image logoImage = logoBox.AddComponent<Image>();
        logoImage.color = Color.white;
        logoImage.raycastTarget = false;
        logoImage.preserveAspect = true;
        if (institutionLogo != null)
            logoImage.sprite = institutionLogo;
        AddShadow(logoBox, new Color(0f, 0f, 0f, 0.16f), new Vector2(0f, -3f));

        if (institutionLogo == null)
        {
            TextMeshProUGUI fallbackLogo = CreateText(logoBox.transform, "FallbackLogoText", "PTIT", Vector2.zero, new Vector2(58f, 58f), 19, true);
            RectTransform fallbackRect = fallbackLogo.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
            fallbackLogo.alignment = TextAlignmentOptions.Center;
            fallbackLogo.color = brandRed;
        }

        GameObject banner = new GameObject("InstitutionNameBanner");
        banner.transform.SetParent(parent, false);
        RectTransform bannerRect = banner.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0f, 1f);
        bannerRect.anchorMax = new Vector2(0f, 1f);
        bannerRect.pivot = new Vector2(0f, 1f);
        bannerRect.anchoredPosition = new Vector2(70f, -14f);
        bannerRect.sizeDelta = new Vector2(panelSize.x - 84f, 44f);
        Image bannerImage = banner.AddComponent<Image>();
        bannerImage.color = brandRed;
        bannerImage.raycastTarget = false;
        AddShadow(banner, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));

        TextMeshProUGUI nameText = CreateText(banner.transform, "InstitutionNameText", institutionName, new Vector2(18f, 0f), new Vector2(panelSize.x - 124f, 44f), 22, true);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
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
        Image inputBackground = go.AddComponent<Image>();
        inputBackground.color = new Color(0.91f, 0.94f, 0.99f, 1f);
        ApplyRoundedImage(inputBackground);
        AddShadow(go, new Color(0.02f, 0.16f, 0.16f, 0.10f), new Vector2(0f, -2f));

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
        t.alignment = TextAlignmentOptions.Center;

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
        ApplyRoundedImage(image);
        AddShadow(go, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = color * 1.15f;
        colors.pressedColor = color * 0.85f;
        colors.selectedColor = color;
        button.colors = colors;

        int buttonFontSize = Mathf.RoundToInt(Mathf.Clamp(size.y * 0.48f, 20f, 38f));
        TextMeshProUGUI text = CreateText(go.transform, "Text", label, Vector2.zero, size, buttonFontSize, true);
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

        const string feedbackValueColor = "#14963A";
        if (hmiAngleText != null)
            hmiAngleText.text = $"V\u1ECB tr\u00ED (\u0111\u1ED9): <pos=88%><color={feedbackValueColor}>{LatestTelemetry.angle:F0}</color>";
        if (hmiRotText != null)
            hmiRotText.text = $"\u0110\u00E3 quay: <pos=88%><color={feedbackValueColor}>{LatestTelemetry.rotations:F0}</color>";
        if (hmiSpeedSetText != null)
            hmiSpeedSetText.text = hmiTargetSpeed.ToString("F0");
        if (hmiSpeedText != null)
            hmiSpeedText.text = $"T\u1ED1c \u0111\u1ED9 RPM: <pos=88%><color={feedbackValueColor}>{GetDisplayRpm():F0}</color>";

        string stateText;
        Color stateColor;
        if (!IsPiOnline)
        {
            stateText = "M\u1EA5t k\u1EBFt n\u1ED1i";
            stateColor = new Color(0.9f, 0.2f, 0.16f, 1f);
        }
        else if (LatestTelemetry.running)
        {
            stateText = "\u0110ang ch\u1EA1y";
            stateColor = new Color(0.05f, 0.38f, 0.9f, 1f);
        }
        else
        {
            stateText = "\u0110\u00E3 k\u1EBFt n\u1ED1i";
            stateColor = new Color(0.08f, 0.62f, 0.2f, 1f);
        }

        if (hmiStatusValueText != null)
        {
            hmiStatusValueText.text = stateText;
            hmiStatusValueText.color = stateColor;
            if (hmiStatusText != null)
                hmiStatusText.color = new Color(0.07f, 0.075f, 0.085f, 1f);
        }
        else if (hmiStatusText != null)
        {
            hmiStatusText.text = $"Tr\u1EA1ng th\u00E1i: {stateText}";
            hmiStatusText.color = stateColor;
        }
    }

    private void UpdateCanvasHmiInstitutional()
    {
        if (canvasHmiRoot == null)
            return;

        const string feedbackValueColor = "#14963A";
        if (hmiAngleText != null) hmiAngleText.text = $"Vị trí (độ): <color={feedbackValueColor}>{LatestTelemetry.angle:F0}</color>";
        if (hmiRotText != null) hmiRotText.text = $"Đã quay: <color={feedbackValueColor}>{LatestTelemetry.rotations:F0}</color>";
        if (hmiSpeedSetText != null) hmiSpeedSetText.text = hmiTargetSpeed.ToString("F0");
        if (hmiSpeedText != null) hmiSpeedText.text = $"Tốc độ RPM: <color={feedbackValueColor}>{GetDisplayRpm():F0}</color>";
        if (hmiStatusText != null)
        {
            string stateText;
            Color stateColor;
            if (!IsPiOnline)
            {
                stateText = "Mất kết nối";
                stateColor = new Color(0.9f, 0.2f, 0.16f, 1f);
            }
            else if (LatestTelemetry.running)
            {
                stateText = "Đang chạy";
                stateColor = new Color(0.05f, 0.38f, 0.9f, 1f);
            }
            else
            {
                stateText = "Đã kết nối";
                stateColor = new Color(0.08f, 0.62f, 0.2f, 1f);
            }

            hmiStatusText.text = $"Trạng thái: {stateText}";
            hmiStatusText.color = stateColor;
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
        GUILayout.Label($"Set speed: {LatestTelemetry.setSpeedRpm:F0} RPM");
        GUILayout.Label($"Actual speed: {Mathf.Abs(LatestTelemetry.speedRpm):F0} RPM");
        GUILayout.Label($"Count: {LatestTelemetry.count}");
        GUILayout.Label($"Rotations: {LatestTelemetry.rotations:F0}");
        GUILayout.Label($"Angle: {LatestTelemetry.angle:F1}");
        GUILayout.Label($"Mode: {LatestTelemetry.motionMode}");
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
