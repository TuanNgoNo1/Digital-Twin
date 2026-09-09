using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PiGatewayHMI : MonoBehaviour
{
    [Header("Gateway")]
    public PLCController_v2 gateway;

    [Header("Controls")]
    public Button onButton;
    public Button offButton;
    public Button forwardButton;
    public Button reverseButton;
    public Slider speedSlider;
    public TMP_InputField rotationsInput;
    public TMP_InputField angleInput;
    public Button setRotationsButton;
    public Button setAngleButton;

    [Header("Telemetry Text")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI directionText;
    public TextMeshProUGUI rotationsText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI backendText;

    private void Start()
    {
        if (gateway == null)
            gateway = PLCController_v2.Instance != null ? PLCController_v2.Instance : FindObjectOfType<PLCController_v2>();

        if (gateway == null)
        {
            SetStatus("NO PI GATEWAY CLIENT");
            enabled = false;
            return;
        }

        if (onButton != null) onButton.onClick.AddListener(gateway.TurnOn);
        if (offButton != null) offButton.onClick.AddListener(gateway.TurnOff);
        if (forwardButton != null) forwardButton.onClick.AddListener(gateway.SetDirectionForward);
        if (reverseButton != null) reverseButton.onClick.AddListener(gateway.SetDirectionReverse);
        if (setRotationsButton != null) setRotationsButton.onClick.AddListener(SetRotationsFromInput);
        if (setAngleButton != null) setAngleButton.onClick.AddListener(SetAngleFromInput);

        if (speedSlider != null)
        {
            speedSlider.minValue = 0f;
            speedSlider.maxValue = 3000f;
            speedSlider.value = gateway.LatestTelemetry.speedRpm;
            speedSlider.onValueChanged.AddListener(gateway.SetSpeed);
        }

        gateway.OnTelemetryUpdated += UpdateTelemetry;
        gateway.OnConnectionStatusChanged += SetStatus;
        UpdateTelemetry(gateway.LatestTelemetry);
    }

    private void OnDestroy()
    {
        if (gateway == null)
            return;

        gateway.OnTelemetryUpdated -= UpdateTelemetry;
        gateway.OnConnectionStatusChanged -= SetStatus;
    }

    private void SetRotationsFromInput()
    {
        if (rotationsInput != null && float.TryParse(rotationsInput.text, out float rotations))
            gateway.SetTargetRotations(rotations);
    }

    private void SetAngleFromInput()
    {
        if (angleInput != null && float.TryParse(angleInput.text, out float angle))
            gateway.SetTargetAngle(angle);
    }

    private void UpdateTelemetry(PLCController_v2.MotorTelemetry telemetry)
    {
        if (telemetry == null)
            return;

        SetStatus(gateway.IsPiOnline
            ? (telemetry.running ? "MOTOR RUNNING" : "MOTOR STOPPED")
            : (telemetry.running ? "LOCAL FALLBACK RUNNING" : "PI OFFLINE"));

        if (speedText != null) speedText.text = $"Speed: {telemetry.speedRpm:F0} RPM";
        if (countText != null) countText.text = $"Count: {telemetry.count}";
        if (directionText != null) directionText.text = $"Direction: {telemetry.direction}";
        if (rotationsText != null) rotationsText.text = $"Rotations: {telemetry.rotations:F2}";
        if (angleText != null) angleText.text = $"Angle: {telemetry.angle:F1}";
        if (backendText != null) backendText.text = telemetry.backendSynced ? "Backend: synced" : $"Backend: {telemetry.backendStatus}";
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }
}
