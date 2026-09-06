# Tổng hợp công việc thay đổi giao diện từ Trang 1

> Ngày tổng hợp: 2026-08-13  
> Phạm vi: giao diện giới thiệu từ Trang 1 đến Trang 4 trong `StartScene`, giao diện thực hành trong `Sy_scene`, ba bước nối dây và Bước 4 HMI.  
> Không bao gồm: cấu hình Raspberry Pi, gateway PLC, ngrok và các phần backend không trực tiếp làm thay đổi giao diện.

## 1. Mục tiêu chung

Toàn bộ phần giao diện được chỉnh lại theo một luồng thống nhất:

```text
Trang 1: Giới thiệu và mục tiêu bài thực hành
  ↓
Trang 2: Các thành phần chính của mô hình
  ↓
Trang 3: Nguyên lý hoạt động
  ↓
Trang 4: Hướng dẫn thao tác nối dây
  ↓
Sy_scene: Thực hành 3 bước nối dây
  ↓
Bước 4: Vận hành bằng HMI
```

Các nguyên tắc được giữ trong suốt quá trình:

- Dùng chung màu đỏ nhận diện, nền sáng, card trắng, chữ đậm và khoảng cách rõ ràng.
- Giao diện phải đọc được trong Unity Editor và WebGL.
- Không tự ý thay đổi vị trí model, dây, ổ cắm hoặc HMI ngoài phần được yêu cầu.
- Những thành phần có thể dựng lại được tách thành script Editor; logic tương tác nằm trong script Runtime.
- Sau mỗi nhóm thay đổi đều kiểm tra biên dịch C# và quan sát trong Game View hoặc Play Mode.

## 2. Khảo sát và chuẩn bị trước khi sửa

### Công việc đã làm

1. Mở và kiểm tra `Assets/Scenes/StartScene.unity`.
2. Xác định bốn page gốc có tên:
   - `Trang 1`
   - `Trang 2`
   - `Trang 3`
   - `Trang 4`
3. Kiểm tra `StartScreenController` để xác định luồng chuyển trang và scene thực hành.
4. Kiểm tra model, camera, Canvas, TextMeshPro và các asset có thể tái sử dụng.
5. Tạo hoặc dùng lại sprite bo góc `Assets/Resources/UI/RoundedRect.png`.
6. Tách công việc dựng giao diện thành bốn script Editor:
   - `Assets/Editor/PageOneSceneSetup.cs`
   - `Assets/Editor/PageTwoSceneSetup.cs`
   - `Assets/Editor/PageThreeSceneSetup.cs`
   - `Assets/Editor/PageFourSceneSetup.cs`

### Cách thực hiện chung cho mỗi trang

1. Tìm đúng page trong `StartScene`.
2. Xóa phần content cũ của riêng page đó.
3. Tạo lại hierarchy UI bằng `RectTransform`, `Image`, `TextMeshProUGUI`, `Button`, `Shadow` và `Outline`.
4. Gán anchor, pivot, kích thước và vị trí theo bố cục thiết kế.
5. Gán controller Runtime nếu trang có tương tác.
6. Đánh dấu scene đã thay đổi và lưu lại `StartScene`.
7. Kiểm tra trong Game View ở độ phân giải mục tiêu.

## 3. Trang 1 — Giới thiệu và mục tiêu bài thực hành

### Yêu cầu giao diện

- Trang đầu phải thể hiện rõ đây là Bài thực hành 1.
- Tiêu đề bài thực hành phải nổi bật.
- Nội dung mục tiêu phải dễ đọc, không còn cảm giác là giao diện mẫu rời rạc.

### Các bước thực hiện

1. Tìm `Trang 1` trong `StartScene`.
2. Xóa `PageOneContent` cũ nếu đã tồn tại.
3. Đổi nền chung sang màu xám rất nhạt.
4. Tạo badge đỏ `Bài thực hành 1` kèm bóng đổ.
5. Tạo tiêu đề lớn:
   - `ĐẤU NỐI HỆ THỐNG ĐIỀU KHIỂN ĐỘNG CƠ SERVO`.
   - Dùng màu đỏ, chữ đậm, không xuống dòng.
   - Bổ sung `Outline` nhẹ để tiêu đề chắc và rõ hơn.
