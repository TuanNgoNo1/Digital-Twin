**HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG KHOA ĐA PHƯƠNG TIỆN** ----🙞🕮🙜---- ~~PY~~ **BÁO CÁO THỰC TẬP CHUYÊN SÂU** _**Đề tài:**_ **“VR Digital Twin” Giảng viên hướng dẫn** PGS.TS Vũ Hữu Tiến Trần Văn Sỹ - B22DCPT231 Ngô Đức Anh Tuấn – B22DCPT248 **Sinh viên** Nguyễn Minh Lương - B22DCPT155 Nguyễn Thành Đạt – B22DCPT053 **Lớp** D22PTUD02 **Khóa** D22 **Hà Nội, 5/2026** ~~bee and~~ 

1 

## **LỜI CẢM ƠN** 

Đầu tiên, chúng em xin gửi lời cảm ơn sâu sắc đến thầy Vũ Hữu Tiến – người đã tận tình chỉ bảo, định hướng về mặt kỹ thuật và học thuật, giúp chúng em hiểu rõ hơn về công nghệ Digital Twin, thực tế ảo (VR) và các hệ thống điều khiển công nghiệp trong suốt quá trình thực hiện đề tài. 

Chúng em cũng xin cảm ơn quý thầy cô trong Khoa Đa Phương Tiện – Học viện Công nghệ Bưu chính Viễn thông đã trang bị cho chúng em nền tảng kiến thức vững chắc từ những năm học trước, tạo tiền đề để chúng em tiếp cận và triển khai thành công dự án này. 

Ngoài ra, chúng em xin cảm ơn các bạn trong nhóm đã cùng nhau nỗ lực, hỗ trợ lẫn nhau vượt qua những khó khăn kỹ thuật trong quá trình kết nối PLC, Raspberry Pi, ZeroTier và xây dựng môi trường mô phỏng trên Unity. 

Dù đã cố gắng hết sức, báo cáo vẫn không thể tránh khỏi những thiếu sót. Chúng em rất mong nhận được sự góp ý, nhận xét từ thầy để hoàn thiện hơn trong những công trình tiếp theo. 

Một lần nữa, chúng em xin chân thành cảm ơn! 

Hà Nội, tháng 5 năm 2026 

2 

## **MỤC LỤC** 

LỜI CẢM ƠN............................................................................................................................ 2 DANH MỤC HÌNH................................................................................................................... 5 DANH MỤC CÁC THUẬT NGỮ VÀ CHỮ VIẾT TẮT.........................................................6 LỜI MỞ ĐẦU............................................................................................................................7 CHƯƠNG 1: TỔNG QUAN LÝ THUYẾT..............................................................................8 1.1 Tổng quan về Digital Twin..............................................................................................8 1.1.1 Khái niệm Digital Twin............................................................................................8 1.1.2 Ứng dụng của Digital Twin trong công nghiệp........................................................8 1.2 Công nghệ Thực tế ảo (VR).............................................................................................8 1.2.1 Giới thiệu về VR.......................................................................................................8 1.2.2 Vai trò của Unity trong dự án....................................................................................9 1.3 Hệ thống PLC và Motor...................................................................................................9 1.3.1 PLC – Bộ điều khiển logic khả trình.........................................................................9 1.3.2 Motor điện và các thông số giám sát.......................................................................10 1.4 Raspberry Pi và mạng ZeroTier.....................................................................................10 1.4.1 Raspberry Pi – Gateway trung gian........................................................................10 1.4.2 ZeroTier – Mạng ảo overlay...................................................................................11 1.5 Phần mềm GX Works....................................................................................................12 Tiểu kết chương 1.................................................................................................................12 CHƯƠNG 2: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG.......................................................13 2.1 Yêu cầu hệ thống............................................................................................................13 2.1.1 Yêu cầu chức năng.................................................................................................. 13 2.1.2 Yêu cầu phi chức năng............................................................................................13 2.2 Kiến trúc tổng thể hệ thống............................................................................................13 2.3 Thiết kế các module chức năng......................................................................................14 2.3.1 Module giao tiếp PLC – Raspberry Pi....................................................................14 2.3.2 Module truyền thông ZeroTier................................................................................14 2.3.3 Module điều khiển trong Unity...............................................................................14 2.3.4 Module camera........................................................................................................14 2.4 Thiết kế giao diện Unity.................................................................................................15 2.4.1 Bảng điều khiển (Control Panel).............................................................................15 2.4.2 Bảng hiển thị số liệu (Dashboard)...........................................................................15 2.5 Biểu đồ tuần tự...............................................................................................................15 2.5.1 Biểu đồ tuần tự: Luồng điều khiển motor từ Unity.................................................15 2.5.2 Biểu đồ Use Case tổng quan...................................................................................16 Tiểu kết Chương 2................................................................................................................16 

3 

CHƯƠNG 3: XÂY DỰNG DỰ ÁN VR DIGITAL TWIN.....................................................17 3.1 Thiết lập hệ thống phần cứng.........................................................................................17 3.2 Kết nối mạng qua ZeroTier............................................................................................17 3.3 Lập trình PLC với GX Works........................................................................................18 3.4 Xây dựng môi trường mô phỏng Unity..........................................................................19 3.5 Tích hợp camera thực tế.................................................................................................20 Tiểu kết chương 3.................................................................................................................21 CHƯƠNG 4: KẾT LUẬN.......................................................................................................22 4.1 Tóm tắt thành tựu...........................................................................................................22 4.2 Bài học rút ra..................................................................................................................22 4.3 Hạn chế của dự án.......................................................................................................... 22 4.4 Hướng phát triển trong tương lai....................................................................................22 4.5 Lời kết............................................................................................................................ 23 TÀI LIỆU THAM KHẢO........................................................................................................23 

