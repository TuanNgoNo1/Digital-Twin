using UnityEngine;

public class VirtualUIButton : MonoBehaviour
{
    [Header("Liên kết thiết bị")]
    public VirtualLight linkedLight; // Kéo vật thể khối cầu (Đèn) vào đây
    public PLCController plc;        // Kéo vật thể NetworkManager vào đây
    
    [Header("Cấu hình PLC")]
    public string diaChiPLC = "Y0";  // Chân ngõ ra trên PLC thật

    // Biến lưu trữ trạng thái hiện tại (Mặc định ban đầu là tắt)
    private bool isON = false; 

    // Hàm này được tạo ra ĐỂ GẮN VÀO SỰ KIỆN ONCLICK của nút bấm trên màn hình
    public void ThucHienOnClick()
    {
        isON = !isON; // Đảo ngược trạng thái (Đang Tắt -> Bật, Đang Bật -> Tắt)

        if (isON)
        {
            // Trạng thái BẬT
            Debug.Log("Gửi tín hiệu BẬT xuống PLC...");
            if (linkedLight != null) linkedLight.TurnOn(); 
            if (plc != null) plc.DieuKhienThietBi(diaChiPLC, true); 
        }
        else
        {
            // Trạng thái TẮT
            Debug.Log("Gửi tín hiệu TẮT xuống PLC...");
            if (linkedLight != null) linkedLight.TurnOff();
            if (plc != null) plc.DieuKhienThietBi(diaChiPLC, false);
        }
    }
}