6. Tạo card trắng cho phần `Mục tiêu bài thực hành`.
7. Tạo biểu tượng check bằng các shape UI thay vì ký tự font để hiển thị ổn định.
8. Thêm đường phân cách và bốn mục tiêu:
   - Nhận biết thành phần của hệ thống servo vòng kín.
   - Hiểu vai trò PLC, HMI, servo driver, động cơ BLDC và encoder.
   - Thực hiện đấu nối mạch điều khiển, phản hồi và động lực.
   - Kiểm tra hoạt động sau khi hoàn thành đấu nối.
9. Lưu scene và chụp ảnh kiểm tra bố cục.

### Tệp liên quan

- `Assets/Editor/PageOneSceneSetup.cs`
- `Assets/Scenes/StartScene.unity`
- `Assets/Resources/UI/RoundedRect.png`
- `Logs/PageOnePreview.png`
- `Logs/PageOnePreviewV2.png`

## 4. Điều hướng chung giữa bốn trang

### Các bước thực hiện

1. Cập nhật `StartScreenController` để quản lý mảng bốn page.
2. Khi mở scene, mặc định hiển thị Trang 1.
3. Tạo nút mũi tên lùi và tiến ở hai góc dưới.
4. Ẩn nút lùi khi đang ở Trang 1.
5. Khi bấm tiến ở Trang 4:
   - Đặt cờ tiếp tục thực hành.
   - Load `Sy_scene`.
6. Khi quay lại trang hướng dẫn từ bài thực hành:
   - Mở trực tiếp Trang 4.
   - Giữ thông tin tiến độ nối dây để có thể tiếp tục.
7. Điều chỉnh nền và camera theo loại trang:
   - Trang 1 và Trang 3 dùng giao diện 2D.
   - Trang 2 và Trang 4 có vùng hiển thị model 3D.

### Tệp liên quan

- `Assets/Scripts/StartScreenController.cs`
- `Assets/Scenes/StartScene.unity`

## 5. Trang 2 — Các thành phần chính của mô hình

### Yêu cầu giao diện

- Có danh sách các bộ phận ở bên trái.
- Có vùng xem model thật ở bên phải.
- Khi chọn bộ phận phải phóng tới đúng vị trí, làm nổi bật và hiện mô tả.

### Danh sách thành phần

1. PLC Mitsubishi FX3U.
2. HMI Mitsubishi GOT1000.
3. Động cơ BLDC Servo.
4. Encoder.
5. Aptomat.
6. Dây cắm.
7. Bảng cắm dây.

### Các bước thực hiện phần giao diện

1. Tạo header đỏ dùng chung với các trang nội dung.
2. Thêm nhãn `Bài thực hành 1` bên trái header.
3. Thêm tên bài thực hành bên phải header.
4. Tạo tiêu đề `CÁC THÀNH PHẦN CHÍNH CỦA MÔ HÌNH`.
5. Tạo danh sách bảy nút chọn bộ phận.
6. Tạo vùng tiêu đề và mô tả chi tiết, mặc định được ẩn.
7. Tạo nút `‹ Danh sách` để trở lại danh sách thành phần.
8. Tạo icon và divider riêng cho chế độ xem chi tiết.

### Các bước thực hiện phần model tương tác

1. Tìm model `3d_Thay_Tien_1`.
2. Tạo `PageTwoPreviewCamera` để chỉ render vùng model của Trang 2.
3. Tạo đèn riêng cho preview để model sáng và rõ.
4. Ánh xạ mỗi nút với tên transform tương ứng trong model.
5. Khi chọn một bộ phận:
   - Tính bounds của bộ phận.
   - Chuyển sang chế độ chi tiết.
   - Di chuyển camera tới bộ phận bằng animation.
   - Hiện tên và mô tả chức năng.
   - Tạo viền highlight màu xanh quanh mesh được chọn.
