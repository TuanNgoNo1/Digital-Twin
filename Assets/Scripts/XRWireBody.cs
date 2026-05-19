using UnityEngine;

/// <summary>
/// Quản lý dây - hỗ trợ XRWirePlug, SimpleWireController, và ClickToConnect.
/// </summary>
public class XRWireBody : MonoBehaviour
{
    [Header("=== 2 ĐẦU DÂY ===")]
    public MonoBehaviour plugA;
    public MonoBehaviour plugB;

    [Header("=== KẾT NỐI ĐÚNG ===")]
    public string correctSocketA;
    public string correctSocketB;

    [Header("=== DÂY VISUAL ===")]
    public LineRenderer lineRenderer;
    public int segments = 15;
    public float sag = 0.02f;
    public WireColor wireColor = WireColor.Yellow;

    [Header("=== TRẠNG THÁI ===")]
    public bool isFullyConnected = false;
    public bool isCorrect = false;

    void Start()
    {
        SetupLineRenderer();
    }

    void Update()
    {
        UpdateWireVisual();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;

        Color c = GetWireColor();
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;

        if (lineRenderer.material == null || lineRenderer.material.name.Contains("Default"))
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = c;
        }
    }

    void UpdateWireVisual()
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

    Color GetWireColor()
    {
        switch (wireColor)
        {
            case WireColor.Red:    return new Color(0.9f, 0.1f, 0.1f);
            case WireColor.Yellow: return new Color(0.95f, 0.85f, 0.1f);
            case WireColor.Black:  return new Color(0.15f, 0.15f, 0.15f);
            default:               return Color.white;
        }
    }

    // Support tất cả loại plug
    public void CheckConnection(XRWirePlug c) => CheckConnectionInternal();
    public void CheckConnection(SimpleWireController c) => CheckConnectionInternal();
    public void CheckConnection(HorizontalWirePlug c) => CheckConnectionInternal();
    public void CheckConnection() => CheckConnectionInternal();

    void CheckConnectionInternal()
    {
        bool aSnapped = false, bSnapped = false;
        string aID = "", bID = "";

        GetPlugState(plugA, ref aSnapped, ref aID);
        GetPlugState(plugB, ref bSnapped, ref bID);

        isFullyConnected = aSnapped && bSnapped;

        if (!isFullyConnected) { isCorrect = false; ResetWireColor(); return; }

        isCorrect = (aID == correctSocketA && bID == correctSocketB)
                 || (aID == correctSocketB && bID == correctSocketA);

        if (isCorrect)
        {
            Debug.Log($"<color=green>★ ĐÚNG! {aID} ↔ {bID}</color>");
            SetLineColor(new Color(0.1f, 0.9f, 0.2f));
        }
        else
        {
            Debug.Log($"<color=red>✗ SAI! {aID} ↔ {bID} (Đúng: {correctSocketA} ↔ {correctSocketB})</color>");
            SetLineColor(new Color(1f, 0.3f, 0.3f));
        }
    }

    void GetPlugState(MonoBehaviour plug, ref bool snapped, ref string socketID)
    {
        if (plug is XRWirePlug xr)
        {
            snapped = xr.isSnapped;
            if (xr.connectedSocket != null) socketID = xr.connectedSocket.socketID;
        }
        else if (plug is SimpleWireController sim)
        {
            snapped = sim.isSnapped;
            if (sim.connectedSocket != null) socketID = sim.connectedSocket.socketID;
        }
        else if (plug is HorizontalWirePlug hwp)
        {
            snapped = hwp.isSnapped;
            if (hwp.connectedSocket != null) socketID = hwp.connectedSocket.socketID;
        }
    }

    void ResetWireColor() { SetLineColor(GetWireColor()); }

    void SetLineColor(Color c)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        if (lineRenderer.material != null)
            lineRenderer.material.color = c;
    }
}
