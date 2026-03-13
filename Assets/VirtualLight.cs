using UnityEngine;

public class VirtualLight : MonoBehaviour
{
    [Header("Cấu hình màu sắc")]
    public Material redMat;   // Kéo Material màu Đỏ vào đây (Tắt)
    public Material greenMat; // Kéo Material màu Xanh vào đây (Sáng)
    
    private MeshRenderer meshRenderer;

    void Start()
    {
        // Lấy thành phần render hình ảnh của vật thể
        meshRenderer = GetComponent<MeshRenderer>();
        TurnOff(); // Mặc định khi ấn Play là đèn tắt
    }

    // Hàm gọi khi muốn BẬT đèn
    public void TurnOn()
    {
        if (meshRenderer != null && greenMat != null)
        {
            meshRenderer.material = greenMat;
        }
    }

    // Hàm gọi khi muốn TẮT đèn
    public void TurnOff()
    {
        if (meshRenderer != null && redMat != null)
        {
            meshRenderer.material = redMat;
        }
    }
}