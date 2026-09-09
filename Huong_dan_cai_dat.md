# Hướng dẫn cài đặt gateway cho WebGL đọc/điều khiển PLC

Tài liệu này dùng cho máy Windows hoặc Raspberry Pi/Ubuntu đóng vai trò **gateway** giữa Unity WebGL và PLC Mitsubishi FX.

Mô hình hệ thống:

```text
Unity WebGL trên trình duyệt
        ↓ HTTPS
Domain / Cloudflare Tunnel / Caddy
        ↓ http://127.0.0.1:8888/plc/*
Python fxplc gateway trên Windows/Raspberry Pi/Ubuntu
        ↓ COM port qua USB SC09/CH340
PLC Mitsubishi FX
```

WebGL không đọc PLC trực tiếp được. WebGL chỉ gọi HTTP API, còn máy Windows/Raspberry Pi/Ubuntu mới là máy thật sự đọc/ghi PLC qua dây SC09.

## 0. Đọc trước để hiểu toàn hệ thống

Trước khi cài, cần hiểu 3 thành phần chính:

```text
Unity WebGL
  ↓ gọi HTTPS
Cloudflare domain + Cloudflare Tunnel
  ↓ đưa request từ Internet về máy gateway/server
Caddy
  ↓ chia tuyến /plc và /cam
Python PLC Gateway
  ↓ đọc/ghi qua USB SC09/CH340
PLC Mitsubishi FX
```

### 0.1 Gateway là gì?

Trong tài liệu này, **gateway** là chương trình Python chạy trên máy đang cắm dây SC09/CH340 vào PLC.

Nó làm nhiệm vụ:

```text
Đọc ô nhớ/chân PLC → trả JSON cho WebGL
Nhận lệnh WebGL → ghi bit/thanh ghi xuống PLC
```

Gateway mở API nội bộ:

```text
GET  http://127.0.0.1:5000/health
GET  http://127.0.0.1:5000/telemetry
POST http://127.0.0.1:5000/control
```

Ví dụ WebGL cần đọc dữ liệu PLC thì nó gọi:

```text
https://ten-mien-cua-ban.com/plc/telemetry
```

Sau đó request đi qua Cloudflare → Caddy → Python gateway → PLC.

Nói ngắn:

```text
Gateway = cầu nối thật giữa WebGL và PLC.
```

Lưu ý: gateway ở đây là **PLC gateway của mình**, không phải sản phẩm “Cloudflare Gateway” trong Cloudflare Zero Trust. Hai cái tên giống nhau nhưng khác việc.

### 0.2 Caddy là gì?

Caddy là reverse proxy chạy trên máy gateway/server.

Nó nhận request ở cổng `8888`, rồi chia tuyến:

```text
/plc/* → http://127.0.0.1:5000
/cam/* → http://127.0.0.1:8080
```

Tức là:

```text
https://ten-mien-cua-ban.com/plc/telemetry
→ Caddy
→ Python gateway port 5000

https://ten-mien-cua-ban.com/cam/?action=stream
→ Caddy
→ camera stream port 8080
```

Caddy còn thêm CORS header để Unity WebGL gọi API được.

Nói ngắn:

```text
Caddy = gom nhiều service nội bộ thành một domain có /plc và /cam.
```

Nếu chỉ có PLC, về lý thuyết có thể cho Cloudflare Tunnel trỏ thẳng vào `127.0.0.1:5000`. Nhưng vẫn nên dùng Caddy vì sau này cần thêm camera, CORS, logging, hoặc route khác thì dễ quản lý hơn.

### 0.3 Cloudflare Tunnel là gì?

Cloudflare Tunnel thay thế ngrok.

Nó chạy chương trình `cloudflared` trên máy gateway/server. `cloudflared` tự mở kết nối đi ra ngoài tới Cloudflare, nên thường không cần:

```text
Không cần mở port router
Không cần public IP
Không cần NAT port 80/443
```

Cloudflare Tunnel sẽ đưa domain của mình về Caddy nội bộ:

```text
https://ten-mien-cua-ban.com
→ Cloudflare Tunnel
→ http://127.0.0.1:8888
```

Nói ngắn:

