using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PageTwoPartsController : MonoBehaviour
{
    private sealed class PartEntry
    {
        public string Label;
        public string[] TransformNames;

        public PartEntry(string label, params string[] transformNames)
        {
            Label = label;
            TransformNames = transformNames;
        }
    }

    private readonly PartEntry[] parts =
    {
        new PartEntry("PLC Mitsubishi FX3U", "PC3"),
        new PartEntry("HMI Mitsubishi GOT1000", "Display"),
        new PartEntry("Động cơ BLDC Servo", "rotor_alt", "rotor_main", "rotor_stand"),
        new PartEntry("Encoder", "rotor_encoder"),
        new PartEntry("Aptomat", "Switch"),
        new PartEntry("Dây cắm", "Page2DemoWires"),
        new PartEntry("Bảng cắm dây", "Board")
    };

    private readonly string[] partDescriptions =
    {
        "Trung tâm điều khiển; nhận dữ liệu từ HMI, tính toán tần số/số xung, phát xung qua Y0 và điều khiển hướng qua Y1. PLC cũng đọc tín hiệu Encoder qua bộ đếm tốc độ cao.",
        "Giao diện nhập tốc độ, vị trí/góc quay, chọn chiều quay, START/STOP/RESET và hiển thị trạng thái vận hành theo thời gian thực.",
        "Động cơ đồng bộ ba pha kích từ bằng nam châm vĩnh cửu, gồm roto và stato, được tích hợp hệ thống cảm biến Hall.",
        "Tạo xung phản hồi pha A/B để PLC xác định tốc độ, chiều quay và vị trí thực tế của trục động cơ.",
        "Aptomat và cầu chì bảo vệ hệ thống khỏi quá dòng, ngắn mạch.",
        "Phục vụ việc đấu nối, có 3 màu đỏ, vàng và đen.",
        "Bảng với các lỗ cắm phục vụ cho việc đấu nối mạch điện. Dùng các dây cắm để đấu nối 2 lỗ cắm với nhau."
    };

    [SerializeField] private float animationDuration = 0.55f;
    [SerializeField] private Color accentColor = new Color(0.08f, 0.34f, 0.58f, 1f);

    private Transform modelRoot;
    private Camera previewCamera;
    [Header("Scene UI references")]
    [SerializeField] private RectTransform buttonList;
    [SerializeField] private TextMeshProUGUI selectedLabel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button listButton;
    [SerializeField] private Button[] partButtons;

    [Header("Existing wire assets")]
    [SerializeField] private GameObject jack35Prefab;
    [SerializeField] private Material[] wireMaterials;
    [SerializeField] private Material[] jackBodyMaterials;

    private RectTransform selectedLabelRect;
    private RectTransform descriptionRect;
    private Bounds fullModelBounds;
    private Coroutine transition;
    private Vector3 cameraStartPosition;
    private float cameraStartSize;
    private Vector3 cameraTargetPosition;
    private float cameraTargetSize;
    private readonly Quaternion previewRotation = Quaternion.Euler(8f, -12f, 0f);
    private readonly List<GameObject> highlightObjects = new List<GameObject>();
    private readonly List<Material> runtimeWireMaterials = new List<Material>();
    private Material highlightMaterial;
    private Vector3 orbitCenter;
    private Vector3 targetOrbitCenter;
    private float orbitDistance;
    private float targetOrbitDistance;
    private float orbitYaw = -12f;
    private float orbitPitch = 8f;
    private bool isOrbiting;
    private Bounds activeViewBounds;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        modelRoot = FindModelRoot();
        ResolveInterfaceReferences();
        BindInterfaceEvents();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (modelRoot == null)
        {
            Debug.LogWarning("[PageTwoPartsController] Không tìm thấy model 3d_Thay_Tien_1 trong Trang 2.");
            yield break;
        }

        modelRoot.SetParent(null, true);
        CreateDemoWires();
        fullModelBounds = CalculateBounds(new[] { modelRoot });
        CreatePreviewCamera();
        FocusImmediate(fullModelBounds, 1.18f);
        previewCamera.enabled = gameObject.activeInHierarchy;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void OnEnable()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = true;
        }
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = false;
        }
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        HandleResolutionChange();
        HandleOrbitInput();
    }

    private void OnDestroy()
    {
        if (previewCamera != null)
        {
            Destroy(previewCamera.gameObject);
        }

        ClearHighlight();
        if (highlightMaterial != null)
        {
            Destroy(highlightMaterial);
        }
        foreach (Material material in runtimeWireMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
        runtimeWireMaterials.Clear();

    }

    private void ResolveInterfaceReferences()
    {
        Transform content = transform.Find("PageTwoContent");
        if (content == null)
        {
            Debug.LogError("[PageTwoPartsController] Thiếu PageTwoContent trong hierarchy Trang 2.");
            return;
        }

        buttonList ??= content.Find("PartButtonList") as RectTransform;
        selectedLabel ??= content.Find("SelectedPartLabel")?.GetComponent<TextMeshProUGUI>();
        descriptionText ??= content.Find("PartDescriptionText")?.GetComponent<TextMeshProUGUI>();
        listButton ??= content.Find("ShowPartListButton")?.GetComponent<Button>();
        Transform obsoleteViewportCover = content.Find("ModelViewportBorder");
        if (obsoleteViewportCover != null)
        {
            obsoleteViewportCover.gameObject.SetActive(false);
        }
        if (partButtons == null || partButtons.Length != parts.Length)
        {
            partButtons = new Button[parts.Length];
            for (int i = 0; i < partButtons.Length; i++)
            {
                partButtons[i] = content.Find($"PartButtonList/PartButton_{i}")?.GetComponent<Button>();
            }
        }
        selectedLabelRect = selectedLabel != null ? selectedLabel.rectTransform : null;
        descriptionRect = descriptionText != null ? descriptionText.rectTransform : null;
    }

    private void BindInterfaceEvents()
    {
        for (int i = 0; i < partButtons.Length; i++)
        {
            if (partButtons[i] == null)
            {
                continue;
            }
            int index = i;
            partButtons[i].onClick.AddListener(() => SelectPart(index, partButtons[index].transform as RectTransform));
        }
        listButton?.onClick.AddListener(ShowPartList);
    }

    private Transform FindModelRoot()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string normalizedName = child.name.ToLowerInvariant();
            if (normalizedName.Contains("3d_thay_tien_1"))
            {
                return child;
            }
        }

        return null;
    }

    private void BuildInterface()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            return;
        }

        CreateText(root, "PageTwoTitle", "Các thành phần chính của mô hình", 48f,
            new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1100f, 80f), TextAlignmentOptions.Center, FontStyles.Normal);

        buttonList = CreateRect(root, "PartButtonList", new Vector2(0f, 0.5f), new Vector2(180f, -18f), new Vector2(460f, 720f));
        for (int i = 0; i < parts.Length; i++)
        {
            int index = i;
            Button button = CreatePartButton(buttonList, parts[i].Label, i);
            button.onClick.AddListener(() => SelectPart(index, button.transform as RectTransform));
        }

        selectedLabel = CreateText(root, "SelectedPartLabel", string.Empty, 42f,
            new Vector2(0f, 0.5f), new Vector2(145f, 170f), new Vector2(700f, 120f), TextAlignmentOptions.Left, FontStyles.Bold);
        selectedLabelRect = selectedLabel.rectTransform;
        selectedLabel.color = accentColor;
        selectedLabel.gameObject.SetActive(false);

        listButton = CreateButton(root, "ShowPartListButton", "‹  Danh sách", new Vector2(0f, 1f), new Vector2(48f, -138f), new Vector2(190f, 54f));
        listButton.onClick.AddListener(ShowPartList);
        listButton.gameObject.SetActive(false);

    }

    private Button CreatePartButton(RectTransform parent, string label, int index)
    {
        Button button = CreateButton(parent, "PartButton_" + index, label, new Vector2(0.5f, 1f), new Vector2(0f, -58f - index * 105f), new Vector2(430f, 82f));
        button.GetComponentInChildren<TextMeshProUGUI>().fontSize = label.Length > 21 ? 29f : 34f;
        return button;
    }

    private Button CreateButton(RectTransform parent, string objectName, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, anchor.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.96f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.87f, 0.94f, 0.98f, 1f);
        colors.pressedColor = new Color(0.72f, 0.86f, 0.94f, 1f);
        button.colors = colors;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.1f, 0.13f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI text = CreateText(rect, "Label", label, 34f, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(24f, 12f), TextAlignmentOptions.Center, FontStyles.Normal);
        text.raycastTarget = false;
        return button;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string objectName, string value, float fontSize, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, anchor.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.black;
        text.enableWordWrapping = true;
        return text;
    }

    private static RectTransform CreateRect(RectTransform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private void SelectPart(int index, RectTransform sourceButton)
    {
        if (modelRoot == null || index < 0 || index >= parts.Length)
        {
            return;
        }

        List<Transform> targets = FindNamedTransforms(parts[index].TransformNames);
        if (targets.Count == 0)
        {
            Debug.LogWarning($"[PageTwoPartsController] Không tìm thấy bộ phận: {parts[index].Label}");
            return;
        }

        selectedLabel.text = parts[index].Label;
        selectedLabel.gameObject.SetActive(true);
        if (descriptionText != null)
        {
            descriptionText.text = partDescriptions[index];
            descriptionText.gameObject.SetActive(true);
        }
        Vector3 worldStart = sourceButton.TransformPoint(sourceButton.rect.center);
        Vector2 localStart;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, RectTransformUtility.WorldToScreenPoint(null, worldStart), null, out localStart);
        localStart.x += (transform as RectTransform).rect.width * 0.5f;
        selectedLabelRect.anchoredPosition = localStart;
        selectedLabelRect.localScale = Vector3.one * 0.82f;
        if (descriptionRect != null)
        {
            descriptionRect.anchoredPosition = localStart + new Vector2(0f, -72f);
            descriptionRect.localScale = Vector3.one * 0.82f;
        }

        buttonList.gameObject.SetActive(false);
        listButton.gameObject.SetActive(true);
        Bounds targetBounds = CalculateBounds(targets);
        if (index != 5)
        {
            ShowHighlight(targets, targetBounds);
        }
        else
        {
            ClearHighlight();
        }
        SetCameraTarget(targetBounds, 1.45f);

        if (transition != null)
        {
            StopCoroutine(transition);
        }
        transition = StartCoroutine(AnimateSelection(new Vector2(145f, 170f)));
    }

    private IEnumerator AnimateSelection(Vector2 labelTarget)
    {
        Vector2 labelStart = selectedLabelRect.anchoredPosition;
        Vector3 scaleStart = selectedLabelRect.localScale;
        Vector2 descriptionStart = descriptionRect != null ? descriptionRect.anchoredPosition : Vector2.zero;
        Vector3 descriptionScaleStart = descriptionRect != null ? descriptionRect.localScale : Vector3.one;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
            selectedLabelRect.anchoredPosition = Vector2.Lerp(labelStart, labelTarget, t);
            selectedLabelRect.localScale = Vector3.Lerp(scaleStart, Vector3.one, t);
            if (descriptionRect != null)
            {
                descriptionRect.anchoredPosition = Vector2.Lerp(descriptionStart, new Vector2(145f, 92f), t);
                descriptionRect.localScale = Vector3.Lerp(descriptionScaleStart, Vector3.one, t);
            }
            previewCamera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, t);
            previewCamera.orthographicSize = Mathf.Lerp(cameraStartSize, cameraTargetSize, t);
            yield return null;
        }
        transition = null;
        ApplyOrbitTarget();
    }

    private void ShowPartList()
    {
        if (transition != null)
        {
            StopCoroutine(transition);
        }
        selectedLabel.gameObject.SetActive(false);
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }
        listButton.gameObject.SetActive(false);
        buttonList.gameObject.SetActive(true);
        ClearHighlight();
        SetCameraTarget(fullModelBounds, 1.18f);
        transition = StartCoroutine(AnimateCameraOnly());
    }

    private IEnumerator AnimateCameraOnly()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
            previewCamera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, t);
            previewCamera.orthographicSize = Mathf.Lerp(cameraStartSize, cameraTargetSize, t);
            yield return null;
        }
        transition = null;
        ApplyOrbitTarget();
    }

    private List<Transform> FindNamedTransforms(IEnumerable<string> names)
    {
        HashSet<string> wanted = new HashSet<string>(names, System.StringComparer.OrdinalIgnoreCase);
        List<Transform> result = new List<Transform>();
        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (wanted.Contains(child.name))
            {
                result.Add(child);
            }
        }
        return result;
    }

    private void CreatePreviewCamera()
    {
        GameObject cameraObject = new GameObject("PageTwoPreviewCamera", typeof(Camera));
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 34f;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.94f, 0.96f, 0.98f, 1f);
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 5000f;
        previewCamera.transform.rotation = previewRotation;
        previewCamera.rect = new Rect(0.49f, 0.14f, 0.42f, 0.68f);
        Camera mainCamera = Camera.main;
        previewCamera.depth = mainCamera != null ? mainCamera.depth + 1f : 1f;

        GameObject lightObject = new GameObject("PageTwoPreviewLight", typeof(Light));
        lightObject.transform.SetParent(cameraObject.transform, false);
        lightObject.transform.localRotation = Quaternion.Euler(28f, -32f, 0f);
        Light previewLight = lightObject.GetComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.color = new Color(1f, 0.96f, 0.9f, 1f);
        previewLight.intensity = 0.78f;
        previewLight.shadows = LightShadows.None;
    }

    private void FocusImmediate(Bounds bounds, float padding)
    {
        activeViewBounds = bounds;
        previewCamera.transform.rotation = previewRotation;
        previewCamera.transform.position = CalculateCameraPosition(bounds, padding);
        UpdateClipPlanes(bounds, previewCamera.transform.position);
        orbitCenter = bounds.center;
        orbitDistance = Vector3.Distance(previewCamera.transform.position, orbitCenter);
    }

    private void SetCameraTarget(Bounds bounds, float padding)
    {
        activeViewBounds = bounds;
        cameraStartPosition = previewCamera.transform.position;
        cameraStartSize = previewCamera.orthographicSize;
        orbitYaw = -12f;
        orbitPitch = 8f;
        cameraTargetPosition = CalculateCameraPosition(bounds, padding);
        cameraTargetSize = cameraStartSize;
        targetOrbitCenter = bounds.center;
        targetOrbitDistance = Vector3.Distance(cameraTargetPosition, targetOrbitCenter);
        UpdateClipPlanes(bounds, cameraTargetPosition);
    }

    private Vector3 CalculateCameraPosition(Bounds bounds, float padding)
    {
        float distance = CalculateCameraDistance(bounds, padding);
        Vector3 viewDirection = previewRotation * Vector3.forward;
        return bounds.center - viewDirection * distance;
    }

    private float CalculateCameraDistance(Bounds bounds, float padding)
    {
        float viewportAspect = Mathf.Max(0.5f, (Screen.width * previewCamera.rect.width) / Mathf.Max(1f, Screen.height * previewCamera.rect.height));
        float framedHalfSize = Mathf.Max(bounds.extents.y, bounds.extents.x / viewportAspect);
        float distance = framedHalfSize / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        distance = distance * padding + bounds.extents.z;
        return Mathf.Max(distance, 0.1f);
    }

    private void HandleResolutionChange()
    {
        if (previewCamera == null || Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        orbitCenter = activeViewBounds.center;
        orbitDistance = CalculateCameraDistance(activeViewBounds, selectedLabel != null && selectedLabel.gameObject.activeSelf ? 1.45f : 1.18f);
        UpdateOrbitCamera();
        UpdateClipPlanes(activeViewBounds, previewCamera.transform.position);
    }

    private void ApplyOrbitTarget()
    {
        orbitCenter = targetOrbitCenter;
        orbitDistance = targetOrbitDistance;
        UpdateOrbitCamera();
    }

    private void HandleOrbitInput()
    {
        if (previewCamera == null || !previewCamera.enabled || transition != null)
        {
            return;
        }

        Rect pixelRect = previewCamera.pixelRect;
        bool pointerInside = pixelRect.Contains(Input.mousePosition);
        if (Input.GetMouseButtonDown(0) && pointerInside)
        {
            isOrbiting = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isOrbiting = false;
        }
        if (!isOrbiting)
        {
            return;
        }

        orbitYaw += Input.GetAxisRaw("Mouse X") * 4f;
        orbitPitch = Mathf.Clamp(orbitPitch - Input.GetAxisRaw("Mouse Y") * 4f, -75f, 75f);
        UpdateOrbitCamera();
    }

    private void UpdateOrbitCamera()
    {
        Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        previewCamera.transform.position = orbitCenter - rotation * Vector3.forward * orbitDistance;
        previewCamera.transform.rotation = Quaternion.LookRotation(orbitCenter - previewCamera.transform.position, Vector3.up);
    }

    private void ShowHighlight(IEnumerable<Transform> targets, Bounds targetBounds)
    {
        ClearHighlight();
        Shader shader = Shader.Find("DigitalTwin/PageTwoSelectionOutline");
        if (shader == null)
        {
            return;
        }
        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(shader);
            highlightMaterial.SetColor("_OutlineColor", new Color(0.05f, 0.72f, 1f, 1f));
        }
        highlightMaterial.SetFloat("_OutlineWidth", Mathf.Max(0.0005f, targetBounds.extents.magnitude * 0.018f));

        foreach (Transform target in targets)
        {
            foreach (MeshRenderer source in target.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                if (sourceFilter == null || sourceFilter.sharedMesh == null)
                {
                    continue;
                }
                GameObject outline = new GameObject("SelectionOutline", typeof(MeshFilter), typeof(MeshRenderer));
                outline.transform.SetParent(source.transform, false);
                outline.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                MeshRenderer outlineRenderer = outline.GetComponent<MeshRenderer>();
                outlineRenderer.sharedMaterial = highlightMaterial;
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                highlightObjects.Add(outline);
            }
        }
    }

    private void ClearHighlight()
    {
        foreach (GameObject highlight in highlightObjects)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        highlightObjects.Clear();
    }

    private void UpdateClipPlanes(Bounds bounds, Vector3 cameraPosition)
    {
        float distance = Vector3.Distance(cameraPosition, bounds.center);
        float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
        previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.5f);
        previewCamera.farClipPlane = Mathf.Max(previewCamera.nearClipPlane + 10f, distance + radius * 2.5f);
    }

    private static Bounds CalculateBounds(IEnumerable<Transform> roots)
    {
        bool hasBounds = false;
        Bounds result = new Bounds(Vector3.zero, Vector3.one);
        foreach (Transform root in roots)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                {
                    continue;
                }
                if (!hasBounds)
                {
                    result = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }
        }
        return result;
    }

    private void CreateDemoWires()
    {
        Bounds modelBounds = CalculateBounds(new[] { modelRoot });
        GameObject group = new GameObject("Page2DemoWires");
        group.transform.SetParent(modelRoot, true);
        float length = Mathf.Max(modelBounds.size.y * 0.22f, 0.08f);
        float spacing = Mathf.Max(modelBounds.size.x * 0.025f, 0.025f);
        Vector3 origin = new Vector3(modelBounds.max.x + spacing * 2f, modelBounds.center.y - spacing, modelBounds.center.z);
        for (int i = 0; i < 3; i++)
        {
            GameObject wire = new GameObject("DemoWire_" + (i + 1), typeof(LineRenderer));
            wire.transform.SetParent(group.transform, true);
            Vector3 start = origin + Vector3.down * spacing * 2.2f * i;
            Vector3 end = start + Vector3.right * length;
            LineRenderer line = wire.GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 18;
            line.startWidth = spacing * 0.5f;
            line.endWidth = spacing * 0.5f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = CreateWireBodyMaterial(i);
            // Giữ vertex color trắng để màu thân dây lấy chính xác từ material của Sy_scene.
            line.startColor = Color.white;
            line.endColor = Color.white;
            for (int p = 0; p < line.positionCount; p++)
            {
                float t = p / (line.positionCount - 1f);
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y -= Mathf.Sin(t * Mathf.PI) * spacing * 0.8f;
                line.SetPosition(p, point);
            }
            CreateJack(wire.transform, start, spacing * 3.2f, i, false);
            CreateJack(wire.transform, end, spacing * 3.2f, i, true);
        }
    }

    private Material CreateWireBodyMaterial(int colorIndex)
    {
        Material source = jackBodyMaterials != null && colorIndex < jackBodyMaterials.Length
            ? jackBodyMaterials[colorIndex]
            : null;
        Color bodyColor = Color.white;
        if (source != null)
        {
            if (source.HasProperty("_BaseColor"))
            {
                bodyColor = source.GetColor("_BaseColor");
            }
            else if (source.HasProperty("_Color"))
            {
                bodyColor = source.GetColor("_Color");
            }
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader) { name = $"Page2Wire_{colorIndex}" };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", bodyColor);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", bodyColor);
        }
        runtimeWireMaterials.Add(material);
        return material;
    }

    private void CreateJack(Transform parent, Vector3 position, float targetLength, int colorIndex, bool faceOpposite)
    {
        if (jack35Prefab == null)
        {
            Debug.LogError("[PageTwoPartsController] Chưa gán asset Assets/Jack 3.5mm.fbx.");
            return;
        }

        GameObject jack = Instantiate(jack35Prefab, parent);
        jack.name = faceOpposite ? "Jack35_B" : "Jack35_A";
        jack.transform.position = position;
        jack.transform.rotation = faceOpposite
            ? Quaternion.Euler(0f, 0f, -90f)
            : Quaternion.Euler(0f, 0f, 90f);

        Renderer[] renderers = jack.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = CalculateBounds(new[] { jack.transform });
        float currentLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (currentLength > 0.00001f)
        {
            jack.transform.localScale *= targetLength / currentLength;
        }
        if (jackBodyMaterials == null || colorIndex >= jackBodyMaterials.Length || jackBodyMaterials[colorIndex] == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length > 0)
            {
                materials[0] = jackBodyMaterials[colorIndex];
                renderer.sharedMaterials = materials;
            }
        }
    }
}
