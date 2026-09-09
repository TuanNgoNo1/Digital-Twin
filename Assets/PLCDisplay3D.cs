using UnityEngine;
using TMPro;
using UnityEngine;
using System.Collections;

public class PLCDisplay3D : MonoBehaviour
{
    [Header("Cấu hình hiển thị")]
    public TextMeshProUGUI valueText;
    public float refreshRate = 0.5f;

    private PLCController_v2 gateway;
    private float lastUpdate = 0f;

    void Start()
    {
        if (valueText == null)
        {
            Debug.LogWarning("[PLCDisplay3D] valueText chưa gán — tắt script.");
            enabled = false;
            return;
        }

        gateway = PLCController_v2.Instance != null ? PLCController_v2.Instance : FindObjectOfType<PLCController_v2>();
        if (gateway == null)
        {
            valueText.text = "NO PLC";
            valueText.color = Color.yellow;
            enabled = false;
            return;
        }

        // Bắt đầu cập nhật telemetry
        StartCoroutine(UpdateDisplay());
    }

    IEnumerator UpdateDisplay()
    {
        while (enabled)
        {
            if (Time.time - lastUpdate >= refreshRate)
            {
                UpdateTelemetry();
                lastUpdate = Time.time;
            }
            yield return null;
        }
    }

    private void UpdateTelemetry()
    {
        if (gateway == null || gateway.LatestTelemetry == null)
        {
            valueText.text = "NO DATA";
            valueText.color = Color.yellow;
            return;
        }

        var t = gateway.LatestTelemetry;
        string status = gateway.IsPiOnline
            ? (t.running ? "RUNNING" : "STOPPED")
            : "PI OFFLINE";

        valueText.text = $"{t.speedRpm:F0} RPM | {status}";
        valueText.color = t.running ? Color.green : Color.red;
    }
}