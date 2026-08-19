# PLAN BÀI THỰC HÀNH 2 - UNITY WEBGL

## 1. Mục tiêu

Xây dựng chế độ Bài 2 ngay trong scene `Assets/Scenes/Sy_scene.unity` với hai luồng chạy song song:

```text
LUỒNG ĐIỀU KHIỂN
GX Works2 -> COM3/SC09 -> PLC -> Motor thật

LUỒNG GIÁM SÁT
PLC/FX3U-485-BD -> COM5/RS485 -> Gateway HTTP -> Unity WebGL
                                                   |- HMI hiển thị dữ liệu thật
                                                   `- Motor ảo quay theo motor thật
```

Nguyên tắc:

- GX Works2 là nơi duy nhất điều khiển PLC và motor thật.
- Unity Bài 2 chỉ đọc và hiển thị dữ liệu, không gửi lệnh điều khiển.
- Không tạo thêm hệ thống bước mới; sử dụng đúng thanh `Bước 1` đến `Bước 4` đang có.
- Không tách scene. Toàn bộ chức năng vẫn nằm trong `Sy_scene`.

Hai thư mục cần phân biệt:

| Vai trò | Đường dẫn chuẩn |
|---|---|
| Project Unity đang chỉnh sửa/build | `C:\Users\Server-Lab602\Desktop\Bai2 (ProjectUnity)\Digital-Twin-main\Digital-Twin-main` |
| Hạ tầng runtime trên server | `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main` |

Không tiếp tục sửa project Unity cũ ở ổ D như thể đó là source chính. Các script gateway, Caddy, Guacamole và vận hành server vẫn thuộc cây runtime ổ D.

## 2. Trải nghiệm khi mở WebGL

Sau khi WebGL tải thành công:

1. Hệ thống tự mở `Bước 4 - Vận hành`.
2. Cả 15 dây được hiển thị ở trạng thái đã đấu đúng.
3. Không thể kéo, tháo hoặc cắm lại dây.
4. HMI mới được hiển thị.
5. Unity bắt đầu nhận telemetry từ gateway COM5.
6. Motor ảo quay theo dữ liệu thực tế.
7. Sinh viên vẫn có thể chọn `Bước 1`, `Bước 2`, `Bước 3` để xem lại sơ đồ đấu dây.

Việc đổi tab chỉ thay đổi nội dung đang xem. Nó không được reset dây, telemetry, HMI, motor hoặc trạng thái PLC.

## 3. Hiệu ứng dây theo từng bước

| Tab đang xem | Cách hiển thị |
|---|---|
| Bước 1 | 6 dây mạch điều khiển nháy theo màu gốc; 9 dây còn lại hiển thị mờ |
| Bước 2 | 6 dây encoder/phản hồi nháy theo màu gốc; 9 dây còn lại hiển thị mờ |
| Bước 3 | 3 dây mạch lực nháy theo màu gốc; 12 dây còn lại hiển thị mờ |
| Bước 4 | Cả 15 dây hiển thị màu bình thường, không nháy; HMI xuất hiện |

Danh sách hiện tại trong scene:

- Bước 1: 6 dây.
- Bước 2: 6 dây.
- Bước 3: 3 dây.
- Tổng cộng: 15 dây.

Quy tắc hiệu ứng:

- Dây đỏ nháy đỏ.
- Dây vàng nháy vàng.
- Dây đen dao động từ đen sang xám sáng để vẫn dễ nhìn.
- Hiệu ứng là nhịp sáng mềm khoảng `0,9 giây`, không chớp tắt hoàn toàn.
- Dây được chọn tăng nhẹ độ rộng từ khoảng `100%` đến `135%`.
- Dây không thuộc bước đang xem vẫn giữ vị trí đã cắm nhưng giảm độ nổi.
- Socket thuộc bước đang xem có vòng sáng ổn định.
- Khi trở về Bước 4, mọi dây và socket được phục hồi trạng thái hiển thị bình thường.

## 4. Kế hoạch triển khai

> **Tiến độ hiện tại (đối chiếu ngày 30/07/2026):** Giai đoạn 1-8 đã hoàn thành. Giai đoạn 9 đã chuyển chính thức từ Modbus RTU sang non-protocol `9600/8N1`; PLC đang phát frame telemetry qua `FX3U-485-BD -> COM5`, gateway `Fx3uTelemetryGateway.exe` trả `/telemetry` HTTP `200`, Unity/HMI đã nhận dữ liệu thật và từng hiển thị đúng khoảng `10 RPM`. Phần còn lại là nghiệm thu lạnh sau reboot, kiểm thử mất COM5/khôi phục, xác nhận chiều và encoder qua nhiều vòng, và hoàn thiện vòng đời ca thực hành/SSO.

Quy ước handoff:

- Sau khi hoàn thành mỗi giai đoạn, cập nhật nhật ký ngay bên dưới giai đoạn đó.
- Nhật ký phải ghi trạng thái, ngày thực hiện, file đã thay đổi, việc đã làm, cách đã kiểm chứng và lưu ý cho người tiếp theo.
- Không đánh dấu hoàn thành nếu Unity chưa biên dịch thành công hoặc chưa thực hiện đủ kiểm tra của giai đoạn.
- Người tiếp quản cần đọc nhật ký của tất cả giai đoạn đã hoàn thành trước khi tiếp tục.

### Giai đoạn 1 - Sao lưu và giữ nguyên chế độ cũ

**Trạng thái:** Hoàn thành ngày 21/07/2026.

- Sao lưu `Sy_scene.unity` và các script sẽ sửa.
- Thêm hai chế độ runtime:

```csharp
InteractiveWiring
Bai2CompletedReview
```

- `InteractiveWiring` giữ nguyên luồng nối dây tương tác hiện tại.
- Bản WebGL Bài 2 sử dụng `Bai2CompletedReview`.
- Không xóa chức năng cũ để có thể quay lại bài nối dây khi cần.

#### Nhật ký triển khai Giai đoạn 1

**Đã thực hiện:**

- Tạo `Assets/Scripts/LessonRuntimeMode.cs` với hai giá trị:

```csharp
InteractiveWiring = 0
Bai2CompletedReview = 1
```

- Thêm trường serialized `runtimeMode` vào `CircuitManager`.
- Thêm thuộc tính chỉ đọc `RuntimeMode` và `IsCompletedReviewMode` để các giai đoạn sau sử dụng.
- Giữ giá trị mặc định là `InteractiveWiring`; chưa thay đổi logic trong `Start()` hoặc `InitializeGame()`.
- Tạo backup tại `_Backups/Bai2_Phase1_20260721_231810`.
- Backup gồm `Plan_Bai2.md`, `Sy_scene.unity`, `CircuitManager.cs`, `WireBody.cs`, `WirePlug.cs` và `PLCController_v2.cs`.

**File đã thay đổi:**

- `Assets/Scripts/CircuitManager.cs`
- Tạo mới `Assets/Scripts/LessonRuntimeMode.cs`
- `Plan_Bai2.md`

**Đã kiểm chứng:**

- Unity import file mới và biên dịch thành công.
- Unity Console có `0` error sau khi biên dịch.
- Hai giá trị enum xuất hiện đúng trong runtime.
- `CircuitManager.runtimeMode` được Unity serialize thành công.
- Chế độ hiện tại vẫn là `InteractiveWiring`.
- `Sy_scene.unity` không bị sửa và có trạng thái `dirty = false`.
- `WireBody.cs`, `WirePlug.cs` và `PLCController_v2.cs` vẫn trùng hash với bản backup.

**Lưu ý bàn giao:**

- Giai đoạn 1 chỉ tạo nền tảng chọn chế độ; `Bai2CompletedReview` chưa có hành vi riêng.
- Không được đặt mặc định sang `Bai2CompletedReview` trước khi Giai đoạn 2 hoàn thành logic snap dây và khóa tương tác.
- Các warning obsolete hiện có là warning cũ của project, không phát sinh từ thay đổi Giai đoạn 1.
- Unity MCP có thể tạm mất kết nối trong lúc Unity import/recompile; chờ biên dịch xong rồi kết nối lại.
- Giai đoạn tiếp theo bắt đầu tại phần khởi tạo trạng thái hoàn thành của 15 dây, không sửa lại enum vừa tạo.

### Giai đoạn 2 - Khởi tạo Bài 2 ở trạng thái hoàn thành

**Trạng thái:** Hoàn thành ngày 21/07/2026.

Khi `Sy_scene` chạy ở chế độ `Bai2CompletedReview`:

1. Tìm đủ 15 dây trong ba `stepRoots`.
2. Tìm socket theo `correctSocketA` và `correctSocketB` của từng dây.
3. Đưa hai đầu dây tới đúng socket.
4. Gán `connectedSocket` và `isSnapped` cho từng đầu dây.
5. Đặt `isFullyConnected = true` và `isCorrect = true`.
6. Khóa toàn bộ component `WirePlug`.
7. Đặt tiến độ hoàn thành đủ 15 dây.
8. Mở khóa cả bốn tab.
9. Chọn sẵn Bước 4.
10. Không hiển thị popup hoàn thành bước.

Không được chỉ thay đổi biến trạng thái. Vị trí đầu dây cũng phải được snap vào socket để mô hình nhìn đúng như đã đấu hoàn chỉnh.

#### Nhật ký triển khai Giai đoạn 2

**Đã thực hiện:**

- Thêm nhánh khởi tạo riêng `InitializeCompletedReviewMode()` trong `CircuitManager`.
- Giữ nguyên nhánh cũ trong `InitializeInteractiveWiringMode()` để không xóa chế độ thực hành nối dây.
- Xác nhận scene có 15 dây chia thành ba nhóm `6 + 6 + 3`.
- Xác nhận 28 `SocketPoint` có 28 ID duy nhất.
- Phát hiện các socket thật nằm dưới root `Sockets`, không nằm bên trong ba `stepRoots`.
- Thêm tra cứu socket trên toàn scene bằng `FindAllSocketsById()`.
- Kiểm tra đủ dây, plug và socket đích trước khi thực hiện snap.
- Tái sử dụng helper `RestorePlugConnection()` hiện có để:
  - Gán `connectedSocket`.
  - Đặt `isSnapped = true`.
  - Đặt socket sang trạng thái occupied.
  - Chuyển vị trí và rotation của đầu dây tới đúng socket.
- Gọi `RefreshConnectionState()` và chỉ mở Bước 4 khi toàn bộ dây đều `isFullyConnected` và `isCorrect`.
- Đặt trạng thái runtime của Bài 2:

```text
currentStepIndex = 3
visibleStepIndex = 3
highestUnlockedStepIndex = 3
completedWires = 15
totalWires = 15
systemUnlocked = true
```

- Hiện cả ba nhóm dây bằng `ShowAllCompletedWires()` và khóa toàn bộ `WirePlug`.
- Hiện `HMI_Runtime_Canvas` ngay khi khởi động Bước 4.
- Trong `Bai2CompletedReview`, chặn việc tải additive `HMI_scene` vì HMI runtime đã nằm trong `Sy_scene`.
- Đặt `CircuitManager.runtimeMode = Bai2CompletedReview` trong `Sy_scene` và lưu scene.
- Không sửa `WirePlug.cs`; API hiện có trong `CircuitManager` đã đủ cho Giai đoạn 2.

**File đã thay đổi:**

- `Assets/Scripts/CircuitManager.cs`
- `Assets/Scenes/Sy_scene.unity`
- `Plan_Bai2.md`

**Backup:**

- `_Backups/Bai2_Phase2_20260721_233450`
- Backup gồm 7 file: kế hoạch, scene, enum runtime và các script liên quan tới dây/HMI.

**Đã kiểm chứng:**

- Unity biên dịch thành công, không có compilation error.
- Chạy Play Mode trực tiếp trên `Sy_scene`.
- Kết quả kiểm thử runtime: `PHASE2_RUNTIME_VALIDATION=PASS`.
- Đủ `15/15` dây được nối đúng.
- Cả 30 đầu dây trỏ đúng socket ID và trùng vị trí socket.
- Cả 30 component `WirePlug` đều bị khóa.
- Cả ba `stepRoots` đều active ở Bước 4.
- Bước 4 được chọn mặc định và hệ thống ở trạng thái unlocked.
- `HMI_Runtime_Canvas` active.
- `HMI_scene` không được tải; runtime chỉ có một scene.
- Unity Console có `0` error sau bài kiểm thử.
- Đã kiểm tra ảnh Game View: toàn bộ dây, motor, HMI và thanh Bước 1-4 đều hiển thị; Bước 4 đang được chọn.
- Sau Play Mode đã reload lại scene từ file đã lưu; scene trở về `dirty = false`.
- So sánh scene với backup cho thấy thay đổi serialized duy nhất là `runtimeMode: 1`.

**Lưu ý bàn giao:**

- `Sy_scene` hiện mặc định chạy chế độ `Bai2CompletedReview`. Muốn kiểm tra luồng nối dây cũ phải đổi Inspector về `InteractiveWiring`.
- Giai đoạn 2 mới bảo đảm trạng thái khởi động Bước 4. Khi bấm xem Bước 1-3, logic cũ vẫn chỉ hiện riêng root của bước đó; Giai đoạn 3 và 4 sẽ tách view-state và giữ toàn bộ dây trên màn hình.
- HMI hiện tại vẫn là giao diện điều khiển cũ với các nút lệnh. Chỉ chuyển sang read-only tại Giai đoạn 7.
- Endpoint telemetry vẫn là cấu hình cũ; chưa xử lý trong Giai đoạn 2.
- Không sửa `WirePlug.cs` ở giai đoạn này.
- Hàm restore tiến độ cũ vẫn tìm socket theo `stepRoot`, trong khi socket thật nằm dưới root `Sockets`. Nếu các giai đoạn sau sửa luồng quay lại hướng dẫn, cần xử lý điểm này riêng.
- Nếu số dây không còn đúng 15 hoặc thiếu socket đích, chế độ Bài 2 sẽ ghi lỗi rõ ràng và quay về nhánh `InteractiveWiring` thay vì mở HMI với sơ đồ sai.

### Giai đoạn 3 - Tách trạng thái tiến độ khỏi tab đang xem

**Trạng thái:** Hoàn thành ngày 21/07/2026.

Hiện tại `CircuitManager` sử dụng trạng thái bước cho cả tiến độ thực hành và phần đang hiển thị. Cần tách rõ:

```csharp
currentWiringStep      // Tiến độ của chế độ nối dây cũ
visibleStepIndex       // Tab sinh viên đang xem
isCompletedReviewMode  // Có đang chạy Bài 2 hay không
```

Trong chế độ Bài 2:

- `visibleStepIndex` được phép thay đổi từ 0 đến 3.
- Trạng thái hoàn thành của 15 dây luôn được giữ nguyên.
- Đổi tab không gọi lại logic đánh giá hoặc reset dây.
- Telemetry vẫn tiếp tục chạy ở mọi tab.

#### Nhật ký triển khai Giai đoạn 3

**Đã thực hiện:**

- Giữ tên field serialized `currentStepIndex` để không làm mất dữ liệu scene, nhưng chuyển field này thành private.
- Chuyển `totalWires` và `completedWires` thành private serialized fields.
- Cung cấp các thuộc tính chỉ đọc để code ngoài không thể sửa trực tiếp tiến độ:

```csharp
CurrentWiringStepIndex
VisibleStepIndex
TotalWires
CompletedWires
IsSystemUnlocked
```

- Thay hàm chuyển tab private cũ bằng API `TryShowStepView(int stepIndex)`.
- `TryShowStepView()` chỉ thay đổi `visibleStepIndex` và phần trình bày tương ứng.
- Trong `Bai2CompletedReview`, cả bốn giá trị tab từ 0 đến 3 luôn được chấp nhận; giá trị ngoài phạm vi bị từ chối mà không đổi view.
- Thêm `PreserveCompletedReviewProgressState()` và gọi trước/sau mỗi lần chuyển tab.
- Hàm bảo vệ luôn khôi phục các bất biến của Bài 2:

```text
currentStepIndex = stepRoots.Count = 3
highestUnlockedStepIndex = 3
completedWires = totalWires = 15
systemUnlocked = true
WirePlug của cả ba nhóm luôn disabled
```

- Không cho `EvaluateCircuit()` và cheat code thay đổi tiến độ khi đang ở `Bai2CompletedReview`.
- Nút của thanh Bước 1-4 hiện gọi `TryShowStepView()` thay vì gọi trực tiếp logic nội bộ cũ.
- Thêm thuộc tính chỉ đọc `PLCController_v2.IsTelemetryPolling` để kiểm tra coroutine telemetry mà không dùng reflection hoặc can thiệp polling.

**File đã thay đổi:**

- `Assets/Scripts/CircuitManager.cs`
- `Assets/PLCController_v2.cs`
- `Plan_Bai2.md`

**Backup:**

- `_Backups/Bai2_Phase3_20260721_235329`
- Backup gồm kế hoạch, scene, `CircuitManager.cs`, `LessonRuntimeMode.cs` và `PLCController_v2.cs`.

**Đã kiểm chứng:**

- Unity biên dịch thành công.
- Chạy Play Mode mới hoàn toàn sau khi recompile.
- Chuyển tab theo chuỗi `0,1,2,3,2,0,3` bằng API thật.
- Kết quả: `PHASE3_STATE_SEPARATION=PASS`.
- Sau mọi lần đổi tab, wiring step vẫn là 3 và tiến độ vẫn `15/15`.
- Snapshot của cả 15 dây không đổi socket A/B hoặc vị trí hai đầu dây.
- Cả 30 `WirePlug` vẫn bị khóa.
- `IsTelemetryPolling` là `true` trước, trong và sau chuỗi chuyển tab.
- Tab ngoài phạm vi `-1` và `4` bị từ chối, không đổi view hiện tại.
- Unity Console có `0` error sau bài test.
- Đã thoát Play Mode và scene trở lại `dirty = false`.

**Lưu ý bàn giao:**

- Tên serialized `currentStepIndex` vẫn tồn tại để tương thích scene cũ; ý nghĩa chính thức từ giai đoạn này là tiến độ đấu dây và chỉ được đọc qua `CurrentWiringStepIndex`.
- `visibleStepIndex` là nguồn trạng thái duy nhất cho tab đang xem.
- Không sửa trực tiếp ba field tiến độ từ script ngoài; dùng API đọc mới.
- Giai đoạn 3 chưa thay đổi bố cục hiển thị: khi xem Bước 1-3, logic hiện tại vẫn chỉ active một `stepRoot`; khi về Bước 4 mới active đủ ba root.
- Việc giữ cả ba nhóm dây trên màn hình khi xem Bước 1-3 thuộc Giai đoạn 4.
- HMI bị ẩn khi xem Bước 1-3 nhưng polling telemetry không dừng.
- HMI vẫn còn nút điều khiển cũ cho tới Giai đoạn 7.

### Giai đoạn 4 - Sửa thanh điều hướng Bước 1-4 hiện tại

**Trạng thái:** Hoàn thành ngày 22/07/2026.

- Giữ nguyên giao diện và nội dung của thanh bốn bước.
- Cho phép bấm cả bốn tab ngay khi WebGL tải xong.
- Khi chọn Bước 1-3:
  - Giữ cả ba nhóm dây hoạt động.
  - Hiện nội dung hướng dẫn của bước tương ứng.
  - Ẩn HMI về mặt giao diện.
  - Không bật lại khả năng kéo dây.
- Khi chọn Bước 4:
  - Ẩn nội dung hướng dẫn nối dây.
  - Hiện toàn bộ dây ở màu bình thường.
  - Hiện HMI.
- Không dừng polling telemetry khi HMI đang bị ẩn.

#### Nhật ký triển khai Giai đoạn 4

**Đã thực hiện:**

- Thêm `ShowCompletedReviewStep(int stepIndex)` dành riêng cho `Bai2CompletedReview`.
- Khi xem Bước 1-3, cả ba `stepRoots` luôn active nên đủ 15 dây vẫn nằm trên mô hình.
- Khóa lại toàn bộ `WirePlug` mỗi khi dựng view review.
- Ẩn các presentation object nội bộ của step root để không đưa khay dây hoặc UI kéo thả cũ trở lại.
- Chỉ active `guideRoot` tương ứng với tab đang xem.
- Giữ `BoardStepHeading` và socket focus theo bước đang xem bằng cơ chế hiện có.
- Khi chọn Bước 4, tiếp tục dùng `ShowAllCompletedWires()`:
  - Cả ba nhóm dây active.
  - Cả ba guide bị ẩn.
  - HMI runtime được bật lại.
- `TryShowStepView()` chỉ gọi `ShowCompletedReviewStep()` trong chế độ Bài 2.
- Chế độ `InteractiveWiring` vẫn dùng `ShowOnlyStep()` cũ và không bị thay đổi hành vi.
- Việc ẩn HMI ở Bước 1-3 chỉ gọi `SetRuntimeHmiVisible(false)`; không gọi `StopTelemetryPolling()`.

**File đã thay đổi:**

- `Assets/Scripts/CircuitManager.cs`
- `Plan_Bai2.md`

**Backup:**

- `_Backups/Bai2_Phase4_20260722_001404`
- Backup gồm kế hoạch, scene, `CircuitManager.cs` và `PLCController_v2.cs`.

**Đã kiểm chứng:**

- Unity biên dịch thành công.
- Chạy Play Mode và chuyển lần lượt qua cả bốn tab.
- Kết quả: `PHASE4_REVIEW_LAYOUT=PASS`.
- Tại Bước 1, 2 và 3:
  - Có 3/3 step root active.
  - Có 15/15 WireBody active.
  - Có 15/15 LineRenderer đang hiển thị.
  - Chỉ có đúng một guide của bước tương ứng active.
  - HMI runtime bị ẩn.
  - Telemetry polling vẫn là `true`.
- Tại Bước 4:
  - Có 3/3 step root và 15/15 dây active.
  - Không có guide nào active.
  - HMI runtime được hiển thị.
  - Telemetry polling vẫn là `true`.
- Cả bốn button Bước 1-4 đều tồn tại và interactable.
- Tiến độ luôn giữ `15/15`, hệ thống unlocked và 30 đầu dây vẫn bị khóa.
- Đã chụp và kiểm tra Game View của Bước 1 và Bước 4.
- Unity Console có `0` error sau kiểm thử.
- Đã thoát Play Mode; scene sạch `dirty = false`.
- `Sy_scene.unity` không cần thay đổi trong giai đoạn này.

**Lưu ý bàn giao:**

- Tại Bước 1-3 hiện đã thấy đủ 15 dây, nhưng tất cả vẫn có độ nổi như nhau. Việc làm mờ dây ngoài bước và pulse nhóm dây được chọn thuộc Giai đoạn 5.
- Socket focus đang tiếp tục hoạt động do đây là hành vi có sẵn của `ShowOnlyStep()` trước Giai đoạn 4; Giai đoạn 6 sẽ kiểm tra và hoàn thiện riêng.
- Nút `Về hướng dẫn` hiện có vẫn được giữ nguyên khi xem Bước 1-3.
- HMI vẫn là giao diện điều khiển cũ và sẽ chuyển sang TelemetryOnly tại Giai đoạn 7.
- Không thay đổi scene hoặc vị trí vật thể; thay đổi chỉ nằm trong logic runtime.

### Giai đoạn 5 - Thêm hệ thống làm nổi dây

**Trạng thái:** Hoàn thành ngày 22/07/2026.

Tạo script mới `Assets/Scripts/WireStepHighlighter.cs`.

Trách nhiệm của script:

- Nhận ba nhóm dây từ `CircuitManager.stepRoots`.
- Lưu màu, material và độ rộng ban đầu của từng `LineRenderer`.
- Làm mờ các dây không thuộc bước đang xem.
- Tạo hiệu ứng pulse cho dây thuộc bước đang xem.
- Giữ đúng màu riêng của từng dây.
- Dừng hiệu ứng cũ trước khi chuyển sang bước mới.
- Phục hồi chính xác màu và độ rộng ban đầu khi mở Bước 4 hoặc khi component bị tắt.
- Không chỉnh trực tiếp material asset dùng chung.
- Không tạo material mới liên tục trong `Update()` để tránh rò bộ nhớ và giảm hiệu năng WebGL.

`CircuitManager.TryShowStepView()` gọi highlighter sau mỗi lần đổi tab.

#### Nhật ký triển khai Giai đoạn 5

**Đã thực hiện:**

- Tạo mới `Assets/Scripts/WireStepHighlighter.cs`.
- `CircuitManager` tự gắn highlighter vào chính GameObject manager khi chạy `Bai2CompletedReview`; không cần thêm component thủ công vào scene.
- Highlighter nhận trực tiếp ba `stepRoots` và cache đúng 15 `WireBody` cùng `LineRenderer` tương ứng.
- Mỗi dây chỉ lưu một lần:
  - Nhóm bước.
  - Màu gốc.
  - Màu vertex gốc.
  - Độ rộng đầu/cuối gốc.
  - `MaterialPropertyBlock` dùng lại.
- Không chỉnh trực tiếp material asset hoặc tạo material trong `LateUpdate()`.
- Hỗ trợ cả ba kiểu lấy màu:
  - `_BaseColor` của shader runtime `Custom/WirePlugAlwaysOnTop`.
  - `_Color` của `DigitalTwin/WireOverlay`.
  - `LineRenderer.startColor` làm fallback.
- Dùng `MaterialPropertyBlock` để thay màu/alpha mà không thay material instance.
- Hiệu ứng nhóm dây được chọn:

```text
Chu kỳ pulse: 0,9 giây
Độ rộng: 100% -> 135%
Alpha: 68% -> 100%
Dây đỏ/vàng giữ nguyên hue
Dây đen: value khoảng 0,10 -> 0,45 để nhìn thấy nhịp sáng
```

- Nhóm dây không thuộc bước:

```text
Độ rộng: 72%
Alpha: 24%
Vẫn giữ màu và vị trí đã nối
```

- Bước 1 làm nổi 6 dây và làm mờ 9 dây.
- Bước 2 làm nổi 6 dây và làm mờ 9 dây.
- Bước 3 làm nổi 3 dây và làm mờ 12 dây.
- Bước 4 gọi `ShowAllNormal()`, phục hồi màu/độ rộng gốc và ngừng animation.
- `OnDisable()` và `OnDestroy()` cũng phục hồi trạng thái dây để không để lại màu tạm.

**File đã thay đổi:**

- Tạo mới `Assets/Scripts/WireStepHighlighter.cs`
- `Assets/Scripts/CircuitManager.cs`
- `Plan_Bai2.md`

**Backup:**

- `_Backups/Bai2_Phase5_20260722_002733`
- Backup gồm kế hoạch, scene, `CircuitManager.cs`, `WireBody.cs`, shader và material gốc của dây.

**Đã kiểm chứng:**

- Unity biên dịch thành công.
- Highlighter cache đủ `15/15` dây.
- Kết quả cuối: `PHASE5_WIRE_HIGHLIGHT_FINAL=PASS`.
- Grouping đúng lần lượt `6/9`, `6/9`, `3/12` cho Bước 1-3.
- Dây focus luôn nằm trong khoảng alpha và độ rộng pulse cấu hình.
- Dây ngoài bước có alpha 24% và độ rộng 72%.
- Dây đỏ/vàng không đổi hue.
- Dây đen được đo tại hai thời điểm pulse:

```text
Mẫu A: width 0.00509, value 0.117, alpha 0.696
Mẫu B: width 0.00660, value 0.421, alpha 0.973
```

- Material instance ID của cả 15 dây không đổi qua các lần chuyển tab.
- Bước 4 phục hồi đủ 15 màu và độ rộng gốc; `FocusedStepIndex = -1`, `IsAnimating = false`.
- Đã chụp và kiểm tra Game View Bước 1: dây của bước hiện rõ, dây ngoài bước mờ nhưng vẫn nhìn thấy.
- Unity Console cuối cùng có `0` error.
- Đã thoát Play Mode; scene sạch `dirty = false`.
- `Sy_scene`, `WireBody`, shader và material asset không bị chỉnh sửa.

**Lưu ý bàn giao:**

- Material dây lúc runtime bị `WireLineAlwaysOnTop` thay bằng shader `Custom/WirePlugAlwaysOnTop`, dùng `_BaseColor`, không phải `_Color`.
- Không gọi `material.GetColor("_Color")` nếu chưa kiểm tra `HasProperty`, vì sẽ tạo Console error dù logic highlight vẫn chạy.
- Không thay `MaterialPropertyBlock` bằng `line.material` trong animation; truy cập/chỉnh `line.material` có thể tạo thêm material instance.
- Highlighter được tạo runtime nên không xuất hiện sẵn trong edit-mode hierarchy.
- Giai đoạn 5 chỉ xử lý thân dây. Vòng focus socket được kiểm tra và hoàn thiện riêng tại Giai đoạn 6.

### Giai đoạn 6 - Làm nổi socket tương ứng

**Trạng thái:** Hoàn thành ngày 22/07/2026.

- Tận dụng `SocketPoint.SetGuideFocus()` đang có.
- Khi xem Bước 1-3, lấy `correctSocketA/B` của nhóm dây tương ứng.
- Bật vòng focus trên các socket đó.
- Socket chỉ dùng vòng sáng ổn định; không nhấp nháy cùng dây.
- Tắt toàn bộ vòng focus khi mở Bước 4.

#### Nhật ký triển khai Giai đoạn 6

**Đã thực hiện:**

- Rà lại toàn bộ luồng đổi tab trong `CircuitManager` và xác nhận cơ chế socket focus đã được nối đúng vào chế độ `Bai2CompletedReview`:
  - `ShowCompletedReviewStep(stepIndex)` gọi `UpdateStepSocketFocus(stepIndex)` khi mở Bước 1-3.
  - `UpdateStepSocketFocus()` luôn xóa focus của bước cũ trước khi lấy `correctSocketA/B` của nhóm dây mới.
  - ID socket được gom bằng `HashSet<string>` không phân biệt chữ hoa/chữ thường, nên một socket dùng chung bởi nhiều dây chỉ có một vòng sáng.
  - Chỉ các `SocketPoint` thuộc scene hợp lệ và có ID đúng mới được bật focus.
  - `ShowAllCompletedWires()` gọi `ClearStepSocketFocus()` khi mở Bước 4.
- Xác nhận `SocketPoint.SetGuideFocus()` đáp ứng đúng thiết kế:
  - Lưu tỉ lệ gốc của socket một lần và phóng nhẹ lên `110%` khi focus.
  - Tạo vòng `SocketGuideFocus` một lần khi cần rồi tái sử dụng khi đổi tab.
  - Vòng có màu sáng cố định, độ rộng đầu/cuối bằng nhau và không có `Animator`, vì vậy không nhấp nháy cùng dây.
  - Khi tắt focus, vòng bị ẩn và socket trở về đúng tỉ lệ ban đầu.
- Không sửa thêm mã C# vì phần triển khai hiện hữu đã thỏa đủ tiêu chí của Giai đoạn 6; tránh tạo thêm component hoặc một hệ thống focus trùng lặp.

**File đã thay đổi:**

- `Plan_Bai2.md`
- Không thay đổi `CircuitManager.cs`, `SocketPoint.cs` hoặc `Sy_scene.unity` trong giai đoạn này.

**Backup:**

- `_Backups/Bai2_Phase6_20260722_005948`
- Backup gồm `Plan_Bai2.md`, `Assets/Scripts/CircuitManager.cs` và `Assets/Scripts/SocketPoint.cs`.

**Đã kiểm chứng:**

- Unity đang mở đúng `Assets/Scenes/Sy_scene.unity`, scene sạch trước khi kiểm thử và chế độ runtime là `Bai2CompletedReview`.
- Kiểm thử chuyển tab trong Play Mode đạt `PHASE6_SOCKET_FOCUS=PASS`:

```text
Bước 1: 10 socket đích, 10 vòng sáng đúng ID
Bước 2: 12 socket đích, 12 vòng sáng đúng ID
Bước 3:  6 socket đích,  6 vòng sáng đúng ID
Bước 4:  0 vòng sáng
```

- Kiểm thử độ ổn định đạt `PHASE6_SOCKET_STABILITY=PASS`:
  - Vòng sáng không có animation và không đổi độ rộng/màu giữa hai đầu line.
  - Material instance của vòng không đổi sau chuỗi chuyển Bước 1 -> Bước 2 -> Bước 1.
  - Khi trở về Bước 4, mọi vòng đều tắt và tỉ lệ của toàn bộ socket được phục hồi.
- Đã chụp và kiểm tra Game View Bước 1; các vòng sáng nằm đúng quanh socket của nhóm dây đang được làm nổi.
- Unity Console có `0` error. Có bốn warning không phát sinh từ socket focus: XR simulator sample bị thiếu, `PLCDisplay3D.valueText` chưa gán và hai warning secure pipe của Unity AI Assistant.

**Lưu ý bàn giao:**

- Socket focus là GameObject runtime tên `SocketGuideFocus`, nên không xuất hiện sẵn trong hierarchy khi chưa chạy Play Mode.
- Số vòng nhỏ hơn hai lần số dây nếu nhiều dây dùng chung một socket; số đúng phải tính theo tập ID socket không trùng lặp.
- Không thêm hiệu ứng pulse cho socket. Chỉ dây nhấp nháy; vòng socket phải giữ ổn định để giao diện không quá rối.
- Giai đoạn 7 tiếp tục với `HMI_Runtime_Canvas` và chế độ `TelemetryOnly`; không cần thay đổi thêm phần socket focus.

### Giai đoạn 7 - Chuyển HMI Bước 4 sang TelemetryOnly

**Trạng thái:** Hoàn thành ngày 22/07/2026.

- Dùng `HMI_Runtime_Canvas` hiện có trong `Sy_scene`.
- Không tải thêm `HMI_scene` bằng additive scene.
- Bỏ hoặc ẩn các nút điều khiển:
  - Start/Stop.
  - Forward/Reverse.
  - Tăng/giảm tốc độ.
  - Nhập tốc độ, góc hoặc số vòng.
- Chặn hàm gửi lệnh trong code khi đang ở `TelemetryOnly`.

Các trường cần hiển thị:

- Trạng thái kết nối RS485.
- PLC/Motor RUN hoặc STOP.
- Tốc độ thực tế RPM.
- Chiều quay.
- Encoder count.
- Số vòng quay.
- Góc rotor.
- Thời điểm nhận telemetry gần nhất.
- Cảnh báo dữ liệu cũ hoặc mất kết nối.

#### Nhật ký triển khai Giai đoạn 7

**Đã thực hiện:**

- Thêm enum `HmiInteractionMode` gồm:
  - `Control`: giữ giao diện điều khiển cũ để tương thích Bài 1.
  - `TelemetryOnly`: chỉ hiển thị dữ liệu và chặn mọi lệnh điều khiển.
- `CircuitManager.OpenHmiScene()` tự đặt `PLCController_v2` sang `TelemetryOnly` khi runtime mode là `Bai2CompletedReview`, sau đó mới mở HMI.
- `PLCController_v2.SetRuntimeHmiVisible(true)` cũng kiểm tra lại mode Bài 2 để tránh phụ thuộc thứ tự `Awake/Start` giữa hai component.
- Tiếp tục dùng `HMI_Runtime_Canvas` có sẵn trong `Sy_scene`; không tải `HMI_scene` bằng additive scene.
- Tạo dashboard `TelemetryCard` riêng và ẩn hoàn toàn `SetupCard` cùng `ControlCard` trong `TelemetryOnly`.
- Dashboard mới hiển thị đủ chín trường:
  - Kết nối RS485.
  - Motor `RUN/STOP`.
  - RPM phản hồi thực tế từ `speedRpm`.
  - Chiều quay thuận/ngược.
  - `encoderCount`.
  - Số vòng quay từ `rotationsExact`, fallback sang `rotations` nếu gateway chưa có giá trị exact.
  - Góc rotor.
  - Thời điểm máy Unity nhận telemetry gần nhất.
  - Trạng thái dữ liệu đang nhận, dữ liệu cũ hoặc mất kết nối.
- Ghi `lastTelemetryReceivedRealtime` và `LastTelemetryReceivedAt` chỉ khi nhận được telemetry thật từ gateway.
- Refresh trạng thái tuổi dữ liệu mỗi `0,25 giây`; mặc định cảnh báo cũ sau `2 giây` không có mẫu mới.
- Mở lại HMI trong `TelemetryOnly` không gọi `ResetHmiInputFields()`, nên không ghi đè dữ liệu phản hồi bằng giá trị đặt cục bộ.
- Chặn ở hai lớp:
  - Các API điều khiển công khai thoát ngay trước khi thay đổi state cục bộ.
  - Hàm `SendControl(...)` riêng tiếp tục kiểm tra lần cuối trước khi tạo coroutine HTTP `POST`.
- Các lệnh được bảo vệ gồm `ON`, `OFF`, đặt tốc độ, tăng/giảm tốc độ, đặt số vòng, đặt góc, đổi chiều và reset.
- Thêm `BlockedControlCommandCount`, `LastBlockedControlAction` và `ControlRequestCount` để kiểm tra và chẩn đoán việc khóa lệnh.
- Fallback HMI dùng `OnGUI()` cũng không vẽ nút điều khiển khi ở `TelemetryOnly`.

**File đã thay đổi:**

- `Assets/PLCController_v2.cs`
- `Assets/Scripts/CircuitManager.cs`
- `Plan_Bai2.md`
- Không thay đổi `Assets/Scenes/Sy_scene.unity`.

**Backup:**

- `_Backups/Bai2_Phase7_20260722_011214`
- Backup gồm kế hoạch, `PLCController_v2.cs`, `CircuitManager.cs` và `Sy_scene.unity`.

**Đã kiểm chứng:**

- Unity biên dịch thành công: `compileFailed = false`.
- Scene đúng `Assets/Scenes/Sy_scene.unity` và không bị đánh dấu dirty trước khi chạy test.
- Kiểm thử chính đạt `PHASE7_TELEMETRY_ONLY=PASS`:

```text
Mode: TelemetryOnly
TelemetryCard đang hiển thị: true
Nút điều khiển đang hiển thị: 0
Ô nhập đang hiển thị: 0
Trường telemetry đang hiển thị: 9/9
HMI_scene additive đang load: false
```

- Gọi thử mười đường điều khiển đạt kết quả:

```text
blockedDelta = 10
controlRequestDelta = 0
telemetryUnchanged = true
polling = true
```

- Kiểm thử cảnh báo dữ liệu cũ đạt `PHASE7_STALE=PASS`; sau khi dừng polling quá ngưỡng, HMI hiển thị `Dữ liệu: CŨ`.
- Bật lại polling đạt `PHASE7_RECOVERY=PASS`; HMI trở về `ĐANG NHẬN`, cập nhật thời gian nhận cuối và gateway online.
- Kiểm thử tương thích đạt `PHASE7_CONTROL_COMPAT=PASS`:
  - `Control` hiện lại 11 button và 3 input.
  - Chuyển lại `TelemetryOnly` còn 0 button và 0 input.
- Kiểm thử đổi tab đạt `PHASE7_TAB_ROUNDTRIP=PASS`; HMI ẩn ở Bước 1 nhưng polling tiếp tục, về Bước 4 vẫn là `TelemetryOnly`.
- Đã chụp và kiểm tra Game View: dashboard không chồng chữ, dữ liệu thật từ gateway đang hiển thị gồm encoder, số vòng, góc và thời gian nhận.
- Unity Console có `0` error. Bốn warning hiện hữu không phát sinh từ Giai đoạn 7: XR simulator sample, `PLCDisplay3D.valueText` và secure pipe của Unity AI Assistant.

**Lưu ý bàn giao:**

- Không dùng `LatestTelemetry.setSpeedRpm` làm RPM hiển thị trong dashboard Bài 2; trường `RPM thực tế` lấy trực tiếp từ `speedRpm`.
- `SyncMotorFromTelemetry()` vẫn còn logic fallback từ RPM phản hồi sang tốc độ đặt/tần số xung. Đây là phần bắt buộc phải sửa ở Giai đoạn 8 để motor ảo chỉ chạy theo dữ liệu thực và dừng khi telemetry cũ.
- Ngưỡng stale hiện là `2 giây`, có thể chỉnh bằng `telemetryStaleAfterSeconds` sau khi đo độ ổn định gateway COM5.
- Endpoint vẫn giữ nguyên trong giai đoạn này. Việc chuyển hẳn sang endpoint gateway COM5 thuộc Giai đoạn 9.
- Không thêm lại listener điều khiển vào `TelemetryCard`; nếu cần dùng Bài 1, chuyển mode sang `Control` thay vì sửa dashboard Bài 2.

### Giai đoạn 8 - Đồng bộ motor ảo

**Trạng thái:** Hoàn thành phần mềm ngày 22/07/2026; đã nhận telemetry phần cứng thật ngày 23/07/2026. Còn nghiệm thu dài hạn và các tình huống lỗi.

- RPM thực tế quyết định tốc độ quay motor ảo.
- Dấu của RPM hoặc trường direction quyết định chiều quay.
- Encoder count hoặc góc phản hồi dùng để hiệu chỉnh sai lệch vị trí rotor.
- Không dùng tốc độ đặt làm dữ liệu thay thế khi mất telemetry.
- Nếu quá thời gian quy định không nhận được dữ liệu:
  - HMI báo mất kết nối.
  - Motor ảo dừng.
  - Giá trị cũ không được giả vờ tiếp tục chạy.
- Đổi tab không làm reset góc, tốc độ hoặc trạng thái motor.

#### Nhật ký triển khai Giai đoạn 8

**Đã thực hiện:**

- Khảo sát scene và xác nhận chỉ có một driver đang sở hữu `Rotor_Main`:
  - `RotateSubmarineBlades` trên `MotorRuntimeController`.
  - `rotatableObjects` chứa đúng `Rotor_Main`.
  - Không có `VirtualMotorController` hoạt động trong `Sy_scene`, nên không có tình trạng hai component cùng quay một rotor.
- Bổ sung trạng thái chẩn đoán trong `PLCController_v2`:
  - `IsTelemetryFresh` và `TelemetryAgeSeconds`.
  - `IsVisualMotorRunning`, `VisualMotorRpm` và `VisualMotorDegreesPerSecond`.
  - `VisualMotorDirectionForward` và `VisualSyncStatus`.
  - Góc feedback cùng sai số hiệu chỉnh rotor gần nhất.
- Trong `TelemetryOnly`, tốc độ motor ảo chỉ lấy từ `abs(LatestTelemetry.speedRpm)`.
- Không dùng `setSpeedRpm`, `pulseFrequency` hoặc tốc độ đặt làm fallback cho Bài 2.
- Giữ logic fallback cũ riêng cho mode `Control` để không làm hỏng hành vi Bài 1.
- Motor chỉ chạy khi đồng thời thỏa:
  - Gateway đang online.
  - Telemetry còn mới.
  - `running = true`.
  - RPM thực tế lớn hơn deadband mặc định `0,5 RPM`.
- Xác định chiều quay theo thứ tự:
  - RPM âm luôn được hiểu là chiều ngược.
  - Nếu RPM không âm, dùng trường `direction = forward/reverse`.
  - Giữ đúng quy ước model hiện tại: thuận dùng hệ số quay `-1`, ngược dùng `+1`.
- Thêm watchdog chạy mỗi `0,1 giây` kể cả khi HMI đang ẩn ở Bước 1-3.
- Watchdog đặt RPM và tốc độ góc về `0`, đồng thời tắt driver ngay khi telemetry stale hoặc gateway offline.
- `SetConnectionStatus(false, ...)` cũng dừng motor ngay, không đợi chu kỳ watchdog tiếp theo.
- Chỉ điều khiển một driver ưu tiên `RotateSubmarineBlades`; nếu scene khác có cả `VirtualMotorController`, controller phụ bị dừng để tránh quay nhân đôi.
- Hiệu chỉnh góc rotor từ feedback theo thứ tự:
  - `angle` nếu có giá trị.
  - `rotationsExact * 360` nếu chưa có angle.
  - `encoderCount / 5000 * 360` nếu chỉ có encoder.
- Lưu local rotation gốc của rotor và hiệu chỉnh bằng `Quaternion.Slerp`; mặc định chỉ sửa khi lệch quá `2°`, strength `0,35` mỗi mẫu để tránh giật hình.
- Thêm `ApplyTelemetryForTesting(...)` chỉ trong `UNITY_EDITOR` để kiểm thử dữ liệu giả mà không mở thêm API trong WebGL production.

**File đã thay đổi:**

- `Assets/PLCController_v2.cs`
- `Plan_Bai2.md`
- Không thay đổi `RotateSubmarineBlades.cs`, `VirtualMotorController.cs` hoặc `Sy_scene.unity`.

**Backup:**

- `_Backups/Bai2_Phase8_20260722_013546`
- Backup gồm kế hoạch, `PLCController_v2.cs`, `RotateSubmarineBlades.cs` và `Sy_scene.unity`.

**Đã kiểm chứng:**

- Unity biên dịch thành công: `compileFailed = false`.
- Kiểm thử dữ liệu tổng hợp đạt `PHASE8_SYNTHETIC_SYNC=PASS`:
  - `120 RPM`, thuận -> `720 deg/s`, driver chạy với direction `-1`.
  - `60 RPM`, ngược -> `360 deg/s`, driver chạy với direction `+1`.
  - RPM `-30` ghi đè chuỗi `forward` và vẫn chọn chiều ngược.
  - `speedRpm = 0`, `setSpeedRpm = 700`, `pulseFrequency = 5000` -> motor vẫn dừng, chứng minh không còn fallback ở Bài 2.
  - Gateway offline -> motor dừng và tốc độ về `0`.
- Hiệu chỉnh rotor với strength test `1,0` đưa sai số góc về dưới `0,1°`.
- Watchdog đạt `PHASE8_STALE_WATCHDOG=PASS`; sau khi ngừng telemetry quá 2 giây, rotor và driver đều dừng.
- Chuyển Bước 4 -> Bước 1 -> Bước 4 đạt `PHASE8_TAB_STATE=PASS`; telemetry, RPM, chiều và local rotation không bị reset.
- Kiểm thử transform đạt `PHASE8_ROTOR_MOTION=PASS`; `Rotor_Main` thay đổi góc và số vòng khi nhận RUN/RPM.
- Kiểm thử STOP đạt `PHASE8_ROTOR_STOP=PASS`; transform và bộ đếm vòng không thay đổi sau khi dừng.
- Polling thật hiện tại đạt `PHASE8_GATEWAY_SAMPLE=PASS_SOFTWARE_CONSISTENCY`: mẫu gần nhất báo `running = false`, `speedRpm = 0`, `setSpeedRpm = 10`, và motor ảo dừng theo `speedRpm` thay vì tốc độ đặt.
- Unity Console có `0` error mới.

**Cập nhật bố cục camera theo tab ngày 23/07/2026:**

- Phát hiện khi rời Bước 4, một camera `Runtime_Motor_PIP_Camera` và overlay runtime mồ côi có thể còn hoạt động sau khi Unity hot-reload script; camera responsive đồng thời trả FOV về khoảng `70°`, làm Bước 1-3 bị zoom out.
- Sửa `PLCController_v2` để khi đóng HMI phải tắt toàn bộ camera `Runtime_Motor_PIP_Camera`, `Runtime_Wiring_PIP_Camera` và mọi `Runtime_Control_Camera_Overlay` trong scene, không chỉ đối tượng còn được giữ trong biến.
- Khi mở lại Bước 4, controller dọn các camera/overlay mồ côi trước khi tạo đúng một bộ PiP mới.
- Thêm `ShowWiringReviewCameraLayout()` cho Bước 1-3: camera chính toàn màn hình, FOV `34°`, lấy bounds của toàn bộ bảng/dây; không hiển thị HMI hoặc camera motor.
- Sửa `CircuitManager.CloseHmiScene()` để chế độ `Bai2CompletedReview` luôn áp dụng camera toàn bảng sau khi rời Bước 4.
- Thanh tab runtime tự dựng lại nếu Unity hot-reload làm mất danh sách tham chiếu, tránh nội dung đã sang Bước 1-3 nhưng nút Bước 4 vẫn giữ màu chọn.
- Kiểm thử thứ tự `1 -> 2 -> 3 -> 4 -> 1 -> 4 -> 3` đạt:
  - Bước 1-3: `PIP=0`, `OVERLAY=0`, FOV `34°`, đúng nút tab được chọn.
  - Bước 4: `PIP=1`, `OVERLAY=1`, đúng nút Bước 4 được chọn.
- Đã chụp và kiểm tra Game View thực tế: Bước 1-3 hiển thị bảng đầy khung và không còn ô `MOTOR ẢO`; Bước 4 vẫn giữ HMI cùng một camera motor.
- File thay đổi: `Assets/PLCController_v2.cs`, `Assets/Scripts/CircuitManager.cs`, `Plan_Bai2.md`.

**Cập nhật phục hồi highlight dây sau hot-reload ngày 23/07/2026:**

- Phát hiện Unity hot-reload giữ lại component `WireStepHighlighter` nhưng làm mất cấu hình runtime và cache dây; vì vậy tab vẫn đổi đúng nhưng Bước 1-3 không còn pulse/highlight.
- Sửa `CircuitManager.ShowCompletedReviewStep()` để luôn gọi `EnsureWireStepHighlighter()` trước khi focus nhóm dây.
- Sửa `ShowAllCompletedWires()` để cũng phục hồi cache trước khi đưa 15 dây về trạng thái bình thường ở Bước 4.
- Kiểm thử runtime đạt: Bước 1/2/3 lần lượt có focus `0/1/2`, cache đủ `15/15` dây và `IsAnimating=true`; Bước 4 có focus `-1` và `IsAnimating=false`.
- Đã kiểm tra ảnh Game View Bước 1: dây đúng bước sáng/dày và nháy, dây ngoài bước mờ; camera motor không xuất hiện.
- Unity biên dịch sạch, Console không có lỗi.

**Lưu ý bàn giao:**

- Hai cầu `RDA-SDA` và `RDB-SDB` đã được lắp; tuyến vật lý PLC -> FX3U-485-BD -> DTech -> COM5 đã nhận dữ liệu thật.
- Unity/HMI đã nhận mẫu motor thật và hiển thị khoảng `10 RPM`; không còn coi gateway hiện tại là nguồn mô phỏng.
- `encoderPulsesPerRevolution` hiện đặt `5000` theo logic PLC cũ. Phải đối chiếu lại encoder và chương trình PLC thật sau khi hoàn tất dây.
- Vẫn cần kiểm thử nhiều vòng, chiều thuận/ngược, wrap bộ đếm, rút/cắm lại COM5 và chạy đồng thời COM3 + COM5 sau cold reboot.
- Kết quả Giai đoạn 8 xác nhận logic motor ảo; trạng thái nghiệm thu production cuối cùng được theo dõi tại Giai đoạn 9 và phần kiểm toán ngày 30/07/2026.

### Giai đoạn 9 - Cấu hình endpoint telemetry

**Trạng thái:** Luồng telemetry non-protocol và API production đã hoạt động. Còn nghiệm thu sau cold reboot, kiểm thử lỗi vật lý và xác nhận đầy đủ trong phiên sinh viên có đăng nhập.

- Loại bỏ URL ngrok cũ khỏi scene.
- Trỏ `PLCController_v2` tới gateway COM5 mới.
- Ưu tiên URL tương đối, ví dụ:

```text
/plc-rs485/telemetry
```

- Nếu phải dùng URL tuyệt đối, kiểm tra CORS và HTTPS mixed-content.
- Polling ban đầu giữ khoảng `0,5 giây`, sau đó điều chỉnh theo độ ổn định của COM5 và PLC.

#### Nhật ký triển khai Giai đoạn 9

**Đã thực hiện ngày 22/07/2026:**

- Hoàn tất đấu dây one-pair: `T/R+ -> RDA`, `T/R- -> RDB`, nối cầu `RDA-SDA` và `RDB-SDB`.
- Xác nhận Windows nhận `COM5` là `USB Serial Port` dùng chip FTDI, trạng thái thiết bị tốt.
- Thử Computer Link cho kết quả timeout nên đã dừng hướng này và chuyển toàn bộ Luồng 2 sang Modbus RTU theo tài liệu Factory Automation mà chủ dự án chọn.
- Xóa script Computer Link cũ `ops/rs485/Test-Fx3uComputerLink.ps1` và thay `TEST-RS485-COM5-READONLY.bat` bằng phép đọc Modbus RTU FC03.
- Tạo gateway độc lập tại `gateway/modbus_rtu_gateway` bằng C#/.NET Framework, không phụ thuộc HslCommunication, MX Component, Python hoặc thư viện thương mại.
- Cấu hình gateway: `COM5`, `38400 bps`, `8 data bits`, `No parity`, `1 stop bit`, slave `3`; chỉ cho phép FC01/FC03 và chặn ghi PLC.
- Gateway đọc `D100..D165` bằng FC03 và `M0..M17` bằng FC01, sau đó trả schema telemetry mà `PLCController_v2` đang sử dụng.
- Tạo `START-RS485-MODBUS-GATEWAY.bat`; API local chạy tại `127.0.0.1:5002` với `/health`, `/telemetry`, `/debug`; `POST /control` trả HTTP `423`.
- Build `ModbusRtuGateway.exe` thành công. `/health` trả đúng cấu hình, `/control` bị khóa, `/telemetry` trả `503` khi PLC chưa phản hồi.
- Test read-only gửi đúng frame đọc `D500`: `03 03 01 F4 00 01 C5 E6`.
- Đã nạp ladder và xác nhận trực tiếp bằng GX Works2: `D8120 = H40A1`, `D8121 = 0003`.
- Sau khi PLC có cấu hình đúng, phép đọc `D500` vẫn timeout và nhận `0 byte`.
- Bổ sung chế độ `probe` gửi tối đa 10 request, cách nhau 500 ms, để quan sát TXD/RXD rõ hơn và loại trừ việc một frame quá ngắn không nhìn thấy LED.
- Tạo `TEST-DTECH-COM5-LOOPBACK.bat` và lệnh `loopback` trong gateway để kiểm tra riêng đường TX/RX của DT-5119. Test này không kết nối PLC: tháo DTech khỏi board, cầu `T/R+ -> RXD+`, `T/R- -> RXD-`, rồi yêu cầu nhận lại nguyên chuỗi đã gửi.
- Đã chạy loopback độc lập ngày 23/07/2026: TX và RX trùng hoàn toàn, kết quả `DTECH_LOOPBACK=PASS`. Đã xác nhận COM5, FTDI, mạch phát/thu và terminal DT-5119 hoạt động tốt; timeout PLC không xuất phát từ adapter USB.
- Phát hiện `PlcBridge.exe COM5 9090` chiếm COM5 do `Start-PixelStack.ps1` tự chọn FTDI. Đã dừng tiến trình để test và sửa script chỉ chấp nhận đúng `CH340/COM3`, chủ động bỏ qua FTDI/COM5.
- Sau khi phục hồi dây PLC, chạy 10 request FC03 liên tiếp: đèn `RD` trên FX3U-485-BD nháy theo từng request nhưng `SD` không nháy và COM5 nhận `0 byte`.
- Kết luận chẩn đoán: lớp vật lý PC -> DTech -> RS485 -> board PLC đã đạt; PLC/board không tạo phản hồi Modbus. Không tiếp tục đảo dây hoặc thay Python/client. Hướng Modbus native với `FX3U-485-BD` hiện tại bị loại bỏ; bước tiếp theo là chọn non-protocol một chiều/Computer Link được board hỗ trợ, hoặc thay bằng `FX3U-485ADP-MB` nếu bắt buộc Modbus.
- Gateway COM3 hiện hữu không bị dừng hoặc thay đổi; không có tiến trình test COM5 nào bị để lại sau kiểm thử.

**Cập nhật non-protocol ngày 23/07/2026:**

- Đọc manual chính hãng và xác nhận `FX3U-485-BD` hỗ trợ N:N, Parallel Link, Computer Link, Non-protocol và Inverter Communication; board này không hỗ trợ Modbus RTU native. Phần Modbus phía trên chỉ được giữ lại làm lịch sử chẩn đoán, không phải cấu hình đang dùng.
- Đọc `D8001 = 24321`: mã PLC `24`, firmware báo Ver. `3.21`, đủ điều kiện sử dụng board.
- Xóa hai rung thử Modbus `MOV H40A1 D8120` và `MOV K3 D8121`.
- Cấu hình `PLC Parameter -> PLC System (2) -> CH1`: `Non-Procedural`, `RS-485`, `9600 bps`, `8 data bits`, `No parity`, `1 stop bit`, không Header/Terminator/Sum Check. Phải tắt/bật nguồn PLC để cấu hình serial mới có hiệu lực; chỉ STOP/RUN là chưa đủ.
- Thêm ladder thử: bật `M8161`, chạy `RS D500 K6 D600 K0`, nạp chuỗi `PLC2\r\n` vào `D500..D505`, và dùng `M500 -> PLS M501 -> SET M8122` để phát một lần.
- Thêm chế độ receive-only `listen` vào `gateway/modbus_rtu_gateway/ModbusRtuGateway.cs` và file `TEST-FX3U-RS485-LISTEN.bat`. Chế độ này chỉ nghe COM5 tại `9600/8N1`, không phát request và không can thiệp COM3.
- Test thực tế PASS: COM5 nhận đúng `50 4C 43 32 0D 0A`, tương ứng `PLC2\r\n`; kết quả `FX3U_RS485_TX=PASS`.
- Đã nghiệm thu tuyến vật lý và truyền byte một chiều: PLC -> FX3U-485-BD -> DTech DT-5119 -> FTDI/COM5 -> ứng dụng Windows.
- Tại thời điểm thử frame `PLC2\r\n`, telemetry motor thật chưa được nghiệm thu. Việc định nghĩa frame motor và đổi sang gateway parser non-protocol đã được hoàn thành ở các cập nhật phía dưới.

**Kết luận sau thử nghiệm Modbus:**

- Toàn bộ cấu hình `H40A1`, `D8121=3`, slave `3`, `38400/8N1`, FC01/FC03 và phép đọc `D500` chỉ là lịch sử thử nghiệm.
- `FX3U-485-BD` không được dùng như Modbus RTU slave trong cấu hình production của Bài 2.
- Không nạp lại hai rung `MOV H40A1 D8120` và `MOV K3 D8121`.
- Không chạy `START-RS485-MODBUS-GATEWAY.bat` cùng gateway production.
- Phần thử Modbus được giữ trong tài liệu để giải thích vì sao hướng đó bị loại bỏ và để tránh người tiếp quản lặp lại cùng phép thử.

**Cấu hình production thay thế:**

- PLC dùng `Non-Procedural`, `RS-485`, `9600/8N1`, không Header/Terminator/Sum Check ở tầng Parameter.
- PLC chủ động phát frame telemetry định kỳ; máy server chỉ nhận, không gửi lệnh qua COM5.
- Gateway production là `gateway/fx3u_telemetry_gateway/bin/Fx3uTelemetryGateway.exe`.
- API local là `http://127.0.0.1:5002`; route public là `/rs485/*`.
- Unity dùng `http://103.238.69.131:8080/rs485` và endpoint `/telemetry`.

