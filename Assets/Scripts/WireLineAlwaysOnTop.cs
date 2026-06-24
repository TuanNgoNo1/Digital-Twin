using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class WireLineAlwaysOnTop : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private LineRenderer lineRenderer;
    private Material overlayMaterial;
    private bool shaderErrorLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToSceneWires()
    {
        WireBody[] wireBodies = Object.FindObjectsByType<WireBody>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (WireBody wireBody in wireBodies)
        {
            if (wireBody.gameObject.scene.IsValid() &&
                !wireBody.TryGetComponent<WireLineAlwaysOnTop>(out _))
            {
                wireBody.gameObject.AddComponent<WireLineAlwaysOnTop>();
            }
        }
    }

    private void Start()
    {
        ApplyOverlayMaterial();
    }

    private void LateUpdate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (overlayMaterial == null || lineRenderer.sharedMaterial != overlayMaterial)
            ApplyOverlayMaterial();
    }

    private void ApplyOverlayMaterial()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        Shader overlayShader = Shader.Find("Custom/WirePlugAlwaysOnTop");
        if (overlayShader == null)
        {
            if (!shaderErrorLogged)
            {
                Debug.LogError("Wire overlay shader was not found.", this);
                shaderErrorLogged = true;
            }

            return;
        }

        Color wireColor = ReadCurrentWireColor();

        if (overlayMaterial == null)
        {
            overlayMaterial = new Material(overlayShader)
            {
                name = $"{name}_WireOverlay (Runtime)",
                hideFlags = HideFlags.DontSave
            };
        }

        overlayMaterial.SetColor(BaseColorId, wireColor);
        lineRenderer.sharedMaterial = overlayMaterial;
        lineRenderer.sortingOrder = 5000;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    private Color ReadCurrentWireColor()
    {
        Material currentMaterial = lineRenderer.sharedMaterial;
        if (currentMaterial != null)
        {
            if (currentMaterial.HasProperty(BaseColorId))
                return currentMaterial.GetColor(BaseColorId);

            if (currentMaterial.HasProperty(ColorId))
                return currentMaterial.GetColor(ColorId);
        }

        return lineRenderer.startColor;
    }

    private void OnDestroy()
    {
        if (overlayMaterial != null)
            Destroy(overlayMaterial);
    }
}
