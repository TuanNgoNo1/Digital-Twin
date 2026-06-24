using UnityEngine;

/// <summary>
/// Điều khiển dây bằng chuột đơn giản - dùng để test, không cần XR.
/// Khi có VR thật thì dùng XRWirePlug thay thế.
/// 
/// SETUP:
/// 1. Gắn script này vào Wire_HeadA_Yellow và Wire_HeadB_Yellow
/// 2. Đảm bảo có Collider (không phải trigger)
/// 3. Scene cần có Camera với tag "MainCamera"
/// </summary>
public class SimpleWireController : MonoBehaviour
{
    [Header("=== CẤU HÌNH ===")]
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.08f;

    [Header("=== THAM CHIẾU ===")]
    public XRWireBody parentWire;

    [Header("=== TRẠNG THÁI ===")]
    public bool isSnapped = false;
    public SocketPoint connectedSocket;

    private Camera mainCam;
    private bool isDragging = false;
    private float dragDepth;
    private Vector3 dragOffset;
    private SocketPoint nearestSocket;

    // Highlight khi hover
    private Renderer rend;
    private Material originalMat;
    public Material hoverMaterial;

    void Start()
    {
        // Tìm camera - thử MainCamera trước, nếu không có thì lấy camera đầu tiên
        mainCam = Camera.main;
        if (mainCam == null)
            mainCam = FindFirstObjectByType<Camera>();

        rend = GetComponent<Renderer>();
        if (rend != null) originalMat = rend.material;
    }

    void OnMouseEnter()
    {
        if (isDragging || isSnapped) return;
        if (rend != null && hoverMaterial != null)
            rend.material = hoverMaterial;
    }

    void OnMouseExit()
    {
        if (isDragging) return;
        if (rend != null) rend.material = originalMat;
    }

    void OnMouseDown()
    {
        if (mainCam == null) return;

        // Nếu đang snap → rút ra
        if (isSnapped) Unsnap();

        isDragging = true;
        dragDepth = mainCam.WorldToScreenPoint(transform.position).z;
        dragOffset = transform.position - mainCam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCam == null) return;

        // Di chuyển đầu dây theo chuột
        transform.position = mainCam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth)) + dragOffset;

        // Tìm socket gần nhất và highlight
        FindNearestSocket();
    }

    void OnMouseUp()
    {
        isDragging = false;
        ClearHighlight();

        // Thử snap
        if (nearestSocket != null)
            SnapTo(nearestSocket);
        else if (rend != null)
            rend.material = originalMat;
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
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < bestDist) { bestDist = d; best = s; }
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
        isSnapped = true;
        connectedSocket = socket;
        socket.isOccupied = true;

        Collider socketCollider = socket.GetComponent<Collider>();
        Vector3 targetPos = socketCollider != null ? socketCollider.bounds.center : socket.transform.position;
        
        transform.position = targetPos;

        if (rend != null) rend.material = originalMat;

        Debug.Log($"<color=green>SNAP: {wireColor} → {socket.socketID}</color>");
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
        if (parentWire != null) parentWire.CheckConnection(this);
    }
}