**Cập nhật kiểm thử Unity ngày 23/07/2026:**

- Đã mở đúng `Assets/Scenes/Sy_scene.unity` qua Unity MCP và xác nhận `PLC_Manager` đang hoạt động.
- Đã bơm trực tiếp một gói telemetry thử nghiệm vào `PLCController_v2`: `60 RPM`, `2.5` vòng, góc `180`, chiều thuận, `backendSynced=true`.
- Kết quả runtime: HMI hiển thị, trạng thái Online, `VisualMotorRpm=60` và motor ảo chạy. Unity Console không có lỗi compile hay runtime mới liên quan telemetry.
- Kiểm thử trên chỉ xác nhận hoàn chỉnh đường JSON -> `PLCController_v2` -> HMI/motor; không thay đổi scene, ladder, PLC Parameter hoặc dây nối.
- Điểm còn lại duy nhất của Giai đoạn 9 là để gateway COM5 phát JSON `/telemetry` cùng schema thay cho gói thử nghiệm.

**Cập nhật telemetry thật và chuẩn bị WebGL ngày 23/07/2026:**

- Gateway `Fx3uTelemetryGateway.exe` đã nhận frame motor thật qua `COM5`, trả JSON `/telemetry` và đồng bộ đúng `9.995 RPM`, HMI làm tròn thành `10 RPM`.
- Xác nhận công thức RPM với encoder `5000` xung/vòng: `RPM = (deltaCount / deltaTime) * 60 / 5000`.
- Sửa `PLCController_v2.ApplyRotorFeedbackCorrection()` để mỗi mẫu `timestamp/encoderCount` chỉ hiệu chỉnh góc rotor một lần; dữ liệu HTTP lặp lại không còn kéo motor ảo về vị trí cũ.
- Tách endpoint production: điều khiển COM3 giữ `http://103.238.69.131:8080/plc`, telemetry COM5 dùng `http://103.238.69.131:8080/rs485`.
- Lưu cấu hình production vào `Assets/Scenes/Sy_scene.unity`: polling `0.5` giây, stale timeout `3` giây, `TelemetryOnly`, đồng bộ motor và hiệu chỉnh encoder bật.
- Thêm route Caddy `/rs485/* -> 127.0.0.1:5002`, giữ nguyên `/plc/* -> 127.0.0.1:5000`; cấu hình đã validate và Caddy đã khởi động lại thành công.
- Kiểm tra sau khi đổi route: `/rs485/health`, Guacamole `/gxworks2/` đều trả HTTP `200`. `/plc/health` có thể trả `502` khi COM3 đang thuộc GX Works2 mode, đây là trạng thái dự kiến.
- Trigger tay `M500` đã được dùng để nghiệm thu frame đầu tiên. Bản production sử dụng trigger định kỳ và phải giữ nguyên nguyên tắc: chỉ tạo one-shot khi cổng truyền rảnh, giữ các rung đóng frame, rồi `SET M8122`; không ghi đè/reset cờ truyền tùy tiện.
- Ngày 30/07/2026, gateway production đang nhận frame liên tục, `/rs485/health` và `/rs485/telemetry` trả HTTP `200`. Chưa đóng Giai đoạn 9 hoàn toàn vì chưa có biên bản test cold reboot, rút/cắm COM5, wrap encoder và phiên sinh viên đầy đủ qua URL có xác thực.

