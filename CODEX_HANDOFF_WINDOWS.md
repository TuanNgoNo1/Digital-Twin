# Bàn giao từ Ubuntu sang Windows 11

> **Lưu ý:** file này ghi kế hoạch/trạng thái ở thời điểm bắt đầu migration và có nhiều mục đã
> hoàn thành hoặc thay đổi. Trạng thái đang chạy mới nhất xem:
> [`TECH_STACK_CURRENT.md`](TECH_STACK_CURRENT.md) và
> [`chats/01-07-windows-public-gateway-plc-camera.md`](chats/01-07-windows-public-gateway-plc-camera.md).

Ngày tạo: 2026-06-29

## Cách tiếp tục với Codex trên Windows

Sau khi cài Windows, mở thư mục dự án trên HDD backup và gửi cho Codex:

```text
Hãy đọc toàn bộ file CODEX_HANDOFF_WINDOWS.md rồi tiếp tục dựng tech stack Windows từ mục "Việc cần làm tiếp theo". Chưa public PLC ra Internet cho đến khi kiểm thử local đạt.
```

File `Codex_Session_Original.jsonl` trong thư mục `Codex_Chat_Archive` là bản lưu session gốc để tra cứu, không phải file cần chạy.

## Quyết định kiến trúc cuối cùng

- Xóa Ubuntu và cài Windows 11 trực tiếp lên NVMe.
- Không dùng KVM, VirtualBox hay VMware trong kiến trúc cuối.
- Dùng Windows 11 Pro để có Remote Desktop host.
- Không dùng ngrok nữa.
- Tạm thời không dùng Cloudflare Tunnel và không mua domain.
- Giữ Caddy vì nó gom WebGL, PLC API và camera qua một cổng/URL, đồng thời xử lý route và CORS.
- Dùng Tailscale + RDP để quản trị và truy cập riêng từ xa.
- Sau này mới xác minh IP public tĩnh và quyết định có public WebGL trực tiếp hay không.
- GX Works2 chạy trực tiếp trên Windows để kết nối PLC qua SC09/CH340.

## Phần cứng máy

- Máy: Dell Precision 5860 Tower.
- CPU: Intel Xeon w5-2455X, 12 core/24 thread.
- RAM: khoảng 16 GB.
- GPU: NVIDIA, PCI ID `10de:25b0`.
- NVMe hệ điều hành: WDC PC SN810 512 GB, Linux nhìn thấy `/dev/nvme0n1`, dung lượng 476.9 GB.
- HDD backup: Seagate ST2000NM012B 2 TB, Linux nhìn thấy `/dev/sda`, dung lượng 1.82 TiB.
- USB cài Windows: Toshiba TransMemory 28.9 GB, Linux nhìn thấy `/dev/sdb`, nhãn `ESD-USB`.
- PLC serial: SC09/CH340 USB ID `1a86:7523`.
- Camera: A4 Tech USB2.0 Camera.

## Trạng thái HDD backup

HDD 2 TB đã được:

- Kiểm tra không có partition/filesystem cũ.
- Tạo GPT.
- Tạo một partition NTFS.
- Đặt nhãn `UBUNTU_BACKUP`.
- Mount, copy dữ liệu, kiểm tra checksum rồi unmount an toàn.

Đường dẫn trên Linux trước khi unmount:

```text
/media/huypc/UBUNTU_BACKUP/MIGRATION_2026-06-29/
```

Cấu trúc backup:

```text
MIGRATION_2026-06-29/
├── Windows_Readable/
│   ├── Digital-Twin-main/
│   ├── PiGatewayFxplc/
│   └── proxy/
├── Linux_Reference/
│   ├── caddy/Caddyfile
│   └── systemd/
│       ├── caddy-proxy.service
│       ├── ngrok.service
│       ├── pi-gateway-fxplc.service
│       └── ustreamer-cam.service
├── Codex_Chat_Archive/
└── Checksums/
```

Đã đối chiếu:

- `Digital-Twin-main`: 850 file ở cả nguồn và HDD.
- `rsync -rcn --delete` không phát hiện khác biệt.
- Gateway và proxy cũng không phát hiện khác biệt khi kiểm tra checksum.
- `.venv`, `__pycache__` và `.pyc` Linux của gateway không được copy vì không dùng được trên Windows; source, `vendor/fxplc` và `requirements.txt` đã được giữ.
- Token/config ngrok không được sao lưu vì ngrok đã bị loại khỏi kiến trúc mới.

## Bộ cài Windows

USB `ESD-USB` là Windows 11, không phải Windows 10.

File `setup.exe` trên USB báo:

```text
Windows 11 Setup
10.0.26100.1
```

