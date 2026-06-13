using UnityEngine;
using UnityEngine.InputSystem;

public class WirePlug : MonoBehaviour
{
    [Header("=== CAMERA ĐIỀU KHIỂN ===")]
    public Camera mainCam;

    [Header("=== CẤU HÌNH ===")]
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.25f;
    public bool isSnapped = false;
    public SocketPoint connectedSocket;
    public WireBody parentWire;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private SocketPoint nearestSocket;
    private Plane boardPlane;
    private Collider myCollider;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();

        myCollider = GetComponent<Collider>();

        // Tự động tìm dây cha nếu chưa gán
        if (parentWire == null)
        {
            WireBody[] bodies = FindObjectsByType<WireBody>(FindObjectsSortMode.None);
            foreach (var body in bodies)
            {
                if (body.plugA == this || body.plugB == this)
                {
                    parentWire = body;
                    break;
                }
            }
        }
        
        // Tạo mặt phẳng làm việc dựa trên vị trí hiện tại của đầu dây (giả định bảng mạch nằm trên mặt phẳng này)
        // Nếu bảng mạch của bạn nằm dọc (trục Z cố định), dùng Vector3.forward
        boardPlane = new Plane(-transform.forward, transform.position);

        if (isSnapped && connectedSocket != null)
            connectedSocket.Connect(this);
    }

    void Update()
    {
        if (mainCam == null) return;

        Vector3 mousePos = GetMousePosition();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        // Kiểm tra Hover
        bool isHovering = false;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider == myCollider) isHovering = true;
        }

        bool leftClickDown = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || Input.GetMouseButtonDown(0);
        bool leftClickPressed = (Mouse.current != null && Mouse.current.leftButton.isPressed) || Input.GetMouseButton(0);
        bool leftClickUp = (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) || Input.GetMouseButtonUp(0);

        if (leftClickDown && isHovering)
        {
            if (isSnapped) Unsnap();
            isDragging = true;
            Debug.Log($"[WirePlug {name}] Bắt đầu kéo");

            // Tính toán offset để khi cầm không bị giật về tâm
            if (boardPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = transform.position - hitPoint;
            }
        }

        if (isDragging && leftClickPressed)
        {
            if (boardPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                transform.position = hitPoint + dragOffset;
                FindNearestSocket();
            }
        }

        if (leftClickUp && isDragging)
        {
            isDragging = false;
            Debug.Log($"[WirePlug {name}] Thả chuột. NearestSocket: {(nearestSocket != null ? nearestSocket.socketID : "NULL")}");
            if (nearestSocket != null) 
                SnapTo(nearestSocket);
            else 
                ClearHighlight();
        }
    }

    Vector3 GetMousePosition()
    {
        if (Mouse.current != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            return new Vector3(screenPos.x, screenPos.y, 0);
        }
        return Input.mousePosition;
    }

    void FindNearestSocket()
    {
        SocketPoint[] all = FindObjectsByType<SocketPoint>(FindObjectsSortMode.None);
        SocketPoint best = null;
        float bestDist = snapDistance;

        foreach (var s in all)
        {
            if (!s.HasCapacity) continue;
            if (s.acceptColor != WireColor.Any && s.acceptColor != wireColor) continue;

            float dist = Vector3.Distance(transform.position, s.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = s;
            }
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
        if (socket == null)
        {
            ClearHighlight();
            return;
        }

        isSnapped = true;
        connectedSocket = socket;

        if (!socket.Connect(this))
        {
            isSnapped = false;
            connectedSocket = null;
            ClearHighlight();
            return;
        }

        socket.RefreshPlugPositions();

        Debug.Log($"<color=cyan>⚡ [SNAP]: {wireColor} -> {socket.socketID}</color>");

        NotifyConnectedWireBodies();
        
        ClearHighlight();
    }

    void Unsnap()
    {
        if (connectedSocket != null)
        {
            connectedSocket.Disconnect(this);
            connectedSocket = null;
        }
        isSnapped = false;
        NotifyConnectedWireBodies();
    }

    private void NotifyConnectedWireBodies()
    {
        bool notified = false;
        WireBody[] bodies = FindObjectsByType<WireBody>(FindObjectsSortMode.None);
        foreach (WireBody body in bodies)
        {
            if (body == null)
                continue;

            if (body.plugA == this || body.plugB == this)
            {
                notified = true;
                body.CheckConnection();
            }
        }

        if (!notified && parentWire != null)
            parentWire.CheckConnection();
    }
}
