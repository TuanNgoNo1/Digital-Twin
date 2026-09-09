using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Thay thế WirePlug cho môi trường XR.
/// Dùng XRGrabInteractable thay vì OnMouse.
/// 
/// SETUP:
/// 1. Gắn script này vào đầu dây (PlugA / PlugB)
/// 2. Đảm bảo GameObject có XRGrabInteractable component
/// 3. Đảm bảo có Collider (không phải trigger)
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class XRWirePlug : MonoBehaviour
{
    [Header("=== CẤU HÌNH ===")]
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.05f;

    [Header("=== TRẠNG THÁI ===")]
    public bool isSnapped = false;
    public SocketPoint connectedSocket;
    public XRWireBody parentWire;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private SocketPoint nearestSocket;
    private bool isGrabbed = false;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Đăng ký sự kiện grab / release
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    // ==================== XR GRAB EVENTS ====================

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Nếu đang snap → rút ra
        if (isSnapped)
        {
            Unsnap();
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        ClearHighlight();

        // Thử snap khi thả
        TrySnap();
    }

    // ==================== UPDATE ====================

    void Update()
    {
        if (!isGrabbed) return;
        FindNearestSocket();
    }

    // ==================== SNAP LOGIC ====================

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

    void TrySnap()
    {
        // Tìm lần cuối trước khi snap
        FindNearestSocket();

        if (nearestSocket != null)
        {
            SnapTo(nearestSocket);
        }
    }

    void SnapTo(SocketPoint socket)
    {
        isSnapped = true;
        connectedSocket = socket;
        socket.isOccupied = true;

        // Tắt grab tạm thời để đặt vị trí
        grabInteractable.enabled = false;

        Collider socketCollider = socket.GetComponent<Collider>();
        Vector3 targetPos = socketCollider != null ? socketCollider.bounds.center : socket.transform.position;

        transform.position = targetPos;
        transform.rotation = socket.transform.rotation;

        // Bật lại grab (để có thể rút ra sau)
        grabInteractable.enabled = true;

        Debug.Log($"<color=green>✓ XR SNAP: {wireColor} → {socket.socketID}</color>");

        if (parentWire != null)
            parentWire.CheckConnection();
    }

    void Unsnap()
    {
        if (connectedSocket != null)
        {
            connectedSocket.isOccupied = false;
            Debug.Log($"<color=yellow>✗ XR UNSNAP: {wireColor} rút khỏi {connectedSocket.socketID}</color>");
            connectedSocket = null;
        }
        isSnapped = false;

        if (parentWire != null)
            parentWire.CheckConnection();
    }

    void ClearHighlight()
    {
        if (nearestSocket != null)
        {
            nearestSocket.SetHighlight(false);
            nearestSocket = null;
        }
    }
}