4 

## **DANH MỤC HÌNH** 

Hình 1.1: Mô hình tổng quan hệ thống Digital Twin trong công nghiệp. Hình 1.2: Kiến trúc VR và các thành phần cơ bản. Hình 1.3: Cấu tạo và nguyên lý hoạt động của PLC. Hình 1.4: Sơ đồ kết nối PLC – Motor trong hệ thống. Hình 1.5: Raspberry Pi 4 và các cổng giao tiếp. Hình 1.6: Giao diện quản lý mạng ZeroTier. Hình 1.7: Giao diện lập trình GX Works 2. Hình 2.1: Sơ đồ kiến trúc tổng thể hệ thống VR Digital Twin. Hình 2.2: Thiết kế bảng điều khiển giao diện motor trên Unity. Hình 2.3: Thiết kế giao diện bảng hiển thị số liệu. Hình 2.4: Biểu đồ tuần tự – Luồng điều khiển Motor từ Unity Hình 2.5: Biểu đồ Usecase tổng quan hệ thống. Hình 2.6: Thiết kế giao diện bảng hiển thị số liệu (vòng quay, chiều quay,...). Hình 3.1: Hình ảnh thực tế bộ máy PLC kết nối với motor. Hình 3.2: Sơ đồ đấu dây Raspberry Pi với hệ thống PLC-Motor. Hình 3.3: Cấu hình mạng ZeroTier kết nối Raspberry Pi và máy tính. Hình 3.4: Chương trình ladder diagram trên GX Works điều khiển motor. Hình 3.5: Mô hình 3D motor và PLC được dựng trong Unity. Hình 3.6: Bảng điều khiển và bảng số liệu trong môi trường Unity. Hình 3.7: Hình ảnh camera thực tế được hiển thị trong Unity. 

5 

## **DANH MỤC CÁC THUẬT NGỮ VÀ CHỮ VIẾT TẮT** 

|**Thuật ngữ / Chữ viết tắt**|**Định nghĩa**|
|---|---|
|Digital Twin|Bản sao kỹ thuật số – mô hình ảo phản ánh trạng thái<br>và hành vi của một thiết bị hoặc hệ thống vật lý trong<br>thời gian thực.|
|VR (Virtual Reality)|Thực tế ảo – công nghệ tạo ra môi trường mô phỏng<br>ba chiều mà người dùng có thể tương tác.|
|PLC (Programmable Logic<br>Controller)|Bộ điều khiển logic khả trình – thiết bị công nghiệp<br>dùng để tự động hóa quy trình và điều khiển máy<br>móc.|
|Motor|Động cơ điện – thiết bị chuyển đổi năng lượng điện<br>thành cơ năng, trong dự án là đối tượng được điều<br>khiển và mô phỏng.|
|Raspberry Pi (Ras Pi)|Máy tính nhúng nhỏ gọn, đóng vai trò gateway trung<br>gian kết nối bộ máy thực tế với máy tính chạy Unity.|
|ZeroTier|Phần mềm mạng ảo (VPN overlay) cho phép các<br>thiết bị ở các mạng khác nhau giao tiếp như trong<br>cùng một mạng LAN.|
|GX Works|Phần mềm lập trình và giám sát PLC của hãng<br>Mitsubishi Electric, dùng để viết chương trình điều<br>khiển và theo dõi trạng thái PLC.|
|Unity|Engine phát triển game và ứng dụng thực tế ảo, được<br>sử dụng để xây dựng giao diện mô phỏng và điều<br>khiển trong dự án.|
|SCADA|Supervisory Control and Data Acquisition – hệ thống<br>giám sát, thu thập dữ liệu và điều khiển từ xa trong<br>công nghiệp.|
|IoT (Internet of Things)|Mạng lưới kết nối các thiết bị vật lý thông qua<br>Internet, cho phép thu thập và trao đổi dữ liệu.|
|GUI|Graphical User Interface – Giao diện đồ họa người<br>dùng.|
|FPS|Frames Per Second – số khung hình trên giây, đo<br>lường độ mượt của hiển thị đồ họa.|
|RPM|Revolutions Per Minute – vòng/phút, đơn vị đo tốc<br>độ quay của motor.|
|LAN|Local Area Network – mạng cục bộ.|
|VPN|Virtual Private Network – mạng riêng ảo, cho phép<br>kết nối an toàn qua Internet.|



6 

## **LỜI MỞ ĐẦU** 

Trong bối cảnh cuộc Cách mạng Công nghiệp 4.0 đang diễn ra mạnh mẽ, công nghệ Digital Twin – bản sao kỹ thuật số của hệ thống vật lý – đã nổi lên như một trong những xu hướng công nghệ quan trọng nhất trong lĩnh vực tự động hóa và sản xuất thông minh. Digital Twin không chỉ cho phép giám sát hệ thống trong thời gian thực mà còn mở ra khả năng mô phỏng, kiểm tra và tối ưu hóa các quy trình sản xuất mà không gây gián đoạn hoạt động thực tế. Kết hợp với công nghệ Thực tế ảo (VR), hướng tiếp cận này còn mang lại trải nghiệm trực quan, sinh động hơn cho người vận hành và kỹ sư. 