## 5. Các file đã thay đổi hoặc được tạo

- `Assets/Scenes/Sy_scene.unity`
- `Assets/Scripts/CircuitManager.cs`
- `Assets/PLCController_v2.cs`
- `Assets/Scripts/LessonRuntimeMode.cs`
- `Assets/Scripts/WireStepHighlighter.cs`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\gateway\fx3u_telemetry_gateway\Fx3uTelemetryGateway.cs`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\Start-TelemetryGateway.ps1`
- `D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\ops\server-control\Start-PixelStack.ps1`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\Start-CameraSnapshot.ps1`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\camera_runtime\browser_camera_worker.py`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\Start-BackendLoopback.ps1`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\Restart-BackendOnly.ps1`
- `D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\RESTART-BACKEND-ONLY.bat`

`WireBody.cs` và `WirePlug.cs` được khảo sát nhưng không cần sửa trong phần triển khai đã nghiệm thu.

## 6. Checklist kiểm thử trong Unity

- [x] Nhấn Play mở thẳng Bước 4.
- [x] Tab Bước 4 được đánh dấu đang chọn.
- [x] Cả 15 dây đã cắm vào đúng socket.
- [x] Không dây nào có thể kéo hoặc tháo.
- [x] Bước 1 làm nổi đúng 6 dây.
- [x] Bước 2 làm nổi đúng 6 dây.
- [x] Bước 3 làm nổi đúng 3 dây.
- [x] Mỗi dây nháy theo đúng màu gốc.
- [x] Dây đen vẫn nhìn rõ khi được làm nổi.
- [x] Các socket tương ứng được đánh dấu đúng.
- [x] Bước 4 phục hồi màu bình thường của cả 15 dây.
- [x] HMI và camera motor chỉ hiện ở Bước 4.
- [x] Telemetry tiếp tục chạy khi xem Bước 1-3.
- [x] Motor ảo không reset khi đổi tab.
- [x] Chuyển tab liên tục không tạo nhiều coroutine hoặc material mới.
- [x] Không còn lỗi tải `HMI_scene`.
- [x] Unity Console không có error mới trong lần kiểm thử gần nhất.

