# Hướng dẫn Guacamole, RemoteApp và chuyển chế độ PLC

Tài liệu này là quy trình vận hành chung cho sinh viên, giảng viên và quản trị viên của máy PLC Digital Twin.

## 1. Ba lớp tài khoản khác nhau

Hệ thống hiện có ba lớp đăng nhập độc lập:

| Lớp | Ví dụ | Mục đích |
|---|---|---|
| Web P-DTwin `:8080` | `SV001`, admin, teacher | Bài học, điểm, quyền web và JWT |
| Apache Guacamole | tài khoản được cấp trong Guacamole | Mở kết nối RemoteApp |
| Windows RDP | `plc_student` | Chạy GX Works2 trong Windows |

Đăng nhập web bằng `SV001` chưa tự động đăng nhập Guacamole và chưa chứng minh rằng cùng người đó đang giữ session Windows. Chưa được coi ba tài khoản trên là SSO.

## 2. Vì sao đóng tab không phải là kết thúc phiên

```text
Đóng tab / Disconnect
  -> đường truyền Guacamole ngắt
  -> session Windows có thể chuyển thành Disc
  -> GD2.exe và các process phụ có thể vẫn chạy
  -> COM3 có thể vẫn bị giữ

Windows Sign out / Logoff
  -> kết thúc session plc_student
  -> đóng toàn bộ process trong session
  -> watchdog có thể trả COM3 về PLC Gateway
```

Phím `Ctrl+Alt+Shift` mở/đóng menu bên của Guacamole. Nút **Disconnect** trong menu này chỉ dùng để rời màn hình RemoteApp; nó không thay thế Sign out.

## 3. Quy trình sinh viên — cách thủ công hiện tại

### 3.1 Bắt đầu ca GX Works2

1. Đăng nhập trang P-DTwin tại `http://103.238.69.131:8080/`.
2. Chỉ bắt đầu khi admin/giảng viên xác nhận COM3 đã được chuyển sang GX Works Mode.
3. Mở `http://103.238.69.131:8080/gxworks2/`.
4. Đăng nhập Guacamole bằng tài khoản được cấp.
5. Chọn kết nối **GX Works2 - PLC Server**.
6. Trong GX Works2, kiểm tra đúng project, đúng PLC và chỉ chạy motor khi có người giám sát phần cứng.

Trong GX Works Mode, Digital Twin báo PLC mất kết nối là trạng thái dự kiến vì Python gateway đã nhả COM3.

### 3.2 Kết thúc ca bằng RemoteApp `Kết thúc phiên PLC`

1. Save project trong GX Works2.
2. Đưa PLC/motor về trạng thái an toàn.
3. Đóng cửa sổ GX Works2.
4. Trở về trang chủ Guacamole. Nếu cửa sổ cũ không tự thoát, dùng `Ctrl+Alt+Shift` rồi Disconnect; Disconnect ở bước này chỉ để quay lại danh sách kết nối.
5. Mở kết nối **Kết thúc phiên PLC**. Có thể mở trang Guacamole trong tab trình duyệt thứ hai nếu tab GX Works2 đang kẹt.
6. Trong hộp xác nhận, chọn **Yes**.
7. Chờ kết nối đóng. Đây mới là Windows Sign out thật.

RemoteApp `Kết thúc phiên PLC` không mở desktop, cmd hoặc PowerShell cho sinh viên. Nó chỉ hiển thị xác nhận và chạy Windows Sign out trong chính session hiện tại.

### 3.3 Nếu chưa cài RemoteApp kết thúc phiên

Sinh viên phải Save và đóng GX Works2, sau đó báo admin. Admin thực hiện quy trình cleanup tại mục 4. Không chỉ đóng tab rồi coi ca học đã kết thúc.

## 4. Quy trình admin/giảng viên — thủ công

### 4.1 Chuyển COM3 cho Guacamole/GX Works2

Trên Desktop server, chạy với quyền admin:

```text
PREPARE-GXWORKS-REMOTE.bat
```

Script này dừng Python PLC gateway và `PlcBridge`, nhả COM3 nhưng giữ Caddy, web `8080`, camera, Pixel Streaming, RDP và Guacamole hoạt động.

