# 📖 CÁCH DÙNG TÀI LIỆU NÀY (đọc 30 giây là dùng được)

> Đây là **file bàn giao 1 phiên làm việc với AI**. Quy ước: mỗi phiên = 1 file `chats/<ngày>-<chủ đề chính>.md`.
> File mới nhất (theo ngày) = trạng thái hiện tại của dự án. **Nhìn tên file là biết phiên đó làm gì.**

**🟢 NGƯỜI NHẬN VIỆC (đầu phiên):**
1. Lấy bản mới nhất:
   ```bash
   git checkout Unify && git pull origin Unify
   ```
2. Mở dự án Unity và đọc file bàn giao mới nhất trong `chats/`.
3. Câu đầu tiên gửi AI:
   > *"Đọc file `chats/15-06-hoan-thien-gameplay-popup-va-webgl-responsive.md` rồi tiếp tục từ mục '➡️ CẦN LÀM TIẾP'."*

**🔵 NGƯỜI BÀN GIAO (cuối phiên):**
1. Viết tổng kết phiên mới theo đúng bốn mục trong `_TEMPLATE.md`.
2. Commit và push lên nhánh `Unify`.

---

# Phiên 2026-06-15 — Hoàn thiện gameplay, popup kết quả và WebGL responsive — Tuấn

> **Dự án:** Digital Twin Unity mô phỏng bài thực hành đấu nối mạch điều khiển động cơ.
> **Mục tiêu phiên:** hoàn thiện UI/UX ba bước nối dây, bổ sung popup kiểm tra kết quả, hoàn thiện trạng thái kết thúc và sửa lỗi crop nội dung trên WebGL/server.
> **Scene chính:** `Assets/Scenes/Sy_scene.unity`.

---

## ✅ ĐÃ LÀM XONG

- Hoàn thiện UI/UX Bước 2 và Bước 3 dựa theo bố cục chuẩn của Bước 1:
  - Có nền trắng, tiêu đề bước và số dây đặt cạnh từng cặp wire head.
  - Bước 2 hiển thị dây số `7–12`.
  - Bước 3 hiển thị dây số `13–15`.
  - Các bảng hướng dẫn bên trái và labels cạnh socket được trình bày đồng bộ giữa ba bước.
- Bổ sung nền trắng phía sau socket labels để chữ rõ ràng trên model.
- Giữ nguyên toàn bộ vị trí thủ công của dây, heads, labels, hướng dẫn và HMI.
- Sửa lỗi thân dây số 6 không xuất hiện ngay khi bấm Play:
  - `WireBody` cập nhật `LineRenderer` liên tục theo vị trí hai wire head.
- Bổ sung popup kết quả sau khi người chơi đã cắm đủ tất cả dây của bước hiện tại:
  - Nếu có dây sai, popup màu cảnh báo hiển thị chính xác số dây sai và cặp socket đúng cần cắm lại.
  - Nếu toàn bộ dây đúng, popup xác nhận hoàn thành bước.
  - Chỉ chuyển sang bước tiếp theo sau khi người chơi bấm `OK`.
  - Khi popup đang mở, thao tác kéo dây phía sau bị khóa.
- Hoàn thiện trạng thái kết thúc Bước 3:
  - Sau khi hoàn thành Bước 3 và bấm `OK`, toàn bộ dây đã nối của cả ba bước xuất hiện lại trên mạch.
  - Các canvas trưng bày wire head, số dây, tiêu đề và bảng hướng dẫn được ẩn để không che model.
  - HMI được mở đồng thời với trạng thái hoàn thành.
- Sửa lỗi crop nội dung khi chạy WebGL trên server:
  - WebGL mặc định chuyển từ `960 × 600` sang `1280 × 720`.
  - WebGL template tự fit canvas vào vùng hiển thị.
  - Thêm `ResponsiveCameraFraming` để tự điều chỉnh FOV theo tỷ lệ màn hình.
  - Giữ nguyên vị trí camera và object, nhưng bảo toàn toàn bộ chiều ngang của bố cục rộng khoảng `2.25:1`.
  - Ở màn hình `16:9`, FOV dọc tự tăng từ `60°` lên khoảng `72.3°` để không crop hai bên.
- Bổ sung source gateway `fxplc` và mã nguồn thư viện MIT dùng để tham khảo/triển khai gateway PLC.
- Đã kiểm tra biên dịch các script Unity sau chỉnh sửa; không có lỗi C#.

## 🔧 ĐANG LÀM DỞ / CHƯA XONG

- Cần thực hiện lại full play test cuối cùng từ Bước 1 đến hết Bước 3 trên bản WebGL build mới nhất.
- Kết nối PLC thật và trạng thái `backendSynced` chưa phải trọng tâm của phiên UI/UX này; vẫn cần kiểm tra riêng trước khi triển khai chính thức.
- Gateway `fxplc` cần tiếp tục kiểm thử phần cứng, đặc biệt đọc/ghi PLC, reconnect và vận hành lâu dài.

## ➡️ CẦN LÀM TIẾP (việc cho người sau)

