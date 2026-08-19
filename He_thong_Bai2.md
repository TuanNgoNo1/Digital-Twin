# HƯỚNG DẪN TÍCH HỢP HỆ THỐNG REMOTE PLC QUA WEB
> **Dành cho:** Kỹ sư hệ thống (DevOps), Lập trình viên Web (Backend/Frontend) và Quản trị viên phòng Lab.

---

## 1. Ý TƯỞNG CỐT LÕI (Dành cho người mới bắt đầu)

[cite_start]Hệ thống này giải quyết bài toán: **Làm sao để sinh viên ngồi tại nhà, chỉ dùng trình duyệt web (Chrome/Edge) nhưng vẫn tự tay lập trình và điều khiển được thiết bị PLC thật đặt tại phòng Lab của trường[cite: 2, 6, 7].**

[cite_start]Hệ thống hoạt động theo triết lý **"Xem Livestream + Điều khiển từ xa"** (tương tự như Cloud Gaming)[cite: 4, 16]:
* [cite_start]Chúng ta **không** biến phần mềm lập trình chuyên dụng (GX Works2) thành một trang web[cite: 4, 70]. [cite_start]Phần mềm này vẫn nằm yên và chạy trên máy tính tại phòng Lab[cite: 4, 20].
* [cite_start]Trình duyệt của sinh viên chỉ mở một trang web có nhúng một khung hình chữ nhật đặc biệt. [cite_start]Khung này chỉ truyền duy nhất hình ảnh của cửa sổ phần mềm GX Works2 (đã được bóc tách, giấu sạch màn hình Desktop Windows)[cite: 19, 35].
* [cite_start]Khi sinh viên bấm chuột hay gõ phím vào khung đó, tín hiệu sẽ truyền về phòng Lab để điều khiển phần mềm và tác động xuống phần cứng thật[cite: 22, 31].

---

## 2. SƠ ĐỒ LUỒNG HOẠT ĐỘNG TỔNG THỂ

```text
[cite_start][ Máy Sinh Viên ] (Chỉ dùng Trình duyệt Web) 
       │
       [cite_start]│ (1) Đăng nhập & Chọn bài học [cite: 24, 25]
       ▼
[ Spring Boot Portal ] ──► "Bác bảo vệ": Kiểm tra quyền, ca học, quản lý khóa phiên [cite: 17, 22]
       │                   - Trạng thái FREE: Cho phép vào điều khiển [cite: 49]
       │                   - Trạng thái ACTIVE: Bắt người sau xếp hàng chờ [cite: 49]
       │
       │ (2) Mở đường ống truyền hình ảnh [cite: 28]
       ▼
[ Apache Guacamole (Docker) ] ──► "Bộ dịch mã": Biến đổi tín hiệu RDP Windows thành luồng dữ liệu Web 
       │
       │ (3) Stream cửa sổ ứng dụng độc lập [cite: 15, 29]
       ▼
+-----------------------------------------------------------------------------------------+
| MÁY CHỦ PHÒNG LAB (Windows 11 Pro / Windows Server)                                     |
|                                                                                         │
|  [ RDP RemoteApp ] ───► Chỉ bóc tách & hiển thị duy nhất CỬA SỔ phần mềm GX Works2 [cite: 19, 35] │
|         │               (Ẩn hoàn toàn màn hình nền, ổ đĩa, nút Start của hệ điều hành) [cite: 19, 36]│
|         ▼                                                                               │
|  [ GX Works2 App ] ───► Ứng dụng gốc đang kết nối trực tiếp với phần cứng PLC [cite: 8, 20]      │
|         │                                                                               │
|         ▼ 🛡️ LỚP KHÓA CỨNG BẢO MẬT (AppLocker / Group Policy)             │
|  Chặn hoàn toàn: Không cho mở ổ C:, CMD, PowerShell, Task Manager, Control Panel [cite: 22, 37, 39, 40] │
+--------------------------------------------------┬--------------------------------------+
                                                   │
                                                   │ (4) Kết nối cáp vật lý (USB/Cáp mạng) [cite: 15, 22]
                                                   ▼
[ THIẾT BỊ PLC MITSUBISHI THẬT ] ──► Nhận lệnh điều khiển cơ cấu chấp hành (Động cơ, băng tải) 
                                      🚨 Chế độ an toàn: Mất kết nối mạng -> Tự dừng máy [cite: 22, 53]