Tại Việt Nam, nhu cầu ứng dụng Digital Twin trong các nhà máy sản xuất và hệ thống điều khiển tự động đang ngày càng gia tăng. Nhiều doanh nghiệp đã và đang đầu tư vào các hệ thống giám sát từ xa, tự động hóa dây chuyền và ứng dụng thực tế ảo trong đào tạo vận hành thiết bị. Điều này tạo ra nhu cầu nhân lực có kỹ năng chuyên sâu về cả phần cứng điều khiển (PLC, motor) lẫn phần mềm mô phỏng (Unity, GX Works). 

Xuất phát từ định hướng đó, nhóm chúng em đã chọn thực hiện đề tài "VR Digital Twin – Mô phỏng và điều khiển bộ máy PLC-Motor trong môi trường thực tế ảo". Hệ thống sử dụng Raspberry Pi làm cầu nối trung gian giữa bộ máy PLC-Motor thực tế và máy tính; kết nối mạng được thiết lập qua ZeroTier; phần mềm GX Works được dùng để lập trình PLC; và Unity được sử dụng để xây dựng toàn bộ môi trường mô phỏng, giao diện điều khiển và hiển thị số liệu. Ngoài ra, hệ thống còn tích hợp camera để truyền hình ảnh thực tế của motor về màn hình mô phỏng, tăng tính chân thực cho bản sao kỹ thuật số. 

7 

## **CHƯƠNG 1: TỔNG QUAN LÝ THUYẾT** 

## **1.1 Tổng quan về Digital Twin** 

## **1.1.1 Khái niệm Digital Twin** 

Digital Twin (Bản sao kỹ thuật số) là một mô hình ảo, đồng bộ theo thời gian thực với một thực thể hoặc hệ thống vật lý trong thế giới thực. Khái niệm này lần đầu tiên được Michael Grieves đề xuất vào năm 2002 trong lĩnh vực quản lý vòng đời sản phẩm (PLM), và sau đó được NASA áp dụng rộng rãi trong mô phỏng tàu vũ trụ. Ngày nay, Digital Twin đã trở thành một thành phần cốt lõi của các nhà máy thông minh (Smart Factory) trong bối cảnh Công nghiệp 4.0. 

Một hệ thống Digital Twin hoàn chỉnh bao gồm ba thành phần chính: thực thể vật lý (Physical Entity), bản sao ảo (Virtual Model) và luồng dữ liệu kết nối (Data Connection). Dữ liệu được thu thập liên tục từ cảm biến, thiết bị đo lường gắn trên hệ thống thực tế, sau đó được truyền lên mô hình ảo để cập nhật trạng thái. Ngược lại, các lệnh điều khiển có thể được gửi từ mô hình ảo xuống hệ thống vật lý, tạo ra vòng điều khiển khép kín. 

_Hình 1.1: Mô hình tổng quan hệ thống Digital Twin trong công nghiệp._ 

## **1.1.2 Ứng dụng của Digital Twin trong công nghiệp** 

Digital Twin đã và đang được ứng dụng rộng rãi trong nhiều lĩnh vực công nghiệp. Trong sản xuất và tự động hóa, Digital Twin cho phép giám sát từ xa và bảo trì dự đoán (predictive maintenance) cho các thiết bị như motor, băng tải, robot lắp ráp. Khi kết hợp với trí tuệ nhân tạo (AI), hệ thống có thể phân tích dữ liệu lịch sử để dự đoán hỏng hóc trước khi chúng xảy ra. 

Trong lĩnh vực đào tạo kỹ thuật, Digital Twin kết hợp với VR tạo ra môi trường mô phỏng an toàn, cho phép kỹ sư và công nhân luyện tập vận hành thiết bị mà không có nguy cơ gây hỏng hóc hoặc tai nạn. Đây cũng chính là một trong những mục tiêu chính của dự án mà nhóm chúng em đang thực hiện. 

## **1.2 Công nghệ Thực tế ảo (VR)** 

## **1.2.1 Giới thiệu về VR** 

8 

Thực tế ảo (Virtual Reality – VR) là công nghệ tạo ra một môi trường ba chiều được máy tính mô phỏng, trong đó người dùng có thể tương tác và trải nghiệm như đang ở trong môi trường thực. Không giống với các màn hình truyền thống, VR bao bọc toàn bộ trường nhìn của người dùng, tạo ra cảm giác hiện diện (presence) mạnh mẽ. 

Trong ngữ cảnh của dự án này, Unity được sử dụng để xây dựng môi trường VR. Unity hỗ trợ đầy đủ các nền tảng VR phổ biến và cung cấp bộ công cụ mạnh mẽ để dựng mô hình 3D, xây dựng bảng điều khiển tương tác và hiển thị dữ liệu thời gian thực. Người dùng có thể quan sát mô hình 3D của motor và PLC, đồng thời thực hiện các thao tác điều khiển ngay trong môi trường ảo. 

_Hình 1.2: Kiến trúc VR và các thành phần cơ bản._ 

## **1.2.2 Vai trò của Unity trong dự án** 

