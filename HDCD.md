# HƯỚNG DẪN CHUYỂN TOÀN BỘ GATEWAY TỪ RASPBERRY PI SANG SERVER 24/7

> Mục tiêu: thay Raspberry Pi bằng một workstation/server chạy 24/7 nhưng giữ nguyên luồng Unity WebGL → Internet → gateway → PLC/camera.  
> Kết quả cuối: Unity/WebGL giữ nguyên URL hiện tại; server mới điều khiển PLC qua SC09, phát camera, chạy Caddy/ngrok và cho phép chọn linh hoạt giữa `fxplc` và HSL.

---

## 0. Quyết định kiến trúc trước khi làm

### Hệ điều hành khuyến nghị

**Dùng Ubuntu Server 24.04 LTS x86_64 trên workstation/server.**

Lý do:

- Giữ nguyên mô hình Linux + systemd đang chạy trên Pi.
- Giữ được `fxplc`, Caddy, ngrok, uStreamer và script chuyển gateway.
- Chỉ cần sửa rất ít đường dẫn/service.
- Dễ vận hành từ xa bằng SSH.

Không khuyến nghị Windows Server cho lần chuyển đầu tiên vì phải viết lại systemd service, script shell, reset USB và camera service.

### Kiến trúc đích

```text
Unity WebGL / LMS
       │ HTTPS
       ▼
ngrok static domain hoặc domain riêng + IP public tĩnh
       │
       ▼
Caddy :8888
   ├── /plc/* → localhost:5000
   │                  │
   │          chọn một gateway
   │          ├── fxplc Python/MIT
   │          └── HSL .NET
   │                  │
   │              SC09 USB
   │                  │
   │            PLC Mitsubishi FX
   │
   └── /cam/* → localhost:8080 → uStreamer → camera USB
```

### Điều kiện phần cứng bắt buộc

- Server phải đặt gần bộ PLC hoặc có đường USB ổn định tới PLC.
- Chuyển cáp **SC09/CH340** từ Pi sang server.
- Chuyển camera USB **A4 Tech USB2.0 Camera** từ Pi sang server.
- PLC, motor và server phải có nguồn ổn định.
- Nên dùng UPS cho server, PLC và thiết bị mạng.

---

## 1. Trạng thái hệ thống nguồn trên Pi

| Thành phần | Giá trị hiện tại |
|---|---|
| Pi ZeroTier IP | `10.38.100.27` |
| User Pi | `admin` |
| OS Pi | Raspbian 12 Bookworm, ARM64 |
| Public URL | `https://unacquiescent-quiana-excepable.ngrok-free.dev` |
| Caddy | `:8888`, `/plc` → `5000`, `/cam` → `8080` |
| PLC serial | `/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0` |
| CH340 USB ID | `1a86:7523` |
| Serial config | `9600 / 7 / Even / 1` |
| Camera | A4 Tech USB2.0 Camera, hiện là `/dev/video0` |
| Camera stream | `640x480`, `15 FPS`, port `8080` |
| fxplc | `/home/admin/PiGatewayFxplc`, đã test đọc/ghi/motor thành công |
| HSL | `/home/admin/PiGatewayHsl`, có thể lỗi authorization |
| Caddy config | `/home/admin/proxy/Caddyfile` |
| ngrok config secret | `/home/admin/.config/ngrok/ngrok.yml` |

Trạng thái tại thời điểm viết tài liệu:

- `pi-gateway-fxplc`: active, ghi PLC đã bật.
- `pi-gateway-hsl`: failed/inactive do không được chạy đồng thời với fxplc.
- `ngrok`, `caddy-proxy`, `ustreamer-cam`: active.
- HSL và fxplc dùng chung port `5000`; chỉ một gateway được chạy tại một thời điểm.

---

## 2. Nguyên tắc migration để rollback nhanh

Không tắt Pi ngay từ đầu. Làm theo hai giai đoạn:

### Giai đoạn A — Clone và test nội bộ

- Server mới được cài đầy đủ.
- Chưa chạy ngrok static domain trên server.
- Pi vẫn phục vụ Unity/WebGL public.
- Chuyển SC09/camera sang server trong cửa sổ test ngắn.
- Test server qua LAN/ZeroTier.
- Nếu lỗi, cắm SC09/camera lại Pi và hệ thống cũ hoạt động lại.

