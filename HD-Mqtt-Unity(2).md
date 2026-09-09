# HƯỚNG DẪN MQTT - UNITY (TIẾP)

Cập nhật: 04/08/2026

## 1. Mục tiêu

Kết nối Unity WebGL với PLC nhóm Trường Bình thông qua MQTT WebSocket:

```text
PLC ↔ PLC Adapter ↔ Mosquitto ↔ Caddy ↔ Unity WebGL
```

URL Unity sử dụng:

```text
ws://103.238.69.131:8080/plc2
```

`/plc2` là đường dẫn MQTT WebSocket, không phải trang web hoặc REST API.

## 2. Những phần đã hoàn thiện

- Mosquitto đã được cài dưới dạng Windows Service và tự khởi động cùng Windows.
- MQTT TCP đang lắng nghe tại `10.170.43.240:1884`.
- MQTT WebSocket đang lắng nghe tại `127.0.0.1:9001`.
- Caddy đã có route `/plc2` chuyển tiếp tới Mosquitto `9001`.
- Caddyfile đã validate thành công và Caddy đã được restart.
- Handshake MQTT WebSocket qua Caddy local trả `101 Switching Protocols`.
- Handshake qua public IP cũng trả `101 Switching Protocols`.
- Subprotocol trả về đúng là `mqtt`.
- Các route `/`, `/plc`, `/rs485` và camera cũ vẫn hoạt động.
- Không cần mở thêm NAT hoặc public port mới.

Luồng đã có:

```text
ws://103.238.69.131:8080/plc2
→ Caddy
→ ws://127.0.0.1:9001
→ Mosquitto
```

## 3. Thông số kết nối hiện tại

### PLC Adapter trong LAN

```text
Broker host: 10.170.43.240
Broker port: 1884
Protocol: MQTT TCP
```

Nếu PLC Adapter chạy ngay trên server:

```text
Broker host: 127.0.0.1
Broker port: 1884
```

### Unity WebGL

```text
Broker URL: ws://103.238.69.131:8080/plc2
Protocol: MQTT 3.1.1
Transport: WebSocket
Keep Alive: 30 giây
Reconnect: 2–5 giây
Clean Session: true
```

Mỗi phiên Unity phải có Client ID khác nhau, ví dụ:

```text
bai3-unity-4f8912
```

Không dùng chung một Client ID cho nhiều sinh viên vì Mosquitto sẽ ngắt client cũ.

## 4. Topic đề xuất

```text
lab/bai3/truong-binh/plc/telemetry
lab/bai3/truong-binh/plc/status
lab/bai3/truong-binh/plc/command
lab/bai3/truong-binh/plc/ack
```

| Topic | Bên gửi | Bên nhận | QoS |
|---|---|---|---:|
| `.../telemetry` | PLC Adapter | Unity | 0 hoặc 1 |
| `.../status` | PLC Adapter | Unity | 1 |
| `.../command` | Unity | PLC Adapter | 1 |
| `.../ack` | PLC Adapter | Unity | 1 |

Không dùng topic chung như `plc/control` vì có thể xung đột với nhóm khác.

## 5. Việc nhóm cần làm tiếp theo

### Bước 1 - Hoàn thiện PLC Adapter

PLC Adapter phải:

1. Kết nối PLC riêng của nhóm Trường Bình.
2. Kết nối Mosquitto tại `10.170.43.240:1884`.
3. Subscribe topic `.../command`.
4. Chuyển command MQTT thành lệnh PLC.
5. Publish dữ liệu thật vào `.../telemetry`.
6. Publish kết quả thực hiện vào `.../ack`.
7. Publish trạng thái online/offline vào `.../status`.

Telemetry mẫu:

```json
{
  "deviceId": "plc-bai3-truong-binh",
  "sequence": 125,
  "timestamp": "2026-08-04T09:00:00Z",
  "connected": true,
  "running": true,
  "direction": "forward",
  "speedRpm": 10.2,
  "encoderCount": 12530,
  "rotationsExact": 2.506,
  "angle": 182.2
}
```

Command mẫu:

```json
{
  "commandId": "cmd-000125",
  "deviceId": "plc-bai3-truong-binh",
  "action": "START",
  "direction": "forward",
  "speedRpm": 10,
  "rotations": 2
}
```

ACK mẫu:

```json
{
  "commandId": "cmd-000125",
  "accepted": true,
  "executed": true,
  "message": "PLC acknowledged command"
}
```

### Bước 2 - Tích hợp MQTT vào Unity

Thư viện MQTT dùng trong Unity phải hỗ trợ:

- Unity WebGL.
- MQTT qua WebSocket.
- MQTT 3.1.1.
- Tự reconnect.
- Subscribe và publish bất đồng bộ.

Trong Unity:

1. Kết nối tới `ws://103.238.69.131:8080/plc2`.
2. Subscribe `telemetry`, `status` và `ack` sau khi kết nối.
3. Parse JSON telemetry rồi cập nhật HMI.
4. Publish nút START/STOP vào topic command.
5. Tạo `commandId` riêng cho mỗi lệnh.
6. Chỉ báo “Đang chạy” sau khi nhận ACK hoặc telemetry `running=true`.
7. Báo “Mất kết nối” nếu timestamp không đổi quá 3 giây.

Không dùng các URL sau:

```text
http://103.238.69.131:8080/plc2/health
http://103.238.69.131:8080/plc2/telemetry
```

Vì hệ thống hiện dùng MQTT WebSocket trực tiếp, không có REST HTTP Gateway.

### Bước 3 - Bảo mật Mosquitto

Hiện Mosquitto đang cấu hình:

```text
allow_anonymous true
```

Đây chỉ phù hợp để kiểm thử ban đầu. Trước khi nối lệnh tới motor thật cần:

1. Đổi thành `allow_anonymous false`.
2. Tạo username/password riêng cho PLC Adapter và Unity.
3. Tạo ACL chỉ cho phép topic `lab/bai3/truong/binh/*`.
4. Không cho nhóm Trường Bình truy cập topic nhóm khác.
5. Giới hạn RPM, số vòng và action tại PLC Adapter.
6. Ghi log command, thời gian và ACK.

Thông tin đăng nhập nằm trong WebGL vẫn có thể bị người dùng xem. Khi triển khai chính thức cần token theo phiên hoặc backend trung gian; username/password MQTT trong Unity không phải lớp bảo mật tuyệt đối.

### Bước 4 - Đảm bảo tự khởi động

Mosquitto và Caddy đã có cơ chế khởi động. Nếu PLC Adapter là một chương trình riêng, cần thêm nó thành Windows Service hoặc Scheduled Task với:

- Start at boot.
- Restart on failure.
- Log riêng.
- Không phụ thuộc người dùng đăng nhập.

## 6. Trình tự kiểm thử

### Kiểm thử phần mềm trước

1. Unity kết nối MQTT thành công.
2. Unity subscribe được topic test.
3. Publish telemetry giả và xác nhận HMI thay đổi.
4. Unity publish command thử, chưa nối motor.
5. PLC Adapter nhận đúng command và trả ACK.
6. Ngắt MQTT và xác nhận Unity báo mất kết nối.

### Kiểm thử PLC thật

1. Đặt motor ở vùng an toàn.
2. Dùng tốc độ và số vòng thấp.
3. Gửi một lệnh duy nhất.
4. Kiểm tra PLC nhận lệnh.
5. Kiểm tra ACK trùng `commandId`.
6. Kiểm tra telemetry thay đổi theo encoder thật.
7. Kiểm tra STOP vật lý và STOP từ phần mềm.

## 7. Cách nhận biết route hoạt động

Không mở `/plc2` như một trang web. Kết quả kỹ thuật đúng là:

```text
HTTP/1.1 101 Switching Protocols
Sec-WebSocket-Protocol: mqtt
```

Nếu Unity báo lỗi:

- `404`: sai đường dẫn `/plc2` hoặc Caddy chưa dùng config mới.
- `502`: Mosquitto port `9001` không chạy.
- `Connection refused`: Caddy hoặc Mosquitto đang tắt.
- `Not authorized`: sai username/password hoặc ACL.
- Kết nối được nhưng không có dữ liệu: sai topic hoặc PLC Adapter chưa publish.
- Kết nối chập chờn: kiểm tra Client ID có bị trùng không.

## 8. Checklist bàn giao

### Đã hoàn thiện

- [x] Mosquitto Windows Service.
- [x] MQTT TCP port `1884`.
- [x] MQTT WebSocket port `9001`.
- [x] Caddy route `/plc2`.
- [x] Public URL `ws://103.238.69.131:8080/plc2`.
- [x] Handshake MQTT WebSocket local và public.
- [x] Các route cũ vẫn hoạt động.

### Cần hoàn thiện

- [ ] Chốt topic MQTT.
- [ ] Hoàn thiện PLC Adapter.
- [ ] Chốt schema JSON telemetry/command/ACK.
- [ ] Tích hợp MQTT WebSocket vào Unity WebGL.
- [ ] Dùng Client ID riêng cho từng phiên.
- [ ] Tắt anonymous và cấu hình password/ACL.
- [ ] Thêm PLC Adapter vào startup.
- [ ] Test mất kết nối và tự reconnect.
- [ ] Test end-to-end với PLC thật ở điều kiện an toàn.
- [ ] Xác nhận dữ liệu Unity là phản hồi thật, không phải giá trị đặt.

## 9. Kết luận

Phần server public đã sẵn sàng:

```text
Unity → ws://103.238.69.131:8080/plc2 → Caddy → Mosquitto:9001
```

Nhóm tiếp theo tập trung vào ba việc:

```text
PLC Adapter + chuẩn MQTT topic/JSON + MQTT client trong Unity
```

Chưa gửi lệnh tới motor thật khi Mosquitto vẫn cho phép anonymous và PLC Adapter chưa có giới hạn an toàn.