Không dùng shortcut `1 - GX Works Mode.lnk` cho phiên Guacamole nếu không muốn mở thêm GX Works2 tại console server. Shortcut đó dành cho thao tác trực tiếp trên server; file BAT `PREPARE-GXWORKS-REMOTE.bat` là lựa chọn đúng cho RemoteApp.

### 4.2 Kết thúc cưỡng chế một ca bị treo

1. Xác nhận sinh viên đã Save bài nếu còn liên lạc được.
2. Chạy:

   ```text
   CLEANUP-PLC-STUDENT-SESSIONS.bat
   ```

3. Script tìm session theo username `plc_student`; không dùng session ID cố định.
4. Sau cleanup, chạy:

   ```text
   2 - PLC Gateway Mode.lnk
   ```

5. Kiểm tra:

   ```powershell
   curl.exe http://127.0.0.1:5000/health
   curl.exe http://127.0.0.1:5000/telemetry
   ```

6. Chỉ cấp ca mới khi không còn session `plc_student`, không còn `GD2.exe`, gateway trả HTTP 200 và PLC ở trạng thái an toàn.

### 4.3 Kiểm tra nhanh session

```powershell
quser
Get-Process GD2 -ErrorAction SilentlyContinue
```

`Disc` nghĩa là Disconnected, không phải đã logoff.

## 5. Cài RemoteApp `Kết thúc phiên PLC`

### 5.1 Đăng ký trên Windows

Chạy:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\remoteapp
.\Run-ConfigureGXWorksRemoteApp.ps1
```

Chấp nhận UAC. Script đăng ký hai alias:

| Alias | Chương trình |
|---|---|
| `GXWorks2` | GX Works2 `GD2.EXE` |
| `PLCLogoff` | `C:\ProgramData\PDTwin\RemoteApp\EndPlcSession.exe` |

### 5.2 Tạo kết nối thứ hai trong Guacamole

Đăng nhập Guacamole bằng admin, mở **Settings → Connections → New Connection** và nhập:

```text
Name: Kết thúc phiên PLC
Protocol: RDP
Hostname: host.docker.internal
Port: 3389
Username: plc_student
Security mode: Any / NLA
Ignore server certificate: enabled
RemoteApp: ||PLCLogoff
RemoteApp directory: C:\ProgramData\PDTwin\RemoteApp
```

Không lưu mật khẩu Windows nếu chủ hệ thống chưa chấp nhận rủi ro. Cấp quyền **READ** kết nối này cho tài khoản/nhóm sinh viên, ví dụ `SV001`. Không cấp UPDATE, DELETE hoặc ADMINISTER.

Giữ `max-connections=1` và `max-connections-per-user=1` cho kết nối dùng PLC thật.

## 6. Tự động hóa từ web `8080`

Có thể tự động hóa, nhưng trình duyệt không được chạy BAT, không được giữ token Windows admin và không được gọi trực tiếp controller hệ thống.

Kiến trúc đúng:

```text
Browser
  -> Spring Boot :8080 (JWT, role, session lease)
     -> 127.0.0.1:5010 (token server-to-server)
        -> Lab Session Controller chạy bằng SYSTEM
           -> chuyển GX Works/Gateway
           -> logoff plc_student
           -> kiểm tra COM3 và /health
