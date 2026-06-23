using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ResponsiveCameraFraming : MonoBehaviour
{
    [Tooltip("Ty le man hinh luc bo cuc scene duoc can chuan.")]
    public float designAspect = 2.25f;

    [Tooltip("Vertical FOV cua camera tai ty le thiet ke.")]
    public float designVerticalFov = 60f;

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ApplyFraming();
    }

    private void Update()
    {
        if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
            ApplyFraming();
    }

    public void ApplyFraming()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null || targetCamera.orthographic || Screen.height <= 0)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float currentAspect = (float)Screen.width / Screen.height;
        float referenceAspect = Mathf.Max(0.01f, designAspect);
        float referenceFov = Mathf.Clamp(designVerticalFov, 1f, 179f);

        if (currentAspect >= referenceAspect)
        {
            targetCamera.fieldOfView = referenceFov;
            return;
        }

        float referenceFovRadians = referenceFov * Mathf.Deg2Rad;
        float fittedFovRadians = 2f * Mathf.Atan(
            Mathf.Tan(referenceFovRadians * 0.5f) * referenceAspect / currentAspect);

        targetCamera.fieldOfView = fittedFovRadians * Mathf.Rad2Deg;
    }
}