### Giai đoạn B — Cutover public

- Dừng ngrok trên Pi.
- Bật ngrok trên server với static domain cũ.
- Test URL public.
- Giữ Pi nguyên trạng ít nhất 7 ngày để rollback.

**Không chạy cùng static ngrok domain trên Pi và server cùng lúc.**

---

## 3. Checklist chuẩn bị

### Thông tin cần có

- [ ] Quyền quản trị server.
- [ ] Quyền SSH vào Pi.
- [ ] Quyền truy cập tài khoản ngrok đang giữ static domain.
- [ ] IP LAN tĩnh cho server.
- [ ] Nếu dùng IP public trực tiếp: IP public tĩnh, domain DNS và quyền cấu hình router/firewall.
- [ ] Cửa sổ bảo trì để chuyển SC09 và camera.
- [ ] Người đứng cạnh bộ motor để bấm STOP/ngắt nguồn khi test.

### Quy tắc bảo mật

- Không commit mật khẩu, ngrok authtoken hoặc HSL activation code.
- File ngrok config phải giữ quyền `600`.
- Chưa mở port `5000`, `8080`, `8888` trực tiếp ra Internet.
- Gateway hiện chưa có authentication; ưu tiên bổ sung token trước khi triển khai production.

---

## 4. Cài Ubuntu Server và cấu hình nền

Trong lúc cài Ubuntu Server:

- Tạo user tên `admin` để giữ nguyên đường dẫn `/home/admin/...` và giảm công sửa service.
- Bật OpenSSH Server.
- Đặt hostname, ví dụ `digital-twin-server`.
- Đặt IP LAN tĩnh hoặc DHCP reservation trên router.

Sau khi cài:

```bash
sudo apt update
sudo apt full-upgrade -y
sudo apt install -y \
  curl git rsync unzip jq \
  python3 python3-venv python3-pip python3-serial \
  v4l-utils usbutils build-essential \
  libevent-dev libjpeg-dev libbsd-dev \
  ufw
```

Thêm quyền serial và camera:

```bash
sudo usermod -aG dialout,video,plugdev admin
```

Đăng xuất SSH rồi đăng nhập lại để group mới có hiệu lực:

```bash
exit
ssh admin@<SERVER_LAN_IP>
id
```

Kết quả `id` phải có `dialout` và `video`.

### ZeroTier quản trị từ xa (tùy chọn nhưng nên có)

Nếu muốn quản trị server giống Pi hiện tại:

1. Cài ZeroTier từ nguồn chính thức.
2. Join server vào cùng ZeroTier network với máy quản trị.
3. Authorize server trong trang quản lý ZeroTier.
4. Ghi lại ZeroTier IP mới của server.
5. Test SSH qua ZeroTier trước khi cutover.

Không cần đổi Unity sang ZeroTier IP nếu Unity/WebGL vẫn dùng URL ngrok public.

### Cấu hình firewall ban đầu

Nếu đang quản trị qua SSH:

```bash
sudo ufw allow OpenSSH
sudo ufw enable
sudo ufw status
```

Khi dùng ngrok, không cần mở port `5000`, `8080` hoặc `8888` ra Internet.

---

## 5. Cài phần mềm đúng kiến trúc x86_64

**Không copy binary từ Pi sang server.** Binary hiện tại trên Pi là ARM và không chạy trên workstation x86_64.

### 5.1 Cài .NET 8

```bash
sudo apt install -y dotnet-runtime-8.0 dotnet-sdk-8.0
dotnet --version
```

Nếu Ubuntu repository không có package, cài theo hướng dẫn Microsoft cho đúng phiên bản Ubuntu.

### 5.2 Cài Caddy

Cài Caddy bản x86_64 từ repository chính thức của Caddy, sau đó kiểm tra:

```bash
caddy version
command -v caddy
```

Nếu binary nằm ở `/usr/bin/caddy`, service phía dưới phải dùng `/usr/bin/caddy`, không dùng đường dẫn ARM cũ `/usr/local/bin/caddy`.