Unity là một engine phát triển ứng dụng đa nền tảng mạnh mẽ, đặc biệt phù hợp để xây dựng các ứng dụng VR và mô phỏng tương tác. Trong dự án VR Digital Twin, Unity đảm nhiệm các vai trò chính bao gồm: dựng và hiển thị mô hình 3D của PLC và motor, cung cấp bảng điều khiển (Control Panel) để người dùng ra lệnh cho motor (start/stop, đảo chiều, thay đổi tốc độ), hiển thị bảng số liệu thời gian thực (vòng quay RPM, chiều quay, trạng thái), và nhận/gửi dữ liệu qua giao thức mạng đến Raspberry Pi. 

## **1.3 Hệ thống PLC và Motor** 

## **1.3.1 PLC – Bộ điều khiển logic khả trình** 

PLC (Programmable Logic Controller) là một thiết bị điện tử công nghiệp được thiết kế để thực hiện các chức năng điều khiển logic, tuần tự và thời gian. PLC nhận tín hiệu đầu vào từ các cảm biến và công tắc, xử lý theo chương trình được lập trình sẵn, sau đó xuất tín hiệu điều khiển đến các thiết bị chấp hành như motor, van, đèn báo. Trong dự án, PLC của hãng Mitsubishi được sử dụng để điều khiển motor thông qua biến tần (inverter). 

PLC được lập trình bằng phần mềm GX Works với ngôn ngữ Ladder Diagram (LD) – một trong những ngôn ngữ lập trình PLC phổ biến nhất, có hình thức tương tự sơ đồ mạch điện, giúp kỹ sư dễ dàng thiết kế và kiểm tra logic điều khiển. 

9 

_Hình 1.3: Cấu tạo và nguyên lý hoạt động của PLC._ 

## **1.3.2 Motor điện và các thông số giám sát** 

Motor điện xoay chiều (AC Motor) là thiết bị chuyển đổi năng lượng điện thành cơ năng quay. Trong hệ thống của dự án, motor được kết nối với PLC thông qua biến tần để điều chỉnh tốc độ linh hoạt. Các thông số quan trọng cần giám sát bao gồm: tốc độ quay (RPM), chiều quay (thuận/nghịch), trạng thái hoạt động (đang chạy/dừng), và dòng điện tiêu thụ. Các giá trị này được đọc từ PLC và truyền về Unity để hiển thị trên bảng số liệu trong thời gian thực. 

_Hình 1.4: Sơ đồ kết nối PLC – Motor trong hệ thống._ 

## **1.4 Raspberry Pi và mạng ZeroTier** 

## **1.4.1 Raspberry Pi – Gateway trung gian** 

10 

Raspberry Pi là một máy tính nhúng nhỏ gọn, giá thành thấp nhưng có đủ khả năng xử lý và kết nối mạng để đóng vai trò gateway trong hệ thống IoT. Trong dự án VR Digital Twin, Raspberry Pi được đặt tại vị trí của bộ máy thực tế, kết nối trực tiếp với PLC thông qua cổng truyền thông (RS-485 hoặc Ethernet), đọc dữ liệu trạng thái và ghi lệnh điều khiển từ/lên PLC, sau đó chuyển tiếp thông tin này đến máy tính chạy Unity thông qua mạng ZeroTier. 

_Hình 1.5: Raspberry Pi 4 và các cổng giao tiếp._ 

## **1.4.2 ZeroTier – Mạng ảo overlay** 

ZeroTier là một phần mềm mạng ảo (VPN overlay) cho phép các thiết bị ở các mạng vật lý khác nhau giao tiếp với nhau như thể chúng đang trong cùng một mạng LAN. Điều này đặc biệt hữu ích khi Raspberry Pi và máy tính chạy Unity có thể đặt ở các vị trí địa lý khác nhau hoặc sử dụng các kết nối Internet khác nhau mà không cần cấu hình phức tạp như port forwarding hay VPN truyền thống. 

ZeroTier hoạt động theo mô hình peer-to-peer, mỗi thiết bị được cấp một địa chỉ IP ảo trong mạng ZeroTier. Dữ liệu truyền qua ZeroTier được mã hóa end-to-end, đảm bảo an toàn thông tin. Trong dự án, cả Raspberry Pi và máy tính đều cài đặt và kết nối vào cùng một mạng ZeroTier, cho phép giao tiếp ổn định và bảo mật. 

_Hình 1.6: Giao diện quản lý mạng ZeroTier._ 

11 

## **1.5 Phần mềm GX Works** 

GX Works là phần mềm lập trình và giám sát PLC của hãng Mitsubishi Electric, hỗ trợ các dòng PLC MELSEC. GX Works 3 là phiên bản mới nhất, cung cấp môi trường lập trình thống nhất cho nhiều dòng PLC khác nhau. Phần mềm hỗ trợ nhiều ngôn ngữ lập trình theo chuẩn IEC 61131-3 bao gồm Ladder Diagram, Structured Text, Function Block Diagram và Sequential Function Chart. 

Trong dự án, GX Works được sử dụng để viết chương trình điều khiển motor (khởi động, dừng, đảo chiều, điều chỉnh tốc độ), đồng thời theo dõi trạng thái các thanh ghi và relay nội bộ của PLC trong quá trình vận hành. Kết hợp với Raspberry Pi, dữ liệu từ các thanh ghi PLC được đọc và truyền về Unity. 

_Hình 1.7: Giao diện Gx Works2._ 

## **Tiểu kết chương 1** 

