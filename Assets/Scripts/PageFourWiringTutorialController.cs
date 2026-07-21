using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PageFourWiringTutorialController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform cursorObject;
    [SerializeField] private RawImage cursorImage;
    [SerializeField] private Texture handIconsTexture;
    [SerializeField] private GameObject jack35Prefab;
    [SerializeField] private Material wireMaterial;
    [SerializeField] private Material jackBodyMaterial;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 1.35f;
    [SerializeField] private float cursorApproachDuration = 1f;
    [SerializeField] private float cursorTransferDuration = 0.8f;
    [SerializeField] private float cursorReleaseDuration = 1f;
    [SerializeField] private float resetDelay = 1f;
    [SerializeField] private Vector2 socketANormalized = new Vector2(0.332f, 0.295f);
    [SerializeField] private Vector2 socketBNormalized = new Vector2(0.716f, 0.322f);

    private Camera previewCamera;
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
    private Bounds boardBounds;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private IEnumerator Start()
    {
        yield return null;
        ResolveReferences();
        CreateFallbackSceneObjects();
        if (modelRoot == null || jack35Prefab == null)
        {
            Debug.LogError("[PageFourWiringTutorial] Thiếu model hoặc Jack 3.5mm.");
            yield break;
        }

        visualRoot = new GameObject("PageFourVisualRoot");
        modelRoot.SetParent(visualRoot.transform, true);
        Transform board = FindDescendant(modelRoot, "Board");
        boardBounds = board != null ? CalculateBounds(board) : CalculateBounds(modelRoot);
        CreatePreviewCamera(boardBounds);
        ResolveSocketTargets(boardBounds);
        CreateDemoWire(boardBounds);
        playButton?.onClick.AddListener(PlayTutorial);
        ResetTutorial();
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
        if (visualRoot != null)
        {
            Destroy(visualRoot);
        }
    }

    private void ResolveReferences()
    {
        modelRoot ??= transform.Find("PageFourModel");
        Transform content = transform.Find("PageFourContent");
        playButton ??= content != null ? content.Find("PlayButton")?.GetComponent<Button>() : null;
        cursorObject ??= content != null ? content.Find("CursorObject") as RectTransform : null;
        if (cursorObject != null)
        {
            cursorObject.sizeDelta = new Vector2(36f, 36f);
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
            cursorObject.sizeDelta = new Vector2(36f, 36f);
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

        cursorTarget = null;
        yield return MoveCursorBetween(cursorIdlePosition, plugA.position, cursorApproachDuration);
        cursorTarget = plugA;
        SetCursorHolding(true);
        yield return MovePlugWithCursor(plugA, plugA.position, socketA, plugA.rotation, socketARotation);
        SetCursorHolding(false);
        yield return new WaitForSeconds(0.2f);
        yield return MoveCursorBetween(plugA.position, plugB.position, cursorTransferDuration);
        cursorTarget = plugB;
        SetCursorHolding(true);
        yield return MovePlugWithCursor(plugB, plugB.position, socketB, plugB.rotation, socketBRotation);
        SetCursorHolding(false);
        cursorTarget = null;
        yield return MoveCursorBetween(plugB.position, cursorIdlePosition, cursorReleaseDuration);
        yield return new WaitForSeconds(resetDelay);

        ResetTutorial();
        isPlaying = false;
        if (playButton != null)
        {
            playButton.interactable = true;
        }
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
            cursorObject.gameObject.SetActive(true);
            SetCursorFromWorld(cursorIdlePosition);
        }
    }

    private void CreatePreviewCamera(Bounds bounds)
    {
        GameObject cameraObject = new GameObject("PageFourPreviewCamera", typeof(Camera));
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        previewCamera.fieldOfView = 36f;
        previewCamera.rect = new Rect(0.18f, 0.15f, 0.64f, 0.72f);
        previewCamera.depth = Camera.main != null ? Camera.main.depth + 1f : 1f;

        FramePreviewCamera(bounds);

        GameObject lightObject = new GameObject("PageFourPreviewLight", typeof(Light));
        lightObject.transform.SetParent(cameraObject.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(28f, -30f, 0f);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.55f;
        light.shadows = LightShadows.None;
    }

    private void FramePreviewCamera(Bounds bounds)
    {
        Quaternion rotation = Quaternion.identity;
        float aspect = Mathf.Max(0.6f, (Screen.width * previewCamera.rect.width) / Mathf.Max(1f, Screen.height * previewCamera.rect.height));
        // Chừa khoảng ngang bên phải cho dây nhưng vẫn giữ tâm camera tại tâm Board.
        float halfSize = Mathf.Max(bounds.extents.y * 0.92f, bounds.size.x * 0.98f / aspect);
        float distance = halfSize / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f + bounds.extents.z;
        Vector3 direction = rotation * Vector3.forward;
        previewCamera.transform.position = bounds.center - direction * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(bounds.center - previewCamera.transform.position, Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.magnitude * 1.5f);
        previewCamera.farClipPlane = distance + bounds.extents.magnitude * 2.5f;
    }

    private void ResolveSocketTargets(Bounds bounds)
    {
        cursorIdlePosition = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, 0.12f),
            Mathf.Lerp(bounds.min.y, bounds.max.y, 0.82f),
            bounds.min.z - Mathf.Max(0.002f, bounds.size.z * 0.01f));

        Transform socket1 = FindDescendant(modelRoot, "Socket1");
        Transform socket2 = FindDescendant(modelRoot, "Socket2");
        socket1 ??= FindDescendant(transform, "Socket1");
        socket2 ??= FindDescendant(transform, "Socket2");
        if (socket1 != null && socket2 != null)
        {
            socketA = socket1.position;
            socketB = socket2.position;
            socketARotation = socket1.rotation;
            socketBRotation = socket2.rotation;
            return;
        }

        Debug.LogWarning("[PageFourWiringTutorial] Không tìm thấy Socket1/Socket2, dùng tọa độ dự phòng.");
        float z = bounds.min.z - Mathf.Max(0.002f, bounds.size.z * 0.01f);
        socketA = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, socketANormalized.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, socketANormalized.y),
            z);
        socketB = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, socketBNormalized.x),
            Mathf.Lerp(bounds.min.y, bounds.max.y, socketBNormalized.y),
            z);
        socketARotation = Quaternion.identity;
        socketBRotation = Quaternion.identity;
    }

    private void CreateDemoWire(Bounds bounds)
    {
        float wireLength = bounds.size.x * 0.38f;
        float y = bounds.center.y - bounds.size.y * 0.28f;
        float z = bounds.min.z - Mathf.Max(0.006f, bounds.size.z * 0.02f);
        plugAStart = new Vector3(bounds.max.x + bounds.size.x * 0.05f, y, z);
        plugBStart = plugAStart + Vector3.right * wireLength;

        GameObject wire = new GameObject("TutorialWire", typeof(LineRenderer));
        wire.transform.SetParent(visualRoot.transform, true);
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

        plugA = CreateJack("TutorialJackA", plugAStart, bounds.size.x * 0.085f, false);
        plugB = CreateJack("TutorialJackB", plugBStart, bounds.size.x * 0.085f, true);
        plugAStartRotation = plugA.rotation;
        plugBStartRotation = plugB.rotation;
    }

    private Transform CreateJack(string name, Vector3 position, float targetLength, bool opposite)
    {
        GameObject jack = Instantiate(jack35Prefab);
        jack.name = name;
        jack.transform.SetParent(visualRoot.transform, true);
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
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length > 0 && jackBodyMaterial != null)
            {
                materials[0] = jackBodyMaterial;
                renderer.sharedMaterials = materials;
            }
        }
        return jack.transform;
    }

    private void UpdateWire()
    {
        if (wireLine == null || plugA == null || plugB == null)
        {
            return;
        }
        Vector3 a = plugA.position;
        Vector3 b = plugB.position;
        float sag = Vector3.Distance(a, b) * 0.08f;
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
