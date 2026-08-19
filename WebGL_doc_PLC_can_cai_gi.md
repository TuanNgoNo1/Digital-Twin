# WebGL muốn đọc dữ liệu từ chân/ô nhớ PLC thì máy gateway cần cài gì?

Tài liệu này dùng để gửi nhanh cho nhóm cài đặt lại từ đầu.

## 1. Mô hình hoạt động

Unity WebGL không đọc trực tiếp được PLC qua COM/USB.

WebGL phải gọi API qua một máy trung gian gọi là **gateway**:

```text
Unity WebGL trên trình duyệt
        ↓ HTTPS
Domain / Cloudflare Tunnel / Caddy
        ↓ /plc/telemetry hoặc /plc/control
Máy gateway: Windows / Raspberry Pi / Ubuntu
        ↓ USB SC09/CH340
PLC Mitsubishi FX
```

Máy gateway là máy đang cắm dây SC09/CH340 vào PLC.

## 2. Phần mềm bắt buộc trên máy gateway

### Nếu dùng Windows

Cần cài:

```text
Python 3.10 hoặc 3.11 64-bit
Git for Windows
Driver CH340/CH341 cho dây SC09 USB
Python virtual environment
pyserial
fxplc gateway
Caddy reverse proxy
Cloudflare Tunnel hoặc tunnel/domain public khác
NSSM để chạy gateway 24/7 dạng Windows service
```

Nếu cần nạp/sửa chương trình PLC thì cài thêm:

```text
GX Works2
```

Nếu chỉ để WebGL đọc dữ liệu PLC thì **không bắt buộc cài GX Works2**.

### Nếu dùng Raspberry Pi / Ubuntu

Cần cài:

```text
python3
python3-venv
python3-pip
python3-serial
git
curl
jq
caddy
fxplc gateway
cloudflared nếu dùng Cloudflare Tunnel
systemd service để chạy 24/7
```

Lệnh cài cơ bản:

```bash
sudo apt update
sudo apt install -y \
  python3 python3-venv python3-pip python3-serial \
  git curl jq caddy
```

Thêm quyền đọc/ghi cổng serial:

```bash
sudo usermod -aG dialout $USER
sudo reboot
```

Kiểm tra dây SC09/CH340:

```bash
lsusb
ls -l /dev/serial/by-id/
```

Thường sẽ thấy:

```text
/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0
```

## 3. Project gateway cần copy

Trên máy gateway cần có thư mục:

```text
PiGatewayFxplc
├── gateway.py
├── requirements.txt
└── vendor
    └── fxplc
```

Không được chỉ copy mỗi `gateway.py`, vì gateway cần thư viện `vendor/fxplc`.

## 4. Cài Python gateway

### Windows PowerShell

```powershell
cd C:\PLC\PiGatewayFxplc
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\pip.exe install -r requirements.txt
.\.venv\Scripts\pip.exe install -e .\vendor\fxplc
```

### Raspberry Pi / Ubuntu

```bash
cd ~/PiGatewayFxplc
python3 -m venv .venv
./.venv/bin/python -m pip install --upgrade pip
./.venv/bin/pip install -r requirements.txt
./.venv/bin/pip install -e ./vendor/fxplc
```

## 5. Chạy thử gateway

### Windows

Đổi `COM3` thành COM thật trong Device Manager.

```powershell
cd C:\PLC\PiGatewayFxplc
$env:FXPLC_SERIAL_PORT="COM3"
$env:FXPLC_HTTP_HOST="127.0.0.1"
$env:FXPLC_HTTP_PORT="5000"
$env:FXPLC_ALLOW_WRITES="0"
.\.venv\Scripts\python.exe gateway.py
```

### Raspberry Pi / Ubuntu

Đổi đường dẫn serial nếu máy hiện tên khác.

```bash
cd ~/PiGatewayFxplc
export FXPLC_SERIAL_PORT="/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0"
export FXPLC_HTTP_HOST="127.0.0.1"
export FXPLC_HTTP_PORT="5000"
export FXPLC_ALLOW_WRITES="0"
./.venv/bin/python gateway.py
```

Nếu chạy đúng sẽ thấy kiểu:

```text
fxplc gateway on http://127.0.0.1:5000; serial=...; allow_writes=False
```

## 6. Test đọc dữ liệu PLC

Mở terminal khác và test:

### Windows

```powershell
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:5000/telemetry
```

### Raspberry Pi / Ubuntu

```bash
curl -sS http://127.0.0.1:5000/health | jq
curl -sS http://127.0.0.1:5000/telemetry | jq
```