USB hiện tại có thể dùng để cài nhanh. Khả năng cao nó cài nền 24H2; sau khi cài phải chạy Windows Update và kiểm tra bằng `winver`, rồi cập nhật lên 25H2.

Trên Ubuntu từng có ISO:

```text
/home/huypc/Downloads/Win11_25H2_English_x64_v2.iso
```

SHA-256 đã xác minh khớp Microsoft:

```text
768984706B909479417B2368438909440F2967FF05C6A9195ED2667254E465E3
```

ISO này không được copy sang HDD theo yêu cầu chỉ giữ dữ liệu dự án/tech stack; có thể tải lại từ Microsoft.

## Cách cài Windows

1. Tắt máy hoàn toàn.
2. Nên tháo dây SATA của HDD 2 TB backup trước khi cài để không xóa nhầm hoặc đặt bootloader lên HDD.
3. Giữ USB `ESD-USB` cắm vào máy.
4. Bật máy, nhấn `F12`, chọn USB ở chế độ UEFI.
5. Chọn `Install now`.
6. Nếu hỏi key, chọn `I don't have a product key`.
7. Chọn Windows 11 Pro, không chọn Home.
8. Chọn `Custom: Install Windows only`.
9. Ở màn hình phân vùng, chỉ thao tác trên NVMe khoảng 476.9 GB.
10. Xóa các partition Ubuntu trên NVMe cho đến khi chỉ còn `Unallocated Space` khoảng 476.9 GB.
11. Không xóa USB 28.9 GB và tuyệt đối không xóa HDD 1.8 TB.
12. Chọn vùng trống NVMe và bấm `Next`.
13. Sau lần restart đầu, không boot lại USB.
14. Đặt tên máy Windows là `PLC-SERVER`.

Sau khi vào Windows:

1. Chạy Windows Update cho tới khi không còn cập nhật.
2. Cài Dell Command Update và driver chipset, network, NVIDIA.
3. Tắt Sleep/Hibernate.
4. Mở Terminal Admin và chạy `powercfg /h off`.
5. Tắt máy, gắn lại HDD backup.
6. Boot `Windows Boot Manager`.
7. Mở HDD `UBUNTU_BACKUP` và kiểm tra `MIGRATION_2026-06-29`.
8. Chỉ dựng tech stack sau khi xác nhận Windows đọc được project backup.

## Tech stack Windows cần dựng

```text
Windows 11 Pro
├── GX Works2
├── Driver CH340/SC09
├── Python 3.11 x64
├── Python fxplc gateway chạy Windows Service
├── Caddy Windows
├── Camera streamer
│   ├── ưu tiên thử uStreamer Windows nếu có binary đã kiểm chứng
│   └── fallback: go2rtc Windows + FFmpeg
├── Tailscale chạy Unattended
└── Windows Remote Desktop
```

Không cài lại:

```text
ngrok
Cloudflare Tunnel trong giai đoạn đầu
HSL
KVM/VirtualBox/VMware
```

## Gateway PLC hiện tại

Source backup:

```text
MIGRATION_2026-06-29\Windows_Readable\PiGatewayFxplc
```

Các biến quan trọng:

```text
FXPLC_SERIAL_PORT=COMx
FXPLC_HTTP_HOST=127.0.0.1
FXPLC_HTTP_PORT=5000
FXPLC_ALLOW_WRITES=1
FXPLC_PULSE_SECONDS=0.1
FXPLC_ENCODER_PULSES_PER_REV=5000
FXPLC_SPEED_SAMPLE_SECONDS=0.1
```

Serial:

```text
9600 baud
7 data bits
Even parity
1 stop bit
```

API:

```text
GET  /health
GET  /telemetry
POST /control
```

Gateway Linux trước đây chạy:

```text
127.0.0.1:5000
```

Trên Windows cần:

- Tạo virtualenv Windows mới, không dùng `.venv` Linux.
- Cài dependency từ `requirements.txt`.
- Giữ `vendor\fxplc`.
- Xác định COM thật trong Device Manager.
- Chạy gateway bằng WinSW hoặc Windows Service wrapper.
- Bật tự restart khi lỗi.

## Xung đột GX Works2 và gateway

Một dây SC09/CH340 chỉ được một chương trình giữ tại một thời điểm.

```text
Gateway đang dùng COM → GX Works2 không online PLC được.
GX Works2 đang dùng COM → gateway phải dừng.
```

Cần tạo hai script PowerShell:

```text
Start-GXWorksMode.ps1
Stop-GXWorksMode.ps1
```

Luồng mong muốn:

1. Vào GX Works2 mode: dừng Windows Service của gateway rồi mở GX Works2.
2. Thoát GX Works2: bật lại gateway.
3. Kiểm tra `/health` sau khi bật lại.

