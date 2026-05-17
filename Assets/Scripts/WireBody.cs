using UnityEngine;

public class WireBody : MonoBehaviour
{
    public WirePlug plugA;
    public WirePlug plugB;
    public string correctSocketA;
    public string correctSocketB;
    public LineRenderer lineRenderer;
    public int segments = 15;
    public float sag = 0.02f;
    public WireColor wireColor = WireColor.Yellow;
    public bool isFullyConnected = false;
    public bool isCorrect = false;

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = segments;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
    }

    void Update()
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

    public void CheckConnection()
    {
        isFullyConnected = plugA.isSnapped && plugB.isSnapped;
        if (!isFullyConnected) { isCorrect = false; return; }
        string a = plugA.connectedSocket.socketID;
        string b = plugB.connectedSocket.socketID;
        isCorrect = (a == correctSocketA && b == correctSocketB)
                 || (a == correctSocketB && b == correctSocketA);
        if (isCorrect)
            Debug.Log("DUNG! " + a + " <-> " + b);
        else
            Debug.Log("SAI! " + a + " <-> " + b);
    }
}