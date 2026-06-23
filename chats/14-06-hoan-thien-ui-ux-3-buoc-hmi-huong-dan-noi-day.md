# 📖 CÁCH DÙNG TÀI LIỆU NÀY (đọc 30 giây là dùng được)

> Đây là **file bàn giao 1 phiên làm việc với AI**. Quy ước: mỗi phiên = 1 file `chats/<ngày>-<chủ đề chính>.md`.
> File mới nhất (theo ngày) = trạng thái hiện tại của dự án. **Nhìn tên file là biết phiên đó làm gì.**

**🟢 NGƯỜI NHẬN VIỆC (đầu phiên):**
1. Lấy bản mới nhất:
   ```bash
   git checkout master && git pull
   ```
2. Mở Kiro trong thư mục dự án: `kiro-cli chat`
3. Câu **ĐẦU TIÊN** gõ cho AI:
   > *"Đọc file `chats/<file mới nhất>.md` rồi tiếp tục từ mục '➡️ CẦN LÀM TIẾP'."*

   → AI hiểu ngay đã/đang/cần làm gì, **không phải hỏi lại người cũ**.

**🔵 NGƯỜI BÀN GIAO (cuối phiên):**
1. Bảo AI: *"Viết tổng kết phiên này vào `chats/<ngày>-<chủ đề>.md` theo đúng 4 mục như file mẫu."*
2. Đẩy lên GitHub (branch master):
   ```bash
   git add chats/
   git commit -m "chat <ngày>: <tóm tắt ngắn>"
   git push origin master
   ```

**Quy ước nội dung:** giữ 4 mục — ✅ Đã làm / 🔧 Đang dở / ➡️ Cần làm tiếp / ⚠️ Lưu ý. Phần "CHI TIẾT KỸ THUẬT" cuối file = tra cứu sâu khi cần.
**(Tùy chọn)** muốn AI đọc nguyên văn hội thoại cũ: `/chat save chats/<ngày>-session.json`, phiên sau `/chat load <file đó>`.

---

# Phiên 2026-06-14 — Hoàn thiện UI/UX ba bước nối dây, hướng dẫn và HMI — Tuấn

> **Dự án:** Digital Twin Unity mô phỏng bài thực hành đấu nối PLC, mạch phản hồi và mạch lực.
> **Mục tiêu phiên:** hoàn thiện trải nghiệm người dùng cho toàn bộ luồng 3 bước nối dây; bổ sung hướng dẫn trực quan theo từng bước; bố trí HMI trên model và chỉ mở sau khi hoàn thành đủ 15 dây.
> **Scene chính:** `Assets/Scenes/Sy_scene.unity`.

---

## ✅ ĐÃ LÀM XONG

- Hoàn thiện luồng chơi gồm **15 dây và 30 wire head**, chia thành ba bước:
  - **Bước 1 — Mạch điều khiển:** 6 dây.
  - **Bước 2 — Mạch phản hồi:** 6 dây.
  - **Bước 3 — Mạch lực:** 3 dây.
- Giữ luồng chuyển bước tuần tự:
  - Chỉ hiển thị dây, wire head và hướng dẫn của bước hiện tại.
  - Nối đúng toàn bộ dây của bước hiện tại thì bước đó tự ẩn.
  - Tự chuyển sang bước tiếp theo.
  - Hoàn thành đủ 15 dây mới mở HMI.
- Console thông báo kết quả khi nối dây:
  - Nối đúng: báo dấu tick và cặp socket đã nối.
  - Nối sai: báo dấu X, socket đang nối và đáp án đúng.
- Hoàn thiện hệ thống hướng dẫn nối dây cho cả ba bước:
  - Có dòng hướng dẫn cho từng dây, hiển thị cặp socket cần nối.
  - Có label đặt cạnh các socket tương ứng.
  - Màu chữ của hướng dẫn và label khớp với màu dây.
  - Dùng `TextMeshProUGUI`, font `LiberationSans SDF`, kiểu chữ `Bold` để dễ đọc trên model.
- Tổ chức hướng dẫn thành hierarchy riêng để tiện chỉnh thủ công:
  ```text
  WiringGuides_Storage
  ├── Buoc_1
  │   ├── InstructionPanel
  │   └── SocketLabels
  ├── Buoc_2
  └── Buoc_3
  ```
- `CircuitManager` đã được cập nhật để bật/tắt nhóm hướng dẫn đồng bộ với từng bước nối dây.
- Đã tắt các text hướng dẫn mẫu cũ để tránh hiển thị trùng.
- Hoàn thiện màn HMI:
  - Loại bỏ `Dashboard` World Space cũ quá lớn bên dưới model.
  - HMI hiện là object riêng trong Hierarchy:
    ```text
    HMI_Runtime_Canvas
    └── HMI_Screen
    ```
  - Chuyển HMI thành `World Space`, đặt trực tiếp tại vùng trống phía trên bên phải của bảng model.
  - Có thể chọn và di chuyển/resize trực tiếp `HMI_Screen` bằng Rect Tool.
  - HMI vẫn bị khóa khi bắt đầu và chỉ xuất hiện sau khi hoàn thành đủ 15 dây.
