# Tech stack hiện tại — P-DTwin / Digital Twin PLC

> Cập nhật: 2026-07-01. Đây là tài liệu onboarding cho người mới vào dự án sau khi gateway
> được chuyển từ Raspberry Pi/ngrok sang Windows.

## 1. Mục tiêu hệ thống

Hệ thống cho phép một sinh viên:

1. Mở P-DTwin bằng trình duyệt.
2. Đăng nhập và chạy Unity WebGL.
3. Xem camera của mô hình thật.
4. Đọc telemetry PLC.
5. Gửi lệnh điều khiển motor theo chiều, tốc độ, số vòng hoặc góc.
6. Backend lưu dữ liệu ứng dụng vào database H2.

## 2. Kiến trúc tổng thể

```text
Browser / Unity WebGL
        │
        │ HTTP http://103.238.69.131:8080
        ▼
     Caddy 2.11.4 — Windows SERVER-LAB602
        ├── /          → Spring Boot 3.2.5 / Java 17 / React / H2
        ├── /cam/*     → static snapshot JPEG do FFmpeg tạo
        └── /plc/*     → Python 3.11 fxplc gateway
                              │
                              │ COM3 / CH340 / SC09 / 9600 7E1
                              ▼
                       PLC Mitsubishi FX
                              │
                              ▼
                            Motor
```

Máy sinh viên chỉ cần trình duyệt; không cần cài Tailscale. Tailscale được giữ để quản trị
riêng máy server.

## 3. Nền tảng và phiên bản

| Lớp | Công nghệ | Phiên bản/trạng thái |
|---|---|---|
| OS gateway | Windows Pro 25H2 x64 | build `26200.8655` |
| Unity | Unity 6 | `6000.3.11f1` |
| Web client | Unity WebGL + React frontend | React được bundle trong backend JAR |
| Backend | Spring Boot | `3.2.5` |
| Java runtime | Eclipse Temurin | `17.0.19+10` |
| App database | H2 | `2.2.224`, profile `h2` |
| Auth app | Spring Security + JWT | JJWT `0.11.5` |
| DB driver dự phòng | MySQL Connector/J | `8.3.0` có trong JAR nhưng chưa dùng |
| Reverse proxy | Caddy | `2.11.4` |
| PLC gateway | Python + `fxplc` MIT + pyserial | Python `3.11.9`, pyserial `3.5` |
| Camera | FFmpeg DirectShow | `8.1.2` |
| Private remote admin | Tailscale | `1.98.8` |
| PLC engineering | GX Works2 | project `server-to-plc.gxw` |

## 4. Máy và network

| Mục | Giá trị |
|---|---|
| Hostname | `SERVER-LAB602` |
| Adapter | `Ethernet 2` |
| LAN tĩnh | `10.170.43.240/24` |
| Gateway | `10.170.43.1` |
| DNS | `8.8.8.8`, `8.8.4.4` |
| Tailscale | `100.118.190.85` |
| Public IP đang dùng | `103.238.69.131` |
| Public app port | TCP `8080` |

Public port 8080 hiện được mạng upstream/NAT chuyển về `10.170.43.240:8080`. Mạng có
nhiều private hops; chưa có bằng chứng hợp đồng rằng `103.238.69.131` là IP tĩnh dành riêng.

## 5. Public URL contract

| Consumer | URL |
|---|---|
| P-DTwin frontend/backend | `http://103.238.69.131:8080/` |
| Camera page | `http://103.238.69.131:8080/cam/` |
| Camera snapshot | `http://103.238.69.131:8080/cam/snapshot.jpg` |
| PLC health | `http://103.238.69.131:8080/plc/health` |
| PLC telemetry | `http://103.238.69.131:8080/plc/telemetry` |
| PLC debug | `http://103.238.69.131:8080/plc/debug` |
| PLC control | `POST http://103.238.69.131:8080/plc/control` |

## 6. Caddy và quy tắc bind port

Caddy config:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile
```

Caddy nghe:

- `:80` trên LAN/Tailscale.
- `10.170.43.240:8080` cho public NAT.

Java nghe cùng số port nhưng khác địa chỉ:

```text
127.0.0.1:8080
```

Điều này là bắt buộc. Nếu Java bind `0.0.0.0:8080` hoặc `::8080`, Java sẽ chiếm port của
Caddy; `/cam` và `/plc` không còn được phân tuyến.

Caddy hiện:

- `admin off`.
- `auto_https off`.
- HTTP only.
- CORS `Access-Control-Allow-Origin: *`.
- Public `/plc/control` không có auth.

## 7. Backend/application layer

Deploy directory:

```text
C:\Users\Server-Lab602\Ptwin\sendRM
├── pdtwin-backend-0.0.1-SNAPSHOT.jar
├── start-server.bat
├── data\
│   ├── pdtwin.mv.db
│   ├── pdtwin.trace.db
│   └── pdtwin.lock.db
├── uploads\
└── unity-builds\
```

Runtime command tương đương:

```powershell
java -jar pdtwin-backend-0.0.1-SNAPSHOT.jar `
  --spring.profiles.active=h2 `
  --app.upload.dir=uploads/ `
  --app.unity.dir=unity-builds `
  --server.address=127.0.0.1 `
  --server.port=8080
```

