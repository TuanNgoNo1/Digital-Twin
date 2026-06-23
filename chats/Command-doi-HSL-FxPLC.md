Đã có sẵn script chuyển đổi trên Pi. SSH vào Pi rồi chạy:

```bash
ssh admin@10.38.100.27
```

**Xem gateway đang dùng:**

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh status
```

**Chuyển sang fxplc miễn phí:**

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh fxplc
curl -s http://localhost:5000/health
```

Kết quả cần có:

```json
{"gateway":"fxplc","allowWrites":true}
```

**Chuyển về HSL:**

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh hsl
curl -s http://localhost:5000/debug
```

**Restart gateway đang chạy:**

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh restart
```

**Reset cáp SC09 rồi bật lại gateway đang dùng:**

```bash
/home/admin/PiGatewayFxplc/switch-gateway.sh reset-sc09
```

Unity, WebGL, Caddy và ngrok không cần đổi cấu hình vì gateway được chọn luôn dùng port `5000`.

Lưu ý:

- HSL và fxplc không thể chạy cùng lúc vì dùng chung SC09.
- Nếu HSL trả lỗi authorization, chuyển sang fxplc hoặc restart HSL.
- Hiện sau khi reboot Pi, HSL vẫn tự chạy mặc định.
- fxplc hiện đã bật quyền ghi nên có thể điều khiển motor thật.