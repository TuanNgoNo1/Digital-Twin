# HƯỚNG DẪN THIẾT LẬP URL RIÊNG CHO BÀI 3 - NHÓM TRƯỜNG BÌNH

## 1. Mục đích

Tài liệu này hướng dẫn nhóm Bài 3 dùng chung IP public và Caddy của hệ thống:

```text
103.238.69.131:8080
```

nhưng có URL, PLC, gateway và MQTT topic riêng, không xung đột với hệ thống hiện tại.

URL được đề xuất cho nhóm Bài 3:

```text
Base URL:  http://103.238.69.131:8080/plc2
Health:    http://103.238.69.131:8080/plc2/health
Telemetry: http://103.238.69.131:8080/plc2/telemetry
Control:   http://103.238.69.131:8080/plc2/control
```

Đây là **URL theo đường dẫn trên IP public**, chưa phải tên miền DNS thực sự. Nếu sau này có tên miền, có thể thay phần:

```text
http://103.238.69.131:8080
```

bằng:

```text
https://ten-mien-cua-he-thong
```

và giữ nguyên đường dẫn `/plc2`.

## 2. Những thành phần tuyệt đối không được dùng trùng

Hệ thống đang có các tuyến:

| Đường dẫn | Thành phần hiện tại |
|---|---|
| `/` | Java/Spring Boot |
| `/plc/*` | Gateway PLC Bài 1, COM3, port nội bộ `5000` |
| `/rs485/*` | Telemetry Bài 2, COM5, port nội bộ `5002` |
| `/cam1/*`, `/cam2/*` | Camera |
| `/gxworks2/*` | Guacamole/GX Works2 |

Nhóm Bài 3 không được sửa hoặc tái sử dụng:

```text
/plc
/rs485
127.0.0.1:5000
127.0.0.1:5002
COM3
COM5
```

Nhóm Bài 3 nên dành riêng:

```text
Public path:  /plc2/*
HTTP gateway: 127.0.0.1:5003
PLC port:     một COM hoặc địa chỉ Ethernet riêng
MQTT topics:  lab/bai3/truong-binh/...
```

Port `5003` chỉ là đề xuất. Trước khi dùng phải kiểm tra nó chưa bị tiến trình khác chiếm.

Do nhóm Bài 3 vẫn dùng cùng IP public và cùng cổng `8080`, router không cần tạo thêm NAT/port-forward. Rule hiện tại:

```text
103.238.69.131:8080
→ 10.170.43.240:8080
→ Caddy
```

vẫn được giữ nguyên. Caddy phân biệt các nhóm bằng phần đường dẫn phía sau.

Nếu PLC Bài 3 nằm tại một máy chủ khác, public request vẫn đi vào máy Caddy `10.170.43.240`. Khi đó gateway/MQTT của nhóm Bài 3 phải kết nối được về máy này qua LAN, VPN hoặc kết nối outbound phù hợp; không thể NAT cùng một public port `8080` trực tiếp tới hai máy LAN khác nhau.

## 3. Kiến trúc đề xuất

```text
PLC Bài 3
   ↕ giao thức riêng của PLC/Ethernet/Serial
MQTT PLC Adapter
   ↕ MQTT
MQTT Broker (Mosquitto hoặc broker tương đương)
   ↕ MQTT nội bộ
Bai3 MQTT-HTTP Gateway tại 127.0.0.1:5003
   ↕ HTTP JSON
Caddy tại 10.170.43.240:8080
   ↕ Internet
Unity Editor hoặc Unity WebGL
```

Luồng điều khiển:

```text
Unity
→ POST /plc2/control
→ Caddy
→ MQTT-HTTP Gateway :5003
→ publish MQTT command
→ MQTT PLC Adapter
→ PLC Bài 3
```

Luồng phản hồi:

```text
PLC Bài 3
→ MQTT PLC Adapter
→ publish MQTT telemetry
→ MQTT-HTTP Gateway lưu mẫu mới nhất
→ GET /plc2/telemetry
→ Caddy
→ Unity
```