- Tắt hai `PLCController` legacy đang tranh cổng `COM6`; giữ `PLCController_v2`.
- Sửa lỗi wire head tự bay về giữa model khi bấm Play:
  - Nguyên nhân là `CircuitManager.arrangeWireHeadsOnStart` tự gọi `ArrangeAllSteps()`.
  - Đã đặt `arrangeWireHeadsOnStart = false` trong code và Inspector.
  - Đã kiểm tra đủ 30 wire head giữ nguyên vị trí trước và trong Play Mode.
- Đã kiểm tra Pi/ngrok qua SSH:
  - `ngrok`, `caddy-proxy` và camera vẫn chạy.
  - Gateway đang dùng là `pi-gateway-fxplc`.
  - Vấn đề PLC được tạm thời bỏ qua trong phiên UI/UX này.

---

## 🔧 ĐANG LÀM DỞ / CHƯA XONG

- Chưa thực hiện full play test thủ công toàn bộ 15 dây sau lần căn chỉnh vị trí cuối cùng của người dùng.
- Chưa Build WebGL/SCORM để kiểm tra bố cục UI/UX ở độ phân giải mục tiêu.
- Kết nối Pi → PLC hiện chưa đồng bộ dữ liệu thật (`backendSynced:false`); phần này tạm thời không thuộc phạm vi hoàn thiện UI/UX.
- Các thay đổi hiện tại **chưa commit/push**.

---

## ➡️ CẦN LÀM TIẾP (việc cho người sau)

1. Mở `Assets/Scenes/Sy_scene.unity`, bấm Play và kiểm tra toàn bộ luồng:
   - Bước 1 hiện đúng 6 dây và hướng dẫn Bước 1.
   - Hoàn thành Bước 1 thì dây/head/hướng dẫn Bước 1 ẩn, Bước 2 hiện.
   - Hoàn thành Bước 2 thì chuyển sang Bước 3.
   - Hoàn thành đủ 15 dây thì hướng dẫn cuối ẩn và HMI xuất hiện.
2. Kiểm tra thao tác kéo/snap của từng wire head sau khi tắt tự động sắp xếp.
3. Kiểm tra HMI tại vị trí đã căn chỉnh:
   - Không che model hoặc khu vực thao tác quan trọng.
   - Các nút HMI có thể bấm được trên World Space Canvas.
   - Kích thước chữ và nút dễ đọc ở độ phân giải build thực tế.
4. Build WebGL/SCORM và test lại toàn bộ UI/UX trên bản build.
5. Khi đã xác nhận ổn định, commit riêng các thay đổi Unity/UI và file bàn giao này.

---

## ⚠️ LƯU Ý / CẠM BẪY / THÔNG TIN CẦN BIẾT

- **Không tự ý chỉnh vị trí, rotation, scale hoặc RectTransform của bất kỳ object nào.**
  - Người dùng đã căn chỉnh đẹp vị trí dây, wire head, labels, hướng dẫn và HMI.
  - Chỉ thay đổi transform khi có yêu cầu trực tiếp.
- Không bật lại `arrangeWireHeadsOnStart`; nếu bật, toàn bộ wire head sẽ bị kéo về `layoutCenter` khi Play.
- Không gọi lại hoặc dùng `ArrangeAllSteps()` để bố trí wire head nếu không được yêu cầu.
- HMI là World Space Canvas trên model:
  - Di chuyển toàn bộ HMI bằng `HMI_Runtime_Canvas`.
  - Chỉnh phần màn/panel bằng `HMI_Screen`.
  - Logic hiện/ẩn vẫn do `CircuitManager` và `PLCController_v2.SetRuntimeHmiVisible()` quản lý.
- `WiringGuides_Storage/Buoc_1`, `Buoc_2`, `Buoc_3` là ba nhóm hướng dẫn độc lập; `CircuitManager.guideRoots` điều khiển nhóm đang hiển thị.
- Socket dùng chung như `5VDC` và `GND_5V` cho phép nhiều kết nối; không đổi lại logic này.
- Tên dây Bước 3 đang sử dụng:
  - `Wire_13_oA-Motor_S`
  - `Wire_14_oB-Motor_B`
  - `Wire_15_oC-Motor_A`
- Không bật đồng thời `pi-gateway-hsl` và `pi-gateway-fxplc`; hai gateway dùng chung cổng `5000` và cùng cáp SC09.
- Không commit secret, mật khẩu Pi, activation code HSL hoặc ngrok authtoken.