```text
Cloudflare Tunnel = đưa máy gateway/server ra Internet bằng domain ổn định.
```

Tài liệu chính thức:

- Cloudflare Tunnel: https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/
- Cloudflare Registrar: https://domains.cloudflare.com/

### 0.4 Domain mua ở Cloudflare giá bao nhiêu?

Cloudflare Tunnel thường không phải thứ phải trả tiền riêng cho mô hình nhỏ này. Thứ cần mua là **domain**.

Cloudflare Registrar bán domain kiểu “at-cost”, tức là theo giá gốc của registry, không cộng thêm markup/upsell. Giá phụ thuộc đuôi domain:

```text
.com thường khoảng 10–12 USD/năm
.net/.org thường gần tương tự hoặc cao hơn chút
đuôi lạ có thể rẻ hơn nhưng không nên ham nếu dùng cho lab lâu dài
```

Ước lượng:

```text
10–12 USD/năm ≈ 250k–320k VND/năm
```

Nên chọn:

```text
.com
.net
.org
```

Không nên chọn domain quá lạ chỉ vì rẻ, vì dễ khó nhớ, khó tin cậy, hoặc năm sau gia hạn đắt.

Ví dụ domain:

```text
hcdlab.com
plclab247.com
digitaltwinplc.com
```

Sau khi mua domain, nên dùng subdomain riêng cho hệ PLC:

```text
plc.hcdlab.com
```

Các URL sau cùng sẽ là:

```text
https://plc.hcdlab.com/plc/health
https://plc.hcdlab.com/plc/telemetry
https://plc.hcdlab.com/plc/control
https://plc.hcdlab.com/cam/?action=stream
```

### 0.5 Mua domain xong cần làm gì?

Tóm tắt A-Z:

```text
1. Mua domain trên Cloudflare Registrar.
2. Cài gateway Python và test http://127.0.0.1:5000/health.
3. Cài Caddy và test http://127.0.0.1:8888/plc/health.
4. Vào Cloudflare Zero Trust tạo Tunnel.
5. Cài cloudflared trên máy gateway/server.
6. Tạo Public Hostname: plc.domain.com → http://localhost:8888.
7. Test https://plc.domain.com/plc/health.
8. Sửa Unity URL sang https://plc.domain.com/plc.
9. Rebuild WebGL.
10. Tắt ngrok cũ.
```

Điểm mấu chốt:

```text
Cloudflare Tunnel trỏ tới Caddy port 8888.
Caddy trỏ /plc tới Python gateway port 5000.
Unity WebGL chỉ gọi domain HTTPS, không gọi 127.0.0.1.
```

## Tóm tắt đúng yêu cầu: WebGL muốn đọc chân/ô nhớ PLC thì cần cài gì?

Trên máy đóng vai trò gateway, tức là máy đang cắm dây SC09/CH340 vào PLC, cần cài:

```text
Python 3
pip / venv
pyserial
fxplc gateway
Driver CH340/CH341 nếu là Windows
Caddy reverse proxy
Cloudflare Tunnel hoặc domain/public tunnel
Service chạy 24/7: NSSM trên Windows, systemd trên Linux/Raspberry Pi
```

Nếu cần camera thì cài thêm phần camera stream:

```text
Linux/Raspberry Pi/Ubuntu: uStreamer
Windows: nên dùng IP camera hoặc phần mềm MJPEG stream riêng
```

Không cần cài các thứ này nếu chỉ để WebGL đọc dữ liệu PLC:

```text
Không cần Unity Editor trên máy gateway
Không cần GX Works2 nếu không nạp/sửa chương trình PLC
Không cần HSL
Không cần mở trực tiếp port 5000 ra Internet
```

WebGL sẽ đọc dữ liệu qua API:

```text
GET https://domain-cua-ban/plc/telemetry
```

Gateway hiện tại đang trả các giá trị như tốc độ, số xung, số vòng, góc, trạng thái chạy/dừng. Nếu muốn đọc thêm chân/ô nhớ khác như `X`, `Y`, `M`, `D` mới, cần bổ sung địa chỉ đó trong `gateway.py` rồi trả thêm vào JSON `/telemetry`.

## 1. Phần cứng cần chuẩn bị

