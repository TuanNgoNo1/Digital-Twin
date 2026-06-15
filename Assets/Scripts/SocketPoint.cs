using System;
using UnityEngine;

public class SocketPoint : MonoBehaviour
{
    public string socketID;
    public WireColor acceptColor = WireColor.Any;
    public bool isOccupied = false;
    public Material highlightMat;

    private Material originalMat;
    private Renderer socketRenderer;

    public bool AllowsMultipleConnections =>
        string.Equals(socketID, "5VDC", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(socketID, "GND_5V", StringComparison.OrdinalIgnoreCase);

    public bool CanAccept(WireColor wireColor)
    {
        bool colorAccepted = acceptColor == WireColor.Any || acceptColor == wireColor;
        return colorAccepted && (AllowsMultipleConnections || !isOccupied);
    }

    void Awake()
    {
        socketRenderer = GetComponent<Renderer>();
        if (socketRenderer != null)
            originalMat = socketRenderer.material;
    }

    public void SetHighlight(bool on)
    {
        if (socketRenderer == null || highlightMat == null) return;
        socketRenderer.material = on ? highlightMat : originalMat;
    }
}
