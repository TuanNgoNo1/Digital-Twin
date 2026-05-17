using UnityEngine;

public class WirePlug : MonoBehaviour
{
    public WireColor wireColor = WireColor.Yellow;
    public float snapDistance = 0.05f;
    public bool isSnapped = false;
    public SocketPoint connectedSocket;
    public WireBody parentWire;

    private Camera mainCam;
    private bool isDragging = false;
    private float dragDepth;
    private Vector3 dragOffset;
    private SocketPoint nearestSocket;

    void Start()
    {
        mainCam = Camera.main;
    }

    void OnMouseDown()
    {
        if (isSnapped) Unsnap();
        isDragging = true;
        dragDepth = mainCam.WorldToScreenPoint(transform.position).z;
        dragOffset = transform.position - mainCam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth));
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        transform.position = mainCam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, dragDepth)) + dragOffset;
        FindNearestSocket();
    }

    void OnMouseUp()
    {
        isDragging = false;
        if (nearestSocket != null) SnapTo(nearestSocket);
        ClearHighlight();
    }

    void FindNearestSocket()
    {
        SocketPoint[] all = FindObjectsOfType<SocketPoint>();
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
        if (nearestSocket != null) { nearestSocket.SetHighlight(false); nearestSocket = null; }
    }

    void SnapTo(SocketPoint socket)
    {
        isSnapped = true;
        connectedSocket = socket;
        socket.isOccupied = true;
        transform.position = socket.transform.position;
        Debug.Log("SNAP: " + wireColor + " -> " + socket.socketID);
        if (parentWire != null) parentWire.CheckConnection();
    }

    void Unsnap()
    {
        if (connectedSocket != null) { connectedSocket.isOccupied = false; connectedSocket = null; }
        isSnapped = false;
        if (parentWire != null) parentWire.CheckConnection();
    }
}