6. Cho phép giữ chuột và kéo để xoay góc nhìn model.
7. Khi bấm `Danh sách`, trả camera và giao diện về trạng thái tổng quan.
8. Tạo bộ dây minh họa bằng các asset dây có sẵn để mục `Dây cắm` có đối tượng thực tế.
9. Xử lý lại camera khi độ phân giải màn hình thay đổi.

### Tệp liên quan

- `Assets/Editor/PageTwoSceneSetup.cs`
- `Assets/Scripts/PageTwoPartsController.cs`
- `Assets/Shaders/PageTwoSelectionOutline.shader`
- `Assets/Scenes/StartScene.unity`

## 6. Trang 3 — Nguyên lý hoạt động

### Yêu cầu giao diện

- Trình bày nguyên lý vòng kín bằng nội dung ngắn gọn.
- Có thứ tự xử lý rõ ràng.
- Có hình sơ đồ minh họa và ghi chú kỹ thuật.

### Các bước thực hiện

1. Xóa toàn bộ content cũ trong `Trang 3`.
2. Tạo header đỏ đồng bộ với Trang 2 và Trang 4.
3. Tạo tiêu đề `NGUYÊN LÝ HOẠT ĐỘNG`.
4. Tạo card `ProcessCard` cho quy trình hoạt động.
5. Tạo bốn bước có ô số riêng:
   1. Người vận hành nhập lệnh trên HMI.
   2. PLC xử lý lệnh và phát xung tốc độ cao tới Servo Driver.
   3. Driver khuếch đại và biến đổi tín hiệu thành điện áp ba pha cho động cơ.
   4. Encoder phản hồi về PLC để xác định chiều quay, tốc độ và vị trí.
6. Tạo card `NotesCard` cho các ghi chú về tần số xung, tổng số xung, encoder A/B và quá trình tăng/giảm tốc.
7. Chèn sơ đồ vòng kín từ `Assets/IntroImages/intro_page_1.png`.
8. Căn lại kích thước chữ, khoảng cách dòng và chiều cao card để không bị tràn.
9. Lưu scene và kiểm tra bằng ảnh Game View.

### Tệp liên quan

- `Assets/Editor/PageThreeSceneSetup.cs`
- `Assets/IntroImages/intro_page_1.png`
- `Assets/Scenes/StartScene.unity`
- `Logs/PageThreePreview.png`
- `Logs/PageThreeGameView.png`

## 7. Trang 4 — Hướng dẫn thao tác thực hành

### Yêu cầu giao diện

- Người học phải nhìn được thao tác kéo một đầu dây tới ổ cắm.
- Dùng model và asset thật của dự án thay cho hình minh họa trừu tượng.
- Có thể phát lại animation hướng dẫn.

### Các bước thực hiện phần giao diện

1. Tạo header đỏ đồng bộ với Trang 2 và Trang 3.
2. Tạo tiêu đề `HƯỚNG DẪN THỰC HÀNH`.
3. Tạo card ghi chú:
   - `Dùng chuột kéo các đầu dây tới vị trí các lỗ cắm và thả chuột để cắm.`
4. Tạo nút `Play`.
5. Tạo con trỏ bàn tay từ `Assets/HandIcons.png`.
6. Đặt model thực tế vào `PageFourModel`.

### Các bước thực hiện animation hướng dẫn

1. Tạo camera preview riêng cho model Trang 4.
2. Xác định hai vị trí socket mẫu trên model.
3. Tạo dây demo bằng `Jack 3.5mm.fbx` và material dây có sẵn.
4. Tạo camera overlay để dây demo luôn nhìn rõ.
5. Khi bấm `Play`:
   - Đưa con trỏ tới đầu dây.
   - Chuyển icon sang trạng thái đang giữ.
   - Kéo đầu dây tới socket.
   - Thả đầu dây vào socket.
   - Tiếp tục minh họa đầu còn lại.
   - Chờ rồi reset để có thể phát lại.
6. Cập nhật vị trí con trỏ theo tọa độ screen của camera preview.
7. Xử lý lại camera và overlay khi thay đổi độ phân giải.

### Công việc hướng dẫn trước đó