## 7. Checklist tích hợp telemetry thật

- [x] Gateway COM5 mở được cổng RS485.
- [x] PLC phát dữ liệu non-protocol qua FX3U-485-BD và COM5 nhận đúng frame thử.
- [x] Endpoint `/telemetry` trả JSON hợp lệ.
- [x] HMI báo Online khi gateway hoạt động.
- [x] RPM trên HMI từng khớp dữ liệu thực tế khoảng `10 RPM`.
- [ ] Chiều quay motor ảo khớp motor thật.
- [ ] Encoder/góc trên Unity được hiệu chuẩn đúng qua nhiều vòng và qua điểm wrap.
- [ ] Rút COM5 hoặc dừng gateway khiến HMI báo mất kết nối.
- [x] Motor ảo dừng khi telemetry quá hạn trong kiểm thử phần mềm.
- [x] GX Works2 trên COM3 điều khiển PLC đồng thời COM5 giám sát.

## 8. Checklist build WebGL

- [x] Build Settings chỉ sử dụng `Assets/Scenes/Sy_scene.unity`.
- [x] Đã tạo build `C:\Users\Server-Lab602\PTnew\unity-builds\demo-Bai2` ngày 23/07/2026.
- [ ] Rebuild WebGL vì `Sy_scene.unity` hiện mới hơn build `demo-Bai2` khoảng 34 phút.
- [ ] Đăng nhập P-DTwin bằng tài khoản sinh viên và xác nhận build tải xong, vào thẳng Bước 4 sau lần cập nhật JAR gần nhất.
- [ ] Thanh bốn bước không che màn HMI.
- [ ] Chữ và dữ liệu không tràn khỏi vùng hiển thị.
- [ ] Giao diện hoạt động khi đặt cạnh cửa sổ GX Works2.
- [x] Hiệu ứng dây đã chạy ổn định trong Unity Play Mode, không tạo thêm material.
- [ ] Endpoint telemetry hoạt động liên tục từ WebGL trong trình duyệt triển khai thực tế.
- [ ] Không có lỗi CORS hoặc mixed-content sau khi hệ thống chuyển domain/HTTPS trong tương lai.