### Vì sao nên có MQTT-HTTP Gateway?

Unity WebGL chạy trong trình duyệt nên không thể dùng kết nối TCP MQTT thông thường tới cổng `1883`. Muốn MQTT trực tiếp từ WebGL phải dùng MQTT over WebSocket và xử lý thêm xác thực, reconnect, CORS/origin và TLS.

Để hoạt động giống hệ thống hiện tại, phương án đơn giản và ổn định hơn là:

- PLC/gateway phần cứng vẫn sử dụng MQTT.
- MQTT broker không public trực tiếp ra Internet.
- Unity chỉ gọi HTTP JSON qua Caddy.
- Caddy tiếp tục dùng duy nhất public port `8080`.

Không đặt trong Unity:

```text
mqtt://103.238.69.131:8080
```

vì port `8080` hiện là HTTP của Caddy, không phải raw MQTT broker.

## 4. Chuẩn hóa MQTT riêng cho nhóm Bài 3

### 4.1 Topic

Đề xuất sử dụng:

```text
lab/bai3/truong-binh/plc/telemetry
lab/bai3/truong-binh/plc/status
lab/bai3/truong-binh/plc/command
lab/bai3/truong-binh/plc/ack
```

Không sử dụng topic chung chung như:

```text
plc/telemetry
plc/control
motor/status
```

vì các topic đó dễ bị trùng với nhóm khác.

### 4.2 Quy tắc MQTT

| Topic | Hướng | QoS đề xuất | Retain |
|---|---|---:|---|
| `.../telemetry` | PLC Adapter → Gateway | `0` hoặc `1` | Không |
| `.../status` | PLC Adapter → Gateway | `1` | Có |
| `.../command` | Gateway → PLC Adapter | `1` | Không |
| `.../ack` | PLC Adapter → Gateway | `1` | Không |

Mỗi tiến trình phải có MQTT client ID riêng:

```text
bai3-truong-binh-plc-adapter
bai3-truong-binh-http-gateway
```

Không để hai tiến trình đang chạy dùng cùng client ID vì broker sẽ ngắt kết nối client cũ.

### 4.3 Payload telemetry đề xuất

```json
{
  "deviceId": "plc-bai3-truong-binh",
  "sequence": 15231,
  "timestamp": "2026-07-30T10:30:15.420Z",
  "connected": true,
  "running": true,
  "direction": "forward",
  "speedRpm": 10.2,
  "encoderCount": 12530,
  "rotationsExact": 2.506,
  "angle": 182.16
}
```

Ý nghĩa:

- `deviceId`: xác định đúng PLC của nhóm.
- `sequence`: số thứ tự tăng dần để phát hiện frame cũ hoặc mất frame.
- `timestamp`: thời điểm lấy dữ liệu, không phải thời điểm Unity vẽ giao diện.
- `connected`: PLC Adapter có đang giao tiếp được với PLC hay không.
- `speedRpm`: tốc độ phản hồi thực tế.
- `encoderCount`: số xung encoder thực.
- `rotationsExact`: số vòng chính xác, có phần thập phân.
- `angle`: góc hiện tại trong khoảng `0..360`.

Nếu PLC không có encoder thì phải đổi tên trường cho đúng bản chất, ví dụ:

```text
setSpeedRpm
estimatedRotations
commandedAngle
```

Không được gọi dữ liệu đặt là dữ liệu phản hồi thực tế.

### 4.4 Payload lệnh đề xuất

```json
{
  "commandId": "bai3-20260730-000125",
  "deviceId": "plc-bai3-truong-binh",
  "timestamp": "2026-07-30T10:30:20.000Z",
  "action": "START",
  "direction": "forward",
  "speedRpm": 10,
  "rotations": 2
}
```

PLC Adapter nên gửi xác nhận:

```json
{
  "commandId": "bai3-20260730-000125",
  "deviceId": "plc-bai3-truong-binh",
  "accepted": true,
  "executed": true,
  "message": "PLC acknowledged command",
  "timestamp": "2026-07-30T10:30:20.180Z"
}
```

`commandId` giúp chống xử lý lặp khi MQTT QoS 1 gửi lại message.

## 5. Cài và cấu hình MQTT Broker

Có thể dùng Mosquitto hoặc broker tương đương. Không bật anonymous broker trên Internet.

### Trường hợp A: PLC Adapter và broker chạy cùng máy server

Chỉ cho broker lắng nghe loopback:

```text
listener 1883 127.0.0.1
allow_anonymous false
password_file C:\duong-dan-bao-mat\mosquitto.passwd
persistence true
```

### Trường hợp B: PLC Adapter là thiết bị khác trong cùng LAN

Broker có thể lắng nghe trên IP LAN của server:

```text
listener 1883 10.170.43.240
allow_anonymous false
password_file C:\duong-dan-bao-mat\mosquitto.passwd
persistence true
```

Khi đó:

- Đặt IP tĩnh riêng cho PLC Adapter.
- Windows Firewall chỉ cho IP của PLC Adapter truy cập TCP `1883`.
- Không NAT/public cổng `1883` ra Internet.
- Không dùng chung username/password với nhóm khác.

### Trường hợp C: PLC Adapter nằm ngoài mạng LAN

Không mở raw MQTT `1883` thẳng ra Internet. Chọn một trong các phương án:

1. VPN site-to-site/Tailscale cho chính gateway phần cứng.
2. MQTT over TLS tại cổng `8883`.
3. MQTT over WebSocket có TLS và xác thực.

Việc Unity không cần cài Tailscale không đồng nghĩa broker hoặc PLC Adapter phải được public không bảo vệ.

## 6. Xây dựng MQTT-HTTP Gateway riêng

Gateway Bài 3 phải:

1. Kết nối MQTT broker bằng tài khoản riêng.
2. Subscribe:

   ```text
   lab/bai3/truong-binh/plc/telemetry
   lab/bai3/truong-binh/plc/status
   lab/bai3/truong-binh/plc/ack
   ```

3. Publish lệnh vào:

   ```text
   lab/bai3/truong-binh/plc/command
   ```

4. Chạy HTTP chỉ trên:

   ```text
   127.0.0.1:5003
   ```

5. Cung cấp tối thiểu ba endpoint:

   ```text
   GET  /health
   GET  /telemetry
   POST /control
   ```

### 6.1 `GET /health`

Kết quả đề xuất:

```json
{
  "ok": true,
  "mqttConnected": true,
  "plcOnline": true,
  "lastTelemetryAgeMs": 124
}
```

Chỉ báo `plcOnline=true` nếu telemetry mới hơn ngưỡng cho phép, ví dụ `3000 ms`.

HTTP `200` nhưng timestamp cũ không được coi là PLC đang online.

### 6.2 `GET /telemetry`

Trả mẫu telemetry MQTT mới nhất:

```json
{
  "deviceId": "plc-bai3-truong-binh",
  "sequence": 15231,
  "timestamp": "2026-07-30T10:30:15.420Z",
  "connected": true,
  "running": true,
  "direction": "forward",
  "speedRpm": 10.2,
  "encoderCount": 12530,
  "rotationsExact": 2.506,
  "angle": 182.16,
  "backendSynced": true,
  "backendStatus": "MQTT AND PLC ONLINE"
}
```

Nếu dữ liệu quá hạn, gateway phải trả rõ:

```json
{
  "connected": false,
  "backendSynced": false,
  "backendStatus": "PLC TELEMETRY STALE"
}
```

Không được giữ `connected=true` mãi chỉ vì gateway vẫn đang chạy.

### 6.3 `POST /control`

Gateway thực hiện:

1. Kiểm tra `deviceId`, action và giới hạn thông số.
2. Kiểm tra PLC đang online.
3. Tạo hoặc xác nhận `commandId`.
4. Publish MQTT với QoS 1 và `retain=false`.
5. Chờ `ack` đúng `commandId` nếu quy trình yêu cầu xác nhận.
6. Trả trạng thái rõ ràng cho Unity.

Ví dụ phản hồi khi PLC xác nhận:

```json
{
  "accepted": true,
  "executed": true,
  "commandId": "bai3-20260730-000125"
}
```

Nếu chỉ publish thành công nhưng chưa có PLC ACK, nên trả HTTP `202 Accepted`, không khẳng định motor đã chạy.

### 6.4 An toàn điều khiển

Gateway phải giới hạn:

- RPM tối thiểu và tối đa.
- Số vòng/góc tối đa cho một lệnh.
- Chỉ chấp nhận danh sách action cho phép.
- Timeout lệnh.
- Một người/phiên điều khiển PLC tại một thời điểm.
- Lệnh STOP an toàn khi phiên hết hạn nếu thiết kế phần cứng cho phép.
- Ghi log `commandId`, người dùng, thời gian và kết quả ACK.

Không đưa mật khẩu MQTT vào Unity/WebGL vì người dùng có thể xem mã và traffic của trình duyệt.

## 7. Kiểm tra port nội bộ trước khi cấu hình Caddy

Trên máy server, mở PowerShell:

```powershell
Get-NetTCPConnection -State Listen |
    Where-Object LocalPort -EQ 5003
```

Nếu không có kết quả thì port `5003` đang rảnh. Sau khi chạy gateway:

```powershell
Test-NetConnection 127.0.0.1 -Port 5003
curl.exe http://127.0.0.1:5003/health
curl.exe http://127.0.0.1:5003/telemetry
```

Phải kiểm tra local thành công trước khi sửa Caddy. Caddy không thể sửa lỗi MQTT, PLC hoặc gateway nội bộ.

## 8. Thêm route riêng vào Caddy

Caddyfile hiện nằm tại:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile
```

Trước khi sửa, tạo bản sao:

```powershell
Copy-Item `
  'D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile' `
  'D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile.before-bai3.bak'