Trước khi chuyển sang animation model trực tiếp, ba ảnh hướng dẫn đã được tạo từ chính `Sy_scene`:

- `Assets/GuideImages/huongdan_bai1_step1.png`
- `Assets/GuideImages/huongdan_bai1_step2.png`
- `Assets/GuideImages/huongdan_bai1_step3.png`

Ảnh bước 2 đã được sửa để bỏ nét/mũi tên khó hiểu và chỉ giữ vòng tròn đỏ khoanh socket. Các ảnh này vẫn được giữ làm tư liệu.

### Tệp liên quan

- `Assets/Editor/PageFourSceneSetup.cs`
- `Assets/Scripts/PageFourWiringTutorialController.cs`
- `Assets/HandIcons.png`
- `Assets/3d_Thay_Tien_1.fbx`
- `Assets/Jack 3.5mm.fbx`
- `Assets/Scenes/StartScene.unity`
- `Logs/PageFourGameView.png`
- `Logs/PageFourGameViewFinal.png`

## 8. Chuyển từ Trang 4 sang bài thực hành

### Các bước thực hiện

1. Người dùng bấm mũi tên tiếp theo ở Trang 4.
2. `StartScreenController` đặt `ContinuePracticeFromGuide = true`.
3. Load `Assets/Scenes/Sy_scene.unity`.
4. `CircuitManager` khởi tạo ba nhóm dây và ba nhóm hướng dẫn.
5. HMI bị khóa ở trạng thái đầu.
6. Chỉ Bước 1 được hiển thị và cho phép tương tác.

## 9. Giao diện thực hành ba bước nối dây

### Phân chia công việc

| Bước | Nội dung | Số dây |
|---|---|---:|
| 1 | Mạch điều khiển | 6 |
| 2 | Encoder/phản hồi | 6 |
| 3 | Mạch lực | 3 |

### Mapping 15 dây

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

### Các bước hoàn thiện giao diện thực hành

1. Chia dây thành ba prefab/root độc lập trong `WireHeads_Storage`.
2. Tạo ba nhóm hướng dẫn độc lập trong `WiringGuides_Storage`.
3. Mỗi bước chỉ hiện:
   - Dây của bước hiện tại.
   - Số dây tương ứng.
   - Bảng hướng dẫn của bước hiện tại.
   - Nhãn cạnh những socket cần dùng.
4. Dùng màu chữ theo màu dây.
5. Dùng font TextMeshPro đậm để chữ rõ trên nền model.
6. Tạo nền trắng bo góc phía sau nhãn socket.
7. Tắt các text hướng dẫn cũ bị trùng.
8. Không tự sắp xếp lại wire head khi Play nếu người dùng đã căn tay.
9. Cập nhật thân dây bằng `LineRenderer` liên tục để dây luôn hiện đúng giữa hai đầu.
10. Cho dây render ở lớp overlay để không bị model che.

### Hierarchy chính