- Máy Windows 10/11 64-bit.
- PLC Mitsubishi FX.
- Dây SC09 USB/CH340.
- Internet ổn định nếu cần cho học sinh truy cập từ ngoài mạng.
- Nguồn điện ổn định, nên có UPS nếu chạy 24/7.

Lưu ý quan trọng: một dây SC09 chỉ nên được một phần mềm dùng tại một thời điểm.

```text
Gateway đang chạy → GX Works2 không nên chiếm COM.
GX Works2 đang online PLC → gateway nên tạm dừng.
```

## 2. Phần mềm cần cài trên Windows

### Bắt buộc cho gateway

1. Python 3.10 hoặc 3.11 64-bit  
   Link: https://www.python.org/downloads/windows/

   Khi cài nhớ tick:

   ```text
   Add python.exe to PATH
   ```

2. Git for Windows  
   Link: https://git-scm.com/download/win

3. Driver CH340/CH341 cho dây SC09 USB  
   Link tham khảo: https://www.wch-ic.com/downloads/CH341SER_EXE.html

4. Caddy for Windows  
   Link: https://caddyserver.com/download

5. Cloudflared nếu dùng Cloudflare Tunnel thay ngrok  
   Link: https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/

### Bắt buộc trên Raspberry Pi/Ubuntu

Nếu gateway chạy trên Raspberry Pi hoặc Ubuntu, cài:

```bash
sudo apt update
sudo apt install -y \
  python3 python3-venv python3-pip python3-serial \
  git curl jq caddy
```

Thêm quyền serial:

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

Nếu dùng camera USB trên Linux/Raspberry Pi:

```bash
sudo apt install -y ustreamer v4l-utils
```

Nếu dùng Cloudflare Tunnel trên Linux/Raspberry Pi thì cài thêm `cloudflared` theo hướng dẫn Cloudflare.

### Nên cài thêm

1. VS Code  
   Link: https://code.visualstudio.com/

2. NSSM để chạy gateway/Caddy thành Windows service  
   Link: https://nssm.cc/download

3. GX Works2 nếu máy này cũng dùng để nạp/chẩn đoán PLC.

### Nếu cần build lại Unity WebGL

Cài Unity Hub và đúng version của project:

```text
Unity 6000.3.11f1
```

Link Unity Hub: https://unity.com/download

Nếu chỉ chạy gateway PLC thì không cần cài Unity Editor.

### Nếu cần camera stream

Hệ hiện tại trên Ubuntu đang dùng `uStreamer` để đưa camera ra:

```text
/cam/?action=stream
/cam/?action=snapshot
```

`uStreamer` hợp Linux/Raspberry Pi hơn Windows. Nếu nhóm kia cài lại hoàn toàn trên Windows thì có 3 lựa chọn:

1. Khuyến nghị nhất: dùng IP camera có URL MJPEG/RTSP riêng.
2. Giữ camera stream trên một máy Linux/Raspberry Pi riêng, còn Windows chỉ chạy PLC gateway.
3. Nếu bắt buộc dùng webcam USB trên Windows, cần cài thêm phần mềm stream MJPEG cho Windows và chỉnh lại Unity/Caddy theo URL của phần mềm đó.

Nếu chỉ cần WebGL đọc/điều khiển PLC thì bỏ qua phần camera.

## 3. Cài bằng winget cho nhanh

Mở PowerShell bằng quyền Administrator:

```powershell
winget install -e --id Python.Python.3.11
winget install -e --id Git.Git
winget install -e --id Microsoft.VisualStudioCode
winget install -e --id CaddyServer.Caddy
winget install -e --id Cloudflare.cloudflared
```

Nếu lệnh nào báo không tìm thấy package thì tải thủ công từ các link ở mục trên.

Sau khi cài Python/Git xong, đóng PowerShell rồi mở lại.

Kiểm tra:

```powershell
python --version
git --version
caddy version
cloudflared --version
```

## 4. Cài driver và kiểm tra COM port

Cắm dây SC09/CH340 vào máy Windows.

Mở:

```text
Device Manager → Ports (COM & LPT)
```

Tìm thiết bị kiểu:

```text
USB-SERIAL CH340 (COM3)
```

Ghi lại COM port, ví dụ:

```text
COM3
```

