// Loại khỏi build WebGL: dùng System.IO.Ports + HslCommunication (không hỗ trợ WebGL).
// Vẫn biên dịch được trong Editor và bản Desktop. Bản WebGL dùng PLCController_v2 (HTTP).
#if !UNITY_WEBGL || UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using HslCommunication.Profinet.Melsec;
using System.Collections;
using System.Collections.Generic;

public class PLCController : MonoBehaviour
{
    [Header("Cấu hình Cổng COM")]
    public string portName = "COM6";
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
    public string regTanSoXung = "D100";
    public string regTocDo = "D128";

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

    [Header("Đọc dữ liệu PLC")]
    [Tooltip("Tắt mặc định để tránh đơ Unity khi COM/PLC timeout. Bật lại sau khi đường serial ổn định.")]
    public bool enableAutoReadback = false;
    [Tooltip("Tắt mặc định để Play không tự ghi D128. Bật nếu muốn Unity đặt tốc độ ngay khi kết nối.")]
    public bool writeDefaultSpeedOnConnect = false;

    private MelsecFxSerial melsecSerial;
    private Dictionary<string, Coroutine> pulseJobs = new Dictionary<string, Coroutine>();
    private RotateSubmarineBlades rotateBlades;

    private bool trangThaiDenTest = false;
    private bool dangNhayDen = false;
    private float timerDen = 0f;

    // Lưu giá trị soVong và goc hiện tại
    private int soVongHienTai = 0;
    private int gocHienTai = 0;
    private int tocDoHienTai = 0;
    private int soXungHienTai = 0;
    private int tanSoXungHienTai = 0;

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
            tocDoHienTai = tocDoMacDinh;
            if (writeDefaultSpeedOnConnect)
                DatTocDo(tocDoMacDinh);