Nếu trả JSON là WebGL có thể đọc dữ liệu PLC qua gateway.

## 7. Cho WebGL gọi qua domain/public URL

Không nên để WebGL gọi thẳng:

```text
http://127.0.0.1:5000
http://localhost:5000
```

Vì trong trình duyệt của học sinh, `127.0.0.1` là máy học sinh, không phải máy gateway.

Cần dùng domain hoặc tunnel:

```text
https://domain-cua-ban.com/plc/telemetry
https://domain-cua-ban.com/plc/control
```

Khuyến nghị dùng:

```text
Cloudflare Tunnel + Caddy
```

Không khuyến nghị dùng ngrok free lâu dài vì có thể hết bandwidth bất ngờ.

## 8. Caddy reverse proxy cần có

Caddy nhận request public rồi chuyển vào gateway:

```text
/plc/* → http://127.0.0.1:5000
```

Caddyfile mẫu:

```caddyfile
{
    admin off
    auto_https off
}

(corsheaders) {
    header {
        Access-Control-Allow-Origin "*"
        Access-Control-Allow-Methods "GET, POST, OPTIONS"
        Access-Control-Allow-Headers "Content-Type, ngrok-skip-browser-warning"
        Access-Control-Max-Age "86400"
    }
}

:8888 {
    handle_path /plc/* {
        @preflight method OPTIONS
        handle @preflight {
            import corsheaders
            respond "" 204
        }

        import corsheaders
        reverse_proxy 127.0.0.1:5000 {
            header_down -Access-Control-Allow-Origin
            header_down -Access-Control-Allow-Headers
            header_down -Access-Control-Allow-Methods
        }
    }
}
```

Test:

```bash
curl -sS http://127.0.0.1:8888/plc/health
curl -sS http://127.0.0.1:8888/plc/telemetry
```

## 9. Các API WebGL dùng

Đọc trạng thái gateway:

```text
GET /plc/health
```

Đọc dữ liệu PLC:

```text
GET /plc/telemetry
```

Gửi lệnh điều khiển:

```text
POST /plc/control
Content-Type: application/json
```

Ví dụ dừng motor:

```json
{"action":"OFF"}
```

Ví dụ chạy thuận:

```json
{"action":"ON","direction":"forward","speed":5,"mode":"rotations","rotations":1}
```

## 10. Gateway hiện đang đọc/ghi các ô nhớ nào?

Bit:

```text
M1  Start
M2  Forward
M4  Run theo số vòng
M5  Run theo góc
M8  Reverse
M12 Reset counter
M13 Reset all
M15 Tăng tốc
M16 Giảm tốc
M17 Stop
```

Thanh ghi:

```text
D104 Target pulses
D112 Target rotations
D114 Target angle
D120 Encoder count
D124 Feedback rotations
D128 Pulse frequency
D146 Speed set
D164 Speed sample legacy
D210 Speed sample
D220 Speed sample signed
D230 Feedback angle
```

Nếu muốn WebGL đọc thêm chân/ô nhớ khác như `X`, `Y`, `M`, `D`, cần sửa `gateway.py` để đọc thêm địa chỉ đó và trả về trong JSON của `/telemetry`.

## 11. Không cần cài gì?

Nếu máy chỉ làm gateway cho WebGL đọc PLC thì không cần:

```text
Không cần Unity Editor
Không cần HSL
Không cần GX Works2 nếu không nạp/sửa PLC
Không cần mở port 5000 trực tiếp ra Internet
```

GX Works2 chỉ cần cho việc lập trình, nạp, monitor PLC.

## 12. Lưu ý khi dùng GX Works2 chung dây SC09

Một COM port không nên bị hai phần mềm dùng cùng lúc.

Khi dùng GX Works2 để online PLC:

```text
Dừng gateway trước.
Mở GX Works2 và dùng đúng COM port.
Làm xong thì đóng GX Works2.
Bật lại gateway.
```

Nếu không dừng gateway, GX Works2 có thể không connect được hoặc gateway bị timeout.

## 13. Ghi chú bảo mật

Endpoint `/plc/control` có thể gửi lệnh điều khiển PLC thật.

Nếu public ra Internet cho học sinh dùng, nên có bảo vệ bằng Cloudflare Access hoặc cơ chế đăng nhập.

Nếu chỉ muốn cho xem dữ liệu, đặt:

```text
FXPLC_ALLOW_WRITES=0
```

Nếu cho phép WebGL điều khiển motor, đặt:

```text
FXPLC_ALLOW_WRITES=1
```

