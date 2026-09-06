using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PageFourWiringTutorialController : MonoBehaviour
{
    private const int WireOverlayLayer = 31;

    [Header("Scene references")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform cursorObject;
    [SerializeField] private RawImage cursorImage;
    [SerializeField] private Texture handIconsTexture;
    [SerializeField] private GameObject connectedWirePrefab;
    [SerializeField] private GameObject jack35Prefab;
    [SerializeField] private Material wireMaterial;
    [SerializeField] private Material jackBodyMaterial;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 1.35f;
    [SerializeField] private float cursorApproachDuration = 1f;
    [SerializeField] private float cursorTransferDuration = 0.8f;
    [SerializeField] private float cursorReleaseDuration = 1f;
    [SerializeField] private float resetDelay = 1f;
    [Tooltip("Vi tri Y0 trong he toa do local cua model bang.")]
    [SerializeField] private Vector3 socketALocal = new Vector3(-0.102497f, -0.025666f, 0.24773f);
    [Tooltip("Vi tri Pin11 trong he toa do local cua model bang.")]
    [SerializeField] private Vector3 socketBLocal = new Vector3(-0.03568f, 0.116647f, 0.24773f);

    private Camera previewCamera;
    private Camera wireOverlayCamera;
    private RenderTexture previewTexture;
    private LineRenderer wireLine;
    private Transform plugA;
    private Transform plugB;
    private Vector3 plugAStart;
    private Vector3 plugBStart;
    private Vector3 socketA;
    private Vector3 socketB;
    private Vector3 cursorIdlePosition;
    private bool isPlaying;
    private Transform cursorTarget;
    private TextMeshProUGUI cursorLabel;
    private Quaternion plugAStartRotation;
    private Quaternion plugBStartRotation;
    private Quaternion socketARotation = Quaternion.identity;
    private Quaternion socketBRotation = Quaternion.identity;
    private GameObject visualRoot;
    private GameObject connectedWireInstance;
    private Bounds boardBounds;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private IEnumerator Start()
    {
        yield return null;
        ResolveReferences();
        CreateFallbackSceneObjects();
        if (modelRoot == null || connectedWirePrefab == null)
        {
            Debug.LogError("[PageFourWiringTutorial] Thiếu model bảng hoặc model dây hướng dẫn.");
            yield break;
        }

        visualRoot = new GameObject("PageFourVisualRoot");
        modelRoot.SetParent(visualRoot.transform, true);
        Transform board = FindDescendant(modelRoot, "Board");
        boardBounds = board != null ? CalculateBounds(board) : CalculateBounds(modelRoot);
        CreatePreviewCamera(boardBounds);
        ResolveSocketTargets(boardBounds);
        CreateConnectedWirePreview();
        CreateWireOverlayCamera();
        playButton?.onClick.AddListener(PlayTutorial);
        ResetTutorial();
        SetTutorialWireVisible(false);
        previewCamera.enabled = gameObject.activeInHierarchy;
        visualRoot.SetActive(gameObject.activeInHierarchy);
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void LateUpdate()
    {
        HandleResolutionChange();
        UpdateWire();
        UpdateCursorPosition();
    }

    private void OnEnable()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = true;
        }
        if (wireOverlayCamera != null)
        {
            wireOverlayCamera.enabled = true;
        }
        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = false;
        }
        if (wireOverlayCamera != null)
        {
            wireOverlayCamera.enabled = false;
        }
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (previewCamera != null)
        {
            Destroy(previewCamera.gameObject);
        }
        if (wireOverlayCamera != null)
        {
            Destroy(wireOverlayCamera.gameObject);
        }
        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }
        if (previewImage != null && previewImage.texture == previewTexture)
        {
            previewImage.texture = null;
        }
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }

    private void ResolveReferences()
    {
        modelRoot ??= transform.Find("PageFourModel");
        Transform content = transform.Find("PageFourContent");
        previewImage ??= content != null
            ? content.Find("PageFourPreviewCard/PreviewImage")?.GetComponent<RawImage>()
            : null;
        playButton ??= content != null ? content.Find("PlayButton")?.GetComponent<Button>() : null;
        cursorObject ??= content != null ? content.Find("CursorObject") as RectTransform : null;
        if (cursorObject != null)
        {
            cursorObject.sizeDelta = new Vector2(25.2f, 25.2f);
        }
        cursorLabel = cursorObject != null ? cursorObject.GetComponent<TextMeshProUGUI>() : null;
        cursorImage ??= cursorObject != null ? cursorObject.GetComponentInChildren<RawImage>(true) : null;
        if (cursorObject != null && cursorImage == null && handIconsTexture != null)
        {
            GameObject handImage = new GameObject("HandImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            handImage.transform.SetParent(cursorObject, false);
            RectTransform handRect = handImage.GetComponent<RectTransform>();
            handRect.anchorMin = Vector2.zero;
            handRect.anchorMax = Vector2.one;
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;
            cursorImage = handImage.GetComponent<RawImage>();
            cursorImage.texture = handIconsTexture;
            cursorImage.raycastTarget = false;
            if (cursorLabel != null)
            {
                cursorLabel.enabled = false;
            }
        }
    }

    private void CreateFallbackSceneObjects()
    {
        if (modelRoot == null && modelPrefab != null)
        {
            GameObject model = Instantiate(modelPrefab, transform);
            model.name = "PageFourModel";
            model.transform.localPosition = new Vector3(647f, 0.66335f, -334f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 773.7875f;
            modelRoot = model.transform;
        }

        if (playButton != null && cursorObject != null)
        {
            return;
        }

        RectTransform page = transform as RectTransform;
        GameObject contentObject = new GameObject("PageFourContent", typeof(RectTransform));
        contentObject.transform.SetParent(page, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        CreateRuntimeText(content, "PageFourTitle", "Hướng dẫn thao tác cắm dây", 48f,
            new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 76f));

        GameObject buttonObject = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(content, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 62f);
        buttonRect.sizeDelta = new Vector2(180f, 60f);
        buttonObject.GetComponent<Image>().color = Color.white;
        playButton = buttonObject.GetComponent<Button>();
        TextMeshProUGUI playLabel = CreateRuntimeText(buttonRect, "Label", "▶  Play", 30f,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(164f, 52f));
        playLabel.raycastTarget = false;

        if (handIconsTexture != null)
        {
            GameObject cursorImageObject = new GameObject("CursorObject", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            cursorImageObject.transform.SetParent(content, false);
            cursorObject = cursorImageObject.GetComponent<RectTransform>();
            cursorObject.anchorMin = new Vector2(0.5f, 0.5f);
            cursorObject.anchorMax = new Vector2(0.5f, 0.5f);
            cursorObject.sizeDelta = new Vector2(25.2f, 25.2f);
            cursorImage = cursorImageObject.GetComponent<RawImage>();
            cursorImage.texture = handIconsTexture;
            cursorImage.raycastTarget = false;
        }
        else
        {
            TextMeshProUGUI cursorText = CreateRuntimeText(content, "CursorObject", "↖", 62f,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 74f));
            cursorText.fontStyle = FontStyles.Bold;
            cursorText.raycastTarget = false;
            cursorObject = cursorText.rectTransform;
            cursorLabel = cursorText;
        }
    }

    private static TextMeshProUGUI CreateRuntimeText(RectTransform parent, string name, string value, float size,
        Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        return text;
    }

    public void PlayTutorial()
    {
        if (!isPlaying)
        {
            if (cursorObject != null)
            {
                cursorObject.gameObject.SetActive(true);
            }
            StartCoroutine(PlaySequence());
        }
    }

    private IEnumerator PlaySequence()
    {
        isPlaying = true;
        if (playButton != null)
        {
            playButton.interactable = false;
        }

        SetTutorialWireVisible(false);
        cursorTarget = null;
        yield return MoveCursorBetween(cursorIdlePosition, socketA, cursorApproachDuration);
        yield return AnimateClick();
        yield return MoveCursorBetween(socketA, socketB, cursorTransferDuration);
        yield return AnimateClick();

        SetTutorialWireVisible(true);
        yield return new WaitForSeconds(cursorReleaseDuration);
        yield return MoveCursorBetween(socketB, cursorIdlePosition, cursorTransferDuration);
        yield return new WaitForSeconds(resetDelay);

        ResetTutorial();
        SetTutorialWireVisible(false);
        isPlaying = false;
        if (playButton != null)
        {
            playButton.interactable = true;
        }
    }

    private IEnumerator AnimateClick()
    {
        SetCursorHolding(true);
        yield return new WaitForSeconds(0.22f);
        SetCursorHolding(false);
        yield return new WaitForSeconds(0.18f);
    }

    private IEnumerator MovePlugWithCursor(Transform plug, Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation)
    {
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            plug.position = Vector3.Lerp(from, to, t);
            plug.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }
        plug.position = to;
        plug.rotation = toRotation;
    }

    private IEnumerator MoveCursorBetween(Vector3 from, Vector3 to, float duration)
    {
        cursorTarget = null;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetCursorFromWorld(Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration))));
            yield return null;
        }
    }

    private void ResetTutorial()
    {
        if (plugA != null)
        {
            plugA.position = plugAStart;
            plugA.rotation = plugAStartRotation;
        }
        if (plugB != null)
        {
            plugB.position = plugBStart;
            plugB.rotation = plugBStartRotation;
        }
        cursorTarget = null;
        SetCursorHolding(false);
        if (cursorObject != null)
        {
            SetCursorFromWorld(cursorIdlePosition);
            cursorObject.gameObject.SetActive(false);
        }
    }

    private void CreatePreviewCamera(Bounds bounds)
    {
        GameObject cameraObject = new GameObject("PageFourPreviewCamera", typeof(Camera));
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.white;
        previewCamera.fieldOfView = 36f;
        if (previewImage != null)
        {
            int textureWidth = Mathf.Max(16, Mathf.RoundToInt(previewImage.rectTransform.rect.width));
            int textureHeight = Mathf.Max(16, Mathf.RoundToInt(previewImage.rectTransform.rect.height));
            previewTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "PageFourPreviewTexture",
                antiAliasing = 4,
                filterMode = FilterMode.Bilinear
            };
            previewTexture.Create();
            previewCamera.targetTexture = previewTexture;
            previewCamera.rect = new Rect(0f, 0f, 1f, 1f);
            previewImage.texture = previewTexture;
        }
        else
        {
            previewCamera.rect = new Rect(0.196f, 0.235f, 0.61f, 0.583f);
        }
        previewCamera.depth = Camera.main != null ? Camera.main.depth + 1f : 1f;

        FramePreviewCamera(bounds);

        GameObject lightObject = new GameObject("PageFourPreviewLight", typeof(Light));
        lightObject.transform.SetParent(cameraObject.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(28f, -30f, 0f);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.44f;
        light.shadows = LightShadows.None;
    }

    private void FramePreviewCamera(Bounds bounds)
    {
        Quaternion rotation = Quaternion.identity;
        float previewWidth = previewTexture != null
            ? previewTexture.width
            : previewImage != null ? previewImage.rectTransform.rect.width : Screen.width * previewCamera.rect.width;
        float previewHeight = previewTexture != null
            ? previewTexture.height
            : previewImage != null ? previewImage.rectTransform.rect.height : Screen.height * previewCamera.rect.height;
        float aspect = Mathf.Max(0.6f, previewWidth / Mathf.Max(1f, previewHeight));
        // Chừa khoảng ngang bên phải cho dây nhưng vẫn giữ tâm camera tại tâm Board.
        float halfSize = Mathf.Max(bounds.extents.y * 0.92f, bounds.size.x * 0.98f / aspect);
        float distance = halfSize / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f + bounds.extents.z;
        Vector3 direction = rotation * Vector3.forward;
        previewCamera.transform.position = bounds.center - direction * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(bounds.center - previewCamera.transform.position, Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.magnitude * 1.5f);
        previewCamera.farClipPlane = distance + bounds.extents.magnitude * 2.5f;
    }

    private static Bounds CalculatePreviewBounds(Transform model)
    {
        string[] focusNames =
        {
            "PC3", "Encoder_Stand", "Rotor_Alt", "Rotor_Encoder", "Rotor_Main", "Rotor_Stand"
        };
        bool initialized = false;
        Bounds focusBounds = default;
        foreach (string focusName in focusNames)
        {
            Transform focus = FindDescendant(model, focusName);
            if (focus == null)
            {
                continue;
            }
            foreach (Renderer renderer in focus.GetComponentsInChildren<Renderer>(true))
            {
                if (!initialized)
                {
                    focusBounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    focusBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (initialized)
        {
            return focusBounds;
        }

        Transform board = FindDescendant(model, "Board");
        return board != null ? CalculateBounds(board) : CalculateBounds(model);
    }

    private void ResolveSocketTargets(Bounds bounds)
    {
        cursorIdlePosition = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, 0.12f),
            Mathf.Lerp(bounds.min.y, bounds.max.y, 0.82f),
            bounds.min.z - Mathf.Max(0.002f, bounds.size.z * 0.01f));

        if (modelRoot != null)
        {
            socketA = modelRoot.TransformPoint(socketALocal);
            socketB = modelRoot.TransformPoint(socketBLocal);
            socketARotation = modelRoot.rotation;
            socketBRotation = modelRoot.rotation;
            return;
        }

        Debug.LogWarning("[PageFourWiringTutorial] Khong tim thay model bang de doi toa do o cam.");
        socketA = bounds.center;
        socketB = bounds.center;
        socketARotation = Quaternion.identity;
        socketBRotation = Quaternion.identity;
    }

    private void CreateConnectedWirePreview()
    {
        connectedWireInstance = Instantiate(connectedWirePrefab, modelRoot, false);
        connectedWireInstance.name = "TutorialConnectedWire";
        connectedWireInstance.transform.localPosition = Vector3.zero;
        connectedWireInstance.transform.localRotation = Quaternion.identity;
        connectedWireInstance.transform.localScale = Vector3.one;
        SetLayerRecursively(connectedWireInstance, WireOverlayLayer);

        foreach (Collider collider in connectedWireInstance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Renderer renderer in connectedWireInstance.GetComponentsInChildren<Renderer>(true))
            renderer.sortingOrder = 5000;

        connectedWireInstance.SetActive(false);
    }

    private void CreateDemoWire(Bounds bounds)
    {
        float wireLength = bounds.size.x * 0.38f;
        float y = bounds.center.y - bounds.size.y * 0.28f;
        float z = bounds.min.z - Mathf.Max(0.006f, bounds.size.z * 0.02f);
        float originalLeftX = bounds.max.x + bounds.size.x * 0.05f;
        plugAStart = new Vector3(bounds.max.x + bounds.size.x * 0.17f, y, z);
        plugBStart = new Vector3(originalLeftX + wireLength, y, z);

        GameObject wire = new GameObject("TutorialWire", typeof(LineRenderer));
        wire.transform.SetParent(visualRoot.transform, true);
        wire.layer = WireOverlayLayer;
        wireLine = wire.GetComponent<LineRenderer>();
        wireLine.useWorldSpace = true;
        wireLine.positionCount = 20;
        wireLine.startWidth = bounds.size.x * 0.009f;
        wireLine.endWidth = wireLine.startWidth;
        wireLine.numCapVertices = 4;
        wireLine.numCornerVertices = 4;
        wireLine.sharedMaterial = wireMaterial;
        wireLine.startColor = Color.white;
        wireLine.endColor = Color.white;
        wireLine.sortingOrder = 5000;
        wire.AddComponent<WireLineAlwaysOnTop>();

        plugA = CreateJack("TutorialJackA", plugAStart, bounds.size.x * 0.085f, false);
        plugB = CreateJack("TutorialJackB", plugBStart, bounds.size.x * 0.085f, true);
        plugAStartRotation = plugA.rotation;
        plugBStartRotation = plugB.rotation;
    }

    private void SetTutorialWireVisible(bool visible)
    {
        if (connectedWireInstance != null)
        {
            connectedWireInstance.SetActive(visible);
            return;
        }

        if (wireLine != null)
        {
            wireLine.gameObject.SetActive(visible);
        }
        if (plugA != null)
        {
            plugA.gameObject.SetActive(visible);
        }
        if (plugB != null)
        {
            plugB.gameObject.SetActive(visible);
        }
    }

    private void CreateWireOverlayCamera()
    {
        int wireMask = 1 << WireOverlayLayer;
        previewCamera.cullingMask &= ~wireMask;

        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.cullingMask &= ~wireMask;
        }

        GameObject cameraObject = new GameObject("PageFourWireOverlayCamera", typeof(Camera));
        cameraObject.transform.SetParent(previewCamera.transform, false);
        wireOverlayCamera = cameraObject.GetComponent<Camera>();
        wireOverlayCamera.CopyFrom(previewCamera);
        wireOverlayCamera.targetTexture = null;
        wireOverlayCamera.cullingMask = wireMask;

        UniversalAdditionalCameraData previewCameraData = previewCamera.GetUniversalAdditionalCameraData();
        UniversalAdditionalCameraData overlayCameraData = wireOverlayCamera.GetUniversalAdditionalCameraData();
        previewCameraData.renderType = CameraRenderType.Base;
        overlayCameraData.renderType = CameraRenderType.Overlay;
        overlayCameraData.renderPostProcessing = false;
        previewCameraData.cameraStack.Add(wireOverlayCamera);
    }

    private Transform CreateJack(string name, Vector3 position, float targetLength, bool opposite)
    {
        GameObject jack = Instantiate(jack35Prefab);
        jack.name = name;
        jack.transform.SetParent(visualRoot.transform, true);
        SetLayerRecursively(jack, WireOverlayLayer);
        jack.transform.position = position;
        jack.transform.rotation = opposite
            ? Quaternion.Euler(0f, 0f, -90f)
            : Quaternion.Euler(0f, 0f, 90f);
        Bounds bounds = CalculateBounds(jack.transform);
        float length = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (length > 0.00001f)
        {
            jack.transform.localScale *= targetLength / length;
        }
        foreach (Renderer renderer in jack.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sortingOrder = 5000;
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length > 0 && jackBodyMaterial != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = jackBodyMaterial;
                }
                renderer.sharedMaterials = materials;
            }
        }
        return jack.transform;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void UpdateWire()
    {
        if (wireLine == null || plugA == null || plugB == null)
        {
            return;
        }
        Vector3 a = plugA.position;
        Vector3 b = plugB.position;
        float sag = 0f;
        for (int i = 0; i < wireLine.positionCount; i++)
        {
            float t = i / (wireLine.positionCount - 1f);
            Vector3 point = Vector3.Lerp(a, b, t);
            point.y -= Mathf.Sin(t * Mathf.PI) * sag;
            wireLine.SetPosition(i, point);
        }
    }

    private void UpdateCursorPosition()
    {
        if (cursorTarget != null)
        {
            SetCursorFromWorld(cursorTarget.position);
        }
    }

    private void SetCursorFromWorld(Vector3 worldPosition)
    {
        if (cursorObject == null || previewCamera == null)
        {
            return;
        }
        Vector3 screen = previewCamera.WorldToScreenPoint(worldPosition);
        if (previewImage != null && previewTexture != null)
        {
            Vector3 viewport = previewCamera.WorldToViewportPoint(worldPosition);
            Rect imageBounds = previewImage.rectTransform.rect;
            Vector3 imageLocal = new Vector3(
                Mathf.Lerp(imageBounds.xMin, imageBounds.xMax, viewport.x),
                Mathf.Lerp(imageBounds.yMin, imageBounds.yMax, viewport.y),
                0f);
            Vector3 imageWorld = previewImage.rectTransform.TransformPoint(imageLocal);
            cursorObject.position = new Vector3(imageWorld.x, imageWorld.y, cursorObject.position.z);
            return;
        }
        RectTransform pageRect = transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screen, null, out Vector2 local))
        {
            cursorObject.anchoredPosition = local;
        }
    }

    private void SetCursorHolding(bool holding)
    {
        if (cursorImage != null)
        {
            cursorImage.uvRect = holding
                ? new Rect(0.5f, 0f, 0.5f, 1f)
                : new Rect(0f, 0f, 0.5f, 1f);
            cursorImage.color = Color.black;
            cursorImage.rectTransform.localScale = holding ? Vector3.one * 0.95f : Vector3.one;
            return;
        }
        if (cursorLabel == null)
        {
            return;
        }
        cursorLabel.text = holding ? "●\nGIỮ" : "↖";
        cursorLabel.fontSize = holding ? 25f : 62f;
        cursorLabel.color = holding ? new Color(0.05f, 0.42f, 0.72f, 1f) : new Color(0.05f, 0.1f, 0.16f, 1f);
        cursorLabel.rectTransform.localScale = holding ? Vector3.one * 0.86f : Vector3.one;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(child.name, objectName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }
        return null;
    }

    private void HandleResolutionChange()
    {
        if (previewCamera == null || Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        FramePreviewCamera(boardBounds);
        if (wireOverlayCamera != null)
        {
            wireOverlayCamera.rect = previewCamera.rect;
        }
        if (!isPlaying)
        {
            SetCursorFromWorld(cursorIdlePosition);
        }
    }

    private static Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }
}
