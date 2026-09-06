using System;
using UnityEngine;

public class SocketPoint : MonoBehaviour
{
    private const float GuideFocusScale = 1.2f;
    private const int GuideFocusSegments = 32;
    private const float GuideFocusRingRadius = 0.4f;
    private const float GuideFocusRingWorldWidth = 0.0022f;

    public string socketID;
    public WireColor acceptColor = WireColor.Any;
    public bool isOccupied = false;
    public Material highlightMat;

    private Material originalMat;
    private Material guideFocusRingMat;
    private Renderer socketRenderer;
    private GameObject guideFocusRing;
    private Vector3 originalLocalScale;
    private bool hasOriginalState;
    private bool guideFocused;
    private bool clickSelected;

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
        CaptureOriginalState();
    }

    public void SetHighlight(bool on)
    {
        if (socketRenderer == null || highlightMat == null) return;
        socketRenderer.material = on ? highlightMat : originalMat;
    }

    public void SetGuideFocus(bool on)
    {
        CaptureOriginalState();
        guideFocused = on;
        RefreshFocusVisual();
    }

    public void SetClickSelection(bool on)
    {
        CaptureOriginalState();
        clickSelected = on;
        RefreshFocusVisual();
    }

    private void RefreshFocusVisual()
    {
        bool showRing = guideFocused || clickSelected;
        transform.localScale = clickSelected
            ? originalLocalScale * 1.38f
            : guideFocused
                ? originalLocalScale * GuideFocusScale
                : originalLocalScale;

        EnsureGuideFocusRing();
        guideFocusRing.SetActive(showRing);
        LineRenderer ring = guideFocusRing.GetComponent<LineRenderer>();
        if (ring != null)
        {
            Color ringColor = clickSelected
                ? new Color(0.12f, 0.36f, 0.78f, 1f)
                : new Color(0.05f, 0.45f, 0.9f, 1f);
            ring.startColor = ringColor;
            ring.endColor = ringColor;
        }
    }

    private void CaptureOriginalState()
    {
        if (hasOriginalState)
            return;

        originalLocalScale = transform.localScale;
        socketRenderer = GetComponent<Renderer>();
        if (socketRenderer != null)
            originalMat = socketRenderer.material;

        hasOriginalState = true;
    }

    private void EnsureGuideFocusRing()
    {
        if (guideFocusRing != null)
            return;

        guideFocusRing = new GameObject("SocketGuideFocus");
        guideFocusRing.transform.SetParent(transform, false);
        guideFocusRing.transform.localPosition = new Vector3(0f, 0f, 0.16f);
        guideFocusRing.transform.localRotation = Quaternion.identity;
        guideFocusRing.transform.localScale = Vector3.one;

        LineRenderer ring = guideFocusRing.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = GuideFocusSegments;
        ring.startWidth = GuideFocusRingWorldWidth;
        ring.endWidth = GuideFocusRingWorldWidth;
        ring.numCapVertices = 2;
        ring.numCornerVertices = 2;
        ring.alignment = LineAlignment.View;
        ring.sortingOrder = 4500;
        ring.material = GetGuideFocusRingMaterial();
        Color ringColor = new Color(0.05f, 0.45f, 0.9f, 1f);
        ring.startColor = ringColor;
        ring.endColor = ringColor;

        for (int i = 0; i < GuideFocusSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / GuideFocusSegments;
            ring.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * GuideFocusRingRadius,
                    Mathf.Sin(angle) * GuideFocusRingRadius,
                    0f));
        }

        guideFocusRing.SetActive(false);
    }

    private Material GetGuideFocusRingMaterial()
    {
        if (guideFocusRingMat != null)
            return guideFocusRingMat;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        guideFocusRingMat = new Material(shader);
        guideFocusRingMat.renderQueue = 4500;
        return guideFocusRingMat;
    }
}
