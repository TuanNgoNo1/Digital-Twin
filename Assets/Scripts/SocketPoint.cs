using System;
using UnityEngine;

public class SocketPoint : MonoBehaviour
{
    private const int FocusDashCount = 10;
    private const float FocusDashFill = 0.58f;
    private const float GuideFocusRingRadiusPixels = 15f;
    private static readonly Color DeepSkyBlue = new Color(0f, 0.32f, 0.88f, 1f);

    public string socketID;
    public WireColor acceptColor = WireColor.Any;
    public bool isOccupied = false;
    public Material highlightMat;

    private Material originalMat;
    private Renderer socketRenderer;
    private GameObject guideFocusRing;
    private SpriteRenderer guideFocusRenderer;
    private Vector3 originalLocalScale;
    private bool hasOriginalState;
    private bool guideFocused;
    private bool clickSelected;
    private static Texture2D dashedFocusTexture;
    private static Sprite dashedFocusSprite;

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
        transform.localScale = originalLocalScale;

        EnsureGuideFocusRing();
        RebuildGuideFocusRing(clickSelected ? 1.08f : 1f);
        guideFocusRing.SetActive(showRing);
        guideFocusRenderer.color = DeepSkyBlue;
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

        guideFocusRing = new GameObject($"SocketGuideFocus_{socketID}");
        guideFocusRing.transform.SetParent(null, false);
        guideFocusRenderer = guideFocusRing.AddComponent<SpriteRenderer>();
        guideFocusRenderer.sprite = GetDashedFocusSprite();
        guideFocusRenderer.color = DeepSkyBlue;
        guideFocusRenderer.sortingOrder = 4500;

        guideFocusRing.SetActive(false);
    }

    private void RebuildGuideFocusRing(float scale)
    {
        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (camera == null || guideFocusRing == null)
            return;

        Vector3 socketScreen = camera.WorldToScreenPoint(transform.position);
        if (socketScreen.z <= camera.nearClipPlane)
            return;

        float ringDepth = Mathf.Max(camera.nearClipPlane + 0.01f, socketScreen.z - 0.006f);
        float radiusPixels = GuideFocusRingRadiusPixels * scale;
        Vector3 center = camera.ScreenToWorldPoint(
            new Vector3(socketScreen.x, socketScreen.y, ringDepth));
        Vector3 radiusRight = camera.ScreenToWorldPoint(
            new Vector3(socketScreen.x + radiusPixels, socketScreen.y, ringDepth)) - center;
        Vector3 radiusUp = camera.ScreenToWorldPoint(
            new Vector3(socketScreen.x, socketScreen.y + radiusPixels, ringDepth)) - center;
        float worldDiameter = radiusRight.magnitude + radiusUp.magnitude;
        guideFocusRing.transform.position = center;
        guideFocusRing.transform.rotation = camera.transform.rotation;
        guideFocusRing.transform.localScale = Vector3.one * worldDiameter;
    }

    private static Sprite GetDashedFocusSprite()
    {
        if (dashedFocusSprite != null)
            return dashedFocusSprite;

        const int textureSize = 256;
        const float outerRadius = 112f;
        const float innerRadius = 82f;
        const float edgeFeather = 1.5f;
        float center = (textureSize - 1) * 0.5f;
        float dashStep = Mathf.PI * 2f / FocusDashCount;
        float halfDashAngle = dashStep * FocusDashFill * 0.5f;
        float angularFeather = edgeFeather / ((outerRadius + innerRadius) * 0.5f);

        dashedFocusTexture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.ARGB32,
            false)
        {
            name = "RuntimeDashedSocketFocus",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float radialAlpha = Mathf.Clamp01(
                    Mathf.Min(
                        (radius - innerRadius) / edgeFeather,
                        (outerRadius - radius) / edgeFeather));

                float angle = Mathf.Atan2(dy, dx);
                float localAngle = Mathf.Repeat(angle + dashStep * 0.5f, dashStep) -
                    dashStep * 0.5f;
                float angularAlpha = Mathf.Clamp01(
                    (halfDashAngle - Mathf.Abs(localAngle)) / angularFeather);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(radialAlpha, angularAlpha));
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        dashedFocusTexture.SetPixels(pixels);
        dashedFocusTexture.Apply(false, true);
        dashedFocusSprite = Sprite.Create(
            dashedFocusTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize,
            0,
            SpriteMeshType.FullRect);
        dashedFocusSprite.name = "RuntimeDashedSocketFocus";
        dashedFocusSprite.hideFlags = HideFlags.HideAndDontSave;
        return dashedFocusSprite;
    }

    private void OnDestroy()
    {
        if (guideFocusRing != null)
            Destroy(guideFocusRing);
    }
}