```

Port `5010` chỉ bind loopback. Không thêm route `5010` vào Caddy và không chép `controller-token.txt` vào JavaScript/frontend.

### 6.1 Cài controller nội bộ

Controller đã được chuẩn bị trong `ops/server-control`. Khi sẵn sàng triển khai, chạy:

```powershell
cd D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control
.\Run-InstallLabSessionController.ps1
```

Installer tạo Scheduled Task chạy bằng SYSTEM, token tại:

```text
C:\ProgramData\PDTwin\LabControl\controller-token.txt
```

Kiểm tra không cần token:

```powershell
curl.exe http://127.0.0.1:5010/health
```

API nội bộ, bắt buộc header `X-Lab-Control-Token`:

| Method | Endpoint | Tác dụng |
|---|---|---|
| GET | `/api/lab/status` | Mode, COM3, gateway, GD2 và session |
| POST | `/api/lab/mode/gxworks` | Dừng gateway/PlcBridge và nhả COM3 |
| POST | `/api/lab/mode/gateway` | Trả về gateway nếu không còn session/GD2 |
| POST | `/api/lab/session/end` | Logoff `plc_student` rồi bật gateway |

Watchdog đi kèm chỉ tự trả gateway khi session `plc_student` đã từng tồn tại và sau đó biến mất do Sign out. Disconnect vẫn để session tồn tại nên không kích hoạt auto-return.

### 6.2 API cần bổ sung trong Spring Boot

Backend JAR hiện có JWT và các role `ADMIN`, `TEACHER`, `STUDENT`, nhưng source Java không nằm trong workspace này. Khi có source backend, bổ sung các endpoint public qua `8080`:

| Endpoint backend đề xuất | Quyền | Xử lý |
|---|---|---|
| `GET /api/lab-session/status` | authenticated | Trả trạng thái và người đang giữ ca |
| `POST /api/lab-session/start` | student/teacher/admin | Tạo lease, gọi controller chuyển GX mode |
| `POST /api/lab-session/end` | chủ lease hoặc admin | Gọi controller logoff + gateway, giải phóng lease |
| `POST /api/admin/lab-session/force-end` | admin | Cleanup cưỡng chế và ghi audit log |

Backend phải lấy username/role từ JWT, không nhận role do browser tự gửi. Mọi thao tác phải có audit gồm username, thời gian, hành động, kết quả và trạng thái trước/sau.

### 6.3 Nút giao diện đề xuất

Sinh viên:

- **Bắt đầu GX Works2**: xin lease, chuyển GX mode rồi mở `/gxworks2/`.
- **Kết thúc ca và trả PLC**: yêu cầu Save/đóng GX Works2, sau đó logoff và bật gateway.
- Hiển thị rõ `Đang dùng bởi bạn`, `Đang bận`, `Đang trả PLC`, `Lỗi cần admin`.

Admin/giảng viên:

- Xem mode hiện tại, session ID, COM3, gateway health.
- **Kết thúc cưỡng chế**.
- **Trả về Digital Twin**.
- Xem audit log và thời gian giữ ca.

## 7. State machine bắt buộc cho một PLC thật

```text
FREE/GATEWAY
  -> PREPARING_GX
  -> ACTIVE_GX
  -> RETURNING_GATEWAY
  -> FREE/GATEWAY

Bất kỳ bước lỗi nào
  -> FAULT
  -> admin kiểm tra trước khi cấp ca mới
```

Không cho hai sinh viên cùng giữ lease. Không coi `max-connections=1` của Guacamole là cơ chế cleanup session Windows.

## 8. Bảng xử lý sự cố

| Hiện tượng | Nguyên nhân thường gặp | Xử lý |
|---|---|---|
| Web Digital Twin báo PLC đỏ khi GX Works đang dùng | Gateway đã nhả COM3 | Bình thường trong GX mode |
| Đóng tab nhưng GX Works vẫn tồn tại | Session chuyển `Disc` | Chạy RemoteApp logoff hoặc admin cleanup |
| Gateway không lấy lại COM3 | Còn `GD2.exe`/session hoặc `PlcBridge` | Logoff session, kiểm tra process, chạy Gateway Mode |
| `/plc/health` trả 502 trong GX mode | Python gateway đang tắt | Bình thường; chỉ bật lại sau khi logoff GX Works |
| Camera mất hình | Luồng FFmpeg/camera riêng | Không liên quan COM3; kiểm tra `/cam/snapshot.jpg` |
| Nút web start/end chưa xuất hiện | Backend JAR chưa tích hợp API mới | Dùng quy trình thủ công cho đến khi build backend mới |

## 9. Điều không được làm

- Không public port `5010`.
- Không đặt controller token trong frontend, URL, Git hoặc ảnh chụp.
- Không cho sinh viên quyền chạy PowerShell/cmd hoặc quyền admin Windows.
- Không kill riêng `GD2.exe` rồi coi như đã cleanup; phải logoff session.
- Không bật Python gateway, `PlcBridge` và GX Works2 cùng tranh COM3.
- Không cấp ca mới khi trạng thái đang `FAULT` hoặc chưa xác nhận PLC an toàn.
