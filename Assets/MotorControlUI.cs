using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MotorControlUI : MonoBehaviour
{
    [Header("Nút ?i?u Khi?n H??ng")]
    [SerializeField] private Button btnThuan;
    [SerializeField] private Button btnNguoc;

    [Header("Nút ?i?u Khi?n Kh?i ??ng/D?ng")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnStop;

    [Header("Nút Reset")]
    [SerializeField] private Button btnRst;
    [SerializeField] private Button btnRst2;

    [Header("Nút ??t V? Trí")]
    [SerializeField] private Button btnSetVong;
    [SerializeField] private Button btnSetDo;

    [Header("?i?u Khi?n T?c ??")]
    [SerializeField] private Slider tocDoSlider;
    [SerializeField] private TextMeshProUGUI tocDoValueText;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField soVongInput;
    [SerializeField] private TMP_InputField gocInput;

    [Header("Nút Ch?y Theo Ch? ??")]
    [SerializeField] private Button btnChayTheoSoVong;
    [SerializeField] private Button btnChayTheoGoc;

    [Header("Hi?n Th? Tr?ng Thái")]
    [SerializeField] private TextMeshProUGUI statusText;

    private PLCController plcController;
    private int currentSpeed = 123;
    private bool isForwardDirection = true;

    private void Start()
    {
        plcController = FindObjectOfType<PLCController>();
        if (plcController == null)
        {
            Debug.LogError("<color=red>? Không tìm th?y PLCController</color>");
            return;
        }

        // ?i?u khi?n h??ng - Thu?n/Ng??c
        RegisterButton(btnThuan, "Thu?n", OnThuanClicked);
        RegisterButton(btnNguoc, "Ng??c", OnNguocClicked);

        // ?i?u khi?n kh?i ??ng/d?ng
        RegisterButton(btnStart, "START", OnStartClicked);
        RegisterButton(btnStop, "STOP", OnStopClicked);

        // Reset
        RegisterButton(btnRst, "RST", OnResetClicked);
        RegisterButton(btnRst2, "RST2", OnResetClicked);

        // ??t v? trí (Set)
        RegisterButton(btnSetVong, "Set vòng", OnSetVongClicked);
        RegisterButton(btnSetDo, "Set ??", OnSetDoClicked);

        // T?c ?? Slider
        if (tocDoSlider != null)
        {
            tocDoSlider.minValue = 0;
            tocDoSlider.maxValue = 3000;
            tocDoSlider.value = currentSpeed;
            tocDoSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        // Ch?y theo s? vòng
        RegisterButton(btnChayTheoSoVong, "Ch?y theo s? vòng", OnChayTheoSoVongClicked);

        // Ch?y theo góc
        RegisterButton(btnChayTheoGoc, "Ch?y theo góc", OnChayTheoGocClicked);

        UpdateStatus("? Kh?i t?o xong");
        Debug.Log("<color=green>? MotorControlUI ?ã k?t n?i v?i t?t c? nút</color>");
    }

    private void RegisterButton(Button button, string buttonName, UnityEngine.Events.UnityAction callback)
    {
        if (button != null)
        {
            button.onClick.AddListener(callback);
            Debug.Log($"<color=cyan>?? Nút '{buttonName}' ?ã ???c liên k?t</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>?? Nút '{buttonName}' không ???c gán trong Inspector</color>");
        }
    }

    private void OnThuanClicked()
    {
        if (plcController == null) return;
        plcController.ChonThuan();
        isForwardDirection = true;
        UpdateStatus("? Chi?u Thu?n (Clockwise)");
        Debug.Log("<color=cyan>?? Chi?u Thu?n (Clockwise) ???c ch?n</color>");
    }

    private void OnNguocClicked()
    {
        if (plcController == null) return;
        plcController.ChonNguoc();
        isForwardDirection = false;
        UpdateStatus("? Chi?u Ng??c (Counter-clockwise)");
        Debug.Log("<color=cyan>?? Chi?u Ng??c (Counter-clockwise) ???c ch?n</color>");
    }

    private void OnStartClicked()
    {
        if (plcController == null) return;
        plcController.StartDongCo();
        UpdateStatus("?? Motor ?ang ch?y");
        Debug.Log("<color=green>?? START - Motor kh?i ??ng</color>");
    }

    private void OnStopClicked()
    {
        if (plcController == null) return;
        plcController.StopDongCo();
        UpdateStatus("?? Motor d?ng");
        Debug.Log("<color=red>?? STOP - Motor d?ng l?i</color>");
    }

    private void OnResetClicked()
    {
        if (plcController == null) return;
        plcController.ResetAll();
        UpdateStatus("?? H? th?ng ?ã reset");
        Debug.Log("<color=yellow>?? RESET - H? th?ng ?ã reset</color>");
    }

    private void OnSetVongClicked()
    {
        if (plcController == null || soVongInput == null) return;

        if (int.TryParse(soVongInput.text, out int soVong))
        {
            plcController.DatSoVong(soVong);
            UpdateStatus($"? ??t s? vòng: {soVong}");
            Debug.Log($"<color=green>? S? vòng ???c ??t: {soVong}</color>");
        }
        else
        {
            UpdateStatus("? L?i: S? vòng không h?p l?");
            Debug.LogError("<color=red>? S? vòng không h?p l?</color>");
        }
    }

    private void OnSetDoClicked()
    {
        if (plcController == null || gocInput == null) return;

        if (int.TryParse(gocInput.text, out int goc))
        {
            plcController.DatGocQuay(goc);
            UpdateStatus($"? ??t góc: {goc}°");
            Debug.Log($"<color=green>? Góc ???c ??t: {goc}°</color>");
        }
        else
        {
            UpdateStatus("? L?i: Góc không h?p l?");
            Debug.LogError("<color=red>? Góc không h?p l?</color>");
        }
    }

    private void OnSpeedChanged(float value)
    {
        currentSpeed = (int)value;
        if (plcController != null)
            plcController.DatTocDo(currentSpeed);

        if (tocDoValueText != null)
            tocDoValueText.text = $"{currentSpeed} RPM";

        UpdateStatus($"T?c ??: {currentSpeed} RPM");
    }

    private void OnChayTheoSoVongClicked()
    {
        if (plcController == null || soVongInput == null) return;

        if (int.TryParse(soVongInput.text, out int soVong))
        {
            if (isForwardDirection)
                plcController.KhoiDongThuanTheoSoVong(soVong, currentSpeed);
            else
                plcController.KhoiDongNguocTheoSoVong(soVong, currentSpeed);

            UpdateStatus($"Ch?y theo {soVong} vòng v?i t?c ?? {currentSpeed} RPM");
            Debug.Log($"<color=green>? Ch?y theo {soVong} vòng - H??ng: {(isForwardDirection ? "Thu?n" : "Ng??c")}</color>");
        }
        else
        {
            UpdateStatus("L?i: Nh?p s? vòng không h?p l?");
            Debug.LogError("<color=red>? S? vòng không h?p l?</color>");
        }
    }

    private void OnChayTheoGocClicked()
    {
        if (plcController == null || gocInput == null) return;

        if (int.TryParse(gocInput.text, out int goc))
        {
            if (isForwardDirection)
                plcController.KhoiDongThuanTheoGoc(goc, currentSpeed);
            else
                plcController.KhoiDongNguocTheoGoc(goc, currentSpeed);

            UpdateStatus($"Ch?y theo {goc}° v?i t?c ?? {currentSpeed} RPM");
            Debug.Log($"<color=green>? Ch?y theo {goc}° - H??ng: {(isForwardDirection ? "Thu?n" : "Ng??c")}</color>");
        }
        else
        {
            UpdateStatus("L?i: Nh?p góc không h?p l?");
            Debug.LogError("<color=red>? Góc không h?p l?</color>");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