COM này sẽ dùng cho biến:

```text
FXPLC_SERIAL_PORT=COM3
```

## 5. Copy project gateway

Tạo thư mục:

```text
C:\PLC
```

Copy toàn bộ thư mục gateway vào:

```text
C:\PLC\PiGatewayFxplc
```

Bên trong phải có đủ:

```text
C:\PLC\PiGatewayFxplc
├── gateway.py
├── requirements.txt
└── vendor
    └── fxplc
```

Không được chỉ copy mỗi `gateway.py`, vì gateway cần thư viện `vendor\fxplc`.

## 6. Tạo Python virtual environment

### Trên Windows

Mở PowerShell:

```powershell
cd C:\PLC\PiGatewayFxplc
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\pip.exe install -r requirements.txt
.\.venv\Scripts\pip.exe install -e .\vendor\fxplc
```

Nếu không lỗi là OK.

### Trên Raspberry Pi/Ubuntu

Copy thư mục gateway vào:

```text
/home/<user>/PiGatewayFxplc
```

Ví dụ:

```bash
cd ~/PiGatewayFxplc
python3 -m venv .venv
./.venv/bin/python -m pip install --upgrade pip
./.venv/bin/pip install -r requirements.txt
./.venv/bin/pip install -e ./vendor/fxplc
```

Nếu không lỗi là OK.

## 7. Chạy gateway thử lần đầu

### Trên Windows

Vẫn trong PowerShell:

```powershell
cd C:\PLC\PiGatewayFxplc
$env:FXPLC_SERIAL_PORT="COM3"
$env:FXPLC_HTTP_HOST="127.0.0.1"
$env:FXPLC_HTTP_PORT="5000"
$env:FXPLC_ALLOW_WRITES="0"
.\.venv\Scripts\python.exe gateway.py
```

Đổi `COM3` thành COM thực tế trên máy.

Nếu chạy đúng sẽ thấy kiểu:

```text
fxplc gateway on http://127.0.0.1:5000; serial=COM3; allow_writes=False
```

### Trên Raspberry Pi/Ubuntu

Kiểm tra đường dẫn dây SC09:

```bash
ls -l /dev/serial/by-id/
```

Chạy thử:

```bash
cd ~/PiGatewayFxplc
export FXPLC_SERIAL_PORT="/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0"
export FXPLC_HTTP_HOST="127.0.0.1"
export FXPLC_HTTP_PORT="5000"
export FXPLC_ALLOW_WRITES="0"
./.venv/bin/python gateway.py
```

Nếu đường dẫn `/dev/serial/by-id/...` khác thì thay đúng tên trên máy đó.

Mở PowerShell cửa sổ khác để test:

```powershell
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:5000/telemetry
```

Trên Raspberry Pi/Ubuntu thì test bằng:

```bash
curl -sS http://127.0.0.1:5000/health | jq
curl -sS http://127.0.0.1:5000/telemetry | jq
```

Nếu trả JSON là gateway đã đọc được PLC.

Sau khi đọc ổn, nếu cần cho WebGL gửi lệnh bật/tắt PLC thì đổi:

```powershell
$env:FXPLC_ALLOW_WRITES="1"
```

Rồi chạy lại gateway.

Khuyến nghị:

```text
Test đọc dữ liệu trước với FXPLC_ALLOW_WRITES=0.
Chỉ bật FXPLC_ALLOW_WRITES=1 sau khi chắc chắn COM/PLC đọc đúng.
```

## 8. Tạo file chạy nhanh gateway

Tạo file:

```text
C:\PLC\PiGatewayFxplc\run_gateway.bat
```

Nội dung:

```bat
@echo off
cd /d C:\PLC\PiGatewayFxplc
set FXPLC_SERIAL_PORT=COM3
set FXPLC_HTTP_HOST=127.0.0.1
set FXPLC_HTTP_PORT=5000
set FXPLC_ALLOW_WRITES=1
.\.venv\Scripts\python.exe gateway.py
```

Nhớ đổi `COM3` thành COM thật.

Chạy thử bằng cách double-click `run_gateway.bat`.

## 9. Cấu hình Caddy reverse proxy

Tạo file:

```text
C:\PLC\Caddyfile
```

