using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    [Header("Danh sach day trong bai demo")]
    public List<WireBody> allWires = new List<WireBody>();

    [Tooltip("Tu tim tat ca WireBody trong scene moi lan cham diem.")]
    public bool autoFindWires = true;

    [Tooltip("Demo hien tai can dung 2 ket noi.")]
    public int requiredWiresCount = 2;

    [Header("Cac cap socket dung bat buoc")]
    [Tooltip("Khong phu thuoc ten object day. Chi can day noi dung cap socket nay la duoc tinh diem.")]
    public List<string> requiredSocketPairs = new List<string>
    {
        "Y0-Pin11",
        "Y1-Pin9"
    };

    [Header("Ten WireBody neu muon loc rieng")]
    [Tooltip("Co the de rong. Ban nay uu tien cham theo socket pair nen khong can dung ten day.")]
    public List<string> demoWireNames = new List<string>
    {
        "Wire_Body_Yellow",
        "Wire_04_Y1-Pin9"
    };

    [Header("UI can kich hoat khi cam dung")]
    public GameObject hmiPanel;
    public GameObject cameraStream;
    public bool createRuntimeOnOffHmi = true;

    [Header("Cham diem")]
    public float totalScore = 100f;
    [Tooltip("Tinh diem rieng cho moi WireBody co khai bao correctSocketA/B. Ho tro nhieu day dung chung socket hoac cung cap socket.")]
    public bool scoreEveryConfiguredWire = true;

    private readonly HashSet<WireBody> correctlyConnectedWires = new HashSet<WireBody>();
    private readonly HashSet<string> connectedConnectionKeys = new HashSet<string>();
    private readonly HashSet<string> requiredConnectionKeys = new HashSet<string>();

    private bool isUnlocked;
    private int lastLoggedCorrectCount = -1;
    private int lastLoggedWiresToCheck = -1;

    private GameObject runtimeHmiRoot;

    private PLCController plcController;
    private PLCController_v2 plcControllerV2;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        plcController = FindObjectOfType<PLCController>();
        plcControllerV2 = PLCController_v2.Instance != null
            ? PLCController_v2.Instance
            : FindObjectOfType<PLCController_v2>();

        BuildRequiredConnectionKeys();

        if (createRuntimeOnOffHmi)
            CreateRuntimeOnOffHmi();

        PrepareDemoWireList();
        LockSystem();
        EvaluateCircuit();
    }

    public void OnWireConnectedCorrectly(WireBody wire)
    {
        if (wire == null)
            return;

        if (!allWires.Contains(wire))
        {
            allWires.Add(wire);
            Debug.Log($"[Circuit] Auto add wire vao allWires: {wire.name}");
        }

        EvaluateCircuit();

        int correctCount = correctlyConnectedWires.Count;
        float scorePerWire = totalScore / Mathf.Max(1, requiredWiresCount);
        float currentScore = correctCount * scorePerWire;

        Debug.Log($"[Circuit] Dung day {wire.name} ({correctCount}/{requiredWiresCount}) - diem {Mathf.Round(currentScore)}/100");
    }

    public void EvaluateCircuit()
    {
        correctlyConnectedWires.Clear();
        connectedConnectionKeys.Clear();

        BuildRequiredConnectionKeys();

        if (autoFindWires)
            AddAllSceneWires();

        List<WireBody> wiresToEvaluate = new List<WireBody>();

        foreach (WireBody wire in allWires)
        {
            if (wire == null)
                continue;

            if (string.IsNullOrWhiteSpace(wire.correctSocketA) ||
                string.IsNullOrWhiteSpace(wire.correctSocketB))
                continue;

            if (!wiresToEvaluate.Contains(wire))
                wiresToEvaluate.Add(wire);
        }

        foreach (WireBody wire in wiresToEvaluate)
        {
            if (wire == null)
                continue;

            wire.RefreshConnectionState(logResult: true);

            if (wire.isFullyConnected && wire.isCorrect)
            {
                string answerKey = MakeConnectionKey(wire.correctSocketA, wire.correctSocketB);
                if (scoreEveryConfiguredWire || requiredConnectionKeys.Count == 0 || requiredConnectionKeys.Contains(answerKey))
                {
                    correctlyConnectedWires.Add(wire);
                    connectedConnectionKeys.Add(answerKey);
                }
                else
                {
                    Debug.Log($"[Circuit] Day {wire.name} dung nhung khong nam trong requiredSocketPairs: {answerKey}");
                }
            }
            else if (wire.isFullyConnected && !wire.isCorrect)
            {
                Debug.LogWarning($"[Circuit] Sai day {wire.name}: dang cam vao {DescribeWireSockets(wire)}, dap an {wire.correctSocketA}-{wire.correctSocketB}");
            }
        }

        int wiresToCheck = scoreEveryConfiguredWire
            ? Mathf.Max(1, wiresToEvaluate.Count)
            : Mathf.Max(1, requiredWiresCount);
        requiredWiresCount = wiresToCheck;
        int correctCount = correctlyConnectedWires.Count;

        if (lastLoggedCorrectCount != correctCount || lastLoggedWiresToCheck != wiresToCheck)
        {
            lastLoggedCorrectCount = correctCount;
            lastLoggedWiresToCheck = wiresToCheck;

            float scorePerWire = totalScore / Mathf.Max(1, requiredWiresCount);
            float currentScore = correctCount * scorePerWire;

            Debug.Log($"[Circuit] Tien do demo: {correctCount}/{wiresToCheck} ket noi, diem {Mathf.Round(currentScore)}/100");
            Debug.Log($"[Circuit] Required keys: {string.Join(", ", requiredConnectionKeys)}");
            Debug.Log($"[Circuit] Connected keys: {string.Join(", ", connectedConnectionKeys)}");
        }

        if (correctCount >= wiresToCheck && wiresToCheck > 0)
            UnlockSystem();
        else
            LockSystem();
    }

    private void BuildRequiredConnectionKeys()
    {
        requiredConnectionKeys.Clear();

        if (requiredSocketPairs == null)
            return;

        foreach (string pairRaw in requiredSocketPairs)
        {
            if (string.IsNullOrWhiteSpace(pairRaw))
                continue;

            string pair = pairRaw.Trim();

            string[] parts = pair.Split('-');

            if (parts.Length < 2)
            {
                Debug.LogWarning($"[Circuit] requiredSocketPairs sai format: {pair}. Dung kieu Y0-Pin11");
                continue;
            }

            string socketA = parts[0].Trim();
            string socketB = parts[1].Trim();

            string key = MakeConnectionKey(socketA, socketB);

            if (!string.IsNullOrEmpty(key))
                requiredConnectionKeys.Add(key);
        }

        requiredWiresCount = Mathf.Max(1, requiredConnectionKeys.Count);
    }

    private void AddAllSceneWires()
    {
        WireBody[] foundWires = FindObjectsByType<WireBody>(FindObjectsSortMode.None);

        foreach (WireBody wire in foundWires)
        {
            if (wire == null)
                continue;

            if (string.IsNullOrWhiteSpace(wire.correctSocketA) ||
                string.IsNullOrWhiteSpace(wire.correctSocketB))
                continue;

            if (!allWires.Contains(wire))
                allWires.Add(wire);
        }
    }

    private void PrepareDemoWireList()
    {
        BuildRequiredConnectionKeys();

        if (demoWireNames != null && demoWireNames.Count > 0)
        {
            WireBody[] foundWires = FindObjectsByType<WireBody>(FindObjectsSortMode.None);

            foreach (string wireNameRaw in demoWireNames)
            {
                if (string.IsNullOrWhiteSpace(wireNameRaw))
                    continue;

                string wireName = wireNameRaw.Trim();

                foreach (WireBody wire in foundWires)
                {
                    if (wire == null)
                        continue;

                    if (wire.name.Trim().Equals(wireName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!allWires.Contains(wire))
                            allWires.Add(wire);
                    }
                }
            }
        }

        if (autoFindWires)
            AddAllSceneWires();

        Debug.Log($"[Circuit] Demo yeu cau dung {requiredWiresCount} ket noi: {string.Join(", ", requiredConnectionKeys)}");
        Debug.Log($"[Circuit] Wires dang duoc scan: {string.Join(", ", allWires.ConvertAll(w => w != null ? w.name : "NULL"))}");
    }

    private string DescribeWireSockets(WireBody wire)
    {
        return wire != null ? wire.GetSocketSummary() : "missing-wire";
    }

    private static string MakeConnectionKey(string socketA, string socketB)
    {
        if (string.IsNullOrWhiteSpace(socketA) || string.IsNullOrWhiteSpace(socketB))
            return string.Empty;

        string a = socketA.Trim().ToUpperInvariant();
        string b = socketB.Trim().ToUpperInvariant();

        return string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
    }

    private void UnlockSystem()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;

        SetObjectAndParentsActive(hmiPanel, true);

        if (runtimeHmiRoot != null)
            runtimeHmiRoot.SetActive(true);

        if (cameraStream != null)
            cameraStream.SetActive(true);

        if (plcControllerV2 == null)
            plcControllerV2 = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindObjectOfType<PLCController_v2>();

        if (plcControllerV2 != null)
            plcControllerV2.SetRuntimeHmiVisible(true);

        Debug.Log("[Circuit] Da noi dung day demo. Mo HMI/Camera.");
    }

    private void LockSystem()
    {
        isUnlocked = false;

        if (hmiPanel != null)
            hmiPanel.SetActive(false);

        if (runtimeHmiRoot != null)
            runtimeHmiRoot.SetActive(false);

        if (cameraStream != null)
            cameraStream.SetActive(false);

        if (plcControllerV2 == null)
            plcControllerV2 = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindObjectOfType<PLCController_v2>();

        if (plcControllerV2 != null)
            plcControllerV2.SetRuntimeHmiVisible(false);
    }

    private void SetObjectAndParentsActive(GameObject target, bool active)
    {
        if (target == null)
            return;

        Transform current = target.transform;

        while (current != null)
        {
            current.gameObject.SetActive(active);
            current = current.parent;
        }
    }

    private void CreateRuntimeOnOffHmi()
    {
        if (runtimeHmiRoot != null)
            return;

        runtimeHmiRoot = new GameObject("Runtime_PLC_OnOff_HMI");

        Canvas canvas = runtimeHmiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = runtimeHmiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        runtimeHmiRoot.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(runtimeHmiRoot.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(360f, 150f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.9f);

        Text title = CreateRuntimeText(
            panel.transform,
            "Title",
            "PLC HMI",
            new Vector2(16f, -12f),
            new Vector2(328f, 30f),
            22,
            Color.white
        );

        title.fontStyle = FontStyle.Bold;

        CreateRuntimeText(
            panel.transform,
            "Hint",
            "ON/OFF qua PLCController_v2 neu co, fallback PLCController",
            new Vector2(16f, -44f),
            new Vector2(328f, 24f),
            13,
            new Color(0.8f, 0.86f, 0.92f)
        );

        Button onButton = CreateRuntimeButton(
            panel.transform,
            "ON_Button",
            "ON",
            new Vector2(16f, -88f),
            new Vector2(150f, 44f),
            new Color(0.02f, 0.48f, 0.16f, 1f)
        );

        Button offButton = CreateRuntimeButton(
            panel.transform,
            "OFF_Button",
            "OFF",
            new Vector2(194f, -88f),
            new Vector2(150f, 44f),
            new Color(0.74f, 0.08f, 0.08f, 1f)
        );

        onButton.onClick.AddListener(() =>
        {
            if (plcControllerV2 == null)
                plcControllerV2 = PLCController_v2.Instance != null
                    ? PLCController_v2.Instance
                    : FindObjectOfType<PLCController_v2>();

            if (plcControllerV2 != null)
            {
                plcControllerV2.TurnOn();
                Debug.Log("[HMI] ON qua PLCController_v2.");
                return;
            }

            if (plcController == null)
                plcController = FindObjectOfType<PLCController>();

            if (plcController != null)
            {
                plcController.StartDongCo();
                Debug.Log("[HMI] ON qua PLCController cu.");
            }
            else
            {
                Debug.LogError("[HMI] Khong tim thay PLCController_v2 hoac PLCController de ON.");
            }
        });

        offButton.onClick.AddListener(() =>
        {
            if (plcControllerV2 == null)
                plcControllerV2 = PLCController_v2.Instance != null
                    ? PLCController_v2.Instance
                    : FindObjectOfType<PLCController_v2>();

            if (plcControllerV2 != null)
            {
                plcControllerV2.TurnOff();
                Debug.Log("[HMI] OFF qua PLCController_v2.");
                return;
            }

            if (plcController == null)
                plcController = FindObjectOfType<PLCController>();

            if (plcController != null)
            {
                plcController.StopDongCo();
                Debug.Log("[HMI] OFF qua PLCController cu.");
            }
            else
            {
                Debug.LogError("[HMI] Khong tim thay PLCController_v2 hoac PLCController de OFF.");
            }
        });

        runtimeHmiRoot.SetActive(false);
    }

    private Text CreateRuntimeText(
        Transform parent,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color
    )
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = go.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;

        return text;
    }

    private Button CreateRuntimeButton(
        Transform parent,
        string name,
        string label,
        Vector2 position,
        Vector2 size,
        Color color
    )
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;

        Button button = go.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.selectedColor = color;
        button.colors = colors;

        Text text = CreateRuntimeText(
            go.transform,
            "Text",
            label,
            Vector2.zero,
            size,
            22,
            Color.white
        );

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;

        return button;
    }
}
