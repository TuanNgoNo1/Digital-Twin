using UnityEngine;
using UnityEngine.InputSystem;
using HslCommunication.Profinet.Melsec;
using System.Collections;
using System.Collections.Generic;

public class PLCController : MonoBehaviour
{
    [Header("Cấu hình Cổng COM")]
    public string portName = "COM8";
    public int baudRate = 9600;

    [Header("Bit test riêng (KHÔNG trùng bit điều khiển PLC)")]
    public string diaChiDenTest = "M20";
    public float thoiGianNhayDen = 0.5f;

    [Header("Các bit chức năng theo ladder PLC")]
    public string bitStart = "M1";
    public string bitThuan = "M2";
    public string bitQuayTheoGoc = "M3";
    public string bitSoVong = "M4";
    public string bitGocQuay = "M5";
    public string bitNguoc = "M8";
    public string bitResetCounter = "M12";
    public string bitResetAll = "M13";
    public string bitErrReset = "M14";
    public string bitTang = "M15";
    public string bitGiam = "M16";
    public string bitStop = "M17";

    [Header("Thanh ghi dữ liệu theo ladder PLC")]
    public string regSoXung = "D104";
    public string regSoVong = "D112";
    public string regGocQuay = "D114";
    public string regTanSoXung = "D128";
    public string regTocDo = "D146";

    [Header("Giới hạn dữ liệu")]
    public int tocDoMacDinh = 123;
    public int tocDoMin = 0;
    public int tocDoMax = 3000;
    public int soVongMin = 0;
    public int soVongMax = 100000;
    public int gocMin = 0;
    public int gocMax = 360000;

    [Header("Thời gian xung cho nút chức năng")]
    public float pulseDuration = 0.10f;

    private MelsecFxSerial melsecSerial;
    private Dictionary<string, Coroutine> pulseJobs = new Dictionary<string, Coroutine>();

    private bool trangThaiDenTest = false;
    private bool dangNhayDen = false;
    private float timerDen = 0f;