Nội dung:

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

Chạy thử Caddy:

```powershell
caddy run --config C:\PLC\Caddyfile
```

Mở PowerShell khác test:

```powershell
curl.exe http://127.0.0.1:8888/plc/health
curl.exe http://127.0.0.1:8888/plc/telemetry
```

Nếu trả JSON là Caddy đã proxy đúng.

## 10. Public ra Internet bằng Cloudflare Tunnel

Khuyến nghị dùng Cloudflare Tunnel thay ngrok cho hệ chạy lâu dài.

Mục tiêu sau khi làm xong:

```text
https://plc.lab-tenban.com/plc/health     → kiểm tra gateway
https://plc.lab-tenban.com/plc/telemetry  → WebGL đọc dữ liệu PLC
https://plc.lab-tenban.com/plc/control    → WebGL gửi lệnh điều khiển PLC
https://plc.lab-tenban.com/cam/?action=stream → xem camera nếu có
```

### 10.1 Trước khi public ra Internet phải test nội bộ trước

Không làm Cloudflare khi gateway/Caddy nội bộ còn lỗi.

Test gateway:

```powershell
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:5000/telemetry
```

Test Caddy:

```powershell
curl.exe http://127.0.0.1:8888/plc/health
curl.exe http://127.0.0.1:8888/plc/telemetry
```

Trên Raspberry Pi/Ubuntu:

```bash
curl -sS http://127.0.0.1:5000/health | jq
curl -sS http://127.0.0.1:8888/plc/health | jq
```

Nếu `127.0.0.1:8888/plc/health` chưa OK thì chưa cần đụng Cloudflare.

### 10.2 Domain giá bao nhiêu?

Cloudflare Tunnel dùng cho mô hình nhỏ này thường không phải trả riêng theo tháng. Thứ cần mua là domain.

Cloudflare Registrar bán domain theo giá gốc/wholesale, không markup thêm. Giá phụ thuộc đuôi domain:

```text
.com thường khoảng 10–12 USD/năm
.net/.org thường gần tương tự hoặc nhỉnh hơn chút
đuôi lạ có thể rẻ hơn nhưng không nên ham nếu dùng lâu dài
```

Ước lượng:

```text
10–12 USD/năm ≈ 250k–320k VND/năm
```

Khuyên dùng:

```text
.com
.net
.org
```

Ví dụ:

```text
hcdlab.com
plclab247.com
digitaltwinplc.com
```

Không cần mua domain có chữ `plc` nếu không thích. Có thể mua domain ngắn dễ nhớ, rồi tạo subdomain:

```text
plc.tenmiencuaban.com
```

### 10.3 Mua domain trên Cloudflare xong làm gì?

Nếu mua domain trực tiếp trên Cloudflare Registrar:

```text
Cloudflare tự quản lý DNS cho domain đó.
Không cần đổi nameserver ở nơi khác.
```

Nếu mua domain ở nơi khác như Namecheap/GoDaddy/Porkbun:

```text
Phải add domain vào Cloudflare.
Sau đó đổi nameserver tại nơi mua domain sang nameserver Cloudflare đưa.
```

Để đơn giản nhất, nên mua luôn trong Cloudflare.

### 10.4 Tạo Cloudflare Tunnel bằng Dashboard

Vào:

```text
https://one.dash.cloudflare.com/
```

Làm theo thứ tự:

```text
Zero Trust → Networks → Tunnels → Create a tunnel
```

Chọn:

```text
Tunnel type: Cloudflared
Tunnel name: plc-lab
```

Cloudflare sẽ hỏi cài connector trên máy nào. Chọn đúng hệ điều hành:

```text
Windows nếu gateway chạy Windows
Debian/Ubuntu nếu gateway chạy Ubuntu/Raspberry Pi
```

Sau đó Cloudflare sẽ đưa một command dài để cài/chạy `cloudflared`. Copy command đó chạy trên máy gateway/server.

Lưu ý:

```text
cloudflared phải chạy trên cùng máy đang chạy Caddy port 8888.
```

### 10.5 Tạo Public Hostname

Trong tunnel `plc-lab`, thêm Public Hostname:

