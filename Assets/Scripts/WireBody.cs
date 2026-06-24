using System;
using UnityEngine;

public class WireBody : MonoBehaviour
{
    [Header("Hai dau day (Plugs)")]
    public WirePlug plugA;
    public WirePlug plugB;

    [Header("Muc tieu cam (Dap an)")]
    public string correctSocketA;
    public string correctSocketB;

    [Header("Giao dien day")]
    public WireColor wireColor = WireColor.Yellow;
    [Range(0.002f, 0.02f)]
    public float wireWidth = 0.005f;
    [Range(0f, 0.3f)]
    public float sagAmount = 0.05f;
    [Range(2, 20)]
    public int curveSegments = 8;

    [Header("Trang thai")]
    public bool isFullyConnected = false;
    public bool isCorrect = false;

    private bool wasCorrect = false;
    private LineRenderer lr;
    private Vector3 lastP0;
    private Vector3 lastP3;
    private MeshRenderer meshRend;

    void Start()
    {
        RebindPlugs();

        meshRend = GetComponent<MeshRenderer>();
        if (meshRend != null)
            meshRend.enabled = false;

        lr = GetComponent<LineRenderer>();
        if (lr == null)
            lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount = curveSegments;
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.useWorldSpace = true;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.enabled = plugA != null && plugB != null;

        SetWireMaterial();
        ForceRefreshLine();
    }

    private void RebindPlugs()
    {
        if (plugA != null)
            plugA.parentWire = this;

        if (plugB != null)
            plugB.parentWire = this;
    }

    void Update()
    {
        if (lr == null)
            return;

        if (plugA == null || plugB == null)
        {
            lr.enabled = false;
            return;
        }

        if (!lr.enabled)
            lr.enabled = true;

        Vector3 p0 = plugA.transform.position;
        Vector3 p3 = plugB.transform.position;
        lastP0 = p0;
        lastP3 = p3;
        UpdateLinePositions();
    }

    void SetWireMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        switch (wireColor)
        {
            case WireColor.Yellow: mat.color = new Color(1f, 0.85f, 0f); break;
            case WireColor.Red: mat.color = Color.red; break;
            case WireColor.Black: mat.color = new Color(0.1f, 0.1f, 0.1f); break;
            case WireColor.Green: mat.color = new Color(0f, 0.8f, 0.2f); break;
            case WireColor.Blue: mat.color = new Color(0.1f, 0.4f, 1f); break;
            default: mat.color = Color.white; break;
        }
        lr.material = mat;
    }

    void UpdateLinePositions()
    {
        Vector3 mid = (lastP0 + lastP3) * 0.5f + Vector3.down * sagAmount;

        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = (1 - t) * (1 - t) * lastP0 + 2 * (1 - t) * t * mid + t * t * lastP3;
            lr.SetPosition(i, pos);
        }
    }

    private void ForceRefreshLine()
    {
        if (plugA == null || plugB == null || lr == null)
            return;

        lastP0 = plugA.transform.position;
        lastP3 = plugB.transform.position;
        UpdateLinePositions();
    }

    public void CheckConnection()
    {
        bool previousCorrect = wasCorrect;
        RefreshConnectionState();

        if (isFullyConnected)
        {
            string actual = GetSocketSummary();
            string expected = $"{correctSocketA}-{correctSocketB}";
            if (isCorrect)
                Debug.Log($"<color=green>✓ ĐÚNG: {name} → {actual}</color>");
            else
                Debug.LogWarning($"<color=red>✗ SAI SOCKET: {name} → {actual} (đúng: {expected})</color>");
        }

        if (isCorrect && !previousCorrect)
        {
            wasCorrect = true;
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.OnWireConnectedCorrectly(this);
        }
        else if (!isCorrect && previousCorrect)
        {
            wasCorrect = false;
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.EvaluateCircuit();
        }
        else if (isFullyConnected && !isCorrect)
        {
            if (CircuitManager.Instance != null)
                CircuitManager.Instance.EvaluateCircuit();
        }
    }

    public void RefreshConnectionState(bool logResult = false)
    {
        if (plugA == null || plugB == null)
        {
            isFullyConnected = false;
            isCorrect = false;
            return;
        }

        isFullyConnected = plugA.isSnapped && plugB.isSnapped;

        if (!isFullyConnected)
        {
            isCorrect = false;
        }
        else if (plugA.connectedSocket != null && plugB.connectedSocket != null)
        {
            string a = plugA.connectedSocket.socketID.Trim();
            string b = plugB.connectedSocket.socketID.Trim();
            string targetA = correctSocketA.Trim();
            string targetB = correctSocketB.Trim();

            isCorrect =
                (string.Equals(a, targetA, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(b, targetB, StringComparison.OrdinalIgnoreCase)) ||
                (string.Equals(a, targetB, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(b, targetA, StringComparison.OrdinalIgnoreCase));

            if (logResult)
                Debug.Log($"[WireBody] {name}: dang cam {a}-{b}, dap an {targetA}-{targetB}, correct={isCorrect}");
        }
        else
        {
            isCorrect = false;
            if (logResult)
                Debug.LogWarning($"[WireBody] {name}: da snap ca 2 dau nhung connectedSocket bi null. A={GetSocketId(plugA)}, B={GetSocketId(plugB)}");
        }
    }

    public string GetSocketSummary()
    {
        return $"{GetSocketId(plugA)}-{GetSocketId(plugB)}";
    }

    private static string GetSocketId(WirePlug plug)
    {
        if (plug == null)
            return "missing-plug";

        if (!plug.isSnapped)
            return "chua-snap";

        return plug.connectedSocket != null ? plug.connectedSocket.socketID : "null-socket";
    }
}
