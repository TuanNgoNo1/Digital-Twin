# HANDOFF — Digital Twin TH1: WebGL → Pi → HslCommunication → PLC

Tài liệu bàn giao cho người tiếp tục **phần Unity**. Phần Pi gateway đã hoàn tất, test điều khiển động cơ thật ON/OFF thành công.

---

## 1. Kiến trúc (đã chạy được)

```
Unity (Build WebGL)  --HTTP /control,/telemetry-->  Raspberry Pi (.NET + HslCommunication)  --serial 9600 7E1-->  PLC Mitsubishi FX  -->  Servo
                     <----------- telemetry --------                                         <---------- readback ----------
Camera Pi (MJPEG :8080) ---------> Unity (texture lên màn hình ảo)
```

- Unity **không** nói chuyện trực tiếp với PLC nữa (WebGL không có serial/socket). Unity chỉ gọi **HTTP** tới Pi.
- Pi chạy gateway .NET dùng **HslCommunication** để ghi/đọc PLC, mapping **giống hệt `Assets/PLCController.cs`**.
- Logic điều khiển: **pulse 100ms** các bit (M1/M2/M8/M17...), ghi tốc độ D128 (DWord).

---

## 2. Trạng thái hiện tại

| Thành phần | Trạng thái |
|:---|:---|
| Pi gateway (`pi-gateway-hsl.service`) | ✅ chạy, auto-start khi reboot, `PLC connected on /dev/ttyUSB0` |
| Điều khiển ON/OFF từ PowerShell/curl | ✅ test thành công, động cơ quay |
| ModemManager | đã tắt (tránh tranh chiếm cổng serial) |
| Unity `PLCController_v2.cs` (HTTP client) | ✅ đã có sẵn, `piBaseUrl = http://10.38.100.27:5000` |
| Build WebGL | ⚠️ **chưa loại trừ PLCController.cs/HslCommunication → cần làm (Mục 6.1)** |

---

## 3. Pi gateway (tham khảo — đã xong)

- IP Pi (ZeroTier): `10.38.100.27`, user `admin`. Đăng nhập SSH bằng key `~/.ssh/pi_gateway_key` (đã cấu hình passwordless).
- Mã nguồn gateway: trong repo tại `Tools/PiGatewayHsl/` (Program.cs, .csproj, libs/HslCommunication.dll). Trên Pi: `~/PiGatewayHsl/`.
- Service: `/etc/systemd/system/pi-gateway-hsl.service`.

**Lệnh quản lý (chạy trên Pi qua SSH):**
```bash
systemctl status pi-gateway-hsl          # trạng thái
journalctl -u pi-gateway-hsl -f          # log realtime
sudo systemctl restart pi-gateway-hsl    # restart
```

**Nếu sửa mapping (Program.cs) rồi deploy lại:**
```bash
# từ máy Windows (thư mục repo):
scp -i $env:USERPROFILE\.ssh\pi_gateway_key Tools\PiGatewayHsl\Program.cs admin@10.38.100.27:~/PiGatewayHsl/Program.cs
ssh -i $env:USERPROFILE\.ssh\pi_gateway_key admin@10.38.100.27 "cd ~/PiGatewayHsl && dotnet build -c Release && sudo systemctl restart pi-gateway-hsl"
```

---

## 4. API contract (Unity ⇄ Pi)

**Unity gửi lệnh:** `POST http://10.38.100.27:5000/control`
```json
{ "action":"ON", "speed":123, "direction":"forward", "rotations":0, "angle":0,
  "runId":"TH1-...", "lessonId":"TH1", "userId":"demo-user", "timestamp":"..." }
```
`action` hỗ trợ: `ON`, `OFF`, `SET_SPEED`, `SET_DIRECTION` (forward/reverse), `SET_ROTATIONS`, `SET_ANGLE`, `RESET`, `ERR_RESET`.

**Unity đọc telemetry:** `GET http://10.38.100.27:5000/telemetry`
```json
{ "running":true, "speedRpm":123, "count":1200, "rotations":0, "angle":0,
  "direction":"forward", "action":"ON", "backendSynced":true, "backendStatus":"SYNCED", ... }
```
Gateway đã bật **CORS** + xử lý preflight `OPTIONS` (bắt buộc cho fetch từ WebGL).

---

## 5. Mapping PLC (theo `Assets/PLCController.cs`)

| Lệnh HTTP | Hành vi PLC (HslCommunication) |
|:---|:---|
| `ON` | ghi D128 (tốc độ, DWord, clamp 0–3000) → **pulse M2/M8** (chiều) → **pulse M1** (start) |
| `OFF` | M1=false → **pulse M17** (stop) |
| `SET_SPEED` | ghi D128 |
| `SET_DIRECTION` | pulse M2 (thuận) / M8 (ngược) |
| `SET_ROTATIONS` | ghi D112 (số vòng, Word) |
| `SET_ANGLE` | ghi D114 (góc, Word) |
| `RESET` / `ERR_RESET` | pulse M13 / M14 |
| telemetry | đọc D128 (tốc độ), D104 (số xung) |

Bit/thanh ghi: M1 start, M2 thuận, M8 ngược, M17 stop, M13 reset all, M14 err reset; D128 tốc độ, D112 số vòng, D114 góc, D104 số xung. **Pulse = 100ms.** (Sửa trong `Tools/PiGatewayHsl/Program.cs` nếu ladder đổi.)

---

## 6. CÁC BƯỚC TIẾP THEO TRONG UNITY