Chương 1 đã trình bày những cơ sở lý thuyết nền tảng của dự án VR Digital Twin, bao gồm khái niệm và ứng dụng của Digital Twin trong công nghiệp, công nghệ thực tế ảo và vai trò của Unity, nguyên lý hoạt động của PLC và motor, cơ chế kết nối mạng thông qua Raspberry Pi và ZeroTier, cũng như phần mềm lập trình PLC GX Works. Những kiến thức này tạo nền tảng vững chắc cho việc phân tích, thiết kế và triển khai hệ thống sẽ được trình bày ở các chương tiếp theo. 

12 

## **CHƯƠNG 2: PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG** 

## **2.1 Yêu cầu hệ thống** 

## **2.1.1 Yêu cầu chức năng** 

Hệ thống VR Digital Twin cần đáp ứng các yêu cầu chức năng chính sau đây: 

- Mô phỏng trực quan: Hiển thị mô hình 3D của PLC và motor trong môi trường Unity, phản ánh trạng thái vật lý thực tế. 

- Điều khiển motor: Người dùng có thể khởi động, dừng, đảo chiều quay và điều chỉnh tốc độ motor thông qua bảng điều khiển trong Unity. 

- Hiển thị số liệu thời gian thực: Bảng dashboard hiển thị các thông số như vòng quay (RPM), chiều quay (thuận/nghịch), trạng thái hoạt động. 

- Truyền dữ liệu hai chiều: Lệnh điều khiển từ Unity được gửi xuống PLC qua Raspberry Pi; dữ liệu trạng thái từ PLC được đọc và cập nhật lên Unity. 

- Hiển thị hình ảnh camera: Camera thực tế ghi lại hình ảnh motor đang vận hành và stream trực tiếp vào môi trường Unity. 

- Kết nối mạng ảo: Raspberry Pi và máy tính kết nối thông qua mạng ZeroTier, cho phép hoạt động ở các vị trí địa lý khác nhau. 

## **2.1.2 Yêu cầu phi chức năng** 

Ngoài các yêu cầu chức năng, hệ thống cần đáp ứng một số yêu cầu phi chức năng quan trọng: 

- Độ trễ thấp: Thời gian từ khi người dùng ra lệnh điều khiển đến khi motor thực tế phản hồi cần được tối thiểu hóa, lý tưởng dưới 200ms. 

- Ổn định: Hệ thống cần hoạt động liên tục mà không bị mất kết nối hoặc treo trong suốt quá trình vận hành. 

- Tính trực quan: Giao diện Unity cần thân thiện, dễ sử dụng với cả người dùng không có chuyên môn kỹ thuật sâu. 

- Bảo mật: Kết nối ZeroTier đảm bảo mã hóa dữ liệu, ngăn chặn truy cập trái phép vào hệ thống điều khiển. 

## **2.2 Kiến trúc tổng thể hệ thống** 

Hệ thống VR Digital Twin được tổ chức theo mô hình phân tầng gồm ba lớp chính: lớp phần cứng (Hardware Layer), lớp truyền thông (Communication Layer) và lớp ứng dụng (Application Layer). 

Lớp phần cứng bao gồm bộ máy PLC Mitsubishi kết nối với motor điện xoay chiều thông qua biến tần, Raspberry Pi kết nối với PLC qua cổng truyền thông, và camera USB/IP được gắn hướng vào motor để ghi hình thực tế. 

Lớp truyền thông sử dụng ZeroTier để tạo mạng ảo overlay giữa Raspberry Pi và máy tính chủ. Trên nền tảng này, giao thức TCP/IP hoặc MQTT được dùng để truyền dữ liệu điều khiển và trạng thái theo thời gian thực. Stream hình ảnh từ camera được truyền qua giao thức RTSP hoặc WebSocket. 

Lớp ứng dụng là môi trường Unity chạy trên máy tính, bao gồm các scene mô phỏng, script C# xử lý truyền thông và giao diện người dùng. 

13 

Hình 2.1: Sơ đồ kiến trúc tổng thể hệ thống VR Digital Twin. 

## **2.3 Thiết kế các module chức năng** 

## **2.3.1 Module giao tiếp PLC – Raspberry Pi** 

Module này chạy trên Raspberry Pi, thực hiện hai nhiệm vụ chính: đọc dữ liệu từ PLC (polling) và ghi lệnh điều khiển xuống PLC (writing). Thư viện pymcprotocol (MC Protocol) hoặc pymodbus được sử dụng để giao tiếp với PLC Mitsubishi qua cổng Ethernet. Dữ liệu đọc về bao gồm giá trị thanh ghi D (Data Register) chứa tốc độ RPM, trạng thái các relay M (coil) biểu thị chiều quay và trạng thái hoạt động. Chu kỳ polling được cấu hình ở 100ms để đảm bảo dữ liệu luôn cập nhật kịp thời. 

## **2.3.2 Module truyền thông ZeroTier** 

Module này thiết lập kênh truyền thông hai chiều giữa Raspberry Pi và máy tính chạy Unity qua mạng ZeroTier. Một server socket chạy trên Raspberry Pi lắng nghe lệnh đến từ Unity và gửi gói dữ liệu trạng thái lên Unity theo chu kỳ. Định dạng dữ liệu sử dụng JSON để dễ dàng parse ở cả hai phía. Ví dụ gói dữ liệu trạng thái: {"rpm": 1450, "direction": "forward", "running": true}. 

## **2.3.3 Module điều khiển trong Unity** 