            // Bắt đầu vòng lặp đồng bộ dữ liệu tự động
            if (enableAutoReadback)
                StartCoroutine(SyncDataWithPLCRoutine());
        }
        else
        {
            Debug.LogError($"<color=red>❌ Lỗi kết nối: {connectResult.Message}</color>");
        }

        // Find RotateSubmarineBlades in the scene
        rotateBlades = FindObjectOfType<RotateSubmarineBlades>();
        if (rotateBlades == null)
        {
            Debug.LogWarning("<color=orange>⚠️ Không tìm thấy RotateSubmarineBlades trong scene</color>");
        }
    }

    // Vòng lặp đồng bộ dữ liệu liên tục từ PLC về Unity
    private int syncFailCount = 0;
    private const int maxSyncFails = 3;

    private IEnumerator SyncDataWithPLCRoutine()
    {
        while (true)
        {
            if (syncFailCount >= maxSyncFails)
            {
                Debug.LogWarning("<color=orange>⚠️ SyncData dừng do COM timeout liên tục. Nhấn Play lại khi có PLC.</color>");
                yield break;
            }

            if (KiemTraKetNoi() && rotateBlades != null)
            {
                // 1. Đồng bộ Tốc độ (D146)
                var readTocDo = melsecSerial.ReadInt16(regTocDo);
                if (readTocDo.IsSuccess)
                {
                    rotateBlades.rotationSpeed = readTocDo.Content;
                    syncFailCount = 0;
                }
                else
                {
                    syncFailCount++;
                }

                // 2. Đồng bộ Chiều quay
                if (syncFailCount < maxSyncFails)
                {
                    var readChieu = melsecSerial.ReadBool(bitThuan);
                    if (readChieu.IsSuccess)
                        rotateBlades.SetRotationDirection(readChieu.Content);
                }
            }
            yield return new WaitForSeconds(0.5f);
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

            if (dangNhayDen && !suppressWriteErrors)
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
        tocDoHienTai = tocDo;
        GhiDWord(regTocDo, tocDo, "Tần số/tốc độ đặt D128");

        // Đồng bộ tốc độ với model 3D
        if (rotateBlades != null)
            rotateBlades.rotationSpeed = tocDo;
    }

    public void DatSoVong(int soVong)
    {
        soVong = Mathf.Clamp(soVong, soVongMin, soVongMax);
        soVongHienTai = soVong;
        soXungHienTai = soVong * 5000;
        GhiWord(regSoVong, soVong, "Số vòng đặt D112");
    }

    public void DatGocQuay(int goc)
    {
        goc = Mathf.Clamp(goc, gocMin, gocMax);
        gocHienTai = goc;
        soXungHienTai = Mathf.RoundToInt(goc * 5000f / 360f);
        GhiWord(regGocQuay, goc, "Góc quay đặt D114");
    }

    // Ghi thẳng D128 thay vì nhấn M15/M16
    public void DatTocDoDocLap(int delta)
    {
        if (!KiemTraKetNoi()) return;

        int current = tocDoHienTai;
        if (enableAutoReadback)
        {
            var read = melsecSerial.ReadInt16(regTocDo);
            if (!read.IsSuccess)
            {
                Debug.LogError($"<color=red>❌ Không đọc được {regTocDo}: {read.Message}</color>");
                return;
            }

            current = read.Content;
        }

        int moi = Mathf.Clamp(current + delta, tocDoMin, tocDoMax);
        tocDoHienTai = moi;
        GhiDWord(regTocDo, moi, "Tần số/tốc độ đặt D128");

        // Đồng bộ tốc độ với model 3D
        if (rotateBlades != null)
            rotateBlades.rotationSpeed = (float)moi;
    }

    // =====================================================
    // NÚT CHỨC NĂNG DẠNG XUNG
    // =====================================================
    public void StartDongCo()
    {
        PulseBit(bitStart, pulseDuration, "START");
        // Đồng bộ kích hoạt model 3D
        if (rotateBlades != null)
            rotateBlades.RotateObject(true);
    }

    public void StopDongCo()
    {
        GhiBit(bitStart, false, "START");
        PulseBit(bitStop, pulseDuration, "STOP");
        Debug.Log($"<color=cyan>START OFF ({bitStart}), STOP PULSE ({bitStop})</color>");
        // Đồng bộ dừng model 3D
        if (rotateBlades != null)
            rotateBlades.RotateObject(false);
    }

    public void ChonThuan()
    {
        PulseBit(bitThuan, pulseDuration, "THUẬN");
        if (rotateBlades != null)
            rotateBlades.SetRotationDirection(true);
    }

    public void ChonNguoc()
    {
        PulseBit(bitNguoc, pulseDuration, "NGƯỢC");
        if (rotateBlades != null)
            rotateBlades.SetRotationDirection(false);
    }

    public void ChayTheoSoVong() => PulseBit(bitSoVong, pulseDuration, "CHẠY THEO SỐ VÒNG");
    public void ChayTheoGoc() => PulseBit(bitGocQuay, pulseDuration, "CHẠY THEO GÓC");
    public void TangTocDoBangBit() => PulseBit(bitTang, pulseDuration, "TĂNG TỐC");
    public void GiamTocDoBangBit() => PulseBit(bitGiam, pulseDuration, "GIẢM TỐC");
    public void ResetCounter() => PulseBit(bitResetCounter, pulseDuration, "RESET COUNTER");
    public void ResetAll() => PulseBit(bitResetAll, pulseDuration, "RESET ALL");
    public void ErrReset() => PulseBit(bitErrReset, pulseDuration, "ERR RESET");
    public void QuayTheoGocMode() => PulseBit(bitQuayTheoGoc, pulseDuration, "MODE QUAY THEO GÓC");

    // Tổ hợp tiện dùng
    public void KhoiDongThuanTheoSoVong(int soVong, int tocDo)
    {
        DatSoVong(soVong);
        DatTocDo(tocDo);
        ChonThuan();
        ChayTheoSoVong();
        if (rotateBlades != null)
            rotateBlades.SetNumberOfRotations(soVong);
        StartDongCo();
    }

    public void KhoiDongNguocTheoSoVong(int soVong, int tocDo)
    {
        DatSoVong(soVong);
        DatTocDo(tocDo);
        ChonNguoc();
        ChayTheoSoVong();
        if (rotateBlades != null)
            rotateBlades.SetNumberOfRotations(soVong);
        StartDongCo();
    }

    public void KhoiDongThuanTheoGoc(int goc, int tocDo)
    {
        DatGocQuay(goc);
        DatTocDo(tocDo);
        ChonThuan();
        ChayTheoGoc();
        if (rotateBlades != null)
            rotateBlades.SetNumberOfRotations(goc / 360f);
        StartDongCo();
    }

    public void KhoiDongNguocTheoGoc(int goc, int tocDo)
    {
        DatGocQuay(goc);
        DatTocDo(tocDo);
        ChonNguoc();
        ChayTheoGoc();
        if (rotateBlades != null)
            rotateBlades.SetNumberOfRotations(goc / 360f);
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
        if (suppressWriteErrors) yield break;

        var on = melsecSerial.Write(diaChi, true);
        if (!on.IsSuccess)
        {
            if (!suppressWriteErrors)
            {
                Debug.LogError($"<color=red>❌ {tenLenh} ON lỗi tại {diaChi}: {on.Message} (ẩn lỗi tiếp theo)</color>");
                suppressWriteErrors = true;
            }
            yield break;
        }

        Debug.Log($"<color=cyan>▶ {tenLenh} ON ({diaChi})</color>");
        yield return new WaitForSeconds(duration);

        var off = melsecSerial.Write(diaChi, false);
        if (!off.IsSuccess && !suppressWriteErrors)
        {
            Debug.LogError($"<color=red>❌ {tenLenh} OFF lỗi tại {diaChi}: {off.Message}</color>");
            suppressWriteErrors = true;
        }

        pulseJobs[diaChi] = null;
    }

    private void GhiBit(string diaChi, bool value, string ten)
    {
        if (!KiemTraKetNoi()) return;

        var result = melsecSerial.Write(diaChi, value);
        if (result.IsSuccess)
        {
            suppressWriteErrors = false;
        }
        else if (!suppressWriteErrors)
        {
            Debug.LogError($"<color=red>❌ Lỗi ghi bit {diaChi}: {result.Message} (ẩn lỗi tiếp theo)</color>");
            suppressWriteErrors = true;
            dangNhayDen = false; // Tắt nhấp nháy đèn khi COM lỗi
        }
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

    private void GhiDWord(string diaChi, int value, string ten)
    {
        if (!KiemTraKetNoi()) return;

        var result = melsecSerial.Write(diaChi, value);

        if (result.IsSuccess)
            Debug.Log($"[PLC] {ten}: {diaChi} = {value}");
        else
            Debug.LogError($"<color=red>❌ Lỗi ghi thanh ghi 32-bit {diaChi}: {result.Message}</color>");
    }

    private bool KiemTraKetNoi()
    {
        if (melsecSerial == null || !melsecSerial.IsOpen())
        {
            return false;
        }
        return true;
    }

    // =====================================================
    // ĐỌC THÔNG SỐ TỪ MOTOR/PLC
    // =====================================================
    private bool suppressReadErrors = false;
    private bool suppressWriteErrors = false;

    public int DocTocDoHienTai()
    {
        if (!enableAutoReadback) return tocDoHienTai;
        if (!KiemTraKetNoi()) return 0;

        var read = melsecSerial.ReadInt16(regTocDo);
        if (read.IsSuccess)
        {
            suppressReadErrors = false; // Reset cờ nếu đọc thành công
            return read.Content;
        }

        if (!suppressReadErrors)
        {
            Debug.LogError($"<color=red>❌ Không đọc được tốc độ {regTocDo}: {read.Message} (Sẽ ẩn các lỗi tương tự tiếp theo để tránh giật lag)</color>");
            suppressReadErrors = true; // Bật cờ để ẩn các lỗi tiếp theo
        }
        return 0;
    }

    public int DocSoVongHienTai()
    {
        if (!enableAutoReadback) return soVongHienTai;
        if (!KiemTraKetNoi()) return 0;

        var read = melsecSerial.ReadInt16(regSoVong);
        if (read.IsSuccess) return read.Content;

        if (!suppressReadErrors) Debug.LogError($"<color=red>❌ Không đọc được số vòng {regSoVong}: {read.Message}</color>");
        return 0;
    }

    public int DocGocQuayHienTai()
    {
        if (!enableAutoReadback) return gocHienTai;
        if (!KiemTraKetNoi()) return 0;

        var read = melsecSerial.ReadInt16(regGocQuay);
        if (read.IsSuccess) return read.Content;

        if (!suppressReadErrors) Debug.LogError($"<color=red>❌ Không đọc được góc quay {regGocQuay}: {read.Message}</color>");
        return 0;
    }

    public int DocSoXungHienTai()
    {
        if (!enableAutoReadback) return soXungHienTai;
        if (!KiemTraKetNoi()) return 0;

        var read = melsecSerial.ReadInt16(regSoXung);
        if (read.IsSuccess) return read.Content;

        if (!suppressReadErrors) Debug.LogError($"<color=red>❌ Không đọc được số xung {regSoXung}: {read.Message}</color>");
        return 0;
    }

    public bool DocChieuQuayHienTai()
    {
        if (!enableAutoReadback)
            return rotateBlades == null || rotateBlades.GetRotationDirection() > 0;

        if (!KiemTraKetNoi()) return true;

        var read = melsecSerial.ReadBool(bitThuan);
        if (read.IsSuccess) return read.Content; // true = Thuận, false = Ngược

        if (!suppressReadErrors) Debug.LogError($"<color=red>❌ Không đọc được chiều quay {bitThuan}: {read.Message}</color>");
        return true;
    }

    public int DocTanSoXungHienTai()
    {
        if (!enableAutoReadback) return tanSoXungHienTai;
        if (!KiemTraKetNoi()) return 0;

        var read = melsecSerial.ReadInt16(regTanSoXung);
        if (read.IsSuccess) return read.Content;

        if (!suppressReadErrors) Debug.LogError($"<color=red>❌ Không đọc được tần số xung {regTanSoXung}: {read.Message}</color>");
        return 0;
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
#else
// ====== WebGL stub ======
// System.IO.Ports + HslCommunication khong ho tro tren WebGL.
// Giu nguyen public API de cac script khac van bien dich cho WebGL.
// Tren WebGL dieu khien PLC qua PLCController_v2 (HTTP), khong dung class nay.
using UnityEngine;

public class PLCController : MonoBehaviour
{
    public string portName = "COM6";
    public int baudRate = 9600;
    public string diaChiDenTest = "M20";
    public float thoiGianNhayDen = 0.5f;
    public string bitStart = "M1", bitThuan = "M2", bitQuayTheoGoc = "M3", bitSoVong = "M4";
    public string bitGocQuay = "M5", bitNguoc = "M8", bitResetCounter = "M12", bitResetAll = "M13";
    public string bitErrReset = "M14", bitTang = "M15", bitGiam = "M16", bitStop = "M17";
    public string regSoXung = "D104", regSoVong = "D112", regGocQuay = "D114", regTanSoXung = "D100", regTocDo = "D128";
    public int tocDoMacDinh = 123, tocDoMin = 0, tocDoMax = 3000;
    public int soVongMin = 0, soVongMax = 100000, gocMin = 0, gocMax = 360000;
    public float pulseDuration = 0.10f;
    public bool enableAutoReadback = false, writeDefaultSpeedOnConnect = false;

    public void BatDenTest() {}
    public void TatDenTest() {}
    public void ToggleDenTest() {}
    public void BatNhayDenTest() {}
    public void TatNhayDenTest() {}
    public void DatTocDo(int tocDo) {}
    public void DatSoVong(int soVong) {}
    public void DatGocQuay(int goc) {}
    public void DatTocDoDocLap(int delta) {}
    public void StartDongCo() {}
    public void StopDongCo() {}
    public void ChonThuan() {}
    public void ChonNguoc() {}
    public void ChayTheoSoVong() {}
    public void ChayTheoGoc() {}
    public void TangTocDoBangBit() {}
    public void GiamTocDoBangBit() {}
    public void ResetCounter() {}
    public void ResetAll() {}
    public void ErrReset() {}
    public void QuayTheoGocMode() {}
    public void KhoiDongThuanTheoSoVong(int soVong, int tocDo) {}
    public void KhoiDongNguocTheoSoVong(int soVong, int tocDo) {}
    public void KhoiDongThuanTheoGoc(int goc, int tocDo) {}
    public void KhoiDongNguocTheoGoc(int goc, int tocDo) {}
    public int DocTocDoHienTai() => 0;
    public int DocSoVongHienTai() => 0;
    public int DocGocQuayHienTai() => 0;
    public int DocSoXungHienTai() => 0;
    public bool DocChieuQuayHienTai() => true;
    public int DocTanSoXungHienTai() => 0;
}
#endif
