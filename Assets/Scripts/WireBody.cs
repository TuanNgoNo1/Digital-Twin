using UnityEngine;

public class WireBody : MonoBehaviour
{
    public WirePlug plugA;
    public WirePlug plugB;
    public string correctSocketA;
    public string correctSocketB;
    public LineRenderer lineRenderer;
    public int segments = 15;
    public float sag = 0.02f;
    public WireColor wireColor = WireColor.Yellow;
    public bool isFullyConnected = false;
    public bool isCorrect = false;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.008f;
        lineRenderer.endWidth = 0.008f;

        UpdateWireColor();
    }

    void UpdateWireColor()
    {
        if (lineRenderer == null) return;
        Color c = Color.yellow;
        switch (wireColor)
        {
            case WireColor.Red: c = Color.red; break;
            case WireColor.Yellow: c = new Color(1f, 0.9f, 0f); break;
            case WireColor.Black: c = Color.black; break;
        }
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        
        // Nếu dùng shader chuẩn (như URP Unlit), cần gán vào material
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = c;
        }
    }

    void Update()
    {
        if (plugA == null || plugB == null) return;
        Vector3 start = plugA.transform.position;
        Vector3 end = plugB.transform.position;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y -= Mathf.Sin(t * Mathf.PI) * sag;
            lineRenderer.SetPosition(i, pos);
        }
    }

    public void CheckConnection()
    {
        if (plugA == null || plugB == null) return;

        // Phải cắm thành công cả đầu A và đầu B thì mới tiến hành chấm điểm
        isFullyConnected = plugA.isSnapped && plugB.isSnapped;
        
        Debug.Log($"[WireBody {name}] CheckConnection: FullyConnected={isFullyConnected} (A={plugA.isSnapped}, B={plugB.isSnapped})");

        if (!isFullyConnected) { isCorrect = false; return; }

        if (plugA.connectedSocket == null || plugB.connectedSocket == null) return;

        // Trim để tránh lỗi do dấu cách thừa trong ID
        string a = plugA.connectedSocket.socketID.Trim();
        string b = plugB.connectedSocket.socketID.Trim();
        string targetA = correctSocketA.Trim();
        string targetB = correctSocketB.Trim();

        Debug.Log($"[WireBody {name}] So sánh: ({a} <-> {b}) với Mục tiêu: ({targetA} <-> {targetB})");

        // So khớp điều kiện chấm bài
        isCorrect = (a == targetA && b == targetB)
                 || (a == targetB && b == targetA);

        if (isCorrect)
        {
            Debug.Log("<color=green>★ CHÚC MỪNG: BẠN ĐÃ CẮM MẠCH ĐÚNG! " + a + " <-> " + b + "</color>");
            
            // --- TÍCH HỢP ĐẨY ĐIỂM LÊN SERVER LMS (SCORM 1.2) ---
            if (SCORMManager.Instance != null)
            {
                SCORMManager.Instance.SetScore(100f); // Gửi thẳng 100 điểm lên hệ thống
                SCORMManager.Instance.SetCompletion("passed"); // Báo cáo trạng thái hoàn thành môn học
                SCORMManager.Instance.FinishSCORM(); // Đóng gói phiên làm việc SCORM an toàn
                Debug.Log("<color=green>✅ Đã đồng bộ kết quả thực hành lên Server SCORM thành công!</color>");
            }
        }
        else
        {
            Debug.Log("<color=red>⚠ KẾT QUẢ SAI: Vị trí cắm mạch không khớp yêu cầu! " + a + " <-> " + b + "</color>");
        }
    }
}