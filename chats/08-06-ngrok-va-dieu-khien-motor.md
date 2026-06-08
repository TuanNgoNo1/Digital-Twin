<!--
═══════════════════════════════════════════════════════════════════════════
  HƯỚNG DẪN SỬ DỤNG FILE NÀY (đọc 30 giây là dùng được)
═══════════════════════════════════════════════════════════════════════════

  ĐÂY LÀ FILE BÀN GIAO 1 PHIÊN LÀM VIỆC VỚI AI.
  Quy ước: mỗi phiên = 1 file  chats/<ngày>-<chủ đề chính>.md
  File mới nhất (theo ngày) = trạng thái hiện tại của dự án.
  Nhìn TÊN FILE là biết phiên đó làm về cái gì.

  ┌─ NGƯỜI NHẬN VIỆC (đầu phiên) ──────────────────────────────────────────┐
  │ 1) Lấy bản mới nhất:   git checkout master && git pull                 │
  │ 2) Mở Kiro:  kiro-cli chat   (đứng trong thư mục dự án)                 │
  │ 3) Câu ĐẦU TIÊN gõ cho AI:                                             │
  │      "Đọc file chats/<file mới nhất>.md rồi tiếp tục từ mục            │
  │       '➡️ CẦN LÀM TIẾP'."                                              │
  │    → AI hiểu ngay đã/đang/cần làm gì, không phải hỏi lại người cũ.     │
  └────────────────────────────────────────────────────────────────────────┘

  ┌─ NGƯỜI BÀN GIAO (cuối phiên) ──────────────────────────────────────────┐
  │ 1) Bảo AI: "Viết tổng kết phiên này vào                                │
  │      chats/<ngày>-<chủ đề>.md theo đúng 4 mục như file mẫu."           │
  │ 2) Đẩy lên GitHub (branch master):                                     │
  │      git add chats/                                                     │
  │      git commit -m "chat <ngày>: <tóm tắt ngắn>"                        │
  │      git push origin master                                            │
  └────────────────────────────────────────────────────────────────────────┘

  GIỮ 4 MỤC: ✅ Đã làm / 🔧 Đang dở / ➡️ Cần làm tiếp / ⚠️ Lưu ý.
  Phần "CHI TIẾT KỸ THUẬT" cuối file = tra cứu sâu khi cần.
  (Tùy chọn) muốn AI đọc nguyên văn hội thoại cũ: /chat save
  chats/<ngày>-session.json , phiên sau /chat load file đó.
═══════════════════════════════════════════════════════════════════════════
-->

# Phiên 2026-06-08 — Ngrok tunnel + điều khiển motor (thật & ảo) — Tuấn

> **Dự án:** Digital Twin (Unity WebGL/SCORM) điều khiển động cơ thật + xem camera qua Internet cho LMS.
> **Phần cứng:** Raspberry Pi (ZeroTier `10.38.100.27`, user `admin`) ↔ PLC Mitsubishi FX ↔ motor + camera USB.

---

## ✅ ĐÃ LÀM XONG

- **Đường ra Internet cố định (ngrok):** thay Cloudflare quick-tunnel (URL đổi mỗi reboot) bằng **ngrok static domain**.
  - URL public: **`https://unacquiescent-quiana-excepable.ngrok-free.dev`**
  - Motor: `…/plc/control`, `…/plc/telemetry` · Camera: `…/cam/?action=stream` (hoặc `?action=snapshot`)
  - Trên Pi: `Caddy :8888` gộp `/plc`→`localhost:5000`, `/cam`→`localhost:8080`, tự thêm CORS + xử lý preflight OPTIONS (cho phép header `ngrok-skip-browser-warning`).
- **Service trên Pi tự bật khi boot** (đã `enable`): `ngrok`, `caddy-proxy`, `ustreamer-cam`, `pi-gateway-hsl`. Đã tắt `cloudflared-quick` cũ. Đã sửa lỗi ngrok chết khi mạng chưa lên + camera crash-loop.
- **Pi ↔ PLC thông hoàn toàn** (đọc/ghi thanh ghi + bit OK; PLC RUN, không lỗi).
- **Tìm ra cách điều khiển motor đúng** → viết lại gateway + Unity → **motor thật + motor ảo quay được**, tự dừng đúng số vòng. Test end-to-end qua ngrok OK.
- **Unity đã sửa & scene đã trỏ URL ngrok.**

---

## 🔧 ĐANG LÀM DỞ / CHƯA XONG

- **Chưa Build WebGL/SCORM** bản mới (code đã xong, chỉ còn build).
- `Program.cs` gateway nằm **trên Pi** (ngoài repo) — mới có backup `.bak` trên Pi.
- **Chưa test reboot Pi thật** để xác nhận tự phục hồi đúng URL (mới chỉ cấu hình).

---

## ➡️ CẦN LÀM TIẾP (việc cho người sau)