Module này được xây dựng bằng C# trong Unity, đảm nhiệm các chức năng nhận dữ liệu từ Raspberry Pi qua socket và cập nhật trạng thái mô hình 3D, xử lý tương tác người dùng trên bảng điều khiển (nút bấm, thanh trượt tốc độ), gửi lệnh điều khiển về Raspberry Pi, và cập nhật các giá trị hiển thị trên bảng số liệu (dashboard). Script NetworkManager.cs quản lý kết nối socket; MotorController.cs xử lý animation và trạng thái mô hình 3D; UIController.cs quản lý giao diện người dùng. 

## **2.3.4 Module camera** 

Camera được kết nối với Raspberry Pi (camera module hoặc USB webcam). OpenCV trên Raspberry Pi xử lý và nén hình ảnh trước khi stream về máy tính. Trong Unity, hình ảnh nhận được được render lên một RenderTexture và hiển thị trên một màn hình ảo (quad mesh) đặt trong scene, cho phép người dùng quan sát motor thực tế ngay trong môi trường VR. 

14 

## **2.4 Thiết kế giao diện Unity** 

## **2.4.1 Bảng điều khiển (Control Panel)** 

Bảng điều khiển được thiết kế trực quan, gồm các thành phần: nút Start/Stop để khởi động và dừng motor; nút đảo chiều (Forward/Reverse) để đổi hướng quay; ô trống để điền tốc độ. Tất cả các thành phần được thiết kế theo phong cách công nghiệp, dễ nhận biết và thao tác. 

_Hình 2.2: Thiết kế giao diện bảng điều khiển motor trong Unity._ 

## **2.4.2 Bảng hiển thị số liệu (Dashboard)** 

Dashboard hiển thị các thông số vận hành của motor theo thời gian thực, bao gồm: đồng hồ tốc  độ (speedometer)  dạng  analog  và  digital  hiển  thị RPM;  chỉ báo  chiều  quay (Forward/Reverse); biểu đồ lịch sử tốc độ theo thời gian; và trạng thái kết nối hệ thống (PLC, Raspberry Pi, mạng ZeroTier). Các giá trị được cập nhật theo chu kỳ 100ms từ dữ liệu nhận qua socket. 

_Hình 2.3: Thiết kế giao diện bảng hiển thị số liệu_ . 

## **2.5 Biểu đồ tuần tự** 

## **2.5.1 Biểu đồ tuần tự: Luồng điều khiển motor từ Unity** 

Khi người dùng thao tác trên bảng điều khiển Unity, luồng xử lý diễn ra như sau: (1) Người dùng nhấn nút điều khiển trên giao diện Unity; (2) Script UIController.cs ghi nhận sự kiện và gọi hàm SendCommand() trong NetworkManager.cs; (3) NetworkManager.cs đóng gói lệnh thành JSON và gửi qua socket ZeroTier đến Raspberry Pi; (4) Raspberry Pi nhận lệnh, parse JSON và gọi hàm ghi thanh ghi PLC qua MC Protocol; (5) PLC thực thi lệnh và điều khiển biến tần, motor thay đổi trạng thái; (6) Phản hồi trạng thái mới được đọc lại trong chu kỳ polling tiếp theo và hiển thị trên Unity. 

15 

Hình 2.4: Biểu đồ tuần tự – Luồng điều khiển motor từ Unity. 

## **2.5.2 Biểu đồ Use Case tổng quan** 

Hệ thống có một tác nhân chính là Người vận hành (Operator) với các use case: Xem mô phỏng 3D, Điều khiển motor, Giám sát số liệu, Xem hình ảnh camera thực tế, và Kết nối/Ngắt kết nối hệ thống. 

## Hình 2.5: Biểu đồ Use Case tổng quan hệ thống. 

## **Tiểu kết Chương 2** 

Chương 2 đã trình bày toàn bộ quá trình phân tích yêu cầu và thiết kế hệ thống VR Digital Twin. Kiến trúc ba tầng (phần cứng – truyền thông – ứng dụng) được xác định rõ ràng với các thành phần và trách nhiệm cụ thể. Bốn module chức năng chính (giao tiếp PLCRaspberry Pi, truyền thông ZeroTier, điều khiển Unity, và camera) được thiết kế chi tiết với giao thức và định dạng dữ liệu cụ thể. Các biểu đồ Use Case và tuần tự đã làm rõ luồng xử lý của hệ thống. Những thiết kế này là cơ sở để triển khai thực tế trong Chương 3. 

16 

## **CHƯƠNG 3: XÂY DỰNG DỰ ÁN VR DIGITAL TWIN** 

## **3.1 Thiết lập hệ thống phần cứng** 

Hệ thống phần cứng của mô hình VR Digital Twin bao gồm các thành phần chính: bộ điều khiển PLC Mitsubishi, động cơ điện xoay chiều (AC Motor), biến tần (Inverter), Raspberry Pi và camera giám sát. 

PLC Mitsubishi đóng vai trò trung tâm điều khiển, thực hiện các logic điều khiển motor dựa trên chương trình được lập trình bằng phần mềm GX Works. PLC được kết nối với biến tần để điều chỉnh tốc độ và chiều quay của motor. Các tín hiệu điều khiển như Start/Stop, Forward/Reverse và giá trị tốc độ được truyền từ PLC đến biến tần thông qua các ngõ ra số và analog. 

