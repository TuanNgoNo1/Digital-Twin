using UnityEngine;

public class VirtualButton : MonoBehaviour
{
    [Header("Liên kết thiết bị")]
    public VirtualLight linkedLight; // Kéo vật thể khối cầu (Đèn) vào đây
    public PLCController plc;        // Kéo vật thể NetworkManager vào đây
    
    [Header("Cấu hình PLC")]
    public string diaChiPLC = "Y0";  // Chân ngõ ra trên PLC thật (Y0, Y1, M0...)

    // Hàm tự động chạy khi CHUỘT TRÁI ĐƯỢC NHẤN XUỐNG vật thể này
    void OnMouseDown()
    {
        // 1. Đổi màu đèn ảo sang Xanh
        if (linkedLight != null) 
            linkedLight.TurnOn(); 
        
        // 2. Gửi tín hiệu BẬT (true) xuống PLC thật
        if (plc != null) 
            plc.DieuKhienThietBi(diaChiPLC, true); 
    }

    // Hàm tự động chạy khi CHUỘT TRÁI ĐƯỢC NHẢ RA
    void OnMouseUp()
    {
        // 1. Đổi màu đèn ảo về Đỏ
        if (linkedLight != null) 
            linkedLight.TurnOff();

        // 2. Gửi tín hiệu TẮT (false) xuống PLC thật
        if (plc != null) 
            plc.DieuKhienThietBi(diaChiPLC, false);
    }
}