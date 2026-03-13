using UnityEngine;
using UnityEngine.InputSystem;  // ✅ thêm dòng này
using HslCommunication.Profinet.Melsec;

public class PLCController : MonoBehaviour
{
    [Header("Cấu hình Cổng COM")]
    public string portName = "COM3";
    public int baudRate = 9600;

    [Header("Cấu hình Test Đèn")]
    public string diaChiDen = "Y0";
    public float thoiGianNhayMs = 1.0f;

    private MelsecFxSerial melsecSerial;
    private bool trangThaiDen = false;
    private bool dangNhay = false;
    private float timer = 0f;

    void Start()
    {
        melsecSerial = new MelsecFxSerial();
        melsecSerial.SerialPortInni(portName, baudRate, 7,
            System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity.Even);

        var connectResult = melsecSerial.Open();

        if (connectResult.IsSuccess)
            Debug.Log($"<color=green>✅ Kết nối THÀNH CÔNG qua {portName}</color>");
        else
            Debug.LogError($"<color=red>❌ Lỗi kết nối: {connectResult.Message}</color>");
    }

    void Update()
    {
        // ✅ Dùng Keyboard.current thay cho Input.GetKeyDown
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) BatDen();
        if (kb.digit2Key.wasPressedThisFrame) TatDen();
        if (kb.digit3Key.wasPressedThisFrame) ToggleDen();
        if (kb.digit4Key.wasPressedThisFrame) BatNhay();
        if (kb.digit5Key.wasPressedThisFrame) TatNhay();

        if (dangNhay)
        {
            timer += Time.deltaTime;
            if (timer >= thoiGianNhayMs)
            {
                timer = 0f;
                GhiDen(!trangThaiDen);
            }
        }
    }

    public void BatDen()
    {
        Debug.Log($"<color=yellow>🔆 Lệnh: BẬT đèn {diaChiDen}</color>");
        GhiDen(true);
    }

    public void TatDen()
    {
        Debug.Log($"<color=grey>🌑 Lệnh: TẮT đèn {diaChiDen}</color>");
        GhiDen(false);
    }

    public void ToggleDen()
    {
        Debug.Log($"<color=cyan>🔄 Lệnh: TOGGLE đèn {diaChiDen}</color>");
        GhiDen(!trangThaiDen);
    }

    public void BatNhay()
    {
        dangNhay = true;
        timer = 0f;
        Debug.Log($"<color=orange>✨ Nhấp nháy {diaChiDen} mỗi {thoiGianNhayMs}s</color>");
    }

    public void TatNhay()
    {
        dangNhay = false;
        GhiDen(false);
        Debug.Log($"<color=grey>⛔ Dừng nhấp nháy, tắt đèn {diaChiDen}</color>");
    }

    private void GhiDen(bool trangThai)
    {
        if (melsecSerial == null || !melsecSerial.IsOpen())
        {
            Debug.LogWarning("<color=red>⚠️ Chưa kết nối PLC!</color>");
            return;
        }

        var result = melsecSerial.Write(diaChiDen, trangThai);

        if (result.IsSuccess)
        {
            trangThaiDen = trangThai;
            string trangThaiText = trangThai ?
                "<color=yellow>BẬT 💡</color>" :
                "<color=grey>TẮT ⚫</color>";
            Debug.Log($"[PLC] {diaChiDen} → {trangThaiText}");
        }
        else
        {
            Debug.LogError($"<color=red>❌ Lỗi ghi {diaChiDen}: {result.Message}</color>");
        }
    }

    public void DieuKhienThietBi(string diaChi, bool trangThai)
    {
        diaChiDen = diaChi;
        GhiDen(trangThai);
    }

    void OnApplicationQuit()
    {
        dangNhay = false;
        if (melsecSerial != null && melsecSerial.IsOpen())
        {
            melsecSerial.Write(diaChiDen, false);
            melsecSerial.Close();
            Debug.Log("🔌 Đã đóng cổng COM an toàn.");
        }
    }
}