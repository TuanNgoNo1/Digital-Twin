**Kiến trúc Bài 2 hiện tại**

```text
Luồng điều khiển:
GX Works2 → COM3/SC09 → PLC → motor thật

Luồng phản hồi:
Encoder → PLC → FX3U-485-BD → USB-RS485/COM5
→ Gateway C# → HTTP JSON → Unity WebGL
→ HMI + motor ảo
```

Hai luồng chạy song song và dùng hai cổng riêng:

- `COM3`: GX Works2 điều khiển PLC.
- `COM5`: chỉ nhận dữ liệu encoder, không điều khiển PLC.

**Thiết lập bắt buộc**

1. PLC dùng `FX3U-485-BD`.
2. Cấu hình GX Works2:
   - `Non-Procedural`
   - `RS-485`
   - `9600 baud`
   - `8 data bits`
   - `No parity`
   - `1 stop bit`
3. Ladder PLC tự gửi frame telemetry định kỳ, không cần pulse thủ công.
4. Gateway C# giữ COM5 và chuyển frame thành JSON:
   ```text
   http://127.0.0.1:5002/telemetry
   ```
5. Caddy public API:
   ```text
   http://103.238.69.131:8080/rs485/telemetry
   ```
6. Unity đọc JSON để:
   - Hiển thị RPM thực.
   - Hiển thị encoder, số vòng và góc.
   - Quay motor ảo theo motor thật.
   - Báo mất kết nối nếu quá 3 giây không có dữ liệu mới.

**Điểm quan trọng để thuật lại**

```text
GX Works2 điều khiển PLC qua một cổng.
Một cổng RS485 khác chỉ gửi phản hồi encoder về server.
Gateway chuyển dữ liệu serial thành API để Unity sử dụng.
```

Không để GX Works2 và gateway cùng sử dụng một cổng COM. Bài 2 hiện không dùng MQTT và không dùng Modbus RTU.