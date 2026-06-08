using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hien thi du lieu telemetry tu PLC len man HMI dieu khien motor.
/// Gan vao GameObject chua cac TextMeshProUGUI tren Canvas HMI.
/// Tu dong lang nghe PLCController_v2.OnTelemetryUpdated.
///
/// SETUP:
/// 1. Tao Canvas HMI voi cac Text fields (TextMeshPro - Text)
/// 2. Gan script nay vao 1 GameObject (co the la chinh Canvas)
/// 3. Keo tha cac Text vao Inspector
/// 4. Dam bao scene co 1 GameObject gan PLCController_v2
/// </summary>
public class PLCTelemetryDisplay : MonoBehaviour
{
    // ============================
    // REFERENCES
    // ============================
    [Header("=== PLC Controller ===")]
    [Tooltip("Neu de trong, se tu tim PLCController_v2 trong scene")]
    public PLCController_v2 plcController;

    [Header("=== HIEN THI THONG SO ===")]
    [Tooltip("Hien thi toc do (RPM)")]
    public TextMeshProUGUI tocDoText;

    [Tooltip("Hien thi trang thai: DANG CHAY / DUNG")]
    public TextMeshProUGUI trangThaiText;

    [Tooltip("Hien thi chieu quay: Thuan / Nguoc")]
    public TextMeshProUGUI chieuQuayText;

    [Tooltip("Hien thi so xung (pulse count)")]
    public TextMeshProUGUI soXungText;

    [Tooltip("Hien thi so vong da quay")]
    public TextMeshProUGUI soVongText;

    [Tooltip("Hien thi goc quay")]
    public TextMeshProUGUI gocQuayText;

    [Header("=== TRANG THAI KET NOI ===")]
    [Tooltip("Hien thi trang thai ket noi voi Pi Gateway")]
    public TextMeshProUGUI connectionStatusText;

    [Tooltip("(Tuy chon) Image doi mau theo trang thai ket noi")]
    public Image connectionIndicator;

    [Header("=== CAU HINH HIEN THI ===")]
    public Color colorConnected = new Color(0.2f, 0.9f, 0.3f);
    public Color colorDisconnected = new Color(0.9f, 0.2f, 0.2f);
    public Color colorRunning = new Color(0.2f, 0.9f, 0.3f);
    public Color colorStopped = new Color(0.9f, 0.4f, 0.1f);
    public Color colorValueHighlight = new Color(1f, 0.92f, 0.016f); // vang

    // ============================
    // UNITY LIFECYCLE
    // ============================
    void Start()
    {
        // Tu dong tim PLCController_v2 neu chua gan
        if (plcController == null)
        {
            plcController = FindObjectOfType<PLCController_v2>();
        }

        if (plcController == null)
        {
            Debug.LogError("[TelemetryDisplay] Khong tim thay PLCController_v2 trong scene!");
            return;
        }

        // Dang ky events
        plcController.OnTelemetryUpdated += UpdateDisplay;
        plcController.OnConnectionChanged += OnConnectionChanged;

        // Hien thi trang thai ban dau
        SetInitialState();
    }

    void OnDestroy()
    {
        // Huy dang ky event khi bi destroy
        if (plcController != null)
        {
            plcController.OnTelemetryUpdated -= UpdateDisplay;
            plcController.OnConnectionChanged -= OnConnectionChanged;
        }
    }

    // ============================
    // HIEN THI
    // ============================
    private void SetInitialState()
    {
        SetText(tocDoText, "Toc do: <color=#FFFF00>---</color> RPM");
        SetText(trangThaiText, "Trang thai: <color=#FF6600>CHO KET NOI</color>");
        SetText(chieuQuayText, "Chieu quay: <color=#FFFF00>---</color>");
        SetText(soXungText, "So xung: <color=#FFFF00>---</color>");
        SetText(soVongText, "So vong: <color=#FFFF00>---</color>");
        SetText(gocQuayText, "Goc quay: <color=#FFFF00>---</color>");
        SetText(connectionStatusText, "<color=#FF3333>CHUA KET NOI</color>");

        if (connectionIndicator != null)
            connectionIndicator.color = colorDisconnected;
    }

    private void UpdateDisplay()
    {
        if (plcController == null) return;

        // Toc do (RPM)
        string speedColor = ColorToHex(colorValueHighlight);
        SetText(tocDoText, $"Toc do: <color={speedColor}>{plcController.SpeedRpm}</color> RPM");

        // Trang thai chay/dung
        if (plcController.IsRunning)
        {
            string runColor = ColorToHex(colorRunning);
            SetText(trangThaiText, $"Trang thai: <color={runColor}>DANG CHAY</color>");
        }
        else
        {
            string stopColor = ColorToHex(colorStopped);
            SetText(trangThaiText, $"Trang thai: <color={stopColor}>DUNG</color>");
        }

        // Chieu quay
        string dirText = plcController.Direction == "forward" ? "Thuan (CW)" : "Nguoc (CCW)";
        SetText(chieuQuayText, $"Chieu quay: <color={speedColor}>{dirText}</color>");

        // So xung
        SetText(soXungText, $"So xung: <color={speedColor}>{plcController.PulseCount}</color>");

        // So vong
        SetText(soVongText, $"So vong: <color={speedColor}>{plcController.Rotations}</color>");

        // Goc quay
        SetText(gocQuayText, $"Goc quay: <color={speedColor}>{plcController.Angle}\u00b0</color>");
    }

    private void OnConnectionChanged(bool connected)
    {
        if (connected)
        {
            string connColor = ColorToHex(colorConnected);
            SetText(connectionStatusText, $"<color={connColor}>DA KET NOI</color>");
            if (connectionIndicator != null)
                connectionIndicator.color = colorConnected;
        }
        else
        {
            string discColor = ColorToHex(colorDisconnected);
            SetText(connectionStatusText, $"<color={discColor}>MAT KET NOI</color>");
            if (connectionIndicator != null)
                connectionIndicator.color = colorDisconnected;
        }
    }

    // ============================
    // UTILITIES
    // ============================
    private void SetText(TextMeshProUGUI textField, string value)
    {
        if (textField != null)
            textField.text = value;
    }

    private string ColorToHex(Color color)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(color);
    }
}
