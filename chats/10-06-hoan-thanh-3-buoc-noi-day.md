# Phiên 2026-06-10 - Hoàn thành 3 bước nối dây - Tuấn

> **Dự án:** Digital Twin Unity mô phỏng bài thực hành đấu nối PLC, mạch phản hồi và mạch lực.
> **Scene chính:** `Assets/Scenes/Sy_scene.unity`.

---

## ✅ ĐÃ LÀM XONG

- Hoàn thành hệ thống đấu nối gồm **14 dây và 26 wire head**.
- Tổ chức màn chơi thành ba bước:
  - **Bước 1 - Mạch điều khiển:** các dây và socket thuộc nhóm điều khiển.
  - **Bước 2 - Mạch phản hồi:** các dây và socket thuộc nhóm phản hồi.
  - **Bước 3 - Mạch lực:** các dây và socket nối tới motor.
- Mỗi dây có hai đầu nối, có thể kéo và snap vào socket phù hợp.
- Các socket được cấu hình ID và màu dây chấp nhận tương ứng.
- Mỗi `WireBody` lưu cặp socket đáp án để kiểm tra kết nối đúng/sai.
- Hoàn thành luồng chơi tuần tự:
  - Người chơi phải nối đúng toàn bộ dây của bước hiện tại.
  - Khi hoàn thành một bước, các dây của bước đó tự ẩn.
  - Màn chơi tự chuyển sang bước tiếp theo.
  - Luồng tiếp tục cho tới khi hoàn thành đủ cả ba bước.
- Đã rà soát Hierarchy và Inspector của các object liên quan đến socket, wire head và wire body.

## 🔧 ĐANG LÀM DỞ / CHƯA XONG

- Chưa chỉnh xong vị trí màn HMI trong scene.
- Cần đặt lại HMI để không che khu vực thao tác nối dây và hiển thị hợp lý khi hoàn thành ba bước.

## ➡️ CẦN LÀM TIẾP (việc cho người sau)

1. Mở `Assets/Scenes/Sy_scene.unity` và chỉnh vị trí/kích thước HMI.
2. Play test toàn bộ luồng từ Bước 1 đến Bước 3:
   - Nối đúng toàn bộ dây của từng bước.
   - Kiểm tra dây bước vừa hoàn thành tự ẩn.
   - Kiểm tra bước tiếp theo tự hiển thị.
   - Kiểm tra HMI xuất hiện đúng thời điểm và không che màn chơi.
3. Build WebGL/SCORM và test lại thao tác nối dây, chuyển bước và HMI trên bản build.

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT

- Trạng thái bàn giao hiện tại được xác nhận là **đã hoàn thành 14 dây với 26 wire head**.
- Ba nhóm chính trong Hierarchy:
  - Socket: `Sockets/Buoc1_MachDieuKhien`, `Sockets/Buoc2_MachPhanHoi`, `Sockets/Buoc3_MachLuc`.
  - Dây/wire head: được chia theo từng bước trong màn chơi.
- Luồng kiểm tra kết nối:
  - `SocketPoint`: định danh socket, màu dây chấp nhận và trạng thái occupied.
  - `WirePlug`: điều khiển wire head, snap/unsnap vào socket.
  - `WireBody`: giữ hai wire head và cặp socket đáp án.
  - `CircuitManager`: kiểm tra tiến độ và điều khiển chuyển bước/mở HMI.
- Khi chỉnh HMI, cần test ở đúng độ phân giải mục tiêu của WebGL/SCORM để tránh vị trí trong Editor khác bản build.
- Không thay đổi lại cấu hình dây/socket nếu không cần thiết; phần việc còn lại chủ yếu là bố cục HMI và kiểm thử cuối.

---

## CHI TIẾT KỸ THUẬT

| Bước | Nhóm mạch | Trạng thái |
|---|---|---|
| 1 | Mạch điều khiển | Hoàn thành; nối đúng toàn bộ thì dây tự ẩn và chuyển Bước 2 |
| 2 | Mạch phản hồi | Hoàn thành; nối đúng toàn bộ thì dây tự ẩn và chuyển Bước 3 |
| 3 | Mạch lực | Hoàn thành; kết thúc luồng nối dây sau khi nối đúng toàn bộ |

| Thành phần | Vai trò |
|---|---|
| `Assets/Scripts/SocketPoint.cs` | Quản lý ID, màu chấp nhận, highlight và trạng thái socket |
| `Assets/Scripts/WirePlug.cs` | Điều khiển kéo, snap và unsnap từng wire head |
| `Assets/Scripts/WireBody.cs` | Quản lý hai đầu dây và kiểm tra cặp socket đáp án |
| `Assets/Scripts/CircuitManager.cs` | Theo dõi tiến độ kết nối, chuyển bước và điều khiển HMI |
| `Assets/Scenes/Sy_scene.unity` | Scene chứa toàn bộ socket, dây, wire head và HMI |