1. Build lại WebGL từ nhánh `Unify`, upload thay toàn bộ build cũ và xóa cache trình duyệt/server.
2. Test WebGL ở các chế độ:
   - Khung quiz có camera thật bên cạnh.
   - Fullscreen `16:9`.
   - Resize cửa sổ trình duyệt.
   - Xác nhận hai bảng hướng dẫn và model luôn hiển thị đầy đủ, không crop.
3. Play test đủ 15 dây:
   - Cắm sai một hoặc nhiều dây để kiểm tra popup liệt kê đúng số dây.
   - Sửa dây sai rồi xác nhận popup hoàn thành.
   - Kiểm tra chỉ chuyển bước sau khi bấm `OK`.
   - Hoàn thành Bước 3, bấm `OK`, xác nhận đủ 15 dây hiện lại và HMI bật.
4. Kiểm tra các nút HMI sau khi hoàn thành toàn bộ bài.
5. Tiếp tục kiểm thử gateway `fxplc` với PLC thật trước khi chọn làm gateway mặc định.

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT

- **Không tự ý chỉnh vị trí, rotation, scale hoặc RectTransform của object trong scene.**
- Không bật lại `arrangeWireHeadsOnStart`; nếu bật, wire head sẽ bị kéo về vị trí tự động.
- Bố cục scene rộng hơn `16:9`; không xóa `ResponsiveCameraFraming` nếu vẫn cần hiển thị đủ hai bảng hướng dẫn trên WebGL.
- `CircuitManager` tự gắn `ResponsiveCameraFraming` vào Main Camera khi Play:
  - `cameraDesignAspect = 2.25`
  - `cameraDesignVerticalFov = 60`
- Popup kết quả chỉ xuất hiện khi toàn bộ dây trong bước hiện tại đã được cắm vào socket.
- Sau khi hoàn thành Bước 3, chỉ giữ dây/heads đã kết nối và HMI; UI trình bày từng bước phải được ẩn.
- Không commit mật khẩu Pi, ngrok authtoken, HSL activation code hoặc secret triển khai.
- Không bật đồng thời gateway HSL và fxplc trên cùng cáp SC09.

---

# CHI TIẾT KỸ THUẬT

## 1. Luồng gameplay hoàn chỉnh

```text
Play
  │
  ▼
Bước 1: nối 6 dây
  │ cắm đủ nhưng có dây sai
  ├──────────────► Popup liệt kê dây sai → OK → cắm lại
  │
  │ nối đúng đủ 6 dây
  ▼
Popup hoàn thành Bước 1 → OK → Bước 2
  │
  ▼
Bước 2: nối 6 dây → Popup hoàn thành → OK → Bước 3
  │
  ▼
Bước 3: nối 3 dây → Popup hoàn thành → OK
  │
  ▼
Hiện lại đủ 15 dây đã nối + ẩn UI từng bước + mở HMI
```

## 2. Thành phần chính đã cập nhật

| Thành phần | Vai trò |
|---|---|
| `Assets/Scripts/CircuitManager.cs` | Quản lý ba bước, popup kết quả, trạng thái kết thúc, HMI và camera responsive |
| `Assets/Scripts/WirePlug.cs` | Kéo/snap wire head và khóa thao tác khi popup mở |
| `Assets/Scripts/WireBody.cs` | Kiểm tra đúng/sai và cập nhật thân dây liên tục |
| `Assets/Scripts/SocketPoint.cs` | Quản lý socket, màu dây và socket dùng chung |
| `Assets/Scripts/ResponsiveCameraFraming.cs` | Điều chỉnh FOV để không crop chiều ngang ở các tỷ lệ màn hình khác nhau |
| `Assets/WebGLTemplates/SCORMTemplate/index.html` | Resize canvas WebGL theo vùng hiển thị |
| `Assets/WebGLTemplates/SCORMTemplate/TemplateData/style.css` | Bố cục responsive của WebGL container/canvas |
| `ProjectSettings/ProjectSettings.asset` | Kích thước WebGL mặc định `1280 × 720` |
| `Assets/Scenes/Sy_scene.unity` | Scene hoàn chỉnh chứa UI/UX, dây, labels, hướng dẫn và HMI |

## 3. Popup kết quả

| Trạng thái | Nội dung |
|---|---|
| Chưa cắm đủ dây | Không hiện popup |
| Đã cắm đủ nhưng có dây sai | Hiện số dây sai và cặp socket đúng; giữ nguyên bước |
| Đúng toàn bộ Bước 1/2 | Thông báo hoàn thành; bấm `OK` mới chuyển bước |
| Đúng toàn bộ Bước 3 | Thông báo hoàn thành; bấm `OK` để hiện đủ dây và mở HMI |

## 4. Camera WebGL responsive

```text
Khung thiết kế tham chiếu: 2.25 : 1
Vertical FOV tham chiếu:   60°

Màn hình hẹp hơn 2.25 : 1
→ tăng Vertical FOV
→ giữ nguyên vùng nhìn ngang
→ xuất hiện thêm vùng nhìn trên/dưới thay vì crop trái/phải
```

| Tỷ lệ màn hình | FOV dọc xấp xỉ |
|---|---:|
| `2.25:1` | `60°` |
| `16:9` | `72.3°` |
| `16:10` | `78.2°` |
| `4:3` | `88.5°` |