Backend có:

- React frontend tĩnh trong JAR.
- Spring Security/JWT cho API ứng dụng.
- Proxy camera `/api/proxy/camera`.
- H2 database local.

Backend chưa có PLC proxy được bảo vệ bằng JWT; Caddy đang proxy `/plc/*` trực tiếp tới
Python gateway.

## 8. PLC gateway

Source/runtime:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\PiGatewayFxplc
├── gateway.py
├── requirements.txt
├── .venv-win\
└── vendor\fxplc\
```

Environment production hiện tại:

```text
FXPLC_SERIAL_PORT=COM3
FXPLC_HTTP_HOST=127.0.0.1
FXPLC_HTTP_PORT=5000
FXPLC_ALLOW_WRITES=1
FXPLC_PULSE_SECONDS=0.1
FXPLC_ENCODER_PULSES_PER_REV=5000
```

Serial:

```text
CH340/SC09 — COM3 — 9600 baud — 7 data bits — Even — 1 stop bit
```

API:

| Method | Path | Vai trò |
|---|---|---|
| GET | `/health` | Process/config status |
| GET | `/telemetry` | Telemetry gọn cho Unity |
| GET | `/debug` | Đọc register/bit chi tiết |
| POST | `/control` | Gửi action điều khiển |

Action control:

```text
ON
OFF
SET_SPEED
SPEED_UP
SPEED_DOWN
SET_ROTATIONS
SET_ANGLE
SET_DIRECTION
RESET_COUNTER
RESET
ERR_RESET
```

### PLC memory map

| Device | Kiểu | Ý nghĩa |
|---|---|---|
| `M1` | bit/pulse | Start |
| `M2`, `M8` | bit/pulse | Thuận, nghịch |
| `M4`, `M5` | bit/pulse | Mode vòng, mode góc |
| `M12` | bit/pulse | Reset counter |
| `M13`, `M14` | bit/pulse | Reset, error reset |
| `M15`, `M16` | bit/pulse | Tăng, giảm tốc |
| `M17` | bit/pulse | Stop |
| `M8029` | bit | DPLSY complete |
| `D100:D101` | signed dword | DPLSY frequency |
| `D104` | word/dword theo ladder | Target pulses |
| `D110:D111` | signed dword | DPLSY pulse count |
| `D112` | word | Target rotations |
| `D114` | word | Target angle |
| `D120:D121` | signed dword | Encoder count |
| `D124:D125` | signed dword | Rotation feedback |
| `D128` | word | Legacy pulse frequency |
| `D146` | word | Speed set RPM |
| `D164` | word | Legacy speed sample |
| `D210:D211` | signed dword | Speed sample |
| `D220:D221` | signed dword | Signed speed sample |
| `D230:D231` | signed dword | Angle feedback |

### Motion conversion

```text
D100 frequency = RPM × 5000 ÷ 60
D110 pulses    = rotations × 5000
D110 pulses    = angle × 5000 ÷ 360
```

Gateway reject `ON` nếu speed hoặc target không dương; điều này tránh trường hợp
`D110=0` khiến DPLSY chạy liên tục.

## 9. Unity integration

Ba file có thể copy sang project scene hoàn thiện:

```text
Assets\PLCController_v2.cs
Assets\MjpegStreamer.cs
Assets\MjpegStreamer3D.cs
```

Không copy/xóa `.meta`.

### PLCController_v2

```text
DefaultPiBaseUrl = http://103.238.69.131:8080/plc
controlEndpoint  = /control
telemetryEndpoint = /telemetry
pollInterval = 0.5 giây
```

- Poll telemetry 2 lần/giây.
- Khi telemetry hợp lệ:
  - motor chạy → HMI xanh dương `Đang chạy`.
  - motor dừng → HMI xanh lá `Đã kết nối`.
- URL ngrok cũ được nhận diện là legacy và tự thay bằng URL hiện tại.

### Camera scripts

```text
http://103.238.69.131:8080/cam/snapshot.jpg
```

- Thêm timestamp chống cache.
- Refresh tối thiểu `0.2s` = tối đa 5 request/giây.
- Không còn phụ thuộc MJPEG/ngrok.

## 10. Camera pipeline

```text
A4 tech USB2.0 Camera
        │ DirectShow 640x480 @ 15fps
        ▼
FFmpeg
        │ fps=5, JPEG q=5, update same file
        ▼