---

# CHI TIẾT KỸ THUẬT (tra cứu sâu khi cần)

## 1. Luồng UI/UX ba bước

```text
Play
  │
  ▼
Bước 1: 6 dây + hướng dẫn Bước 1
  │ nối đúng đủ 6 dây
  ▼
Ẩn Bước 1 → hiện Bước 2: 6 dây + hướng dẫn Bước 2
  │ nối đúng đủ 6 dây
  ▼
Ẩn Bước 2 → hiện Bước 3: 3 dây + hướng dẫn Bước 3
  │ nối đúng đủ 3 dây
  ▼
Ẩn Bước 3 → hoàn thành 15/15 → mở HMI
```

## 2. Mapping 15 dây

| Bước | Dây | Cặp socket | Màu |
|---|---|---|---|
| 1 | `Wire_01_5VDC-V0` | `5VDC` ↔ `+V0` | Đỏ |
| 1 | `Wire_02_5VDC-V1` | `5VDC` ↔ `+V1` | Đỏ |
| 1 | `Wire_03_Y0-Pin11` | `Y0` ↔ `Pin11` | Vàng |
| 1 | `Wire_04_Y1-Pin9` | `Y1` ↔ `Pin9` | Vàng |
| 1 | `Wire_05_Pin10-GND_5V` | `GND_5V` ↔ `Pin10` | Đen |
| 1 | `Wire_06_Pin12-GND_5V` | `GND_5V` ↔ `Pin12` | Đen |
| 2 | `Wire_07_24VDC-SS` | `24VDC` ↔ `SS` | Đỏ |
| 2 | `Wire_08_Enc_A-X4` | `Enc_A` ↔ `X4` | Đỏ |
| 2 | `Wire_09_Enc_B-X3` | `Enc_B` ↔ `X3` | Đỏ |
| 2 | `Wire_10_Pin13-X0` | `Pin13` ↔ `X0` | Vàng |
| 2 | `Wire_11_Pin15-X1` | `Pin15` ↔ `X1` | Vàng |
| 2 | `Wire_12_Pin14-GND_5V` | `Pin14` ↔ `GND_5V` | Đen |
| 3 | `Wire_13_oA-Motor_S` | `oA` ↔ `Motor_S` | Đỏ |
| 3 | `Wire_14_oB-Motor_B` | `oB` ↔ `Motor_B` | Vàng |
| 3 | `Wire_15_oC-Motor_A` | `oC` ↔ `Motor_A` | Đen |

## 3. Hierarchy quan trọng

```text
Sockets
├── Buoc1_MachDieuKhien
├── Buoc2_MachPhanHoi
└── Buoc3_MachLuc

WireHeads_Storage
├── Buoc1_MachDieuKhien
├── Buoc_2
└── Buoc_3

WiringGuides_Storage
├── Buoc_1
├── Buoc_2
└── Buoc_3

HMI_Runtime_Canvas
└── HMI_Screen
```

## 4. Script và vai trò

| Thành phần | Vai trò |
|---|---|
| `Assets/Scripts/CircuitManager.cs` | Khởi tạo bài chơi, theo dõi 3 bước, bật/tắt wires/guides và mở HMI sau 15 dây |
| `Assets/Scripts/SocketPoint.cs` | Quản lý socket ID, màu chấp nhận, occupied và socket dùng chung |
| `Assets/Scripts/WirePlug.cs` | Kéo, tìm socket gần nhất, snap/unsnap và thông báo kết nối |
| `Assets/Scripts/WireBody.cs` | Vẽ thân dây, giữ hai wire head, kiểm tra đúng/sai và log kết quả |
| `Assets/PLCController_v2.cs` | Tạo/nạp nội dung HMI, điều khiển hiển thị HMI và giao tiếp Pi gateway |
| `Assets/Scenes/Sy_scene.unity` | Lưu hierarchy, Inspector, vị trí đã căn chỉnh và cấu hình gameplay |

## 5. Trạng thái HMI và Pi

- HMI:
  - Loại: `World Space Canvas`.
  - Object chỉnh trực tiếp: `HMI_Runtime_Canvas/HMI_Screen`.
  - Mặc định bị khóa khi bắt đầu.
  - Chỉ mở sau `completedWires == totalWires == 15`.
- Pi/gateway tại thời điểm kiểm tra:
  - SSH: `admin@10.38.100.27` qua ZeroTier.
  - `ngrok`: active.
  - `caddy-proxy`: active.
  - `ustreamer-cam`: active.
  - `pi-gateway-fxplc`: active.
  - `pi-gateway-hsl`: stopped/failed do đã chuyển sang fxplc.
  - Public ngrok endpoint từng trả `HTTP 200`, nhưng PLC backend chưa đồng bộ.

