using UnityEngine;

public class SocketPoint : MonoBehaviour
{
    public string socketID;
    public WireColor acceptColor = WireColor.Any;
    public bool isOccupied = false;
    public Material highlightMat;

    private Material originalMat;
    private Renderer socketRenderer;

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