```text
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

## 10. Thanh bước, heading và bố cục thực hành dạng card

### Các bước thực hiện

1. Tạo thanh điều hướng bốn bước ở phía trên:
   - `1. Đấu nối mạch điều khiển động cơ`
   - `2. Đấu nối encoder`
   - `Đấu nối mạch lực`
   - `4. Vận hành`
2. Bước hiện tại dùng màu đỏ; bước chưa mở bị khóa.
3. Cho phép xem lại bước đã hoàn thành.
4. Tạo heading trên bảng nối dây cho Bước 1–3 bằng `BoardStepHeading`.
5. Heading được đặt theo phối cảnh World Space, căn với `Board` và `Board_Frame`.
6. Heading tự ẩn ở Bước 4.
7. Tạo bố cục thực hành dạng card:
   - Card hướng dẫn bên trái.
   - Model và bảng socket ở giữa.
   - Card bộ dây bên phải.
8. Thêm nút `← Hướng dẫn` để quay lại Trang 4 mà không làm mất tiến độ.

## 11. Popup kiểm tra đúng/sai và chuyển bước

### Các bước thực hiện

1. Theo dõi trạng thái cắm của toàn bộ dây trong bước hiện tại.
2. Chưa cắm đủ dây thì không hiện popup.
3. Khi đã cắm đủ nhưng có dây sai:
   - Hiện popup cảnh báo.
   - Liệt kê đúng số dây sai.
   - Giữ nguyên bước để người dùng sửa.
4. Khi toàn bộ dây đúng:
   - Hiện popup hoàn thành bước.
   - Chỉ chuyển bước sau khi bấm nút tiếp tục.
5. Khi popup đang mở, khóa thao tác kéo dây phía sau.
6. Sau khi hoàn thành Bước 3:
   - Hiện lại toàn bộ 15 dây đã nối.
   - Ẩn UI trưng bày riêng của từng bước.
   - Mở Bước 4 HMI.

## 12. HMI và trạng thái vận hành

### Các bước thực hiện

1. Loại bỏ dashboard World Space cũ có kích thước quá lớn.
2. Dùng hierarchy `HMI_Runtime_Canvas/HMI_Screen`.
3. Đặt HMI tại vùng trống trên bảng model.
4. Giữ HMI bị ẩn khi mới bắt đầu.
5. Chỉ mở HMI sau khi hoàn thành đủ ba bước nối dây.
6. Bổ sung nhận diện PTIT:
   - Logo PTIT.
   - Tên Học viện Công nghệ Bưu chính Viễn thông.
   - Dòng `Giao diện điều khiển`.
7. Đổi khung motor ảo sang màu trắng.
8. Căn lại HMI và khung motor để không lệch khỏi màn hình.
9. Giữ các nút điều khiển thuận, ngược, START, STOP và RESET.

## 13. Responsive cho Unity và WebGL

### Các bước thực hiện

1. Đổi kích thước WebGL mặc định sang `1280 × 720`.
2. Chỉnh template WebGL để canvas tự fit theo vùng hiển thị.
3. Thêm `ResponsiveCameraFraming`.
4. Dùng tỷ lệ thiết kế tham chiếu khoảng `2.25:1` và FOV dọc `60°`.
5. Khi màn hình hẹp hơn, tăng FOV dọc để giữ đủ nội dung hai bên.
6. Giữ nguyên vị trí camera và object; chỉ thay đổi framing.
7. Kiểm tra ở Game View 16:9 và khi resize cửa sổ.

### Tệp liên quan

- `Assets/Scripts/ResponsiveCameraFraming.cs`
- `Assets/WebGLTemplates/SCORMTemplate/index.html`
- `Assets/WebGLTemplates/SCORMTemplate/TemplateData/style.css`
- `ProjectSettings/ProjectSettings.asset`

## 14. Các chỉnh sửa giao diện nối dây mới nhất

### 14.1. Đổi dây 3 và dây 4 sang màu vàng

1. Kiểm tra dữ liệu hai dây trong prefab và scene.
2. Xác nhận `Wire_03_Y0-Pin11` và `Wire_04_Y1-Pin9` đã dùng `WireColor.Yellow`.
3. Phát hiện phần chữ hướng dẫn đang ép dây 1–4 cùng màu đỏ.
4. Chỉnh `GetPracticalGuideTextColor()` để chỉ ép dây 1–2 thành đỏ.
5. Dây 3–4 từ đó dùng đúng màu vàng theo dữ liệu dây.

### 14.2. Chuyển nhãn pin bị dây che ở cả ba bước

1. Thu thập toàn bộ `SocketPoint` theo `socketID`.
2. Với từng dây, tìm label của hai đầu theo quy ước tên:
   - `Label_<tên dây>_A_<socket>`
   - `Label_<tên dây>_B_<socket>`
3. So sánh hướng từ socket tới label với hướng từ socket tới đầu dây còn lại.
4. Nếu label nằm cùng hướng dây đi vào, phản chiếu label sang phía đối diện của socket.
5. Giữ nguyên nhãn vốn không bị che.
6. Các nhãn được nhận diện để đổi bên gồm những nhóm như:
   - Bước 1: `Pin9`, `Pin10`, `Pin11`, `Pin12`.
   - Bước 2: `Enc_A`, `Enc_B`, `Pin13`, `Pin14`, `Pin15` khi nằm trên hướng dây.
   - Bước 3: các nhãn `oA`, `oB`, `oC`, `Motor_S`, `Motor_B`, `Motor_A` khi nằm trên hướng dây.
7. Thao tác được thực hiện lúc khởi tạo trước khi tạo nền trắng cho label, nên nền luôn đi theo vị trí mới.

### 14.3. Làm nổi bật ổ cắm hợp lệ

1. Từ danh sách dây của bước hiện tại, lấy toàn bộ `correctSocketA` và `correctSocketB`.
2. Chỉ bật focus cho các socket có ID nằm trong tập hợp hợp lệ.
3. Phóng socket hợp lệ lên `1.2` lần.
4. Tạo vòng tròn vàng bằng `LineRenderer` gồm 32 đoạn.
5. Dùng màu vàng sáng, alpha đầy đủ và sorting order cao để dễ nhìn.
6. Bán kính ban đầu được tăng lên `0.48` nhưng bị nhận xét hơi lớn.
7. Thu lại bán kính cuối cùng còn `0.40` để vòng ôm sát ổ hơn.
8. Thông số cuối hiện tại:

```text
GuideFocusScale          = 1.2
GuideFocusRingRadius     = 0.4
GuideFocusRingWorldWidth = 0.0022
GuideFocusRingColor      = (1.0, 0.82, 0.0, 1.0)
```

### 14.4. Kiểm tra chữ `GL` xuất hiện thoáng qua

1. Tìm trong script và scene các text có nội dung `GL`.
2. Không tìm thấy nội dung giao diện nào được tạo cố định với chữ `GL`.
3. Hiện tượng không tái hiện khi thao tác lại.
4. Không sửa code vì chưa có bằng chứng về nguồn phát sinh và lỗi không còn xuất hiện.
5. Nếu tái hiện, cần chụp màn hình đúng thời điểm để xác định object UI.

## 15. Quy trình kiểm tra đã dùng

### Kiểm tra giao diện StartScene

1. Mở `Assets/Scenes/StartScene.unity`.
2. Chạy từ Trang 1.
3. Bấm tiến/lùi qua đủ bốn trang.
4. Kiểm tra header, tiêu đề, card, font và khoảng cách.
5. Ở Trang 2:
   - Chọn đủ bảy thành phần.
   - Kiểm tra zoom, highlight, mô tả và nút quay lại danh sách.
   - Kéo chuột để kiểm tra xoay model.
6. Ở Trang 4:
   - Bấm `Play`.
   - Quan sát con trỏ, hai đầu dây và animation snap.
7. Bấm tiếp ở Trang 4 để xác nhận load `Sy_scene`.

### Kiểm tra giao diện nối dây

1. Xác nhận Bước 1 có sáu dây và đúng hướng dẫn.
2. Kiểm tra dây 3–4 và chữ hướng dẫn đều màu vàng.
3. Kiểm tra label không bị thân dây che.
4. Kiểm tra chỉ các socket hợp lệ có vòng vàng.
5. Kiểm tra vòng vàng ôm sát ổ và không che label.
6. Cắm sai để kiểm tra popup liệt kê dây sai.
7. Cắm đúng để kiểm tra popup hoàn thành.
8. Chuyển lần lượt qua Bước 2 và Bước 3.
9. Sau Bước 3, xác nhận đủ 15 dây hiện lại và HMI được mở.
10. Kiểm tra nút quay lại hướng dẫn và tiếp tục tiến độ.

### Kiểm tra kỹ thuật

1. Theo dõi Unity Console sau khi script được import lại.
2. Xác nhận không có lỗi biên dịch C# do các thay đổi giao diện mới nhất.
3. Dùng lệnh kiểm tra trong Unity Editor để xác nhận:
   - Dây 3 và dây 4 đều là `Yellow`.
   - Thuật toán nhận đúng các label nằm trên hướng dây.
4. Không build WebGL tự động nếu chưa có yêu cầu riêng.

## 16. Danh sách tệp chính đã tham gia

### Giao diện bốn trang

- `Assets/Scenes/StartScene.unity`
- `Assets/Editor/PageOneSceneSetup.cs`
- `Assets/Editor/PageTwoSceneSetup.cs`
- `Assets/Editor/PageThreeSceneSetup.cs`
- `Assets/Editor/PageFourSceneSetup.cs`
- `Assets/Scripts/StartScreenController.cs`
- `Assets/Scripts/PageTwoPartsController.cs`
- `Assets/Scripts/PageFourWiringTutorialController.cs`
- `Assets/Resources/UI/RoundedRect.png`
- `Assets/IntroImages/intro_page_1.png`
- `Assets/HandIcons.png`

### Giao diện và gameplay nối dây

- `Assets/Scenes/Sy_scene.unity`
- `Assets/Scripts/CircuitManager.cs`
- `Assets/Scripts/BoardStepHeading.cs`
- `Assets/Scripts/ResponsiveCameraFraming.cs`
- `Assets/Scripts/SocketPoint.cs`
- `Assets/Scripts/WireBody.cs`
- `Assets/Scripts/WirePlug.cs`
- `Assets/Shaders/WireOverlay.shader`
- `Assets/Resources/WireOverlayMaterial.mat`
- `Assets/Prefabs/Steps/Buoc1_MachDieuKhien.prefab`
- `Assets/Prefabs/Steps/Buoc_2.prefab`
- `Assets/Prefabs/Steps/Buoc_3.prefab`

### WebGL

- `Assets/WebGLTemplates/SCORMTemplate/index.html`
- `Assets/WebGLTemplates/SCORMTemplate/TemplateData/style.css`
- `ProjectSettings/ProjectSettings.asset`

## 17. Trạng thái cuối hiện tại

- Bốn trang giới thiệu đã có bố cục đồng bộ.
- Trang 2 có model 3D tương tác và mô tả từng thành phần.
- Trang 3 trình bày nguyên lý vòng kín theo bốn bước.
- Trang 4 có animation hướng dẫn kéo và cắm dây.
- Bài thực hành có ba bước nối dây và Bước 4 vận hành.
- Dây 3 và dây 4 hiển thị đúng màu vàng.
- Nhãn socket bị dây che được chuyển sang phía đối diện.
- Chỉ ổ cắm hợp lệ của bước hiện tại được đánh dấu.
- Vòng vàng đã được thu lại để ôm sát ổ hơn.
- Những chỉnh sửa mới nhất chỉ tác động tới `CircuitManager.cs` và `SocketPoint.cs`; không sửa thêm scene hoặc prefab.

## 18. Lưu ý khi sửa tiếp

- Không bật lại tự sắp xếp wire head nếu không có yêu cầu, vì sẽ làm mất vị trí đã căn tay.
- Không đổi transform của model, labels, guides hoặc HMI ngoài đúng phần được yêu cầu.
- Không xóa `ResponsiveCameraFraming` nếu vẫn cần hỗ trợ nhiều tỷ lệ WebGL.
- Không đổi logic cho phép nhiều kết nối của `5VDC` và `GND_5V`.
- Khi đổi vị trí label, phải thực hiện trước lúc tạo nền label hoặc cập nhật cả label lẫn nền.
- Khi đổi kích thước vòng focus, chỉ sửa `GuideFocusRingRadius`; không cần đổi socket collider.
- Các script `PageOneSceneSetup` đến `PageFourSceneSetup` có khả năng dựng lại content trong Editor. Trước khi chạy menu rebuild, cần chắc chắn không có chỉnh tay chưa được đưa vào script.
- Chỉ build WebGL khi có yêu cầu; kiểm tra Editor/Play Mode trước để tiết kiệm thời gian.

## 19. Nguồn dùng để tổng hợp

- `chats/13-7-StartScene.md`
- `chats/14-06-hoan-thien-ui-ux-3-buoc-hmi-huong-dan-noi-day.md`
- `chats/15-06-hoan-thien-gameplay-popup-va-webgl-responsive.md`
- `chats/01-07 - sửa UI.md`
- Mã nguồn và scene hiện tại trong workspace tại ngày 2026-08-13.