```

Trong khối `(app_routes)`, thêm đoạn sau **trước khối `handle` cuối cùng đang reverse proxy về Spring Boot**:

```caddyfile
	handle /plc2 {
		redir * /plc2/health 302
	}

	handle /plc2/ {
		redir * /plc2/health 302
	}

	handle_path /plc2/* {
		@preflight method OPTIONS
		handle @preflight {
			import corsheaders
			respond "" 204
		}

		import corsheaders
		reverse_proxy 127.0.0.1:5003 {
			header_down -Access-Control-Allow-Origin
			header_down -Access-Control-Allow-Headers
			header_down -Access-Control-Allow-Methods
		}
	}
```

`handle_path` sẽ bỏ tiền tố `/plc2` trước khi chuyển request vào gateway:

```text
Public:
/plc2/telemetry

Gateway nhận:
/telemetry
```

Không sửa các khối `/plc/*` và `/rs485/*`.

### 8.1 Validate Caddyfile

Trên server hiện tại, Caddy được cài bằng WinGet. Có thể validate bằng:

```powershell
$caddyExe = Join-Path $env:LOCALAPPDATA `
  'Microsoft\WinGet\Packages\CaddyServer.Caddy_Microsoft.Winget.Source_8wekyb3d8bbwe\caddy.exe'

& $caddyExe validate `
  --config 'D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Caddyfile' `
  --adapter caddyfile
```

Chỉ restart Caddy nếu kết quả validate thành công.

Caddy hiện đặt `admin off`, do đó không dựa vào `caddy reload`. Phải restart đúng tiến trình Caddy theo quy trình vận hành của server. Script khởi động hiện tại là:

```text
D:\MIGRATION_2026-06-29\Windows_Readable\proxy\Start-Caddy.ps1
```

Việc restart Caddy làm gián đoạn ngắn toàn bộ `/`, `/plc`, `/rs485`, camera và các route khác, vì vậy thực hiện khi motor đang ở trạng thái an toàn và không có sinh viên đang thao tác.

### 8.2 Kiểm tra sau khi restart

Kiểm tra từ chính server:

```powershell
curl.exe http://10.170.43.240:8080/plc2/health
curl.exe http://10.170.43.240:8080/plc2/telemetry
```

Kiểm tra các route cũ vẫn hoạt động:

```powershell
curl.exe http://10.170.43.240:8080/plc/health
curl.exe http://10.170.43.240:8080/rs485/health
```

Sau đó dùng một máy ngoài LAN, ví dụ mạng 4G:

```text
http://103.238.69.131:8080/plc2/health
http://103.238.69.131:8080/plc2/telemetry
```

Không chỉ test public URL từ cùng mạng LAN vì router có thể không hỗ trợ NAT loopback.

## 9. Cấu hình Unity

### 9.1 Các URL cần đặt

```text
Base URL:          http://103.238.69.131:8080/plc2
Telemetry Endpoint: /telemetry
Control Endpoint:   /control
```

URL hoàn chỉnh:

```text
GET  http://103.238.69.131:8080/plc2/telemetry
POST http://103.238.69.131:8080/plc2/control
```

Không đặt:

```text
http://103.238.69.131:8080/plc
http://103.238.69.131:8080/rs485
http://127.0.0.1:5003
```

Trong máy sinh viên, `127.0.0.1` là máy sinh viên chứ không phải server.

### 9.2 DTO telemetry mẫu

```csharp
using System;

[Serializable]
public class Bai3Telemetry
{
    public string deviceId;
    public int sequence;
    public string timestamp;
    public bool connected;
    public bool running;
    public string direction;
    public float speedRpm;
    public int encoderCount;
    public float rotationsExact;
    public float angle;
    public bool backendSynced;
    public string backendStatus;
}
```

Tên field C# phải giống tên key JSON nếu dùng `JsonUtility`.

### 9.3 Poll telemetry mẫu

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public partial class Bai3PlcClient : MonoBehaviour
{
    [SerializeField]
    private string baseUrl =
        "http://103.238.69.131:8080/plc2";

    [SerializeField] private float pollInterval = 0.5f;
    [SerializeField] private int timeoutSeconds = 3;

    public Bai3Telemetry LatestTelemetry { get; private set; }
    public bool IsOnline { get; private set; }

    private IEnumerator Start()
    {
        while (true)
        {
            yield return ReadTelemetry();
            yield return new WaitForSecondsRealtime(pollInterval);
        }
    }

    private IEnumerator ReadTelemetry()
    {
        using UnityWebRequest request =
            UnityWebRequest.Get(baseUrl.TrimEnd('/') + "/telemetry");

        request.timeout = timeoutSeconds;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            IsOnline = false;
            Debug.LogWarning("Bai3 telemetry error: " + request.error);
            yield break;
        }

        Bai3Telemetry telemetry =
            JsonUtility.FromJson<Bai3Telemetry>(
                request.downloadHandler.text);

        if (telemetry == null)
        {
            IsOnline = false;
            yield break;
        }

        LatestTelemetry = telemetry;
        IsOnline = telemetry.connected && telemetry.backendSynced;
    }
}
```

Trong bản production nên kiểm tra thêm:

- `deviceId` đúng PLC Bài 3.
- `sequence` hoặc `timestamp` đang thay đổi.
- Timestamp không cũ quá `2–3 giây`.
- Khi mất telemetry, HMI báo offline và motor ảo dừng.

### 9.4 Gửi lệnh mẫu

```csharp
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class Bai3Command
{
    public string commandId;
    public string deviceId;
    public string timestamp;
    public string action;
    public string direction;
    public float speedRpm;
    public float rotations;
}

public partial class Bai3PlcClient
{
    public IEnumerator SendStart(
        float speedRpm,
        float rotations,
        string direction)
    {
        Bai3Command command = new Bai3Command
        {
            commandId = Guid.NewGuid().ToString("N"),
            deviceId = "plc-bai3-truong-binh",
            timestamp = DateTime.UtcNow.ToString("o"),
            action = "START",
            direction = direction,
            speedRpm = speedRpm,
            rotations = rotations
        };

        string json = JsonUtility.ToJson(command);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request =
            new UnityWebRequest(
                baseUrl.TrimEnd('/') + "/control",
                "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("Bai3 control error: " + request.error);
        else
            Debug.Log("Bai3 control response: " +
                      request.downloadHandler.text);
    }
}
```

Không hiển thị “motor đã chạy” ngay sau khi HTTP request thành công. Chỉ chuyển sang trạng thái đang chạy khi:

- Gateway nhận ACK từ đúng PLC; hoặc
- Telemetry mới trả về `running=true`.

### 9.5 Unity Player Settings

Vì hệ thống hiện dùng HTTP, trong Unity đặt:

```text
Edit
→ Project Settings
→ Player
→ WebGL
→ Other Settings
→ Configuration
→ Insecure HTTP Option
→ Always Allowed
```

Tên mục có thể hiển thị là `Allow downloads over HTTP` ở một số phiên bản Unity.

Đây chỉ là cấu hình tạm thời cho HTTP. Khi chuyển sang HTTPS/domain phải đổi URL Unity sang `https://`.

### 9.6 CORS và mixed content

Caddy hiện trả CORS cho `GET`, `POST`, `OPTIONS`. Nếu WebGL được tải bằng HTTPS nhưng API vẫn là HTTP, trình duyệt sẽ chặn do mixed content.

Do đó:

```text
WebGL chạy HTTP  → API HTTP có thể hoạt động.
WebGL chạy HTTPS → API cũng phải HTTPS.
```

Khi có domain HTTPS, tốt nhất dùng cùng origin:

```text
https://lab.example.edu.vn/plc2/telemetry
```

## 10. Trình tự kiểm thử bắt buộc

### Bước 1 - MQTT

- Broker chạy.
- PLC Adapter kết nối đúng tài khoản.
- Topic telemetry thay đổi liên tục.
- `deviceId`, `sequence` và timestamp đúng.
- Gửi command thử nghiệm an toàn và nhận đúng ACK.

### Bước 2 - HTTP Gateway local

```powershell
curl.exe http://127.0.0.1:5003/health
curl.exe http://127.0.0.1:5003/telemetry
```

POST một lệnh không gây chuyển động trước:

```powershell
curl.exe `
  -X POST `
  -H 'Content-Type: application/json' `
  -d '{"commandId":"test-health-1","deviceId":"plc-bai3-truong-binh","action":"STATUS"}' `
  http://127.0.0.1:5003/control
```

### Bước 3 - Caddy qua LAN

```powershell
curl.exe http://10.170.43.240:8080/plc2/health
curl.exe http://10.170.43.240:8080/plc2/telemetry
```

### Bước 4 - Public Internet

Test bằng mạng 4G hoặc mạng ngoài:

```text
http://103.238.69.131:8080/plc2/health
http://103.238.69.131:8080/plc2/telemetry
```

### Bước 5 - Unity Editor

- Inspector dùng đúng Base URL.
- Play Mode báo online.
- Telemetry thay đổi theo PLC thật.
- Rút kết nối PLC hoặc dừng publisher thì HMI chuyển offline trong tối đa `3 giây`.
- Không gửi lệnh motor thật trước khi vùng máy an toàn.

### Bước 6 - Unity WebGL

- Build lại sau khi đổi URL.
- Mở Developer Tools của trình duyệt.
- Không có lỗi CORS.
- Không có lỗi mixed content.
- Request `/telemetry` trả `200`.
- Timestamp/sequence thay đổi, không chỉ lặp lại JSON cũ.
- Lệnh điều khiển nhận ACK đúng `commandId`.

## 11. Khởi động cùng Windows và vận hành lâu dài

Các thành phần Bài 3 cần tự khởi động theo thứ tự:

```text
1. MQTT Broker
2. MQTT PLC Adapter
3. MQTT-HTTP Gateway :5003
4. Caddy
```

Mỗi thành phần nên chạy bằng Windows Service hoặc Scheduled Task với:

- Start at boot, không phụ thuộc người dùng đăng nhập.
- Restart on failure.
- Log riêng.
- Không mở cửa sổ console tương tác.
- Health check định kỳ.

Tên service/task phải riêng, ví dụ:

```text
Bai3TruongBinh-MqttAdapter
Bai3TruongBinh-HttpGateway
```

Log phải tách riêng:

```text
logs\bai3-truong-binh-mqtt.log
logs\bai3-truong-binh-gateway.log
```

Không sửa script khởi động Bài 1/Bài 2 trước khi có backup và kiểm tra hồi quy.

## 12. Bảo mật trước khi cho sinh viên sử dụng

Public API hiện dùng HTTP qua IP nên không được coi là an toàn cho điều khiển motor thật. Trước khi triển khai rộng cần:

1. HTTPS và domain.
2. Xác thực token/JWT tại Caddy, backend hoặc gateway.
3. Phân ca/lease: chỉ một sinh viên điều khiển một PLC tại một thời điểm.
4. Rate limit.
5. Giới hạn RPM, góc và số vòng tại gateway, không chỉ ở Unity.
6. Audit log cho từng command.
7. Timeout và cơ chế STOP an toàn.
8. Nút dừng khẩn cấp vật lý.
9. Không public MQTT username/password trong WebGL.
10. Không public trực tiếp port `1883`, `5003` hoặc COM/PLC.

URL khác nhau chỉ chống xung đột định tuyến; **URL riêng không phải cơ chế bảo mật**.

## 13. Checklist bàn giao cho nhóm Bài 3

- [ ] PLC Bài 3 có cổng/giao tiếp riêng, không dùng COM3 hoặc COM5.
- [ ] MQTT client ID riêng.
- [ ] MQTT username/password riêng.
- [ ] Topic bắt đầu bằng `lab/bai3/truong-binh/`.
- [ ] Gateway chỉ bind `127.0.0.1:5003`.
- [ ] `/health`, `/telemetry`, `/control` hoạt động local.
- [ ] Caddy có route `/plc2/*`.
- [ ] Các route `/plc/*` và `/rs485/*` vẫn hoạt động sau thay đổi.
- [ ] Public URL được test từ mạng ngoài.
- [ ] Unity dùng đúng Base URL Bài 3.
- [ ] WebGL đã bật HTTP tạm thời hoặc đã chuyển toàn hệ thống sang HTTPS.
- [ ] HMI xác định offline dựa trên timestamp mới, không chỉ HTTP `200`.
- [ ] Control chỉ báo thành công sau ACK hoặc telemetry xác nhận.
- [ ] Đã test giới hạn lệnh và dừng an toàn.
- [ ] Broker, adapter và gateway tự khởi động lại khi lỗi.
- [ ] Đã có log và tài liệu rollback.

## 14. Cấu hình cuối cùng đề xuất

```text
PUBLIC
http://103.238.69.131:8080/plc2

CADDY
/plc2/* -> 127.0.0.1:5003

MQTT TOPICS
lab/bai3/truong-binh/plc/telemetry
lab/bai3/truong-binh/plc/status
lab/bai3/truong-binh/plc/command
lab/bai3/truong-binh/plc/ack

UNITY
Base URL          = http://103.238.69.131:8080/plc2
Telemetry Endpoint = /telemetry
Control Endpoint   = /control

KHÔNG DÙNG TRÙNG
/plc
/rs485
5000
5002
COM3
COM5
```

Với cấu trúc này, nhóm Bài 3 dùng chung IP public và cổng `8080`, nhưng dữ liệu, PLC, MQTT, gateway và URL đều được tách khỏi Bài 1/Bài 2.