1. **Mở Unity** → đợi recompile `PLCController_v2.cs` (Console không lỗi đỏ).
2. **Play test:** HMI bấm **+** vài lần (tăng tốc) → chọn chiều → đặt số vòng (SET) → **START** → motor thật + ảo quay, tự dừng. Kiểm tra camera hiện hình.
3. **Build WebGL/SCORM** (1 lần là đủ vì URL ngrok cố định) → upload LMS → test (F12 Console không lỗi PNA/CORS).
4. (Nên) copy `Program.cs` gateway từ Pi vào repo để lưu lịch sử.
5. (Nên) **Reboot Pi 1 lần** để xác nhận tự phục hồi đúng URL.

---

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT

**Điều khiển PLC (chỗ tốn thời gian nhất phiên này):**
- Tốc độ `D128` **KHÔNG ghi thẳng được** (ladder ghi đè về 0) → tăng/giảm bằng **xung M15 (tăng) / M16 (giảm)**. `5000 xung = 1 vòng`.
- Chuỗi để motor quay: **tốc độ (M15/M16) → số vòng `D112` → chiều `M2`/`M8` → chế độ `M4` → start `M1`**. Chỉ bấm `M1` mà không có chế độ M4/M5 → motor KHÔNG quay.

**Cạm bẫy phần cứng / vận hành:**
- Reset PLC → `D128` về 0 → bấm **+** lại trước khi START.
- Cáp SC-09 (CH340): sau power-cycle, nếu **TXD không nháy** → **rút/cắm lại cáp USB** hoặc `sudo systemctl restart pi-gateway-hsl`.

**Rủi ro:**
- Gateway **không có xác thực** mà public qua ngrok → ai có URL đều điều khiển motor được. Endpoint `/writetest` còn **pulse motor** — cân nhắc xóa/đặt token. ⚠️ Nếu repo này public, URL ngrok bị lộ.
- ngrok free: quota băng thông; camera 15fps ra Internet khá nặng.
- Encoder (`D164`) = 0 trong test → có thể chưa đấu encoder (bước 2). Không cản motor quay nhưng chưa có phản hồi vị trí thật.

═══════════════════════════════════════════════════════════════════════════
# CHI TIẾT KỸ THUẬT (tra cứu sâu khi cần)
═══════════════════════════════════════════════════════════════════════════

## 1. Kiến trúc

```
Người dùng (LMS, trình duyệt)
        │  HTTPS (URL ngrok cố định)
        ▼
   ngrok  ──►  Caddy :8888 (trên Pi, gộp + CORS)
                   ├── /plc/*  ──► localhost:5000  (PiGatewayHsl, .NET) ──serial 9600 7E1──► PLC FX ──► Motor
                   └── /cam/*  ──► localhost:8080  (uStreamer, camera USB)
```
Trang LMS (HTTP) gọi URL ngrok (HTTPS) → hết PNA / Mixed Content / CORS. Mọi dịch vụ chạy trên Pi, không đụng máy LMS.

## 2. Service trên Pi (đều `enabled`, tự bật khi boot)

| Service | Vai trò | Lệnh chính |
|---|---|---|
| `ngrok.service` | Tunnel static domain | `ngrok http 8888 --domain=unacquiescent-quiana-excepable.ngrok-free.dev --log=stdout` |
| `caddy-proxy.service` | Gộp /plc + /cam + CORS | `caddy run --config /home/admin/proxy/Caddyfile` |
| `ustreamer-cam.service` | Camera USB (8080) | `ustreamer --device=/dev/video0 --host=0.0.0.0 --port=8080 --resolution=640x480 --desired-fps=15` |
| `pi-gateway-hsl.service` | Gateway HTTP→PLC | `dotnet …/PiGatewayHsl/bin/Release/net8.0/PiGatewayHsl.dll` |
| `cloudflared-quick.service` | (CŨ — đã disable) | — |

- ngrok: binary `/usr/local/bin/ngrok`, authtoken ở `/home/admin/.config/ngrok/ngrok.yml`. Service có `After/Wants=network-online.target` (sửa lỗi chết lúc mạng chưa lên).
- ustreamer-cam: `ExecStartPre` dùng `pkill -9 -f 'ustreamer --device'` (sửa crash-loop "Address already in use").
- Caddyfile `/home/admin/proxy/Caddyfile`: listen `:8888`, preflight OPTIONS→204, cho phép header `Content-Type, ngrok-skip-browser-warning`, thêm CORS cho /cam.

## 3. Gateway PLC — `/home/admin/PiGatewayHsl/Program.cs`

- .NET 8 (SDK có sẵn trên Pi), HslCommunication `MelsecFxSerial`. Serial `/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0` (= `/dev/ttyUSB0`, CH340), **9600 / 7 / Even / 1 (7E1)**.
- Build lại: `cd ~/PiGatewayHsl && dotnet build -c Release && sudo systemctl restart pi-gateway-hsl`. Có backup `Program.cs.bak.*`.
- Endpoints: `/control` (POST), `/telemetry` (GET), `/debug` (GET, đọc nhiều thanh ghi), `/writetest` (POST, chẩn đoán — CÓ pulse motor), `/recover` (POST).

