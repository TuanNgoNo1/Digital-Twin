using UnityEngine;

public class VirtualMotor : MonoBehaviour
{
    [Header("Cấu hình Đọc từ PLC")]
    public PLCController plc;            // Kéo NetworkManager vào đây
    public string diaChiPLC = "Y0";      // Chân cấp điện cho motor (Y0, Y1...)
    public float tocDoDoc = 0.1f;        // 0.1 giây hỏi PLC 1 lần
    
    [Header("Cấu hình Chuyển động")]
    public float tocDoXoay = 300f;       // Tốc độ xoay trên màn hình (độ/giây)
    public Vector3 trucXoay = Vector3.up; // Xoay quanh trục Y (Up). Có thể đổi thành Vector3.forward hoặc right

    private float timer = 0f;
    private bool isRunning = false;      // Biến nhớ trạng thái motor

    // Update chạy liên tục theo từng khung hình (Frame)
    void Update()
    {
        // --- PHẦN 1: HỎI THĂM PLC (0.1s / lần) ---
        timer += Time.deltaTime; 
        if (timer >= tocDoDoc)
        {
            timer = 0f; 
            if (plc != null)
            {
                // Cập nhật trạng thái xem Y0 đang có điện hay không
                isRunning = plc.DocTrangThai(diaChiPLC);
            }
        }

        // --- PHẦN 2: THỰC HIỆN HIỆU ỨNG XOAY ---
        if (isRunning)
        {
            // Lệnh transform.Rotate giúp vật thể xoay mượt mà theo thời gian thực
            transform.Rotate(trucXoay * tocDoXoay * Time.deltaTime);
        }
    }
}