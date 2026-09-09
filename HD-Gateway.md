# HƯỚNG DẪN GATEWAY CHO NHÓM TRƯỜNG BÌNH

Cập nhật: 03/08/2026

## 1. Mục tiêu

Nhóm Trường Bình sử dụng URL riêng:

```text
Base URL:  http://103.238.69.131:8080/plc2
Health:    http://103.238.69.131:8080/plc2/health
Telemetry: http://103.238.69.131:8080/plc2/telemetry
Control:   http://103.238.69.131:8080/plc2/control
```

URL này dùng chung IP public và port `8080` với hệ thống hiện tại nhưng không trùng `/plc` hoặc `/rs485` của nhóm khác.

## 2. Những gì server đã có

- IP public: `103.238.69.131`.
- IP LAN của server: `10.170.43.240`.
- NAT public `103.238.69.131:8080` đã chuyển về Caddy trên server.
- Caddy đang chạy và Caddyfile đã validate thành công.
- Route `/plc2` đã được thêm vào Caddy.
- Route `/plc`, `/rs485`, camera và backend cũ vẫn hoạt động HTTP `200`.
- Caddy hiện chuyển `/plc2/*` tới `localhost:9001`.

Không cần mở thêm NAT hoặc public port mới.

## 3. Trạng thái còn thiếu

Hiện `/plc2` trả:

```text
HTTP 502 Bad Gateway
```

Nguyên nhân: chưa có chương trình của nhóm Trường Bình chạy và lắng nghe tại:

```text
127.0.0.1:9001
```

Server hiện chưa có source, file `.exe`, `.dll`, `gateway.py` hoặc project Node.js của HTTP Gateway nhóm Trường Bình.

## 4. Kiến trúc nhóm cần triển khai

```text
PLC Trường Bình
→ MQTT PLC Adapter
→ MQTT Broker localhost:1883
→ HTTP Gateway localhost:9001
→ Caddy /plc2/*
→ Unity
```

Trong kiến trúc này:

- Port `1883` dành cho MQTT broker nội bộ.
- Port `9001` dành cho HTTP Gateway.
- Không dùng port `9001` làm raw MQTT nếu Unity đang gọi API HTTP.
- Không public trực tiếp port `1883` hoặc `9001` ra Internet.

## 5. Việc nhóm Trường Bình cần làm

### Bước 1 - Chuẩn bị MQTT

Cài Mosquitto hoặc broker tương đương và tạo tài khoản riêng cho nhóm.

Topic đề xuất:

```text
lab/bai3/truong-binh/plc/telemetry
lab/bai3/truong-binh/plc/status
lab/bai3/truong-binh/plc/command
lab/bai3/truong-binh/plc/ack
```

Nếu Mosquitto đã được cài dưới dạng Windows Service:

```powershell
Start-Service mosquitto
Test-NetConnection localhost -Port 1883
```

### Bước 2 - Cung cấp và chạy HTTP Gateway

Nhóm phải đưa lên server một trong các dạng:

```text
gateway.py
Bai3Gateway.exe
Bai3Gateway.dll
Project Node.js có package.json
```

Gateway phải:

1. Kết nối MQTT broker `localhost:1883`.
2. Subscribe topic telemetry/status/ack.
3. Publish lệnh vào topic command.
4. Bind HTTP tại `127.0.0.1:9001`.
5. Cung cấp ba endpoint:

   ```text
   GET  /health
   GET  /telemetry
   POST /control
   ```

Ví dụ lệnh chạy tùy công nghệ:

```powershell
python gateway.py
```

hoặc:

```powershell
dotnet Bai3Gateway.dll
```

hoặc:

```powershell
npm start
```

### Bước 3 - Kiểm tra gateway nội bộ

Phải đạt trước khi kiểm tra URL public:

```powershell
Test-NetConnection localhost -Port 9001
curl.exe http://localhost:9001/health
curl.exe http://localhost:9001/telemetry
```

Kết quả mong đợi:

```text
TcpTestSucceeded: True
HTTP 200
JSON telemetry hợp lệ
```

## 6. Lưu ý về đường dẫn Caddy

Caddy hiện dùng:

```caddyfile
handle /plc2/* {
    reverse_proxy localhost:9001
}
```

Với cấu hình này, gateway nhận nguyên đường dẫn:

```text
/plc2/health
/plc2/telemetry
/plc2/control
```

Nếu HTTP Gateway chỉ cung cấp `/health`, `/telemetry`, `/control`, phải đổi Caddy thành:

```caddyfile
handle_path /plc2/* {
    reverse_proxy localhost:9001
}
```

`handle_path` sẽ bỏ `/plc2` trước khi chuyển request vào gateway. Đây là cấu hình được khuyến nghị cho API nêu trong tài liệu này.

Sau khi sửa Caddy phải validate thành công rồi mới restart.

## 7. Kiểm tra URL qua Caddy

Sau khi gateway `9001` chạy:

```powershell
curl.exe http://10.170.43.240:8080/plc2/health
curl.exe http://10.170.43.240:8080/plc2/telemetry
```

Sau đó kiểm tra từ mạng ngoài hoặc 4G:

```text
http://103.238.69.131:8080/plc2/health
http://103.238.69.131:8080/plc2/telemetry
```

Nếu local `9001` chạy nhưng public vẫn `502`, kiểm tra lại `handle` và `handle_path` như mục 6.

## 8. Cấu hình trong Unity

```text
Base URL:          http://103.238.69.131:8080/plc2
Telemetry Endpoint: /telemetry
Control Endpoint:   /control
```

Do hệ thống hiện dùng HTTP, WebGL cần đặt:

```text
Edit
→ Project Settings
→ Player
→ WebGL
→ Other Settings
→ Insecure HTTP Option
→ Always Allowed
```

Unity chỉ báo đã kết nối khi telemetry mới liên tục được trả về. Không coi HTTP `200` với timestamp cũ là PLC còn online.

## 9. Checklist hoàn thành

- [ ] MQTT broker chạy tại `localhost:1883`.
- [ ] PLC Adapter publish đúng topic của nhóm Trường Bình.
- [ ] HTTP Gateway chạy tại `127.0.0.1:9001`.
- [ ] Local `/health` trả HTTP `200`.
- [ ] Local `/telemetry` trả JSON và timestamp thay đổi.
- [ ] Gateway publish lệnh và nhận đúng ACK từ PLC.
- [ ] Caddy dùng `handle_path` nếu gateway chỉ có endpoint tại root.
- [ ] `/plc2/health` qua LAN trả HTTP `200`.
- [ ] `/plc2/telemetry` qua public IP trả HTTP `200`.
- [ ] Các route `/plc` và `/rs485` vẫn hoạt động.
- [ ] Unity sử dụng đúng Base URL `/plc2`.
- [ ] Mất MQTT/PLC khiến Unity báo offline trong thời gian quy định.

## 10. Thông tin nhóm cần bàn giao cho quản trị server

Nhóm Trường Bình cần gửi:

1. Source hoặc file chạy HTTP Gateway.
2. Lệnh khởi động chính xác.
3. Danh sách dependency cần cài.
4. MQTT broker URL và topic sử dụng.
5. Cách gateway kết nối PLC thật.
6. Mẫu JSON telemetry, command và ACK.
7. Giới hạn tốc độ/số vòng an toàn.
8. Cách dừng gateway và phục hồi khi lỗi.

Khi chưa có các thông tin trên, Caddy chỉ mở được đường dẫn `/plc2` nhưng không thể tự tạo dữ liệu MQTT hoặc điều khiển PLC.
