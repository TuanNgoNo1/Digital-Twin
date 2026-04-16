using UnityEngine;
using UnityEngine.InputSystem;

public class TestMotorData : MonoBehaviour
{
    private PLCController plcController;
    private RotateSubmarineBlades rotateBlades;

    [SerializeField] private float updateInterval = 0.5f; // Cập nhật mỗi 0.5 giây
    private float updateTimer = 0f;
    private bool isAutoUpdate = true;

    private void Start()
    {
        plcController = FindObjectOfType<PLCController>();
        rotateBlades = FindObjectOfType<RotateSubmarineBlades>();

        if (plcController == null || rotateBlades == null)
        {
            Debug.LogError("❌ Không tìm thấy controller");
        }
        else
        {
            Debug.Log("<color=green>✅ TestMotorData initialized - Cập nhật tự động mỗi 0.5 giây</color>");
            Debug.Log("<color=cyan>💡 Nhấn T để bật/tắt tự động cập nhật</color>");
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Nhấn T để bật/tắt tự động cập nhật
        if (kb.tKey.wasPressedThisFrame)
        {
            isAutoUpdate = !isAutoUpdate;
            string status = isAutoUpdate ? "BẬT ✅" : "TẮT ❌";
            Debug.Log($"<color=yellow>🔄 Tự động cập nhật: {status}</color>");
        }

        // Tự động cập nhật
        if (isAutoUpdate)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                DisplayMotorData();
            }
        }
    }

    private void DisplayMotorData()
    {
        if (plcController == null || rotateBlades == null)
            return;

        int tocDo = plcController.DocTocDoHienTai();
        float soVongDaQuay = rotateBlades.GetSoVongDaQuay();
        bool isThuan = rotateBlades.GetRotationDirection() > 0;
        float gocQuay = soVongDaQuay * 360f;
        int soXung = plcController.DocSoXungHienTai();
        int tanSoXung = plcController.DocTanSoXungHienTai();
        bool isRotating = rotateBlades.GetIsRotating();

        Debug.Log($"<color=cyan>━━━ THÔNG SỐ MOTOR ━━━</color>");
        Debug.Log($"<color=yellow>📊 Tốc độ:</color> {tocDo} RPM");
        Debug.Log($"<color=yellow>🔄 Số vòng đã quay:</color> {soVongDaQuay:F2}");
        Debug.Log($"<color=yellow>➡️  Chiều quay:</color> {(isThuan ? "Thuận" : "Ngược")}");
        Debug.Log($"<color=yellow>📐 Góc quay:</color> {gocQuay:F1}°");
        Debug.Log($"<color=yellow>⚡ Số xung:</color> {soXung}");
        Debug.Log($"<color=yellow>📈 Tần số xung:</color> {tanSoXung}");
        Debug.Log($"<color=yellow>🔌 Trạng thái:</color> <color={(isRotating ? "green" : "red")}>{(isRotating ? "ĐANG QUAY" : "DỪNG")}</color>");
        Debug.Log($"<color=cyan>━━━━━━━━━━━━━━━━━━</color>");
    }
}