## 9. Tiêu chí hoàn thành

Bài 2 được xem là hoàn thành khi:

1. Sinh viên mở WebGL và nhìn thấy ngay Bước 4 với HMI cùng toàn bộ dây đã nối.
2. Sinh viên có thể xem lại đúng ba bước đấu dây bằng thanh điều hướng hiện có.
3. Nhóm dây của từng bước được làm nổi rõ ràng theo đúng màu dây.
4. Không thể thay đổi trạng thái đấu dây trong chế độ Bài 2.
5. GX Works2 điều khiển motor thật qua COM3.
6. Unity nhận phản hồi thực tế qua gateway COM5.
7. HMI và motor ảo phản ánh đúng trạng thái motor thật.
8. Mất telemetry được phát hiện và hiển thị rõ ràng.
9. Chuyển tab không làm gián đoạn luồng dữ liệu hoặc reset motor.
10. Build WebGL hoạt động ổn định trong bố cục hai cửa sổ của hệ thống thực hành.

## 10. Trạng thái hiện tại

### 10.1 Đã hoàn thành

- Giai đoạn 1-8 đã hoàn thành và có nhật ký kiểm thử tương ứng.
- `Sy_scene` chạy ở `Bai2CompletedReview`, tự nối và khóa đủ 15 dây, mặc định mở Bước 4.
- Bước 1-3 chỉ hiển thị camera toàn bảng, giữ đủ dây, pulse đúng nhóm và không còn camera motor.
- Bước 4 hiển thị HMI `TelemetryOnly` và đúng một camera motor.
- HMI không gửi lệnh điều khiển; GX Works2 qua COM3 là nguồn điều khiển motor thật.
- Motor ảo lấy RPM/chiều/góc từ telemetry, không dùng tốc độ đặt làm fallback trong Bài 2.
- Tuyến RS485 vật lý đã hoạt động ở chế độ non-protocol `9600/8N1`.
- Gateway production COM5 đang trả JSON hợp lệ tại `/telemetry`.
- Đã từng nghiệm thu thực tế motor thật quay khoảng `10 RPM`, HMI hiển thị `10 RPM`.
- Caddy đã tách route `/plc/*` cho COM3 và `/rs485/*` cho COM5.
- Hai camera vật lý đã tách thành `/cam1/` và `/cam2/`.
- Guacamole/GX Works2 đang truy cập được qua `/gxworks2/`.
- Build WebGL Bài 2 hiện có tại `C:\Users\Server-Lab602\PTnew\unity-builds\demo-Bai2`.
- Backend Spring Boot hiện lấy JAR từ `C:\Users\Server-Lab602\PTnew`.
- Đã có `RESTART-BACKEND-ONLY.bat` để nạp JAR mới mà không restart toàn bộ stack.

