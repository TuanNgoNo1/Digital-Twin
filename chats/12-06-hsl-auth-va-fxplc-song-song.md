# 📖 CÁCH DÙNG TÀI LIỆU NÀY (đọc 30 giây là dùng được)

> Đây là **file bàn giao 1 phiên làm việc với AI**. Quy ước: mỗi phiên = 1 file `chats/<ngày>-<chủ đề chính>.md`.
> File mới nhất (theo ngày) = trạng thái hiện tại của dự án. **Nhìn tên file là biết phiên đó làm gì.**

**🟢 NGƯỜI NHẬN VIỆC (đầu phiên):**
1. Lấy bản mới nhất:
   ```bash
   git checkout master && git pull
   ```
2. Mở Kiro trong thư mục dự án: `kiro-cli chat`
3. Câu **ĐẦU TIÊN** gõ cho AI:
   > *"Đọc file `chats/<file mới nhất>.md` rồi tiếp tục từ mục '➡️ CẦN LÀM TIẾP'."*

   → AI hiểu ngay đã/đang/cần làm gì, **không phải hỏi lại người cũ**.

**🔵 NGƯỜI BÀN GIAO (cuối phiên):**
1. Bảo AI: *"Viết tổng kết phiên này vào `chats/<ngày>-<chủ đề>.md` theo đúng 4 mục như file mẫu."*
2. Đẩy lên GitHub (branch master):
   ```bash
   git add chats/
   git commit -m "chat <ngày>: <tóm tắt ngắn>"
   git push origin master
   ```

**Quy ước nội dung:** giữ 4 mục — ✅ Đã làm / 🔧 Đang dở / ➡️ Cần làm tiếp / ⚠️ Lưu ý. Phần "CHI TIẾT KỸ THUẬT" cuối file = tra cứu sâu khi cần.
**(Tùy chọn)** muốn AI đọc nguyên văn hội thoại cũ: `/chat save chats/<ngày>-session.json`, phiên sau `/chat load <file đó>`.

---

# Phiên 2026-06-12 — Chẩn đoán HSL authorization + dựng fxplc song song — Tuấn

> **Dự án:** Digital Twin Unity WebGL điều khiển PLC Mitsubishi FX qua Raspberry Pi, cáp SC09 và gateway HTTP.
> **Mục tiêu phiên:** tìm nguyên nhân SC09 không nháy TXD, đánh giá phương án license HSL và dựng gateway miễn phí `fxplc` riêng để có thể fallback về HSL.

---

## ✅ ĐÃ LÀM XONG

- **Xác định chính xác nguyên nhân TXD SC09 không nháy:**
  - Unity → ngrok → Caddy → gateway trên Pi vẫn hoạt động.
  - `/plc/debug` trả lỗi từ HslCommunication:
    `System authorization failed, need to use activation code authorization`.
  - HSL chặn thao tác trước khi gửi serial nên TXD không nháy; lỗi không phải do ngrok hay URL public.
- **Restart `pi-gateway-hsl` và xác nhận giao tiếp PLC phục hồi tạm thời:**
  - Service trở lại `active`.
  - `/debug` nội bộ và qua ngrok từng trả toàn bộ trường `ok:true`.
  - Journal từng ghi `PLC connected on /dev/serial/by-id/usb-1a86_USB_Serial-if00-port0`.
  - Lỗi authorization có thể quay lại sau khi HSL Free chạy đủ thời gian; restart chỉ là cách phục hồi tạm.
- **Phân tích DLL HSL đang dùng:**
  - `Assets/Plugins/HslCommunication.dll` là HslCommunication `12.6.3.0`, bản `net35`.
  - File này giống tuyệt đối với:
    - `/home/admin/PiGatewayHsl/libs/HslCommunication.dll`
    - `/home/admin/PiGatewayHsl/bin/Release/net8.0/HslCommunication.dll`
  - SHA-256: `09771A0BBEAF4950BA539855FA3268C5D1B0F2EFCB8C9D3E43693E48C31D17DA`.
  - Các DLL `net20`, `net451`, `netstandard2.0`, `netstandard2.1` trong package vẫn cùng cơ chế license; đổi DLL framework không chữa lỗi auth.
- **Đánh giá phương án license HSL:**
  - HSL Free chạy giới hạn khoảng 24 giờ.
  - Personal `¥240 CNY` phù hợp về tính năng kỹ thuật, nhưng cần hỏi nhà cung cấp về quyền dùng cho tổ chức.
  - Nếu triển khai dưới tên công ty/trường học thì cần xác minh điều khoản hoặc dùng Professional.