```text
Subdomain: plc
Domain:    lab-tenban.com
Path:      bỏ trống
Service:   http://localhost:8888
```

Kết quả:

```text
plc.lab-tenban.com → http://localhost:8888
```

Vì Caddy đã chia route `/plc` và `/cam`, nên chỉ cần trỏ Cloudflare Tunnel về `localhost:8888`.

Không trỏ Cloudflare thẳng vào `localhost:5000` nếu còn cần camera hoặc muốn route sạch.

### 10.6 Test domain sau khi tạo tunnel

Test bằng PowerShell:

```powershell
curl.exe https://plc.lab-tenban.com/plc/health
curl.exe https://plc.lab-tenban.com/plc/telemetry
```

Trên Linux/Raspberry Pi:

```bash
curl -sS https://plc.lab-tenban.com/plc/health | jq
curl -sS https://plc.lab-tenban.com/plc/telemetry | jq
```

Nếu health trả JSON kiểu:

```json
{
  "gateway": "fxplc",
  "allowWrites": true,
  "serialPort": "..."
}
```

thì Cloudflare Tunnel đã chạy đúng.

### 10.7 Sửa Unity WebGL URL

Trong Unity, PLC base URL phải đổi từ ngrok cũ sang domain mới:

```text
PLC base URL: https://plc.lab-tenban.com/plc
Control:      https://plc.lab-tenban.com/plc/control
Telemetry:    https://plc.lab-tenban.com/plc/telemetry
Camera:       https://plc.lab-tenban.com/cam/?action=stream
```

Không dùng trong WebGL:

```text
http://127.0.0.1:5000
http://localhost:5000
http://10.x.x.x:5000
http://192.168.x.x:5000
ngrok cũ đã hết bandwidth
```

Vì trong trình duyệt của học sinh, `127.0.0.1` là máy học sinh, không phải máy gateway.

Sau khi sửa URL:

```text
Rebuild WebGL.
Upload lại build lên server/web hosting.
Mở trình duyệt test lại.
```

### 10.8 Tắt ngrok cũ

Sau khi Cloudflare domain đã test OK, tắt ngrok:

Trên Ubuntu/Raspberry Pi:

```bash
sudo systemctl disable --now ngrok
sudo systemctl status ngrok
```

Trên Windows nếu chạy ngrok thủ công:

```text
Đóng cửa sổ ngrok.
Gỡ ngrok khỏi startup nếu có.
```

Nếu chạy ngrok bằng service Windows thì stop service đó.

### 10.9 Cách test nhanh không cần domain cố định

Cloudflare có thể tạo URL tạm để test:

```powershell
cloudflared tunnel --url http://127.0.0.1:8888
```

Nó sẽ tạo URL tạm dạng:

```text
https://xxxx.trycloudflare.com
```

Test:

```powershell
curl.exe https://xxxx.trycloudflare.com/plc/health
```

URL kiểu này chỉ dùng để test, không nên dùng production vì có thể đổi.

### 10.10 Cách cấu hình bằng command thay vì Dashboard

Nếu không dùng Dashboard, có thể cấu hình tunnel bằng command.

Ví dụ domain:


```text
lab-tenban.com
```

Public hostname muốn dùng:

```text
plc.lab-tenban.com
```

Flow chung:

```powershell
cloudflared tunnel login
cloudflared tunnel create plc-lab
cloudflared tunnel route dns plc-lab plc.lab-tenban.com
```

Tạo file config:

```text
C:\Users\<ten-user>\.cloudflared\config.yml
```

Nội dung mẫu:

```yaml
tunnel: <UUID-cua-tunnel>
credentials-file: C:\Users\<ten-user>\.cloudflared\<UUID-cua-tunnel>.json

ingress:
  - hostname: plc.lab-tenban.com
    service: http://127.0.0.1:8888
  - service: http_status:404
```

Chạy thử:

```powershell
cloudflared tunnel run plc-lab
```

Test:

```powershell
curl.exe https://plc.lab-tenban.com/plc/health
curl.exe https://plc.lab-tenban.com/plc/telemetry
```

URL đưa cho Unity WebGL:

```text
PLC base URL: https://plc.lab-tenban.com/plc
Control:      https://plc.lab-tenban.com/plc/control
Telemetry:    https://plc.lab-tenban.com/plc/telemetry
```