    void Start()
    {
        melsecSerial = new MelsecFxSerial();
        melsecSerial.SerialPortInni(
            portName,
            baudRate,
            7,
            System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity.Even
        );

        var connectResult = melsecSerial.Open();

        if (connectResult.IsSuccess)
        {
            Debug.Log($"<color=green>✅ Kết nối THÀNH CÔNG qua {portName}</color>");
            DatTocDo(tocDoMacDinh);
        }
        else
        {
            Debug.LogError($"<color=red>❌ Lỗi kết nối: {connectResult.Message}</color>");
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Test đèn riêng
        if (kb.f1Key.wasPressedThisFrame) BatDenTest();
        if (kb.f2Key.wasPressedThisFrame) TatDenTest();
        if (kb.f3Key.wasPressedThisFrame) ToggleDenTest();
        if (kb.f4Key.wasPressedThisFrame) BatNhayDenTest();
        if (kb.f5Key.wasPressedThisFrame) TatNhayDenTest();

        // Điều khiển PLC
        if (kb.digit1Key.wasPressedThisFrame) ChonThuan();
        if (kb.digit2Key.wasPressedThisFrame) ChonNguoc();
        if (kb.digit3Key.wasPressedThisFrame) StartDongCo();
        if (kb.digit4Key.wasPressedThisFrame) StopDongCo();

        if (kb.digit5Key.wasPressedThisFrame) ChayTheoSoVong();
        if (kb.digit6Key.wasPressedThisFrame) ChayTheoGoc();

        if (kb.digit7Key.wasPressedThisFrame) TangTocDoBangBit();
        if (kb.digit8Key.wasPressedThisFrame) GiamTocDoBangBit();

        if (kb.digit9Key.wasPressedThisFrame) ResetCounter();
        if (kb.digit0Key.wasPressedThisFrame) ResetAll();

        if (kb.numpadPlusKey.wasPressedThisFrame || kb.equalsKey.wasPressedThisFrame)
            DatTocDoDocLap(+1);

        if (kb.numpadMinusKey.wasPressedThisFrame || kb.minusKey.wasPressedThisFrame)
            DatTocDoDocLap(-1);

        if (dangNhayDen)
        {
            timerDen += Time.deltaTime;
            if (timerDen >= thoiGianNhayDen)
            {
                timerDen = 0f;
                GhiBit(diaChiDenTest, !trangThaiDenTest, "Đèn test");
                trangThaiDenTest = !trangThaiDenTest;
            }
        }
    }

    // =====================================================
    // TEST ĐÈN RIÊNG
    // =====================================================
    public void BatDenTest()
    {
        GhiBit(diaChiDenTest, true, "Đèn test");
        trangThaiDenTest = true;
    }

    public void TatDenTest()
    {
        GhiBit(diaChiDenTest, false, "Đèn test");
        trangThaiDenTest = false;
    }

    public void ToggleDenTest()
    {
        GhiBit(diaChiDenTest, !trangThaiDenTest, "Đèn test");
        trangThaiDenTest = !trangThaiDenTest;
    }

    public void BatNhayDenTest()
    {
        dangNhayDen = true;
        timerDen = 0f;
    }

    public void TatNhayDenTest()
    {
        dangNhayDen = false;
        TatDenTest();
    }

    // =====================================================
    // GHI THANH GHI D
    // =====================================================
    public void DatTocDo(int tocDo)
    {
        tocDo = Mathf.Clamp(tocDo, tocDoMin, tocDoMax);
        GhiWord(regTocDo, tocDo, "Tốc độ D146");
    }

    public void DatSoVong(int soVong)
    {
        soVong = Mathf.Clamp(soVong, soVongMin, soVongMax);
        GhiWord(regSoVong, soVong, "Số vòng D112");
    }

    public void DatGocQuay(int goc)
    {
        goc = Mathf.Clamp(goc, gocMin, gocMax);
        GhiWord(regGocQuay, goc, "Góc quay D114");
    }

    // Ghi thẳng D146 thay vì nhấn M15/M16
    public void DatTocDoDocLap(int delta)
    {
        if (!KiemTraKetNoi()) return;

        var read = melsecSerial.ReadInt16(regTocDo);
        if (!read.IsSuccess)
        {
            Debug.LogError($"<color=red>❌ Không đọc được {regTocDo}: {read.Message}</color>");
            return;
        }

        int moi = Mathf.Clamp(read.Content + delta, tocDoMin, tocDoMax);
        GhiWord(regTocDo, moi, "Tốc độ D146");
    }

    // =====================================================
    // NÚT CHỨC NĂNG DẠNG XUNG
    // =====================================================
    public void StartDongCo()       => PulseBit(bitStart, pulseDuration, "START");
    public void StopDongCo()        => PulseBit(bitStop, pulseDuration, "STOP");
    public void ChonThuan()         => PulseBit(bitThuan, pulseDuration, "THUẬN");
    public void ChonNguoc()         => PulseBit(bitNguoc, pulseDuration, "NGƯỢC");
    public void ChayTheoSoVong()    => PulseBit(bitSoVong, pulseDuration, "CHẠY THEO SỐ VÒNG");
    public void ChayTheoGoc()       => PulseBit(bitGocQuay, pulseDuration, "CHẠY THEO GÓC");
    public void TangTocDoBangBit()  => PulseBit(bitTang, pulseDuration, "TĂNG TỐC");
    public void GiamTocDoBangBit()  => PulseBit(bitGiam, pulseDuration, "GIẢM TỐC");
    public void ResetCounter()      => PulseBit(bitResetCounter, pulseDuration, "RESET COUNTER");
    public void ResetAll()          => PulseBit(bitResetAll, pulseDuration, "RESET ALL");
    public void ErrReset()          => PulseBit(bitErrReset, pulseDuration, "ERR RESET");
    public void QuayTheoGocMode()   => PulseBit(bitQuayTheoGoc, pulseDuration, "MODE QUAY THEO GÓC");

    // Tổ hợp tiện dùng
    public void KhoiDongThuanTheoSoVong(int soVong, int tocDo)
    {
        DatSoVong(soVong);
        DatTocDo(tocDo);
        ChonThuan();
        ChayTheoSoVong();
        StartDongCo();
    }

    public void KhoiDongNguocTheoSoVong(int soVong, int tocDo)
    {
        DatSoVong(soVong);
        DatTocDo(tocDo);
        ChonNguoc();
        ChayTheoSoVong();
        StartDongCo();
    }

    public void KhoiDongThuanTheoGoc(int goc, int tocDo)
    {
        DatGocQuay(goc);
        DatTocDo(tocDo);
        ChonThuan();
        ChayTheoGoc();
        StartDongCo();
    }

    public void KhoiDongNguocTheoGoc(int goc, int tocDo)
    {
        DatGocQuay(goc);
        DatTocDo(tocDo);
        ChonNguoc();
        ChayTheoGoc();
        StartDongCo();
    }

    // =====================================================
    // CORE IO
    // =====================================================
    private void PulseBit(string diaChi, float duration, string tenLenh)
    {
        if (!KiemTraKetNoi()) return;

        if (pulseJobs.TryGetValue(diaChi, out Coroutine oldJob) && oldJob != null)
            StopCoroutine(oldJob);

        pulseJobs[diaChi] = StartCoroutine(PulseBitRoutine(diaChi, duration, tenLenh));
    }

    private IEnumerator PulseBitRoutine(string diaChi, float duration, string tenLenh)
    {
        var on = melsecSerial.Write(diaChi, true);
        if (!on.IsSuccess)
        {
            Debug.LogError($"<color=red>❌ {tenLenh} ON lỗi tại {diaChi}: {on.Message}</color>");
            yield break;
        }

        Debug.Log($"<color=cyan>▶ {tenLenh} ON ({diaChi})</color>");
        yield return new WaitForSeconds(duration);

        var off = melsecSerial.Write(diaChi, false);
        if (!off.IsSuccess)
        {
            Debug.LogError($"<color=red>❌ {tenLenh} OFF lỗi tại {diaChi}: {off.Message}</color>");
            yield break;
        }

        Debug.Log($"<color=grey>⏹ {tenLenh} OFF ({diaChi})</color>");
        pulseJobs[diaChi] = null;
    }

    private void GhiBit(string diaChi, bool value, string ten)
    {
        if (!KiemTraKetNoi()) return;

        var result = melsecSerial.Write(diaChi, value);
        if (result.IsSuccess)
            Debug.Log($"[PLC] {ten}: {diaChi} = {value}");
        else
            Debug.LogError($"<color=red>❌ Lỗi ghi bit {diaChi}: {result.Message}</color>");
    }

    private void GhiWord(string diaChi, int value, string ten)
    {
        if (!KiemTraKetNoi()) return;

        short v = (short)Mathf.Clamp(value, short.MinValue, short.MaxValue);
        var result = melsecSerial.Write(diaChi, v);

        if (result.IsSuccess)
            Debug.Log($"[PLC] {ten}: {diaChi} = {value}");
        else
            Debug.LogError($"<color=red>❌ Lỗi ghi thanh ghi {diaChi}: {result.Message}</color>");
    }

    private bool KiemTraKetNoi()
    {
        if (melsecSerial == null || !melsecSerial.IsOpen())
        {
            Debug.LogWarning("<color=red>⚠️ Chưa kết nối PLC!</color>");
            return false;
        }
        return true;
    }

    void OnApplicationQuit()
    {
        dangNhayDen = false;

        if (melsecSerial != null && melsecSerial.IsOpen())
        {
            // Tắt bit test
            melsecSerial.Write(diaChiDenTest, false);

            // Ép các nút chức năng về 0
            string[] bits =
            {
                bitStart, bitThuan, bitQuayTheoGoc, bitSoVong, bitGocQuay,
                bitNguoc, bitResetCounter, bitResetAll, bitErrReset,
                bitTang, bitGiam, bitStop
            };

            foreach (var bit in bits)
                melsecSerial.Write(bit, false);

            melsecSerial.Close();
            Debug.Log("🔌 Đã đóng cổng COM an toàn.");
        }
    }
}