### 5.3 Cài ngrok

Cài ngrok v3 bản Linux x86_64 từ nguồn chính thức, sau đó:

```bash
ngrok version
command -v ngrok
```

Không tạo tunnel bằng static domain cũ khi Pi vẫn đang chạy ngrok.

### 5.4 Cài uStreamer

Ưu tiên package Ubuntu nếu có:

```bash
sudo apt install -y ustreamer
```

Nếu không có package, build từ source:

```bash
cd /tmp
git clone --depth 1 https://github.com/pikvm/ustreamer.git
cd ustreamer
make
sudo install -m 0755 ustreamer /usr/local/bin/ustreamer
ustreamer --version
```

Ghi lại đường dẫn thực tế:

```bash
command -v ustreamer
```

---

## 6. Sao lưu Pi trước khi chuyển

SSH vào Pi:

```bash
ssh admin@10.38.100.27
```

Tạo gói backup không chứa ngrok secret:

```bash
mkdir -p ~/migration-backup
tar -czf ~/migration-backup/digital-twin-services.tar.gz \
  ~/PiGatewayFxplc \
  ~/PiGatewayHsl \
  ~/proxy \
  /etc/systemd/system/ngrok.service \
  /etc/systemd/system/caddy-proxy.service \
  /etc/systemd/system/ustreamer-cam.service \
  /etc/systemd/system/pi-gateway-hsl.service \
  /etc/systemd/system/pi-gateway-fxplc.service

sha256sum ~/migration-backup/digital-twin-services.tar.gz \
  > ~/migration-backup/digital-twin-services.sha256
```

Copy backup về máy quản trị:

```bash
scp admin@10.38.100.27:/home/admin/migration-backup/digital-twin-services.tar.gz .
scp admin@10.38.100.27:/home/admin/migration-backup/digital-twin-services.sha256 .
sha256sum -c digital-twin-services.sha256
```

### Chuyển ngrok secret riêng

Không đưa file này vào Git hoặc gói backup thông thường:

```bash
scp admin@10.38.100.27:/home/admin/.config/ngrok/ngrok.yml ./ngrok.yml.migration
scp ./ngrok.yml.migration admin@<SERVER_LAN_IP>:/home/admin/ngrok.yml.migration
rm ./ngrok.yml.migration
```

Trên server:

```bash
mkdir -p ~/.config/ngrok
mv ~/ngrok.yml.migration ~/.config/ngrok/ngrok.yml
chmod 600 ~/.config/ngrok/ngrok.yml
```

---

## 7. Chuyển source và cấu hình sang server

Có hai cách.

### Cách nhanh nhất: copy trực tiếp từ Pi

Chạy trên server:

```bash
rsync -av admin@10.38.100.27:/home/admin/PiGatewayFxplc/ /home/admin/PiGatewayFxplc/
rsync -av admin@10.38.100.27:/home/admin/PiGatewayHsl/ /home/admin/PiGatewayHsl/
rsync -av admin@10.38.100.27:/home/admin/proxy/ /home/admin/proxy/
```

Xóa virtualenv ARM cũ và tạo lại trên x86_64:

```bash
rm -rf /home/admin/PiGatewayFxplc/.venv
python3 -m venv --system-site-packages /home/admin/PiGatewayFxplc/.venv
/home/admin/PiGatewayFxplc/.venv/bin/python -c "import serial; print(serial.__version__)"
```

Đảm bảo quyền:

```bash
sudo chown -R admin:admin \
  /home/admin/PiGatewayFxplc \
  /home/admin/PiGatewayHsl \
  /home/admin/proxy

chmod +x /home/admin/PiGatewayFxplc/switch-gateway.sh
```

### Cách quản trị tốt hơn: dùng Git

Source fxplc hiện nằm trong workspace:

```text
gateway/fxplc_gateway/
third_party/fxplc/
```

Trước khi dùng cách Git, cần commit và push hai thư mục này. Sau đó clone repo trên server và copy/deploy vào `/home/admin/PiGatewayFxplc`.

---

## 8. Sửa cấu hình cho server mới

### 8.1 Kiểm tra cổng SC09

Cắm SC09 vào server:

```bash
lsusb | grep -i '1a86:7523'
ls -l /dev/serial/by-id/
```

Mục tiêu:

```text
/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0
```

Nếu tên khác, sửa biến `FXPLC_SERIAL_PORT` trong service hoặc sửa đường dẫn trong gateway HSL.

Kiểm tra quyền:

```bash
test -r /dev/serial/by-id/usb-1a86_USB_Serial-if00-port0 && echo READ_OK
test -w /dev/serial/by-id/usb-1a86_USB_Serial-if00-port0 && echo WRITE_OK
```

### 8.2 Kiểm tra camera

Cắm camera vào server:

```bash
v4l2-ctl --list-devices
ls -l /dev/video*
```

Không mặc định camera luôn là `/dev/video0`. Nếu camera thành `/dev/video2`, sửa service camera.

Test camera:

```bash
ustreamer \
  --device=/dev/video0 \
  --host=127.0.0.1 \
  --port=8080 \
  --resolution=640x480 \
  --desired-fps=15
```

Mở terminal khác:

```bash
curl -o /tmp/camera-test.jpg "http://127.0.0.1:8080/?action=snapshot"
file /tmp/camera-test.jpg
```

Dừng uStreamer test bằng `Ctrl+C`.

### 8.3 Caddyfile

Giữ nội dung Caddyfile hiện tại tại:

```text
/home/admin/proxy/Caddyfile
```

Kiểm tra cú pháp:

```bash
sudo caddy validate --config /home/admin/proxy/Caddyfile --adapter caddyfile
```

### 8.4 Gateway fxplc

Service fxplc phải có:

```ini
Environment=PYTHONPATH=/home/admin/PiGatewayFxplc/vendor/fxplc/src
Environment=FXPLC_HTTP_HOST=127.0.0.1
Environment=FXPLC_HTTP_PORT=5000
Environment=FXPLC_ALLOW_WRITES=1
```

Lần test đầu tiên trên server nên đổi tạm:

```ini
Environment=FXPLC_ALLOW_WRITES=0
```

Chỉ bật lại `1` sau khi test đọc thành công.

### 8.5 Gateway HSL

- Có thể giữ để fallback.
- HSL Free vẫn có thể lỗi authorization sau khoảng 24 giờ.
- HSL DLL/.NET assembly có thể chạy trên x86_64, nhưng phải build/test lại.

Build lại HSL trên server:

```bash
cd /home/admin/PiGatewayHsl
dotnet build -c Release
```

Không coi HSL là phương án production lâu dài nếu chưa mua license.

---

## 9. Cài systemd services trên server

Tạo hoặc copy các unit từ gói backup:

```bash
sudo cp /home/admin/PiGatewayFxplc/pi-gateway-fxplc.service \
  /etc/systemd/system/pi-gateway-fxplc.service
```

Tạo các unit còn lại theo nội dung đã backup, nhưng sửa đường dẫn binary:

- Caddy: dùng kết quả `command -v caddy`.
- ngrok: dùng kết quả `command -v ngrok`.
- uStreamer: dùng kết quả `command -v ustreamer`.
- .NET: dùng kết quả `command -v dotnet`.

Ví dụ camera service:

```ini
[Unit]
Description=uStreamer camera
After=network.target

[Service]
ExecStartPre=-/bin/sh -c "pkill -9 -f 'ustreamer --device' || true; sleep 1"
ExecStart=/usr/local/bin/ustreamer --device=/dev/video0 --host=127.0.0.1 --port=8080 --resolution=640x480 --desired-fps=15
Restart=always
RestartSec=3
User=admin

[Install]
WantedBy=multi-user.target
```

Ví dụ Caddy service:

```ini
[Unit]
Description=Caddy PLC/Cam proxy
After=network.target

[Service]
ExecStart=/usr/bin/caddy run --config /home/admin/proxy/Caddyfile --adapter caddyfile
Restart=always
User=admin

[Install]
WantedBy=multi-user.target
```

Reload:

```bash
sudo systemctl daemon-reload
```

Ở giai đoạn clone, chỉ bật Caddy/camera/gateway; chưa bật ngrok:

