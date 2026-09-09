using UnityEngine;

/// <summary>
/// Đầu dây chỉ di chuyển theo trục X (ngang) dựa trên vị trí đặt ban đầu.
/// Camera khóa chính diện board.
/// Kéo trái/phải để snap vào socket.
/// </summary>
public class HorizontalWirePlug : MonoBehaviour
{
    [Header("=== CẤU HÌNH ===")]
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.08f;
    public XRWireBody parentWire;

    [Header("=== GIỚI HẠN KÉO NGANG (RELATIVE) ===")]
    [Tooltip("Khoảng cách tối đa được kéo sang TRÁI so với vị trí đặt ban đầu")]
    public float xMin = -0.5f;
    [Tooltip("Khoảng cách tối đa được kéo sang PHẢI so với vị trí đặt ban đầu")]
    public float xMax = 0.5f;

    [Header("=== TRẠNG THÁI ===")]
    public bool isSnapped = false;
    public SocketPoint connectedSocket;

    // Visual
    private Renderer rend;
    private Material originalMat;
    public Material hoverMat;
    public Material snappedMat;

    private Camera mainCam;
    private bool isDragging = false;
    
    // Lưu tọa độ gốc để tránh bị biến mất (Teleport)
    private float startX;
    private float fixedY;  // Khóa Y
    private float fixedZ;  // Khóa Z
    private SocketPoint nearestSocket;

    void Start()
    {
        mainCam = Camera.main;
        rend = GetComponent<Renderer>();
        if (rend != null) originalMat = rend.material;

        // Ghi nhớ tọa độ thế giới thực tế ban đầu (Ví dụ khu vực X = 387)
        startX = transform.position.x;
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    void OnMouseEnter()
    {
        if (isSnapped || isDragging) return;
        if (rend != null && hoverMat != null)
            rend.material = hoverMat;
    }

    void OnMouseExit()
    {
        if (isDragging || isSnapped) return;
        if (rend != null) rend.material = originalMat;
    }

    void OnMouseDown()
    {
        if (isSnapped)
        {
            Unsnap();
            return;
        }
        isDragging = true;
        if (rend != null && hoverMat != null)
            rend.material = hoverMat;
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCam == null) return;

        // Chuyển vị trí chuột sang world space
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = mainCam.WorldToScreenPoint(transform.position).z;
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos);

        // TÍNH TOÁN GIỚI HẠN DỰA TRÊN VỊ TRÍ GỐC 
        float absoluteMinX = startX + xMin; // Ví dụ: 387 - 0.5 = 386.5
        float absoluteMaxX = startX + xMax; // Ví dụ: 387 + 0.5 = 387.5

        // Chỉ cập nhật X trong vùng an toàn, khóa cứng Y và Z
        float clampedX = Mathf.Clamp(worldPos.x, absoluteMinX, absoluteMaxX);
        transform.position = new Vector3(clampedX, fixedY, fixedZ);

        // Tìm socket gần nhất
        FindNearestSocket();
    }

    void OnMouseUp()
    {
        isDragging = false;

        if (nearestSocket != null)
            SnapTo(nearestSocket);
        else
        {
            ClearHighlight();
            if (rend != null) rend.material = originalMat;
        }
    }

    void FindNearestSocket()
    {
        SocketPoint[] all = FindObjectsByType<SocketPoint>(FindObjectsSortMode.None);
        SocketPoint best = null;
        float bestDist = snapDistance;

        foreach (var s in all)
        {
            if (s.isOccupied) continue;
            if (s.acceptColor != WireColor.Any && s.acceptColor != wireColor) continue;

            // Chỉ tính khoảng cách theo X (vì chỉ di chuyển ngang)
            float dist = Mathf.Abs(transform.position.x - s.transform.position.x);
            if (dist < bestDist) { bestDist = dist; best = s; }
        }

        if (best != nearestSocket)
        {
            ClearHighlight();
            nearestSocket = best;
            if (nearestSocket != null) nearestSocket.SetHighlight(true);
        }
    }

    void ClearHighlight()
    {
        if (nearestSocket != null)
        {
            nearestSocket.SetHighlight(false);
            nearestSocket = null;
        }
    }

    void SnapTo(SocketPoint socket)
    {
        ClearHighlight();
        isSnapped = true;
        connectedSocket = socket;
        socket.isOccupied = true;

        // Lấy tọa độ trung tâm thật sự của Collider ổ cắm
        Collider socketCollider = socket.GetComponent<Collider>();
        float targetX = socketCollider != null ? socketCollider.bounds.center.x : socket.transform.position.x;

        // Snap vào đúng vị trí X tâm của socket, giữ nguyên Y và Z cố định
        transform.position = new Vector3(
            targetX,
            fixedY,
            fixedZ
        );

        if (rend != null && snappedMat != null)
            rend.material = snappedMat;
        else if (rend != null)
            rend.material = originalMat;

        Debug.Log($"<color=green>✓ SNAP: {wireColor} → {socket.socketID}</color>");
        if (parentWire != null) parentWire.CheckConnection(this);
    }

    void Unsnap()
    {
        if (connectedSocket != null)
        {
            connectedSocket.isOccupied = false;
            connectedSocket = null;
        }
        isSnapped = false;

        if (rend != null) rend.material = originalMat;
        Debug.Log($"<color=yellow>✗ UNSNAP: {wireColor}</color>");
        if (parentWire != null) parentWire.CheckConnection(this);
    }
}