### 6.1. ⚠️ Loại PLCController.cs + HslCommunication khỏi build WebGL (BẮT BUỘC)
PLCController.cs dùng `System.IO.Ports`/HslCommunication — **không build/chạy được trên WebGL** (sẽ lỗi IL2CPP/runtime). Cần loại khỏi WebGL nhưng vẫn giữ được cho bản Desktop:

1. Bọc toàn bộ class trong `Assets/PLCController.cs` bằng tiền xử lý:
   ```csharp
   #if !UNITY_WEBGL || UNITY_EDITOR
   // ... toàn bộ class PLCController ...
   #endif
   ```
2. Chọn `Assets/Plugins/HslCommunication.dll` → Inspector → bỏ tick **WebGL** trong Platform settings → Apply.
3. Đảm bảo GameObject gắn `PLCController` **không nằm trong scene build WebGL** (hoặc bị tắt). Bản WebGL chỉ dùng `PLCController_v2`.

### 6.2. Cấu hình scene cho WebGL
- Tạo/giữ 1 GameObject gắn **`PLCController_v2`**:
  - `piBaseUrl = http://10.38.100.27:5000`
  - `pollTelemetryOnStart = true`, `createCanvasHmi = true` (tự sinh HMI ON/OFF), `syncMotorModel = true`.
  - Gán `visualMotorRotor` = trục/rotor của model động cơ (hoặc để tự tìm object tên chứa "rotor"/"shaft"/"gear"). Có thể gán `rotateBlades` (RotateSubmarineBlades) hoặc `virtualMotor` (VirtualMotorController).
- **`CircuitManager`** (logic mở khóa sau khi nối dây):
  - Gán `hmiPanel` + `cameraStream` để bật khi nối đúng.
  - Demo: `requiredWiresCount = 2`, `requiredSocketPairs = [Y0-Pin11, Y1-Pin9]`.
- **Camera**: gắn `MjpegStreamer3D` lên mặt phẳng màn hình ảo, `streamUrl = http://10.38.100.27:8080/?action=stream`.
- (Tuỳ chọn) **`PiGatewayHMI`** nếu muốn HMI đầy đủ (slider tốc độ, nút chiều, nhập số vòng/góc) thay cho Canvas HMI tự sinh.

### 6.3. Player Settings (WebGL)
- `Project Settings > Player > WebGL > Other Settings > Allow downloads over HTTP = Always Allowed`.
- (Nếu cần) tắt `Decompression Fallback` tuỳ cách host.

### 6.4. Build & host
- Build target **WebGL**.
- ⚠️ **Mixed content**: nếu trang WebGL phục vụ qua **HTTPS** mà gọi `http://10.38.100.27:5000` → trình duyệt chặn. Cách xử lý:
  - Host bản WebGL qua **HTTP**, hoặc
  - Đặt **HTTPS/reverse-proxy** cho Pi (rồi đổi `piBaseUrl` sang `https://`).
- Máy chạy trình duyệt phải cùng mạng **ZeroTier** với Pi (hoặc dùng IP tĩnh server thật sau này).

### 6.5. Kiểm thử luồng đầy đủ
1. Mở WebGL → nối đúng dây → CircuitManager mở HMI + camera.
2. Bấm **ON** trên HMI → `PLCController_v2.TurnOn()` → POST /control → động cơ thật quay.
3. `PLCController_v2` poll /telemetry mỗi 0.5s → model 3D quay theo `speedRpm`/`direction`.
4. Bấm **OFF** → động cơ dừng.

---

## 7. Luồng TH1 (nghiệp vụ)
Nối dây (WebGL) → nối đúng → mở HMI + camera → SV set tham số → ON → Pi → PLC chạy động cơ → telemetry phản hồi → model 3D quay khớp tham số. (Chi tiết bài 12 dây + bảng mapping dây xem `Handoff.md` cũ.)

---

## 8. Troubleshooting

| Hiện tượng | Nguyên nhân / xử lý |
|:---|:---|
| WebGL không gọi được Pi, lỗi CORS/mixed content | Host WebGL qua HTTP, hoặc HTTPS cho Pi; gateway đã có CORS. |
| `Insecure connection not allowed` | Bật Allow downloads over HTTP (6.3). |
| Build WebGL lỗi liên quan HslCommunication/SerialPort | Chưa làm Mục 6.1. |
| Động cơ không quay khi ON | Kiểm tra `journalctl -u pi-gateway-hsl -f`; xác nhận `PLC connected`; kiểm tra mapping/clamp tốc độ; đảm bảo SC-09 cắm đúng. |
| `Access to port /dev/ttyUSB0 denied` | Tiến trình khác giữ cổng (gateway cũ/ModemManager). Đã tắt ModemManager; `sudo systemctl restart pi-gateway-hsl`. |
| Model 3D không quay dù telemetry running | Gán đúng `visualMotorRotor`/`rotateBlades`/`virtualMotor` trong `PLCController_v2`. |

---

## 9. TODO còn lại
- [ ] Mục 6.1 (loại serial controller khỏi WebGL) — chưa làm.
- [ ] Đấu nối / kiểm tra map thanh ghi readback (count/speed) cho khớp ladder thật nếu telemetry sai số.
- [ ] (Tuỳ chọn) HTTPS cho Pi nếu host WebGL qua HTTPS.
- [ ] (Backend/LMS) đẩy điểm SCORM trong `CircuitManager.UnlockSystem()` (đang comment).
