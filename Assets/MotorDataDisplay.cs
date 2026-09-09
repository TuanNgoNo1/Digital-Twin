using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MotorDataDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tocDoText;
    [SerializeField] private TextMeshProUGUI soVongDaQuayText;
    [SerializeField] private TextMeshProUGUI chieuQuayText;
    [SerializeField] private TextMeshProUGUI gocQuayText;
    [SerializeField] private TextMeshProUGUI soXungText;
    [SerializeField] private TextMeshProUGUI tanSoXungText;
    [SerializeField] private TextMeshProUGUI trangThaiText;

    private PLCController plcController;
    private RotateSubmarineBlades rotateBlades;

    [SerializeField] private float updateInterval = 0.5f; // C?p nh?t m?i 0.5 giây
    private float updateTimer = 0f;

    private void Start()
    {
        plcController = FindObjectOfType<PLCController>();
        rotateBlades = FindObjectOfType<RotateSubmarineBlades>();

        if (plcController == null)
            Debug.LogWarning("<color=orange>?? Không tìm th?y PLCController trong scene</color>");
        if (rotateBlades == null)
            Debug.LogWarning("<color=orange>?? Không tìm th?y RotateSubmarineBlades trong scene</color>");
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (plcController == null || rotateBlades == null) return;

        // T?c ?? (RPM)
        int tocDo = plcController.DocTocDoHienTai();
        if (tocDoText != null)
            tocDoText.text = $"T?c ??: <color=yellow>{tocDo}</color> RPM";

        // S? vòng ?ã quay
        float soVongDaQuay = rotateBlades.GetSoVongDaQuay();
        if (soVongDaQuayText != null)
            soVongDaQuayText.text = $"S? vòng ?ã quay: <color=yellow>{soVongDaQuay:F2}</color>";

        // Chi?u quay
        bool isThuan = rotateBlades.GetRotationDirection() > 0;
        string chieuQuay = isThuan ? "Thu?n" : "Ng??c";
        if (chieuQuayText != null)
            chieuQuayText.text = $"Chi?u quay: <color=yellow>{chieuQuay}</color>";

        // Góc quay
        float gocQuay = soVongDaQuay * 360f;
        if (gocQuayText != null)
            gocQuayText.text = $"Góc quay: <color=yellow>{gocQuay:F1}°</color>";

        // S? xung
        int soXung = plcController.DocSoXungHienTai();
        if (soXungText != null)
            soXungText.text = $"S? xung: <color=yellow>{soXung}</color>";

        // T?n s? xung
        int tanSoXung = plcController.DocTanSoXungHienTai();
        if (tanSoXungText != null)
            tanSoXungText.text = $"T?n s? xung: <color=yellow>{tanSoXung}</color>";

        // Tr?ng thái quay
        bool isRotating = rotateBlades.GetIsRotating();
        string trangThai = isRotating ? "<color=green>?ANG QUAY</color>" : "<color=red>D?NG</color>";
        if (trangThaiText != null)
            trangThaiText.text = $"Tr?ng thái: {trangThai}";
    }
}