Raspberry Pi được sử dụng như một thiết bị gateway trung gian, kết nối trực tiếp với PLC thông qua cổng Ethernet. Thiết bị này đảm nhiệm việc thu thập dữ liệu trạng thái từ PLC và truyền dữ liệu lên hệ thống Unity, đồng thời nhận lệnh điều khiển từ Unity và gửi xuống PLC. 

_Hình 3.1: Hình ảnh thực tế bộ máy PLC kết nối với motor._ 

Camera được lắp đặt hướng trực tiếp vào motor nhằm ghi lại hình ảnh vận hành thực tế. Camera có thể là USB webcam hoặc camera IP, kết nối với Raspberry Pi để xử lý và truyền hình ảnh. 

Toàn bộ hệ thống phần cứng được lắp đặt theo sơ đồ đảm bảo tính ổn định, an toàn điện và thuận tiện cho việc quan sát và bảo trì. 

## **3.2 Kết nối mạng qua ZeroTier** 

Để đảm bảo khả năng kết nối từ xa giữa Raspberry Pi và máy tính chạy Unity, hệ thống sử dụng nền tảng mạng ảo ZeroTier. 

17 

_Hình 3.2: Sơ đồ đấu dây Raspberry Pi với hệ thống PLC-Motor_ 

Trước tiên, một mạng ZeroTier được tạo thông qua giao diện quản lý trên nền web. Sau đó, cả Raspberry Pi và máy tính đều được cài đặt phần mềm ZeroTier và tham gia vào cùng một mạng ảo thông qua Network ID. Sau khi kết nối thành công, mỗi thiết bị sẽ được cấp một địa chỉ IP ảo, cho phép giao tiếp như trong cùng một mạng LAN. 

_Hình 3.3: Cấu hình mạng ZeroTier_ 

Ưu điểm của ZeroTier là không cần cấu hình port forwarding hay VPN truyền thống, đồng thời đảm bảo tính bảo mật nhờ cơ chế mã hóa end-to-end. Điều này giúp hệ thống có thể hoạt động ổn định ngay cả khi các thiết bị ở các vị trí địa lý khác nhau. 

Sau khi thiết lập thành công, việc kiểm tra kết nối được thực hiện thông qua lệnh ping giữa hai thiết bị để đảm bảo đường truyền thông suốt. 

## **3.3 Lập trình PLC với GX Works** 

PLC Mitsubishi được lập trình bằng phần mềm GX Works 3 với ngôn ngữ Ladder Diagram. 

Chương trình PLC được thiết kế để thực hiện các chức năng chính bao gồm: 

- Khởi động và dừng motor 

- Đảo chiều quay (Forward/Reverse) 

- Điều chỉnh tốc độ thông qua biến tần 

- Gửi dữ liệu trạng thái về Raspberry Pi 

18 

Các thanh ghi dữ liệu (Data Register – D) được sử dụng để lưu trữ giá trị tốc độ (RPM), trong khi các relay nội (M) được dùng để biểu diễn trạng thái hoạt động và chiều quay của motor. 

Ví dụ: 

- M0: Trạng thái chạy/dừng 

- M1: Chiều quay thuận 

- D0: Giá trị tốc độ 

_Hình 3.4: Ladder Diagram_ 

Chương trình PLC cũng được thiết kế để đảm bảo an toàn, bao gồm các điều kiện liên động nhằm tránh xung đột trạng thái như chạy đồng thời hai chiều. 

Sau khi lập trình, chương trình được nạp vào PLC và kiểm tra thông qua chế độ monitoring trong GX Works. 

## **3.4 Xây dựng môi trường mô phỏng Unity** 

Môi trường mô phỏng được xây dựng bằng Unity với các thành phần chính bao gồm mô hình 3D, hệ thống điều khiển và giao diện hiển thị. 

Mô hình 3D của PLC và motor được thiết kế hoặc import từ các thư viện có sẵn, sau đó được đặt trong một scene mô phỏng. Các animation được thiết lập để thể hiện trạng thái quay của motor tương ứng với dữ liệu thực tế. 

19 

_Hình 3.5: Mô hình 3D motor + PLC_ 

Hệ thống script C# được phát triển để xử lý truyền thông và điều khiển, bao gồm: 

- NetworkManager.cs: quản lý kết nối socket với Raspberry Pi 

- MotorController.cs: điều khiển animation và trạng thái mô hình 

- UIController.cs: xử lý tương tác người dùng 

Giao diện người dùng được thiết kế gồm hai phần chính: 

- Control Panel: cho phép người dùng gửi lệnh điều khiển 

- Dashboard: hiển thị thông số thời gian thực như RPM, trạng thái và chiều quay 

## _Hình 3.6: Control Panel + Dashboard_ 

Dữ liệu nhận được từ Raspberry Pi được parse từ định dạng JSON và cập nhật liên tục lên giao diện và mô hình 3D với chu kỳ khoảng 100ms. 

## **3.5 Tích hợp camera thực tế** 

Camera được sử dụng để tăng tính chân thực cho hệ thống Digital Twin bằng cách hiển thị hình ảnh thực tế của motor trong môi trường Unity. 

Trên Raspberry Pi, OpenCV được sử dụng để thu thập và xử lý hình ảnh từ camera. Dữ liệu video được mã hóa và truyền qua giao thức RTSP. 

Trong Unity, stream video được nhận và hiển thị thông qua một đối tượng RenderTexture. Hình ảnh này được gán lên một bề mặt (quad) trong scene, đóng vai trò như một màn hình ảo. 

20 

_Hình 3.7: Camera hiển thị trong Unity_ 