### 10.2 Snapshot kiểm tra ngày 30/07/2026

| Thành phần | Kết quả |
|---|---|
| `127.0.0.1:5000/health` - gateway COM3 | HTTP `200` |
| `127.0.0.1:5002/health` - gateway COM5 | HTTP `200` |
| `127.0.0.1:5002/telemetry` | HTTP `200`, JSON hợp lệ |
| `127.0.0.1:8080/` - Spring Boot | HTTP `200` |
| `10.170.43.240:8080/rs485/health` | HTTP `200` |
| `10.170.43.240:8080/cam1/` | HTTP `200` |
| `10.170.43.240:8080/cam2/` | HTTP `200` |
| `10.170.43.240:8080/gxworks2/` | HTTP `200` |
| Camera `cam1` | `USB Camera (4c4a:4a55)`, online, `1280x720` |
| Camera `cam2` | `A4 tech USB2.0 Camera (0ac8:3450)`, online, `640x480` |

Mẫu telemetry tại thời điểm kiểm tra:

```text
running=false
speedRpm=0
setSpeedRpm=98
encoderCount=1142765
rotationsExact=228.553
direction=reverse
backendSynced=true
backendStatus="COM5 SYNCED"
```

Mẫu này chỉ chứng minh luồng dữ liệu đang sống ở thời điểm kiểm tra; motor đang STOP nên `speedRpm=0` là đúng.

