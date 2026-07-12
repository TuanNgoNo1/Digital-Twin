# Phiên 2026-07-13 — StartScene / HuongdanBai1 — Codex

> **Dự án / bối cảnh ngắn:** Unity project Digital-Twin mô phỏng bài thực hành nối dây điều khiển động cơ Servo. Phiên này tập trung vào StartScene, phần giới thiệu hệ thống và màn hướng dẫn trước khi vào bài nối dây ở `Sy_scene`.

---

## ✅ ĐÃ LÀM XONG
- Xây dựng flow UI trong `StartScene`:
  - Màn đầu có tiêu đề bài thực hành và nút `Tiếp tục`.
  - Sau khi xem hết `IntroductionPanel`, người chơi được chuyển sang `HuongdanBai1`.
  - Trong `HuongdanBai1` có nút `Thực hành`; nhấn nút này load sang `Sy_scene`.
- Tạo nội dung hướng dẫn nối dây theo phương án “Quy trình 3 bước”:
  1. Chọn đầu dây.
  2. Kéo tới lỗ cắm.
  3. Thả để gắn socket.
- Thay minh họa trừu tượng ban đầu bằng ảnh render từ chính assets trong `Sy_scene`, giúp người chơi nhận ra đúng bộ dây, socket và module thực tế.
- Tạo thư mục ảnh minh họa:
  - `Assets/GuideImages/huongdan_bai1_step1.png`
  - `Assets/GuideImages/huongdan_bai1_step2.png`
  - `Assets/GuideImages/huongdan_bai1_step3.png`
- Sửa ảnh bước 2: bỏ nét/mũi tên nguệch ngoạc khó hiểu, chỉ giữ vòng tròn đỏ khoanh vùng lỗ cắm.
- Kiểm tra lại listener:
  - `PracticeButton` gọi `StartScreenController.LoadPracticeScene`.
  - Completion của `IntroductionPanelController` gọi `StartScreenController.ShowGuide`.
- Không lưu thay đổi vào `Sy_scene`; scene này chỉ được mở để render ảnh minh họa.

## 🔧 ĐANG LÀM DỞ / CHƯA XONG
- Không có việc đang làm dở.

## ➡️ CẦN LÀM TIẾP (việc cho người sau)
1. Bổ sung thêm ý tưởng/nội dung hướng dẫn nếu muốn người mới dễ hiểu hơn, ví dụ:
   - thêm highlight từng dây theo thứ tự bài;
   - thêm chú thích ngắn “đầu dây” và “lỗ cắm/socket” ngay trên ảnh;
   - thêm trạng thái đúng/sai hoặc ví dụ “cắm chưa sát tâm socket”.
2. Sửa giao diện `HuongdanBai1` cho đẹp và cân đối hơn:
   - tăng tính nhất quán khoảng cách giữa 3 card;
   - cân lại kích thước chữ, ảnh và nút `Thực hành`;
   - xem xét layout dạng 1 ảnh lớn + 3 bước bên cạnh nếu 3 card ngang còn hơi chật.
3. Nếu có thời gian, chụp lại ảnh minh họa đẹp hơn từ camera/crop khác của `Sy_scene` để giảm cảm giác “ảnh render tạm”.

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT
- Các file chính đã đụng tới:
  - `Assets/Scenes/StartScene.unity`
  - `Assets/Scripts/StartScreenController.cs`
  - `Assets/Scripts/IntroductionPanelController.cs`
  - `Assets/GuideImages/`
- `Sy_scene` có model thực tế `3D_Thay_Tien_1`, `WireHeads_Storage`, `Sockets`, `WiringGuides_Storage`; ảnh minh họa được render từ các object này.
- Unity Console có thể còn log cũ về “Instantiating material due to calling renderer.material during edit mode” từ lần render thử đầu tiên. Ảnh đã được render lại bằng `sharedMaterial`; lỗi này không phản ánh trạng thái scene hiện tại.
- Trong `git status` tại thời điểm bàn giao, một số file đang hiện dạng untracked (`??`) trong môi trường làm việc. Người tiếp theo nên kiểm tra kỹ trước khi commit để tránh bỏ sót asset mới.

---

<!-- Ghi chú phiên: Không có task đang dở. Ưu tiên tiếp theo là bổ sung ý tưởng hướng dẫn và làm lại UI đẹp hơn. -->
