using UnityEngine;
using UnityEngine.InputSystem;

public class WirePlug : MonoBehaviour
{
    // Every plug reads the same mouse state in Update. Only one plug may own
    // the current pointer drag when multiple plug colliders overlap a socket.
    private static WirePlug activePointerDrag;

    [Header("=== CAMERA ĐIỀU KHIỂN ===")]
    public Camera mainCam;

    [Header("=== CẤU HÌNH ===")]
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.25f;
    public string preferredSocketID = "";
    public bool isSnapped = false;
    public SocketPoint connectedSocket;
    public WireBody parentWire;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private SocketPoint nearestSocket;
    private Plane boardPlane;

    private void OnDisable()
    {
        ReleasePointerDragOwnership();
        isDragging = false;
        ClearHighlight();
    }

    private void OnDestroy()
    {
        ReleasePointerDragOwnership();
    }

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();

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
    }

    void Update()
    {
        if (mainCam == null) return;
        if (CircuitManager.Instance != null && CircuitManager.Instance.IsPopupVisible) return;

        Vector3 mousePos = GetMousePosition();
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        bool leftClickDown = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || Input.GetMouseButtonDown(0);
        bool leftClickPressed = (Mouse.current != null && Mouse.current.leftButton.isPressed) || Input.GetMouseButton(0);
        bool leftClickUp = (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) || Input.GetMouseButtonUp(0);

        if (leftClickDown &&
            CircuitManager.Instance != null &&
            CircuitManager.Instance.IsPointerOverStepNavigation(mousePos))
        {
            return;
        }

        // Kiểm tra Hover
        RaycastHit[] hits = Physics.RaycastAll(ray);
        WirePlug pointerTarget = FindPointerTarget(hits);
        bool isHovering = pointerTarget == this;

        if (leftClickDown && isHovering && TryAcquirePointerDragOwnership())
        {
            if (isSnapped) Unsnap();
            isDragging = true;
            boardPlane = new Plane(-transform.forward, transform.position);
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
            ReleasePointerDragOwnership();
            Debug.Log($"[WirePlug {name}] Thả chuột. NearestSocket: {(nearestSocket != null ? nearestSocket.socketID : "NULL")}");
            if (nearestSocket != null) 
                SnapTo(nearestSocket);
            else 
                ClearHighlight();
        }
    }

    private static WirePlug FindPointerTarget(RaycastHit[] hits)
    {
        WirePlug best = null;
        float bestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            WirePlug candidate = hit.collider != null
                ? hit.collider.GetComponentInParent<WirePlug>()
                : null;
            if (candidate == null || !candidate.isActiveAndEnabled)
                continue;

            bool isCloser = hit.distance < bestDistance - 0.0001f;
            bool isTieWithLowerId = Mathf.Abs(hit.distance - bestDistance) <= 0.0001f
                && (best == null || candidate.GetInstanceID() < best.GetInstanceID());
            if (!isCloser && !isTieWithLowerId)
                continue;

            best = candidate;
            bestDistance = hit.distance;
        }

        return best;
    }

    private bool TryAcquirePointerDragOwnership()
    {
        if (activePointerDrag != null && activePointerDrag != this)
            return false;

        activePointerDrag = this;
        return true;
    }

    private void ReleasePointerDragOwnership()
    {
        if (activePointerDrag == this)
            activePointerDrag = null;
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
        SocketPoint preferred = null;
        SocketPoint best = null;
        float preferredDist = snapDistance;
        float bestDist = snapDistance;
        bool hasPreferredSocket = !string.IsNullOrWhiteSpace(preferredSocketID);

        foreach (var s in all)
        {
            if (!s.CanAccept(wireColor)) continue;

            float dist = Vector3.Distance(transform.position, s.transform.position);
            if (hasPreferredSocket &&
                string.Equals(s.socketID, preferredSocketID, System.StringComparison.OrdinalIgnoreCase) &&
                dist < preferredDist)
            {
                preferredDist = dist;
                preferred = s;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = s;
            }
        }

        if (preferred != null)
            best = preferred;

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

        // Snap trực tiếp vào socket (pivot đã chuẩn)
        transform.position = socket.transform.position;
        transform.rotation = socket.transform.rotation;

        Debug.Log($"<color=cyan>⚡ [SNAP]: {wireColor} -> {socket.socketID}</color>");

        NotifyConnectedWireBodies();
        
        ClearHighlight();
    }

    void Unsnap()
    {
        if (connectedSocket != null)
        {
            connectedSocket.isOccupied = false;
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