camera_www\snapshot.jpg
        │
        ▼
Caddy /cam/snapshot.jpg
        │
        ▼
Browser / Unity WebGL
```

Script:

```text
Digital-Twin-main\Start-CameraSnapshot.ps1
```

Nếu camera page trả 403 nhưng backend `/` vẫn chạy, kiểm tra Java có bind wildcard 8080 và
Caddy đã dừng hay không.

## 11. Vận hành GX Works2

COM3 chỉ có một owner.

### Vào GX Works

Double-click:

```text
Desktop\1 - GX Works Mode.lnk
```

Script dừng gateway, đợi port 5000 đóng, rồi mở:

```text
C:\Users\Server-Lab602\Desktop\server-to-plc.gxw
```

### Quay lại gateway

1. Save và đóng GX Works.
2. Double-click:

```text
Desktop\2 - PLC Gateway Mode.lnk
```

Gateway production được bật lại với quyền ghi.

## 12. Startup và độ sẵn sàng

Startup shortcut:

```text
C:\Users\Server-Lab602\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\
DigitalTwinCamStack.lnk
```

Target:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Start-CamStack.ps1
```

Stack script bật:

```text
Start-CameraSnapshot.ps1
Start-BackendLoopback.ps1
Start-PlcGateway.ps1
Start-Caddy.ps1
```

### Giới hạn hiện tại

- Chỉ chạy sau khi user Windows đăng nhập.
- Không có Windows Service/Scheduled Task.
- Không có watchdog/restart on failure.
- Camera/CH340 phụ thuộc USB.
- AC sleep/hibernate đã là Never; DC sleep vẫn có giá trị nhưng máy server nên luôn cắm điện.

Muốn 24/7 đúng nghĩa cần Scheduled Task/Windows Services, watchdog, BIOS restore after AC
loss, USB power management phù hợp và tốt nhất có UPS.

## 13. Health check

```powershell
# Local
curl.exe http://127.0.0.1:8080/
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:5000/telemetry
curl.exe http://127.0.0.1:5000/debug

# Public
curl.exe http://103.238.69.131:8080/
curl.exe http://103.238.69.131:8080/plc/health
curl.exe http://103.238.69.131:8080/plc/telemetry
curl.exe -I http://103.238.69.131:8080/cam/snapshot.jpg
```

Kỳ vọng:

- HTTP 200.
- `/health`: `allowWrites=true`, `serialPort=COM3`.
- `/telemetry`: `backendSynced=true`.
- Snapshot: `Content-Type: image/jpeg`, kích thước khác 0.

## 14. Dữ liệu và backup

Cần backup:

```text
C:\Users\Server-Lab602\Ptwin\sendRM\data\pdtwin.mv.db
C:\Users\Server-Lab602\Ptwin\sendRM\uploads\
C:\Users\Server-Lab602\Ptwin\sendRM\unity-builds\
```

Không copy nóng file H2 khi backend đang ghi. Dừng Java hoặc dùng chức năng backup H2 phù hợp.

## 15. Security — trạng thái phải biết

Hiện tại:

- HTTP, không HTTPS.
- CORS `*`.
- `/plc/control` public trực tiếp.
- `FXPLC_ALLOW_WRITES=1`.
- JWT của Spring backend không bảo vệ `/plc/*`.

Điều này tương đương mô hình ngrok cũ: tiện test nhưng không phải production-safe.

Ưu tiên cải tiến:

1. Domain + HTTPS.
2. Unity gửi JWT đăng nhập.
3. Backend/Caddy validate role/session trước `/plc/control`.
4. Một active control lease tại một thời điểm.
5. Rate limit và audit log mọi control command.
6. Emergency stop luôn có đường ưu tiên.

## 16. Checklist người mới

1. Đọc `chats/01-07-windows-public-gateway-plc-camera.md`.
2. Kiểm tra bốn process Java/Caddy/Python/FFmpeg.
3. Kiểm tra ba public URL `/`, `/cam/`, `/plc/telemetry`.
4. Không mở GX Works khi gateway đang chạy.
5. Chỉ test motor khi khu vực an toàn và có emergency stop.
6. Trước build WebGL, copy ba file `.cs`, giữ `.meta`.
7. Không commit secret hoặc database có dữ liệu sinh viên.

## 17. Known gaps

- WebGL bản scene hoàn thiện chưa nghiệm thu end-to-end.
- Public PLC control chưa auth.
- Chưa HTTPS/domain.
- Chưa service/watchdog 24/7.
- Chưa hiệu chỉnh sai số encoder/coasting.
- Backend source chưa nằm trong repo hiện tại.
- Chưa có backup định kỳ/restore drill.

---

Biên bản phiên migration hiện tại:
[`chats/01-07-windows-public-gateway-plc-camera.md`](chats/01-07-windows-public-gateway-plc-camera.md).