```bash
sudo systemctl enable --now caddy-proxy
sudo systemctl enable --now ustreamer-cam
sudo systemctl disable ngrok
```

Chọn fxplc làm gateway mặc định:

```bash
sudo systemctl disable pi-gateway-hsl
sudo systemctl enable pi-gateway-fxplc
```

---

## 10. Test server nội bộ trước cutover

### 10.1 Test service

```bash
systemctl is-active caddy-proxy ustreamer-cam pi-gateway-fxplc
systemctl is-enabled caddy-proxy ustreamer-cam pi-gateway-fxplc
```

### 10.2 Test fxplc chỉ-đọc

Đảm bảo `FXPLC_ALLOW_WRITES=0`, sau đó:

```bash
sudo systemctl restart pi-gateway-fxplc
curl -s http://127.0.0.1:5000/health | jq
curl -s http://127.0.0.1:5000/debug | jq
curl -s http://127.0.0.1:5000/telemetry | jq
```

Điều kiện đạt:

- `gateway = fxplc`
- `allowWrites = false`
- Các trường `/debug` trả `ok:true`
- `backendSynced = true`

### 10.3 Test ghi an toàn

Sau khi đọc đạt, bật:

```ini
Environment=FXPLC_ALLOW_WRITES=1
```

Reload/restart:

```bash
sudo systemctl daemon-reload
sudo systemctl restart pi-gateway-fxplc
```

Test theo thứ tự:

1. `SPEED_UP` một pulse, không START.
2. Kiểm tra `D128` thay đổi.
3. Ghi `SET_ROTATIONS = 1`, không START.
4. Kiểm tra `D112 = 1`.
5. Gửi `OFF`.
6. Xác nhận mọi bit điều khiển đều OFF.

```bash
curl -s -X POST http://127.0.0.1:5000/control \
  -H "Content-Type: application/json" \
  --data '{"action":"SPEED_UP","speed":1}'

curl -s http://127.0.0.1:5000/debug | jq

curl -s -X POST http://127.0.0.1:5000/control \
  -H "Content-Type: application/json" \
  --data '{"action":"SET_ROTATIONS","rotations":1}'

curl -s -X POST http://127.0.0.1:5000/control \
  -H "Content-Type: application/json" \
  --data '{"action":"OFF"}'
```

Chỉ test START khi có người đứng cạnh motor:

```bash
curl -s -X POST http://127.0.0.1:5000/control \
  -H "Content-Type: application/json" \
  --data '{"action":"ON","direction":"forward","rotations":1}'
```

### 10.4 Test Caddy nội bộ

```bash
curl -s http://127.0.0.1:8888/plc/health | jq
curl -s http://127.0.0.1:8888/plc/debug | jq
curl -o /tmp/caddy-camera.jpg "http://127.0.0.1:8888/cam/?action=snapshot"
```

### 10.5 Test từ máy khác trong LAN/ZeroTier

```bash
curl -s http://<SERVER_LAN_OR_ZEROTIER_IP>:8888/plc/health
```

Nếu firewall chặn, chỉ mở `8888` cho LAN/ZeroTier trong giai đoạn test, không mở toàn Internet.

---

## 11. Cutover public nhanh nhất: giữ nguyên ngrok static domain

Đây là cách giúp Unity/WebGL tiếp tục như chưa thay gateway.

### 11.1 Trước cutover

- Server đã test đủ PLC + camera + Caddy.
- Unity/WebGL vẫn đang trỏ:

```text
https://unacquiescent-quiana-excepable.ngrok-free.dev/plc
https://unacquiescent-quiana-excepable.ngrok-free.dev/cam/?action=stream
```

### 11.2 Dừng tunnel trên Pi

```bash
ssh admin@10.38.100.27
sudo systemctl stop ngrok
sudo systemctl disable ngrok
```

### 11.3 Bật tunnel trên server

Trên server, kiểm tra unit `ngrok.service` dùng binary x86_64 đúng đường dẫn:

```ini
[Unit]
Description=ngrok tunnel (static domain -> Caddy 8888)
After=network-online.target caddy-proxy.service
Wants=network-online.target

[Service]
ExecStart=/usr/local/bin/ngrok http 8888 --domain=unacquiescent-quiana-excepable.ngrok-free.dev --log=stdout
Restart=always
RestartSec=5
User=admin

[Install]
WantedBy=multi-user.target
```