### 10.11 Lỗi thường gặp sau khi đổi sang Cloudflare

Nếu domain lỗi, kiểm tra theo thứ tự này:

```text
1. http://127.0.0.1:5000/health có OK không?
2. http://127.0.0.1:8888/plc/health có OK không?
3. cloudflared service có đang chạy không?
4. Public Hostname có trỏ đúng http://localhost:8888 không?
5. Domain Unity có đúng https://plc.lab-tenban.com/plc không?
6. Đã rebuild WebGL sau khi đổi URL chưa?
```

Nếu localhost OK nhưng domain lỗi:

```text
Lỗi nằm ở Cloudflare Tunnel / DNS / Public Hostname.
```

Nếu localhost `5000` lỗi:

```text
Lỗi nằm ở Python gateway / COM port / PLC / dây SC09.
```

Nếu localhost `5000` OK nhưng `8888/plc` lỗi:

```text
Lỗi nằm ở Caddy/Caddyfile.
```

## 11. Chạy 24/7 bằng Windows service hoặc systemd

Sau khi test thủ công OK, nên chạy gateway và Caddy bằng service.

### Windows: dùng NSSM

### Cài gateway bằng NSSM

Mở PowerShell Administrator:

```powershell
nssm install PiGatewayFxplc
```

Trong cửa sổ NSSM:

```text
Path:        C:\PLC\PiGatewayFxplc\.venv\Scripts\python.exe
Startup dir: C:\PLC\PiGatewayFxplc
Arguments:   gateway.py
```

Vào tab Environment, thêm:

```text
FXPLC_SERIAL_PORT=COM3
FXPLC_HTTP_HOST=127.0.0.1
FXPLC_HTTP_PORT=5000
FXPLC_ALLOW_WRITES=1
```

Sau đó:

```powershell
nssm start PiGatewayFxplc
```

Kiểm tra:

```powershell
curl.exe http://127.0.0.1:5000/health
```

### Cài Caddy bằng NSSM

```powershell
nssm install CaddyPlcProxy
```

Trong cửa sổ NSSM:

```text
Path:        C:\Program Files\Caddy\caddy.exe
Startup dir: C:\PLC
Arguments:   run --config C:\PLC\Caddyfile
```

Nếu `caddy.exe` nằm chỗ khác, chọn đúng đường dẫn thực tế.

Sau đó:

```powershell
nssm start CaddyPlcProxy
```

Kiểm tra:

```powershell
curl.exe http://127.0.0.1:8888/plc/health
```

### Cài cloudflared thành service

Mở PowerShell Administrator:

```powershell
cloudflared service install
```

Sau đó kiểm tra trong:

```text
Services → cloudflared
```

Hoặc:

```powershell
sc.exe query cloudflared
```

### Raspberry Pi/Ubuntu: dùng systemd

Tạo service gateway:

```bash
sudo nano /etc/systemd/system/pi-gateway-fxplc.service
```

Nội dung mẫu:

```ini
[Unit]
Description=FX PLC HTTP Gateway
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=<user>
WorkingDirectory=/home/<user>/PiGatewayFxplc
Environment=FXPLC_SERIAL_PORT=/dev/serial/by-id/usb-1a86_USB_Serial-if00-port0
Environment=FXPLC_HTTP_HOST=127.0.0.1
Environment=FXPLC_HTTP_PORT=5000
Environment=FXPLC_ALLOW_WRITES=1
ExecStart=/home/<user>/PiGatewayFxplc/.venv/bin/python /home/<user>/PiGatewayFxplc/gateway.py
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
```

Đổi `<user>` thành username thật trên máy.