**Bản đồ địa chỉ PLC (ladder FX):**
| Tên | Địa chỉ | Ý nghĩa | Ghi chú |
|---|---|---|---|
| Start | `M1` | Khởi động (xung) | |
| Thuận / Ngược | `M2` / `M8` | Chiều (xung) | |
| Stop | `M17` | Dừng (xung) | |
| **Tăng/Giảm tốc** | `M15` / `M16` | **+/− tốc độ (xung)** | **cách duy nhất chỉnh tốc độ** |
| Chế độ vòng / góc | `M4` / `M5` | Chạy theo vòng / góc (xung) | |
| Tốc độ/tần số | `D128` (≈`D100`) | Tốc độ đặt | **chỉ ĐỌC; ghi thẳng bị ladder ghi đè** |
| Số vòng / Góc | `D112` / `D114` | Mục tiêu | Ghi Word OK |
| Số xung | `D104` | = vòng × 5000 | Đọc |
| RUN / Error | `M8000` / `M8061` | Trạng thái PLC | Đọc |

- `5000 xung = 1 vòng`. RPM thật = `D128 / 5000 × 60`.
- **Chuỗi ON đã viết trong gateway:** chiều (M2/M8) → ghi D112/D114 + chế độ M4/M5 → start M1; telemetry tự đặt `running=false` khi hết thời gian dự kiến (`số xung ÷ tần số`).
- Action `/control`: `ON`, `OFF`, `SPEED_UP`(field `speed`=số xung M15), `SPEED_DOWN`(M16), `SET_ROTATIONS`(D112), `SET_ANGLE`(D114), `SET_DIRECTION`, `RESET_COUNTER`, `RESET`, `ERR_RESET`.

## 4. Thay đổi phía Unity (`Assets/`)

- `PLCController_v2.cs`: thêm `SpeedUp/SpeedDown` (M15/M16); nút **+/−** gửi M15/M16; `TurnOn` mặc định 5 vòng nếu chưa đặt; **sửa quy đổi tốc độ motor ảo** = `tần số/5000×60` (vòng/phút) — trước đây dùng `rpm×6` sai đơn vị; thêm header `ngrok-skip-browser-warning` vào `/control` + `/telemetry`; HMI hiển thị tốc độ + vòng/phút.
- `MjpegStreamer3D.cs`, `MjpegStreamer.cs`: thêm header `ngrok-skip-browser-warning`.
- `Scenes/Sy_scene.unity`: `piBaseUrl` → `…/plc`, `url` → `…/plc/control`, `streamUrl` → `…/cam/?action=stream`.

## 5. Cheat-sheet vận hành

```bash
# SSH vào Pi: admin@10.38.100.27 (ZeroTier)
systemctl is-active ngrok caddy-proxy ustreamer-cam pi-gateway-hsl   # trạng thái
curl -s http://localhost:5000/debug                                  # đọc thanh ghi PLC
# test: tăng tốc rồi chạy 3 vòng thuận
curl -X POST http://localhost:5000/control -H "Content-Type: application/json" --data '{"action":"SPEED_UP","speed":12}'
curl -X POST http://localhost:5000/control -H "Content-Type: application/json" --data '{"action":"ON","direction":"forward","rotations":3}'
curl -X POST http://localhost:5000/control -H "Content-Type: application/json" --data '{"action":"OFF"}'
# build lại gateway
cd ~/PiGatewayHsl && dotnet build -c Release && sudo systemctl restart pi-gateway-hsl
# test public qua ngrok (cần header)
curl -H "ngrok-skip-browser-warning: true" "https://unacquiescent-quiana-excepable.ngrok-free.dev/cam/?action=snapshot" -o test.jpg
```

## 6. Tham chiếu nhanh

| Mục | Giá trị |
|---|---|
| Pi (ZeroTier) | `10.38.100.27`, user `admin` |
| URL public | `https://unacquiescent-quiana-excepable.ngrok-free.dev` |
| Gateway source | `/home/admin/PiGatewayHsl/Program.cs` (.NET 8, HslCommunication) |
| Caddyfile | `/home/admin/proxy/Caddyfile` (`:8888`) |
| Serial PLC | `/dev/ttyUSB0` (CH340), 9600 7E1 |
| Camera | uStreamer `/dev/video0` 640x480@15fps, port 8080 |
| Unity controller / camera | `Assets/PLCController_v2.cs` / `Assets/MjpegStreamer3D.cs`, `MjpegStreamer.cs` |
| Scene | `Assets/Scenes/Sy_scene.unity` |
| Git repo | `github.com/TuanNgoNo1/Digital-Twin` (master) |
