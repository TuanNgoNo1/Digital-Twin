using UnityEngine;

/// <summary>
/// Gắn vào GameObject cha của dây.
/// Dùng cùng với XRWirePlug thay vì WirePlug.
/// 
/// HIERARCHY:
///   Wire_Yellow (XRWireBody + LineRenderer)
///     ├── PlugA (XRWirePlug + XRGrabInteractable + Collider)
///     └── PlugB (XRWirePlug + XRGrabInteractable + Collider)
/// </summary>
public class XRWireBody : MonoBehaviour
{
    [Header("=== 2 ĐẦU DÂY ===")]
    public XRWirePlug plugA;
    public XRWirePlug plugB;

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
            case WireColor.Red: return new Color(0.9f, 0.1f, 0.1f);
            case WireColor.Yellow: return new Color(0.95f, 0.85f, 0.1f);
            case WireColor.Black: return new Color(0.15f, 0.15f, 0.15f);
            default: return Color.white;
        }
    }

    public void CheckConnection()
    {
        isFullyConnected = plugA.isSnapped && plugB.isSnapped;

        if (!isFullyConnected)
        {
            isCorrect = false;
            ResetWireColor();
            return;
        }

        string a = plugA.connectedSocket.socketID;
        string b = plugB.connectedSocket.socketID;

        isCorrect = (a == correctSocketA && b == correctSocketB)
                 || (a == correctSocketB && b == correctSocketA);

        if (isCorrect)
        {
            Debug.Log($"<color=green>★ ĐÚNG! Dây {wireColor}: {a} ↔ {b}</color>");
            SetLineColor(new Color(0.1f, 0.9f, 0.2f)); // Xanh lá
        }
        else
        {
            Debug.Log($"<color=red>✗ SAI! Dây {wireColor}: {a} ↔ {b} (Đúng: {correctSocketA} ↔ {correctSocketB})</color>");
            SetLineColor(new Color(1f, 0.3f, 0.3f)); // Đỏ nhạt
        }
    }

    void ResetWireColor()
    {
        SetLineColor(GetWireColor());
    }

    void SetLineColor(Color c)
    {
        if (lineRenderer == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        if (lineRenderer.material != null)
            lineRenderer.material.color = c;
    }
}