## Caddy hiện tại

Caddy Linux trước đây nghe port `8888`:

```text
/plc/* → localhost:5000
/cam/* → localhost:8080
```

Caddy cũng thêm:

```text
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, OPTIONS
Access-Control-Allow-Headers: Content-Type, ngrok-skip-browser-warning
```

Trên Windows nên giữ Caddy để:

- Chỉ có một địa chỉ WebGL/API/camera.
- Xử lý CORS.
- Giữ route `/plc` và `/cam`.
- Có thể đổi camera backend mà không đổi URL Unity.
- Có thể phục vụ luôn Unity WebGL sau này.

## Camera

Trước đây:

```text
uStreamer
127.0.0.1:8080
640x480
15 FPS
```

URL:

```text
/?action=stream
/?action=snapshot
```

Trên Windows:

1. Thử binary uStreamer Windows nếu có nguồn đáng tin và test được.
2. Phải xác nhận camera tự chạy sau reboot dưới dạng service.
3. Phải xác nhận đúng URL stream/snapshot.
4. Nếu không ổn, dùng go2rtc Windows + FFmpeg.
5. Nếu dùng go2rtc, Caddy sẽ rewrite `/cam/?action=stream` sang endpoint MJPEG của go2rtc.

## Unity project

Project:

```text
MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main
```

Unity version:

```text
6000.3.11f1
```

Build scenes:

```text
Assets/Scenes/Sy_scene.unity
Assets/Scenes/HMI_scene.unity
```

Các file từng được sửa để tránh URL Pi cũ:

```text
Assets/PLCController_v2.cs
Assets/Scripts/HmiSceneBootstrap.cs
Assets/MjpegStreamer3D.cs
Assets/MjpegStreamer.cs
```

URL hiện vẫn đang trỏ tới ngrok cũ:

```text
https://unacquiescent-quiana-excepable.ngrok-free.dev
```

Ngrok cũ đã gặp:

```text
ERR_NGROK_725: hết bandwidth tháng
```

Sau khi chọn IP/Tailscale/public URL mới phải:

1. Sửa URL trong Unity.
2. Kiểm tra cả serialized values trong scene/prefab.
3. Build lại WebGL.

Tốt nhất về sau phục vụ WebGL và API cùng Caddy rồi dùng URL tương đối:

```text
/plc
/plc/control
/cam/?action=stream
```

## Network cũ để tham chiếu

```text
LAN Ubuntu:       10.170.43.240/24
Public egress:    103.238.69.131
Tailscale Ubuntu: 100.83.126.60
ZeroTier Ubuntu:  10.38.100.163
```

`103.238.69.131` thuộc dải Hanoi Telecom nhưng chưa xác nhận là IP public tĩnh dành riêng.

Trước khi bỏ tunnel và dùng IP trực tiếp phải hỏi quản trị mạng:

1. WAN IP router có đúng `103.238.69.131` không?
2. IP có cố định và cấp riêng không?
3. Có quyền forward port `80/443` về Windows không?
4. Có bị CGNAT/double NAT không?

Nếu không có quyền inbound NAT:

- Dùng Tailscale cho truy cập riêng.
- Hoặc sau này quay lại Cloudflare Tunnel/domain nếu cần public WebGL.

## Bảo mật cần xử lý

Gateway cũ đang có:

```text
FXPLC_ALLOW_WRITES=1
Access-Control-Allow-Origin: *
```

Không được public thẳng `/plc/control` ra Internet khi chưa có authentication/authorization.

Tailscale + RDP:

- Cài Tailscale trong Windows.
- Bật `Run Unattended`.
- Không mở port RDP `3389` trực tiếp ra Internet.
- Windows 11 Pro chỉ có một interactive session thông thường tại một thời điểm.

## Việc cần làm tiếp theo

1. Cài Windows 11 Pro lên NVMe.
2. Gắn lại HDD và xác nhận backup đọc được.
3. Cài driver và Windows Update/25H2.
4. Cài Python 3.11, dựng gateway local và test `/health`.
5. Cài CH340, xác nhận COM và đọc PLC.
6. Cài camera streamer, test snapshot/MJPEG.
7. Cài Caddy Windows và test `/plc`, `/cam`.
8. Cài Tailscale, bật RDP.
9. Cài GX Works2.
10. Tạo script chuyển chế độ gateway/GX Works2.
11. Test motor an toàn: STOP trước, tốc độ thấp, có người cạnh nút dừng.
12. Test reboot và tự khởi động service.
13. Chạy ổn định 24 giờ rồi 72 giờ.
14. Sau cùng mới quyết định IP public/domain/Cloudflare.
