using System.Collections.Generic;
using UnityEngine;

public class SocketPoint : MonoBehaviour
{
    public string socketID;
    public WireColor acceptColor = WireColor.Any;
    [Min(1)]
    public int maxConnections = 3;
    [Min(0f)]
    public float snapSpacing = 0.012f;
    public Material highlightMat;

    private readonly List<WirePlug> connectedPlugs = new List<WirePlug>();
    private bool legacyOccupied;
    private Material originalMat;
    private Renderer socketRenderer;

    public bool isOccupied
    {
        get => legacyOccupied || !HasCapacity;
        set => legacyOccupied = value;
    }
    public bool HasCapacity
    {
        get
        {
            RemoveMissingPlugs();
            return !legacyOccupied && connectedPlugs.Count < Mathf.Max(1, maxConnections);
        }
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

    public bool Connect(WirePlug plug)
    {
        if (plug == null)
            return false;

        RemoveMissingPlugs();

        if (connectedPlugs.Contains(plug))
            return true;

        if (!HasCapacity)
            return false;

        connectedPlugs.Add(plug);
        RefreshPlugPositions();
        return true;
    }

    public void Disconnect(WirePlug plug)
    {
        if (plug == null)
            return;

        connectedPlugs.Remove(plug);
        RefreshPlugPositions();
    }

    public void RefreshPlugPositions()
    {
        RemoveMissingPlugs();

        float center = (connectedPlugs.Count - 1) * 0.5f;
        for (int i = 0; i < connectedPlugs.Count; i++)
        {
            WirePlug plug = connectedPlugs[i];
            if (plug == null)
                continue;

            plug.transform.position = transform.position + transform.right * ((i - center) * snapSpacing);
            plug.transform.rotation = transform.rotation;
        }
    }

    private void RemoveMissingPlugs()
    {
        connectedPlugs.RemoveAll(plug => plug == null || !plug.isSnapped || plug.connectedSocket != this);
    }
}