Sau đó:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now ngrok
systemctl status ngrok --no-pager
```

Nếu ngrok báo endpoint/domain đang online ở nơi khác, xác nhận Pi đã dừng ngrok rồi mới thử lại.

### 11.4 Test public

```bash
curl -s \
  -H "ngrok-skip-browser-warning: true" \
  "https://unacquiescent-quiana-excepable.ngrok-free.dev/plc/health" | jq

curl -s \
  -H "ngrok-skip-browser-warning: true" \
  "https://unacquiescent-quiana-excepable.ngrok-free.dev/plc/debug" | jq

curl -o /tmp/public-camera.jpg \
  -H "ngrok-skip-browser-warning: true" \
  "https://unacquiescent-quiana-excepable.ngrok-free.dev/cam/?action=snapshot"
```

Unity/WebGL không cần rebuild nếu URL public giữ nguyên.

---

## 12. Phương án dùng IP public tĩnh và domain riêng

IP public tĩnh không tự tạo HTTPS. WebGL nên gọi một **domain HTTPS**, không gọi IP HTTP trực tiếp.

Quy trình:

1. Mua hoặc dùng một domain/subdomain, ví dụ `digital-twin.example.edu.vn`.
2. Tạo DNS `A` record trỏ về IP public tĩnh.
3. Router NAT port `80/443` về server.
4. Mở firewall `80/443`.
5. Đổi Caddyfile sang domain thật để Caddy tự cấp TLS.
6. Thêm authentication/token trước khi public điều khiển motor.
7. Đổi URL trong Unity rồi build WebGL lại.

Chỉ chuyển sang phương án này sau khi hệ thống chạy ổn với ngrok. Ngrok là con đường cutover nhanh và rollback dễ nhất.

---

## 13. Test nghiệm thu sau cutover

### Dịch vụ

- [ ] Server boot lại, Caddy/camera/fxplc/ngrok tự chạy.
- [ ] `systemctl --failed` không có service Digital Twin lỗi.
- [ ] `/plc/health`, `/plc/debug`, `/plc/telemetry` hoạt động.
- [ ] Camera snapshot và stream hoạt động.

### PLC/motor

- [ ] Đọc `D104`, `D112`, `D114`, `D128`, `D146`, `D164`.
- [ ] Đọc `M1`, `M2`, `M8`, `M12–M17`, `M8029`.
- [ ] Tăng/giảm tốc qua `M15/M16`.
- [ ] Chọn thuận/ngược.
- [ ] Chạy 1 vòng rồi dừng.
- [ ] Chạy theo góc rồi dừng.
- [ ] STOP hoạt động ngay.

### Unity/WebGL

- [ ] Unity Editor nhận telemetry.
- [ ] Unity điều khiển motor thật.
- [ ] Motor ảo đồng bộ theo telemetry hiện tại.
- [ ] WebGL qua ngrok hoạt động.
- [ ] Camera hiển thị trong WebGL.
- [ ] F12 Console không có CORS/PNA/Mixed Content error.

### Độ bền

- [ ] Chạy liên tục 24 giờ.
- [ ] Chạy liên tục 72 giờ.
- [ ] Rút/cắm SC09 rồi phục hồi.
- [ ] Reset PLC rồi phục hồi.
- [ ] Reboot server rồi tự phục hồi.
- [ ] Mất mạng rồi ngrok tự kết nối lại.

---

## 14. Vận hành hằng ngày trên server

### Xem trạng thái

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh status
systemctl is-active ngrok caddy-proxy ustreamer-cam pi-gateway-fxplc pi-gateway-hsl
curl -s http://127.0.0.1:5000/debug | jq
```

### Chọn gateway

```bash
# Dùng fxplc
/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc

# Dùng HSL
/home/admin/PiGatewayFxplc/switch-gateway.sh hsl
```

### Restart gateway đang chạy

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh restart
```

### Reset SC09

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh reset-sc09
```

Nếu server không có lệnh `usbreset` hoặc reset không thành công, dừng gateway rồi rút/cắm lại SC09:

```bash
sudo systemctl stop pi-gateway-fxplc pi-gateway-hsl
# rút/cắm lại SC09
/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc
```

### Xem log

```bash
journalctl -u pi-gateway-fxplc -n 100 --no-pager
journalctl -u pi-gateway-hsl -n 100 --no-pager
journalctl -u caddy-proxy -n 100 --no-pager
journalctl -u ustreamer-cam -n 100 --no-pager
journalctl -u ngrok -n 100 --no-pager
```

---

## 15. Rollback về Pi

Rollback nếu server không điều khiển được PLC/camera hoặc public URL lỗi.

### Trên server

```bash
sudo systemctl stop ngrok
sudo systemctl stop pi-gateway-fxplc pi-gateway-hsl ustreamer-cam
```

### Phần cứng

- Rút SC09 khỏi server và cắm lại Pi.
- Rút camera khỏi server và cắm lại Pi.

### Trên Pi

```bash
ssh admin@10.38.100.27

/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc
sudo systemctl enable --now caddy-proxy ustreamer-cam ngrok

curl -s http://127.0.0.1:5000/health
curl -s http://127.0.0.1:5000/debug
```

Test public:

```bash
curl -s \
  -H "ngrok-skip-browser-warning: true" \
  "https://unacquiescent-quiana-excepable.ngrok-free.dev/plc/health"
```

Do giữ nguyên static domain, Unity/WebGL không cần đổi URL khi rollback.

---

## 16. Cải tiến nên làm sau migration

Ưu tiên cao:

1. Thêm API token/authentication trước khi cho phép điều khiển motor qua Internet.
2. Giới hạn CORS thay vì `Access-Control-Allow-Origin "*"`.
3. Xóa hoặc khóa endpoint nguy hiểm như `/writetest`, `/recover`.
4. Thêm watchdog/health-check tự restart gateway nếu PLC mất kết nối.
5. Ghi log rõ backend đang dùng HSL hay fxplc.
6. Backup định kỳ source/config/service/ngrok secret.
7. Dùng udev rule hoặc `/dev/v4l/by-id` cho camera thay vì phụ thuộc `/dev/video0`.
8. Hoàn thiện encoder để telemetry phản ánh chuyển động motor thực.

Ưu tiên quản trị:

1. Commit `gateway/fxplc_gateway/` và `third_party/fxplc/` vào Git.
2. Tách secret ra environment file quyền `600`.
3. Chuyển service từ `/home/admin` sang `/opt/digital-twin` sau khi hệ thống ổn định.
4. Dùng Ansible/Docker Compose chỉ sau khi migration thủ công đã nghiệm thu.

---

## 17. Thông tin bàn giao cho AI/người tiếp nhận

Khi bắt đầu migration, gửi cho AI/người triển khai:

```text
Đọc HDCD.md và triển khai từ mục 3.
Kiến trúc đích là Ubuntu Server 24.04 LTS x86_64.
Giữ nguyên ngrok static domain để Unity/WebGL không cần build lại.
Ưu tiên fxplc; giữ HSL làm fallback.
Không cutover public trước khi test nội bộ PLC + camera đạt.
Không commit ngrok authtoken, mật khẩu hoặc HSL license.
```

### Điều kiện tuyên bố migration hoàn thành

Chỉ coi là hoàn thành khi:

- Server reboot và tự phục hồi đầy đủ.
- fxplc điều khiển PLC/motor thành công.
- Camera stream thành công.
- Public URL cũ hoạt động từ Unity/WebGL.
- Chạy liên tục ít nhất 24 giờ không lỗi.
- Rollback về Pi đã được diễn tập hoặc ít nhất xác minh bằng checklist.

---

## 18. Nguồn phần mềm chính thức

- fxplc MIT: <https://github.com/KrystianD/fxplc>
- uStreamer: <https://github.com/pikvm/ustreamer>
- Caddy install/docs: <https://caddyserver.com/docs/install>
- ngrok Linux install/docs: <https://ngrok.com/docs/getting-started/>
- .NET Ubuntu install: <https://learn.microsoft.com/dotnet/core/install/linux-ubuntu>