### 10.3 Chưa hoàn thành hoặc chưa nghiệm thu đủ

- Chưa có biên bản kiểm thử đầu-cuối sau cold reboot với một tài khoản sinh viên thật.
- Build `demo-Bai2` được tạo lúc `16:58:14` ngày 23/07/2026, nhưng `Sy_scene.unity` được lưu lúc `17:32:11` cùng ngày. Build đang deploy có thể chưa chứa thay đổi scene cuối; phải rebuild từ project ổ C trước nghiệm thu.
- URL nội bộ của build yêu cầu đăng nhập; request không có JWT trả `403`, nên kiểm tra HTTP tự động chưa thay thế được kiểm thử trong phiên sinh viên.
- Chưa xác nhận chiều thuận/ngược và góc rotor qua nhiều vòng quay.
- Chưa kiểm thử điểm wrap/reset của bộ đếm vòng trong frame compact.
- Chưa rút/cắm lại COM5 để xác nhận HMI chuyển offline, motor ảo dừng và gateway phục hồi.
- Chưa chạy soak test tối thiểu 30-60 phút đồng thời GX Works2 COM3 và telemetry COM5.
- Ba lớp tài khoản P-DTwin, Guacamole và Windows chưa có SSO.
- API lease/phân ca và nút kết thúc phiên từ backend chưa được tích hợp.
- Lab Session Controller ở `127.0.0.1:5010` chưa chạy tại thời điểm kiểm tra.
- RemoteApp `GXWorks2` đã đăng ký, nhưng `PLCLogoff` và `EndPlcSession.exe` chưa được cài tại thời điểm kiểm tra.
- `quser` tại thời điểm kiểm tra còn một session `plc_student` trạng thái `Disc`; phải cleanup trước khi cấp ca mới.
- Chưa có TLS/domain; hệ thống public hiện dùng HTTP và IP.

## 11. Kiến trúc production chuẩn của Bài 2

```text
LUỒNG 1 - ĐIỀU KHIỂN
Sinh viên -> P-DTwin -> /gxworks2/ -> Guacamole -> RDP RemoteApp
          -> GX Works2 -> COM3/SC09/CH340 -> PLC FX3U-16M -> motor thật

LUỒNG 2 - GIÁM SÁT
PLC -> FX3U-485-BD -> DTech DT-5119/FTDI -> COM5
    -> Fx3uTelemetryGateway.exe :5002
    -> Caddy /rs485/*
    -> Unity WebGL HMI + motor ảo
```

Hai luồng dùng hai cổng vật lý riêng:

| Cổng | Thiết bị | Vai trò | Quyền sở hữu |
|---|---|---|---|
| COM3 | SC09/CH340 | GX Works2 điều khiển PLC | Chỉ một trong GX Works2, Python PLC Gateway hoặc `PlcBridge` được giữ cổng |
| COM5 | DTech DT-5119/FTDI | Nhận telemetry RS485 | Chỉ `Fx3uTelemetryGateway.exe` hoặc một tool test được giữ cổng |

### 11.1 Route và tiến trình

| URL/port | Đích | Mục đích |
|---|---|---|
| `http://103.238.69.131:8080/` | Caddy -> Spring `127.0.0.1:8080` | P-DTwin |
| `/gxworks2/` | `127.0.0.1:8081` | Guacamole RemoteApp |
| `/plc/*` | `127.0.0.1:5000` | Gateway COM3 |
| `/rs485/*` | `127.0.0.1:5002` | Telemetry COM5 |
| `/cam1/` | static snapshot | USB Camera mới |
| `/cam2/` | static snapshot | A4Tech |
| `/pixel-stream/*` | `127.0.0.1:8090` | Pixel Streaming hiện hữu |

Không public trực tiếp các port `5000`, `5002`, `5010`, `8081` hoặc `8090`.

## 12. Cấu hình phần cứng và giao thức chuẩn

### 12.1 Đấu dây RS485

```text
DTech T/R+ -> FX3U-485-BD RDA
DTech T/R- -> FX3U-485-BD RDB
DTech GND  -> FX3U-485-BD SG

Trên board:
RDA nối cầu SDA
RDB nối cầu SDB
```

Không thay riêng một cực khi đảo cặp. Tắt nguồn phần cứng trước khi thay dây nếu có nguy cơ chạm chập.

### 12.2 Cấu hình serial production

```text
Protocol: Non-Procedural
H/W Type: RS-485
Baud rate: 9600
Data bits: 8
Parity: None
Stop bits: 1
Gateway mode: receive-only
```

Thay đổi PLC Parameter có thể yêu cầu tắt/bật nguồn PLC; STOP/RUN không luôn đủ.

### 12.3 Frame compact hiện tại

Gateway chấp nhận frame compact 9 byte:

| Byte | Ý nghĩa |
|---|---|
| 0 | `0x50` - ký tự `P` |
| 1 | `0x4C` - ký tự `L` |
| 2 | RPM đặt, 1 byte |
| 3 | Flags: bit 0 thuận, bit 1 ngược, bit 2 RUN |
| 4 | Số vòng nguyên, 1 byte |
| 5 | Xung dư, byte thấp |
| 6 | Xung dư, byte cao |
| 7 | `0x0D` - CR |
| 8 | `0x0A` - LF |

Gateway tính:

```text
encoderCount = rotationsByte * 5000 + residualPulses
RPM = abs(deltaEncoder) * 60 / (5000 * deltaTimeSeconds)
```

Frame 20 byte bắt đầu bằng `AA55/55AA` vẫn được parser hỗ trợ để tương thích thử nghiệm cũ, nhưng frame compact `PL...CRLF` là dữ liệu đang quan sát trong production.

### 12.4 Rủi ro frame cần xử lý

- `rotationsByte` chỉ có 8 bit và sẽ wrap sau 255 vòng; gateway hiện chưa có bộ mở rộng wrap rõ ràng.
- Frame compact không có checksum/CRC, sequence number hoặc version.
- Một frame nhiễu có header `PL` nhưng sai CR/LF có thể làm parser chờ dữ liệu không hợp lệ lâu hơn dự kiến.
- Sau PLC reset hoặc encoder reset, phép lấy delta có thể tạo một mẫu RPM đột biến.
- Gateway hiện cần được kiểm thử khả năng tự phục hồi sau khi COM5 bị rút/cắm lại.

Khuyến nghị frame phiên bản tiếp theo: header, version, payload length, sequence, encoder 32-bit, RPM signed, flags và CRC16.

## 13. Quy trình vận hành chuẩn

### 13.1 Sau khi reboot server

Chạy:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\POST-REBOOT-RUN-THIS.bat
```

Script bật theo thứ tự:

1. Web stack: camera, Spring Boot, COM3 gateway, COM5 gateway và Caddy.
2. Pixel Streaming stack.
3. Docker Desktop và Guacamole.
4. Kiểm tra trạng thái và in URL.

Sau khi chạy, kiểm tra tối thiểu `/`, `/rs485/health`, `/cam1/`, `/cam2/` và `/gxworks2/`.

### 13.2 Chỉ deploy lại JAR backend

1. Thay file:

   ```text
   C:\Users\Server-Lab602\PTnew\pdtwin-backend-0.0.1-SNAPSHOT.jar
   ```

2. Chạy:

   ```text
   D:\MIGRATION_2026-06-29\Windows_Readable\Digital-Twin-main\RESTART-BACKEND-ONLY.bat
   ```

3. Chờ `BACKEND RESTART SUCCESS`, sau đó tải lại trình duyệt bằng `Ctrl+F5`.

Không cần chạy `POST-REBOOT-RUN-THIS.bat` khi chỉ đổi JAR.

### 13.3 Bắt đầu ca GX Works2 thủ công

1. Kiểm tra không có sinh viên khác đang giữ PLC.
2. Chạy `PREPARE-GXWORKS-REMOTE.bat` bằng quyền admin để nhả COM3.
3. Mở `/gxworks2/` và chạy RemoteApp GX Works2.
4. Không chạy đồng thời Python gateway, `PlcBridge` và GX Works2 trên COM3.
5. COM5 telemetry có thể tiếp tục chạy độc lập.

### 13.4 Kết thúc ca hiện tại

Cho tới khi `PLCLogoff` và controller `5010` được cài:

1. Sinh viên Save project và đưa motor về trạng thái an toàn.
2. Đóng GX Works2.
3. Admin chạy `CLEANUP-PLC-STUDENT-SESSIONS.bat`.
4. Xác nhận `quser` không còn `plc_student` và không còn `GD2.exe`.
5. Chuyển lại PLC Gateway Mode.
6. Chỉ cấp ca mới khi COM3 đã được nhả và PLC ở trạng thái an toàn.

Đóng tab hoặc Guacamole Disconnect không phải Windows Logoff.

## 14. Các vấn đề logic còn mở

### 14.1 Tài khoản và phân ca

- P-DTwin, Guacamole và Windows hiện là ba lớp tài khoản độc lập.
- Không nên tạo thủ công một bộ tài khoản Guacamole cho từng sinh viên về lâu dài.
- Backend cần sở hữu lease của một PLC thật: mỗi thời điểm chỉ một sinh viên được ACTIVE.
- Guacamole `max-connections=1` chỉ hạn chế kết nối, không cleanup Windows session.
- Cần SSO/token hoặc cơ chế backend cấp kết nối ngắn hạn; không đưa mật khẩu Guacamole/Windows vào frontend.

### 14.2 Kết thúc bài và cleanup

Nút Nộp bài/Kết thúc ca cần thực hiện theo transaction có trạng thái:

```text
SAVE_RESULT
-> STOP_MOTOR_SAFE
-> LOGOFF_WINDOWS_SESSION
-> KILL_PROCESS_IN_SESSION
-> VERIFY_COM3_FREE
-> RESTORE_GATEWAY_MODE
-> RESET_STUDENT_PROJECT
-> RELEASE_LEASE
```

Nếu bước nào lỗi, chuyển ca sang `FAULT`; không cấp sinh viên mới cho tới khi admin xử lý.

Controller nội bộ `127.0.0.1:5010` và các API backend mô tả trong `HUONG_DAN_GUACAMOLE_REMOTEAPP_VA_CHUYEN_MODE.md` đã được thiết kế nhưng chưa được cài/tích hợp hoàn chỉnh.

### 14.3 Giới hạn nền tảng

- Windows 11 Pro không phải Windows Server/RDS đầy đủ và chỉ phù hợp một ca tương tác với PLC thật tại một thời điểm.
- Một PLC/motor thật không thể phục vụ đồng thời nhiều sinh viên độc lập.
- Muốn mở rộng cần thêm PLC vật lý, hàng đợi/lease hoặc môi trường mô phỏng riêng.

### 14.4 Bảo mật và public URL

- Public URL hiện là HTTP theo IP; tài khoản và session chưa được bảo vệ bằng TLS đầu-cuối.
- Không ghi username/password thật vào tài liệu, Git hoặc ảnh chụp.
- Không public controller `5010` hoặc token SYSTEM.
- Khi chuyển sang HTTPS/domain, đổi Unity sang URL tương đối cùng origin để tránh mixed-content.
- Cần rate limit, audit log và timeout lease ở backend.

### 14.5 Vận hành và khả năng khôi phục

- Ghi rõ project GXW mẫu chuẩn, checksum và cách phục hồi sau mỗi ca.
- Gắn version/build time cho WebGL, JAR, gateway EXE, Caddyfile và ladder PLC.
- Sao lưu cấu hình Guacamole/database, Caddyfile, PLC project và build WebGL.
- Thêm health check sau reboot và cảnh báo khi COM3/COM5 bị process sai chiếm giữ.
- Thêm test tự động xác nhận `/rs485/telemetry` thay đổi timestamp; HTTP `200` với dữ liệu cũ chưa đủ.

## 15. Bộ tài liệu nên tách ra từ file này

`Plan_Bai2.md` nên giữ vai trò nhật ký triển khai và nguồn truy vết. Bộ docs hoàn chỉnh nên tách thành:

1. `01-Kien-truc-Bai2.md`: sơ đồ hai luồng, port, URL và trách nhiệm từng thành phần.
2. `02-Phan-cung-RS485-PLC.md`: thiết bị, đấu dây, PLC Parameter, ladder và frame.
3. `03-Unity-WebGL-HMI.md`: bốn bước, camera, telemetry schema và motor ảo.
4. `04-Gateway-COM5-API.md`: cài đặt, biến môi trường, endpoint, log và lỗi serial.
5. `05-Guacamole-GXWorks2.md`: tài khoản, RemoteApp, COM3 ownership và cleanup.
6. `06-Van-hanh-Server.md`: reboot, restart JAR, backup, monitoring và rollback.
7. `07-Quy-trinh-Sinh-vien.md`: bắt đầu ca, làm bài, nộp bài và kết thúc phiên.
8. `08-Quy-trinh-Admin.md`: force end, xử lý `Disc`, FAULT và phục hồi gateway.
9. `09-Kiem-thu-Nghiem-thu.md`: checklist cold boot, soak test, mất COM, chiều, encoder và bảo mật.

Mỗi tài liệu cần có `Cập nhật lần cuối`, `Phiên bản`, `Người kiểm chứng`, `Điều kiện trước khi chạy`, `Cách rollback` và `Kết quả mong đợi`.