Bật service:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now pi-gateway-fxplc
sudo systemctl status pi-gateway-fxplc
```

Kiểm tra:

```bash
curl -sS http://127.0.0.1:5000/health | jq
curl -sS http://127.0.0.1:5000/telemetry | jq
```

Caddy trên Linux/Raspberry Pi có thể dùng `/etc/caddy/Caddyfile` hoặc một service riêng. Nội dung proxy vẫn giống phần Caddy ở trên, chỉ cần đảm bảo:

```text
/plc/* → 127.0.0.1:5000
```

Nếu dùng camera trên Linux/Raspberry Pi thì thêm:

```text
/cam/* → 127.0.0.1:8080
```

## 12. Khi cần dùng GX Works2

Nếu GX Works2 cần online PLC qua cùng dây SC09, dừng gateway trước:

```powershell
nssm stop PiGatewayFxplc
```

Mở GX Works2, chọn đúng COM port, ví dụ:

```text
COM3
Baudrate: 9600
```

Dùng xong GX Works2 thì bật lại gateway:

```powershell
nssm start PiGatewayFxplc
curl.exe http://127.0.0.1:5000/health
```

## 13. API WebGL sẽ gọi

Health:

```text
GET /plc/health
```

Telemetry:

```text
GET /plc/telemetry
```

Control:

```text
POST /plc/control
Content-Type: application/json
```

Ví dụ test đọc:

```powershell
curl.exe https://plc.lab-tenban.com/plc/health
curl.exe https://plc.lab-tenban.com/plc/telemetry
```

Ví dụ dừng động cơ:

```powershell
curl.exe -X POST https://plc.lab-tenban.com/plc/control `
  -H "Content-Type: application/json" `
  --data "{\"action\":\"OFF\"}"
```

Ví dụ chạy thuận:

```powershell
curl.exe -X POST https://plc.lab-tenban.com/plc/control `
  -H "Content-Type: application/json" `
  --data "{\"action\":\"ON\",\"direction\":\"forward\",\"speed\":5,\"mode\":\"rotations\",\"rotations\":1}"
```

## 14. Các địa chỉ PLC gateway hiện đang đọc/ghi

Các bit chính:

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

Các thanh ghi chính:

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

Nếu nhóm cần đọc thêm ô nhớ khác, phải sửa `gateway.py` để bổ sung register/bit mới vào `/telemetry` hoặc tạo endpoint mới.

## 15. Checklist test cuối

Trên máy Windows gateway:

```powershell
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:5000/telemetry
curl.exe http://127.0.0.1:8888/plc/health
curl.exe http://127.0.0.1:8888/plc/telemetry
```

Qua domain public:

```powershell
curl.exe https://plc.lab-tenban.com/plc/health
curl.exe https://plc.lab-tenban.com/plc/telemetry
```

Trong Unity WebGL, base URL phải là:

```text
https://plc.lab-tenban.com/plc
```

Không dùng:

```text
127.0.0.1
localhost
http://10.x.x.x
http://192.168.x.x
ngrok cũ đã hết bandwidth
```

## 16. Lỗi hay gặp

### Không thấy COM port

- Cài lại CH340 driver.
- Rút cắm lại dây SC09.
- Đổi cổng USB.
- Mở Device Manager kiểm tra lại COM.

### Gateway báo lỗi serial/timeout

- Sai COM port.
- GX Works2 đang chiếm COM.
- Dây SC09 lỏng.
- PLC chưa bật nguồn.
- Thử dừng GX Works2 rồi chạy lại gateway.

### WebGL báo backend đỏ

Kiểm tra lần lượt:

```powershell
curl.exe http://127.0.0.1:5000/health
curl.exe http://127.0.0.1:8888/plc/health
curl.exe https://plc.lab-tenban.com/plc/health
```

Nếu localhost OK nhưng domain lỗi, vấn đề nằm ở Cloudflare Tunnel/Caddy/domain.

Nếu localhost `5000` lỗi, vấn đề nằm ở gateway/COM/PLC.

### GX Works2 không online được PLC

- Dừng service gateway trước.
- Chọn đúng COM port.
- Chọn baudrate 9600 nếu cần cấu hình tay.

## 17. Ghi chú bảo mật

Không mở trực tiếp port `5000` ra Internet.

Nếu public domain cho học sinh dùng, nên bật Cloudflare Access hoặc cơ chế đăng nhập, vì endpoint `/plc/control` có thể gửi lệnh điều khiển PLC thật.

Với chế độ chỉ xem dữ liệu, đặt:

```text
FXPLC_ALLOW_WRITES=0
```

Với chế độ cho phép WebGL bật/tắt motor, đặt:

```text
FXPLC_ALLOW_WRITES=1
```
