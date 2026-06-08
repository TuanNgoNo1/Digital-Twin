using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

/// <summary>
/// PLCController_v2 - Giao tiep PLC qua Pi Gateway (HTTP)
/// Dung cho WebGL build. Poll telemetry tu Pi moi 0.5s.
/// 
/// API:
///   POST /control  -> gui lenh (ON, OFF, SET_SPEED, SET_DIRECTION, SET_ROTATIONS, SET_ANGLE, RESET, ERR_RESET)
///   GET  /telemetry -> doc trang thai motor/PLC
/// </summary>
public class PLCController_v2 : MonoBehaviour
{
    // ============================
    // CAU HINH
    // ============================
    [Header("Cau hinh ket noi Pi Gateway")]
    public string piBaseUrl = "http://10.38.100.27:5000";

    [Header("Polling")]
    public bool pollTelemetryOnStart = true;
    public float pollInterval = 0.5f;

    // ============================
    // DU LIEU TELEMETRY (PUBLIC - DOC TU BEN NGOAI)
    // ============================
    [Header("Telemetry Data (Read-Only)")]
    public bool IsRunning { get; private set; }
    public int SpeedRpm { get; private set; }
    public int PulseCount { get; private set; }
    public int Rotations { get; private set; }
    public int Angle { get; private set; }
    public string Direction { get; private set; } = "forward";
    public string LastAction { get; private set; } = "";
    public bool BackendSynced { get; private set; }
    public string BackendStatus { get; private set; } = "";
    public bool IsConnected { get; private set; }

    // ============================
    // EVENTS
    // ============================
    /// <summary>
    /// Duoc goi moi khi telemetry cap nhat thanh cong.
    /// </summary>
    public event Action OnTelemetryUpdated;

    /// <summary>
    /// Duoc goi khi ket noi that bai / phuc hoi.
    /// </summary>
    public event Action<bool> OnConnectionChanged;

    // ============================
    // PRIVATE
    // ============================
    private Coroutine pollCoroutine;
    private bool wasConnected = false;

    // ============================
    // UNITY LIFECYCLE
    // ============================
    void Start()
    {
        if (pollTelemetryOnStart)
        {
            StartPolling();
        }
    }

    void OnDisable()
    {
        StopPolling();
    }

    // ============================
    // POLLING CONTROL
    // ============================
    public void StartPolling()
    {
        if (pollCoroutine != null) return;
        pollCoroutine = StartCoroutine(PollTelemetryRoutine());
        Debug.Log("<color=cyan>[PLC_v2] Bat dau poll telemetry</color>");
    }

    public void StopPolling()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }
    }

    private IEnumerator PollTelemetryRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(GetTelemetry());
            yield return new WaitForSeconds(pollInterval);
        }
    }

    // ============================
    // GET TELEMETRY
    // ============================
    private IEnumerator GetTelemetry()
    {
        string url = piBaseUrl + "/telemetry";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (IsConnected || !wasConnected)
                {
                    Debug.LogWarning("[PLC_v2] Telemetry error: " + request.error);
                }
                SetConnected(false);
            }
            else
            {
                SetConnected(true);
                ParseTelemetry(request.downloadHandler.text);
            }
        }
    }

    private void ParseTelemetry(string json)
    {
        try
        {
            TelemetryResponse data = JsonUtility.FromJson<TelemetryResponse>(json);

            IsRunning = data.running;
            SpeedRpm = data.speedRpm;
            PulseCount = data.count;
            Rotations = data.rotations;
            Angle = data.angle;
            Direction = data.direction ?? "forward";
            LastAction = data.action ?? "";
            BackendSynced = data.backendSynced;
            BackendStatus = data.backendStatus ?? "";

            OnTelemetryUpdated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("[PLC_v2] Parse telemetry error: " + e.Message);
        }
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            wasConnected = true;
            OnConnectionChanged?.Invoke(connected);
            Debug.Log(connected
                ? "<color=green>[PLC_v2] Ket noi Pi Gateway OK</color>"
                : "<color=red>[PLC_v2] Mat ket noi Pi Gateway</color>");
        }
    }

    // ============================
    // DIEU KHIEN (POST /control)
    // ============================
    public void TurnOn(int speed = 0)
    {
        var cmd = new ControlCommand { action = "ON", speed = speed > 0 ? speed : SpeedRpm };
        StartCoroutine(PostControl(cmd));
    }

    public void TurnOff()
    {
        var cmd = new ControlCommand { action = "OFF" };
        StartCoroutine(PostControl(cmd));
    }

    public void SetSpeed(int speed)
    {
        var cmd = new ControlCommand { action = "SET_SPEED", speed = speed };
        StartCoroutine(PostControl(cmd));
    }

    public void SetDirection(string direction)
    {
        var cmd = new ControlCommand { action = "SET_DIRECTION", direction = direction };
        StartCoroutine(PostControl(cmd));
    }

    public void SetDirectionForward()
    {
        SetDirection("forward");
    }

    public void SetDirectionReverse()
    {
        SetDirection("reverse");
    }

    public void SetRotations(int rotations)
    {
        var cmd = new ControlCommand { action = "SET_ROTATIONS", rotations = rotations };
        StartCoroutine(PostControl(cmd));
    }

    public void SetAngle(int angle)
    {
        var cmd = new ControlCommand { action = "SET_ANGLE", angle = angle };
        StartCoroutine(PostControl(cmd));
    }

    public void ResetAll()
    {
        var cmd = new ControlCommand { action = "RESET" };
        StartCoroutine(PostControl(cmd));
    }

    public void ErrReset()
    {
        var cmd = new ControlCommand { action = "ERR_RESET" };
        StartCoroutine(PostControl(cmd));
    }

    // ============================
    // HTTP POST CORE
    // ============================
    private IEnumerator PostControl(ControlCommand cmd)
    {
        string url = piBaseUrl + "/control";
        string jsonData = JsonUtility.ToJson(cmd);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[PLC_v2] Control error: " + request.error);
            }
            else
            {
                Debug.Log("[PLC_v2] Control OK: " + cmd.action);
            }
        }
    }

    // ============================
    // DATA MODELS
    // ============================
    [Serializable]
    private class TelemetryResponse
    {
        public bool running;
        public int speedRpm;
        public int count;
        public int rotations;
        public int angle;
        public string direction;
        public string action;
        public bool backendSynced;
        public string backendStatus;
    }

    [Serializable]
    private class ControlCommand
    {
        public string action;
        public int speed;
        public string direction;
        public int rotations;
        public int angle;
    }
}