Việc tích hợp camera giúp người dùng có thể đồng thời quan sát cả mô hình ảo và hệ thống thực, từ đó nâng cao độ tin cậy và khả năng giám sát của hệ thống. 

## **Tiểu kết chương 3** 

Chương 3 đã trình bày chi tiết quá trình triển khai hệ thống VR Digital Twin từ phần cứng đến phần mềm. Các bước thiết lập PLC, Raspberry Pi, kết nối mạng ZeroTier, xây dựng môi trường Unity và tích hợp camera đã được thực hiện đồng bộ. Kết quả đạt được là một hệ thống hoàn chỉnh có khả năng điều khiển và giám sát motor trong thời gian thực thông qua môi trường thực tế ảo. 

21 

## **CHƯƠNG 4: KẾT LUẬN** 

## **4.1 Tóm tắt thành tựu** 

Dự án VR Digital Twin đã xây dựng thành công một hệ thống mô phỏng và điều khiển bộ máy PLC-Motor trong môi trường thực tế ảo. Hệ thống cho phép người dùng giám sát trạng thái và điều khiển motor từ xa thông qua giao diện Unity. 

Các thành tựu chính bao gồm: 

- Xây dựng mô hình Digital Twin đồng bộ thời gian thực 

- Thiết lập hệ thống truyền thông hai chiều giữa Unity và PLC 

- Tích hợp thành công mạng ZeroTier cho kết nối từ xa 

- Xây dựng giao diện trực quan và dễ sử dụng 

- Tích hợp camera để tăng tính chân thực 

## **4.2 Bài học rút ra** 

Trong quá trình thực hiện dự án, nhóm đã rút ra nhiều kinh nghiệm quan trọng: 

- Hiểu rõ hơn về kiến trúc hệ thống Digital Twin và IoT 

- Nắm vững cách giao tiếp với PLC thông qua giao thức công nghiệp 

- Kỹ năng làm việc với Unity trong xây dựng hệ thống mô phỏng 

- Kinh nghiệm xử lý dữ liệu thời gian thực và tối ưu độ trễ 

- Kỹ năng làm việc nhóm và phân chia nhiệm vụ 

## **4.3 Hạn chế của dự án** 

Bên cạnh những kết quả đạt được, dự án vẫn còn một số hạn chế: 

- Độ trễ hệ thống vẫn chưa tối ưu hoàn toàn trong một số trường hợp mạng yếu 

- Giao diện Unity chưa hỗ trợ đầy đủ thiết bị VR chuyên dụng 

- Hệ thống chưa tích hợp các thuật toán phân tích dữ liệu nâng cao 

- Chưa có cơ chế lưu trữ dữ liệu lịch sử dài hạn 

## **4.4 Hướng phát triển trong tương lai** 

Trong tương lai, hệ thống có thể được mở rộng theo các hướng sau: 

- Tích hợp AI để dự đoán lỗi và bảo trì thông minh 

- Phát triển phiên bản VR hoàn chỉnh sử dụng kính thực tế ảo 

- Mở rộng hệ thống cho nhiều thiết bị công nghiệp khác 

- Xây dựng hệ thống lưu trữ dữ liệu và phân tích lịch sử 

- Triển khai trên nền tảng cloud để tăng khả năng mở rộng 

22 

## **4.5 Lời kết** 

Dự án VR Digital Twin là một bước thử nghiệm quan trọng trong việc kết hợp giữa công nghệ điều khiển công nghiệp và thực tế ảo. Mặc dù còn nhiều hạn chế, hệ thống đã chứng minh được tính khả thi và tiềm năng ứng dụng trong thực tế. 

Nhóm hy vọng rằng trong tương lai, mô hình này có thể được phát triển và ứng dụng rộng rãi hơn trong các lĩnh vực sản xuất thông minh và đào tạo kỹ thuật. 

## **TÀI LIỆU THAM KHẢO** 

[1] Grieves, M. (2014). Digital twin: Manufacturing excellence through virtual factory replication. White Paper, 1(2014), 1-7. 

[2] Mitsubishi Electric. (2023). MELSEC iQ-F FX5 User's Manual. Mitsubishi Electric Corporation. 

[3] Mitsubishi Electric. (2023). GX Works3 Operating Manual. Mitsubishi Electric Corporation. 

[4] ZeroTier, Inc. (2024). ZeroTier Documentation. https://docs.zerotier.com/ 

[5] Raspberry Pi Foundation. (2024). Raspberry Pi Documentation. https://www.raspberrypi.com/documentation/ 

[6] Unity Technologies. (2024). Unity Documentation. https://docs.unity3d.com/Manual/index.html 

[7] Tao, F., Sui, F., Liu, A., Qi, Q., Zhang, M., Song, B., ... & Nee, A. Y. C. (2019). Digital twin-driven product design framework. International Journal of Production Research, 57(12), 3935-3953. 

[8] Nguyen, T. H., & Le, V. T. (2022). Ứng dụng Digital Twin trong giám sát hệ thống sản xuất tự động. Tạp chí Khoa học và Công nghệ, 60(4), 45-52. 

[9] OpenCV Team. (2024). OpenCV Documentation. https://docs.opencv.org/ 

[10] Pilehvar, M., & Sanei, S. (2021). IoT-based real-time monitoring system using Raspberry Pi and MQTT protocol. Journal of Internet of Things, 12(3), 234-248. 

23 