- **Tìm và chọn phương án miễn phí mã nguồn mở:**
  - Chọn [`KrystianD/fxplc`](https://github.com/KrystianD/fxplc), license MIT.
  - Hỗ trợ Mitsubishi FX qua serial và cấu hình mặc định đúng hệ thống: `9600 / 7 / Even / 1`.
  - Có đọc/ghi `M`, `D`, nhưng vẫn cần test thực tế các relay đặc biệt như `M8029`.
- **Dựng gateway `fxplc` song song, tách biệt khỏi HSL:**
  - HSL giữ nguyên tại `/home/admin/PiGatewayHsl`, service `pi-gateway-hsl`.
  - fxplc được cài riêng tại `/home/admin/PiGatewayFxplc`, service `pi-gateway-fxplc`.
  - Đã tạo backup tại `/home/admin/backups/fxplc-migration-20260612`.
  - `pi-gateway-hsl`: vẫn `enabled`, đang là gateway mặc định khi boot.
  - `pi-gateway-fxplc`: `disabled`, `inactive`, mặc định **read-only** (`FXPLC_ALLOW_WRITES=0`).
  - Hai service dùng cùng port `5000` khi được chọn, nên Unity/Caddy/ngrok không phải đổi URL.
  - Đã tạo script chọn gateway, restart và reset SC09:
    `/home/admin/PiGatewayFxplc/switch-gateway.sh`.

---

## 🔧 ĐANG LÀM DỞ / CHƯA XONG

- **fxplc đã cài nhưng chưa được test với PLC thật**, vì phiên này dừng trước bước chuyển SC09 tạm thời từ HSL sang fxplc.
- Chưa xác nhận:
  - fxplc đọc đúng `D128`, `D104`, `D146`, `D164`.
  - fxplc đọc đúng các bit `M1`, `M2`, `M8`, `M12–M17`, đặc biệt `M8029`.
  - ghi word, pulse bit, chạy/dừng motor bằng fxplc.
  - chạy lâu, reconnect và tự phục hồi sau reboot/reset SC09.
- fxplc vẫn khóa ghi; chưa được dùng làm gateway public.
- Source gateway fxplc và mã MIT đã được tạo trong workspace:
  - `gateway/fxplc_gateway/`
  - `third_party/fxplc/`
  - Các file này hiện chưa được commit cùng summary này.
- `/telemetry` của gateway HSL hiện có lỗi logic: có thể vẫn báo `backendSynced:true` dù đọc PLC thất bại; chưa sửa.

---

## ➡️ CẦN LÀM TIẾP (việc cho người sau)

1. **Test fxplc chỉ-đọc trước, không chạy motor:**
   ```bash
   /home/admin/PiGatewayFxplc/switch-gateway.sh fxplc
   curl -s http://127.0.0.1:5000/health
   curl -s http://127.0.0.1:5000/debug
   ```
   So sánh các giá trị đọc với baseline HSL.
2. **Xác nhận ghi đang bị khóa:** gọi `/control` phải bị từ chối vì `FXPLC_ALLOW_WRITES=0`.
3. **Rollback về HSL ngay sau test:**
   ```bash
   /home/admin/PiGatewayFxplc/switch-gateway.sh hsl
   curl -s http://127.0.0.1:5000/debug
   ```
4. Nếu đọc fxplc đạt yêu cầu, mới bật ghi có kiểm soát và test theo thứ tự:
   - ghi/đọc lại một word an toàn;
   - pulse `M15/M16`;
   - STOP;
   - cuối cùng mới test ON/chạy motor.
5. Test fxplc chạy lâu ít nhất 24 giờ, reset SC09 và reboot Pi trước khi cân nhắc chọn làm mặc định.
6. Sau khi fxplc được xác minh, commit riêng `gateway/fxplc_gateway/`, `third_party/fxplc/` và thay đổi `.gitignore`.
7. Dù tiếp tục HSL hay chuyển fxplc, sửa telemetry để báo `backendSynced:false` khi PLC thực sự mất liên lạc.

---

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT

- **Không thể chạy HSL và fxplc cùng lúc trên cùng cáp SC09.** Một cổng serial chỉ cho một process giữ; “song song” nghĩa là hai gateway/service riêng và chọn một cái để chạy.
- HSL và fxplc đều dùng port `5000` khi được chọn, vì vậy Caddy/ngrok/Unity giữ nguyên:
  - Caddy `/plc/*` → `localhost:5000`
  - URL public: `https://unacquiescent-quiana-excepable.ngrok-free.dev/plc`
- `switch-gateway.sh fxplc` sẽ dừng HSL rồi bật fxplc; `switch-gateway.sh hsl` làm ngược lại.
- fxplc hiện **chưa được kiểm chứng phần cứng**, không được coi là hoàn thành chỉ vì service đã cài.
- HSL Free có thể tiếp tục lỗi authorization. Lệnh khôi phục tạm:
  ```bash
  /home/admin/PiGatewayFxplc/switch-gateway.sh restart
  ```
- Reset riêng cáp CH340/SC09 rồi bật lại gateway đang chọn:
  ```bash
  /home/admin/PiGatewayFxplc/switch-gateway.sh reset-sc09
  ```
- Không commit mật khẩu Pi, activation code HSL hoặc secret ngrok vào repo.
- Không dùng DLL HSL crack/không rõ nguồn: có rủi ro pháp lý, mã độc và gửi sai lệnh tới PLC/motor.
- Endpoint gateway hiện chưa có xác thực; URL ngrok public bị lộ trong repo. Cần thêm token trước khi triển khai rộng.

═══════════════════════════════════════════════════════════════════════════
# CHI TIẾT KỸ THUẬT (tra cứu sâu khi cần)
═══════════════════════════════════════════════════════════════════════════

## 1. Kiến trúc gateway chọn một trong hai

```text
Unity WebGL / LMS
       │ HTTPS
       ▼
ngrok → Caddy :8888 → /plc/* → localhost:5000
                                      │
                     chọn một service │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
          pi-gateway-hsl                      pi-gateway-fxplc
          HslCommunication                    fxplc MIT/Python
                    │                                   │
                    └──────── SC09 9600 7E1 ────────────┘
                                      │
                                PLC Mitsubishi FX
```

## 2. Trạng thái và đường dẫn

| Thành phần | Đường dẫn / trạng thái |
|---|---|
| HSL source/runtime | `/home/admin/PiGatewayHsl` |
| HSL service | `pi-gateway-hsl` — enabled, gateway mặc định |
| fxplc source/runtime | `/home/admin/PiGatewayFxplc` |
| fxplc service | `pi-gateway-fxplc` — disabled/inactive/read-only |
| Script chọn/reset | `/home/admin/PiGatewayFxplc/switch-gateway.sh` |
| Backup trước migration | `/home/admin/backups/fxplc-migration-20260612` |
| Serial ổn định | `/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0` |
| Serial thực | `/dev/ttyUSB0`, CH340 `1a86:7523` |
| Cấu hình serial | `9600 / 7 / Even / 1` |
| Public PLC URL | `https://unacquiescent-quiana-excepable.ngrok-free.dev/plc` |

## 3. Cheat-sheet chọn gateway và phục hồi

```bash
# Xem gateway nào đang chạy
/home/admin/PiGatewayFxplc/switch-gateway.sh status

# Chọn HSL
/home/admin/PiGatewayFxplc/switch-gateway.sh hsl

# Chọn fxplc
/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc

# Restart gateway đang được chọn
/home/admin/PiGatewayFxplc/switch-gateway.sh restart

# Reset cáp SC09 rồi khởi động lại gateway đang được chọn
/home/admin/PiGatewayFxplc/switch-gateway.sh reset-sc09

# Test gateway đang chạy
curl -s http://127.0.0.1:5000/debug
```

## 4. So sánh ngắn HSL và fxplc

| Tiêu chí | HSL | fxplc |
|---|---|---|
| License | Free giới hạn thời gian; bản trả phí cần activation | MIT, miễn phí lâu dài |
| Trạng thái dự án | Đã chạy thực tế với PLC/motor | Đã cài, chưa test phần cứng |
| Ngôn ngữ | C#/.NET 8 gateway | Python 3.11 |
| Serial SC09 | Đã xác nhận `9600 7E1` | Cấu hình đúng `9600 7E1`, cần xác minh |
| Đọc/ghi M, D | Có | Có |
| Relay đặc biệt | Đã đọc được | Chưa xác nhận `M8029` |
| Rủi ro chính | Authorization/license | Tự kiểm thử, bảo trì và xử lý edge case |

## 5. Các địa chỉ cần dùng để test fxplc

| Loại | Địa chỉ |
|---|---|
| Bit điều khiển | `M1`, `M2`, `M4`, `M5`, `M8`, `M12–M17` |
| Bit đặc biệt cần xác minh | `M8029` |
| Word dữ liệu | `D104`, `D112`, `D114`, `D128`, `D146`, `D164` |

