using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace PlcLab.ModbusGateway
{
    internal sealed class ModbusRtuClient : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly string portName;
        private readonly int baudRate;
        private readonly byte slaveId;
        private readonly int timeoutMs;
        private readonly bool debug;
        private SerialPort serial;

        public ModbusRtuClient(string portName, int baudRate, byte slaveId, int timeoutMs, bool debug)
        {
            this.portName = portName;
            this.baudRate = baudRate;
            this.slaveId = slaveId;
            this.timeoutMs = timeoutMs;
            this.debug = debug;
        }

        public ushort[] ReadHoldingRegisters(ushort startAddress, ushort quantity)
        {
            if (quantity < 1 || quantity > 125)
                throw new ArgumentOutOfRangeException("quantity");

            byte[] response = Transact(BuildRequest(0x03, startAddress, quantity));
            int byteCount = response[2];
            if (byteCount != quantity * 2)
                throw new InvalidOperationException("Unexpected register byte count: " + byteCount);

            ushort[] values = new ushort[quantity];
            for (int index = 0; index < quantity; index++)
                values[index] = (ushort)((response[3 + index * 2] << 8) | response[4 + index * 2]);
            return values;
        }

        public bool[] ReadCoils(ushort startAddress, ushort quantity)
        {
            if (quantity < 1 || quantity > 2000)
                throw new ArgumentOutOfRangeException("quantity");

            byte[] response = Transact(BuildRequest(0x01, startAddress, quantity));
            bool[] values = new bool[quantity];
            for (int index = 0; index < quantity; index++)
                values[index] = (response[3 + index / 8] & (1 << (index % 8))) != 0;
            return values;
        }

        private byte[] BuildRequest(byte function, ushort startAddress, ushort quantity)
        {
            byte[] request = new byte[8];
            request[0] = slaveId;
            request[1] = function;
            request[2] = (byte)(startAddress >> 8);
            request[3] = (byte)(startAddress & 0xFF);
            request[4] = (byte)(quantity >> 8);
            request[5] = (byte)(quantity & 0xFF);
            ushort crc = ComputeCrc(request, 0, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)(crc >> 8);
            return request;
        }

        private byte[] Transact(byte[] request)
        {
            lock (syncRoot)
            {
                try
                {
                    EnsureOpen();
                    serial.DiscardInBuffer();
                    serial.DiscardOutBuffer();
                    if (debug)
                        Console.WriteLine("TX: " + ToHex(request));
                    serial.Write(request, 0, request.Length);

                    byte[] header = ReadExact(3);
                    if (header[0] != slaveId)
                        throw new InvalidOperationException("Unexpected slave ID: " + header[0]);

                    if ((header[1] & 0x80) != 0)
                    {
                        byte[] tail = ReadExact(2);
                        byte[] errorFrame = Join(header, tail);
                        ValidateCrc(errorFrame);
                        throw new InvalidOperationException(
                            "Modbus exception " + header[2] + " for function " + (header[1] & 0x7F));
                    }

                    if (header[1] != request[1])
                        throw new InvalidOperationException("Unexpected function code: " + header[1]);

                    byte[] bodyAndCrc = ReadExact(header[2] + 2);
                    byte[] response = Join(header, bodyAndCrc);
                    ValidateCrc(response);
                    if (debug)
                        Console.WriteLine("RX: " + ToHex(response));
                    return response;
                }
                catch
                {
                    CloseSerial();
                    throw;
                }
            }
        }

        private void EnsureOpen()
        {
            if (serial != null && serial.IsOpen)
                return;

            serial = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serial.Handshake = Handshake.None;
            serial.ReadTimeout = timeoutMs;
            serial.WriteTimeout = timeoutMs;
            serial.Open();
        }

        private byte[] ReadExact(int count)
        {
            byte[] result = new byte[count];
            for (int index = 0; index < count; index++)
            {
                int value = serial.ReadByte();
                if (value < 0)
                    throw new InvalidOperationException("Serial port closed while receiving a frame");
                result[index] = (byte)value;
            }
            return result;
        }

        private static byte[] Join(byte[] first, byte[] second)
        {
            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        private static void ValidateCrc(byte[] frame)
        {
            if (frame.Length < 4)
                throw new InvalidOperationException("Modbus response is too short");

            ushort expected = ComputeCrc(frame, 0, frame.Length - 2);
            ushort actual = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
            if (actual != expected)
                throw new InvalidOperationException(
                    "CRC mismatch: received=" + actual.ToString("X4") +
                    " expected=" + expected.ToString("X4"));
        }

        private static ushort ComputeCrc(byte[] bytes, int offset, int count)
        {
            ushort crc = 0xFFFF;
            for (int index = offset; index < offset + count; index++)
            {
                crc ^= bytes[index];
                for (int bit = 0; bit < 8; bit++)
                    crc = (ushort)(((crc & 1) != 0) ? ((crc >> 1) ^ 0xA001) : (crc >> 1));
            }
            return crc;
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace('-', ' ');
        }

        private void CloseSerial()
        {
            if (serial == null)
                return;
            try
            {
                if (serial.IsOpen)
                    serial.Close();
            }
            finally
            {
                serial.Dispose();
                serial = null;
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
                CloseSerial();
        }
    }

    internal sealed class Gateway
    {
        private const int RegisterStart = 100;
        private const int RegisterCount = 66;
        private const double EncoderPulsesPerRevolution = 5000.0;

        private readonly string host;
        private readonly int httpPort;
        private readonly string serialPort;
        private readonly int baudRate;
        private readonly byte slaveId;
        private readonly ModbusRtuClient client;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private volatile bool running = true;

        public Gateway(string host, int httpPort, string serialPort, int baudRate, byte slaveId, int timeoutMs, bool debug)
        {
            this.host = host;
            this.httpPort = httpPort;
            this.serialPort = serialPort;
            this.baudRate = baudRate;
            this.slaveId = slaveId;
            client = new ModbusRtuClient(serialPort, baudRate, slaveId, timeoutMs, debug);
        }

        public void Serve()
        {
            IPAddress bindAddress = IPAddress.Parse(host);
            TcpListener listener = new TcpListener(bindAddress, httpPort);
            listener.Start();
            Console.WriteLine("Modbus RTU gateway listening on http://" + host + ":" + httpPort + "/");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1, slave=" + slaveId + ", read-only=true");

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs args)
            {
                args.Cancel = true;
                running = false;
                listener.Stop();
                client.Dispose();
            };

            while (running)
            {
                try
                {
                    using (TcpClient connection = listener.AcceptTcpClient())
                        Handle(connection);
                }
                catch (SocketException)
                {
                    if (!running)
                        break;
                    throw;
                }
            }
        }

        private void Handle(TcpClient connection)
        {
            NetworkStream stream = connection.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 8192, true);
            string requestLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            string[] requestParts = requestLine.Split(' ');
            if (requestParts.Length < 2)
            {
                Respond(stream, 400, Error("Malformed HTTP request"));
                return;
            }

            string method = requestParts[0].ToUpperInvariant();
            string path = requestParts[1].Split('?')[0].TrimEnd('/').ToLowerInvariant();
            string header;
            while (!string.IsNullOrEmpty(header = reader.ReadLine()))
            {
            }

            if (method == "OPTIONS")
            {
                Respond(stream, 204, null);
                return;
            }

            try
            {
                if (method == "GET" && path == "/health")
                    Respond(stream, 200, Health());
                else if (method == "GET" && path == "/telemetry")
                    Respond(stream, 200, Telemetry(false));
                else if (method == "GET" && path == "/debug")
                    Respond(stream, 200, Telemetry(true));
                else if (method == "POST" && path == "/control")
                    Respond(stream, 423, Error("Modbus RTU gateway is read-only"));
                else
                    Respond(stream, 404, Error("Not found"));
            }
            catch (Exception exception)
            {
                Dictionary<string, object> payload = Error(exception.Message);
                payload["gateway"] = "modbus-rtu";
                payload["backendSynced"] = false;
                payload["backendStatus"] = "OFFLINE: " + exception.Message;
                Respond(stream, 503, payload);
            }
        }

        private Dictionary<string, object> Health()
        {
            return new Dictionary<string, object>
            {
                { "gateway", "modbus-rtu" },
                { "serialPort", serialPort },
                { "baudRate", baudRate },
                { "dataBits", 8 },
                { "parity", "None" },
                { "stopBits", 1 },
                { "slaveId", slaveId },
                { "allowWrites", false }
            };
        }

        private Dictionary<string, object> Telemetry(bool includeRaw)
        {
            ushort[] registers = client.ReadHoldingRegisters(RegisterStart, RegisterCount);
            bool[] coils = client.ReadCoils(0, 18);

            int pulseFrequency = GetInt32(registers, 100);
            int targetPulses = GetInt32(registers, 104);
            int encoderCount = GetInt32(registers, 120);
            int speedRawD164 = GetInt16(registers, 164);
            double rotationsExact = encoderCount / EncoderPulsesPerRevolution;
            double speedRpm = speedRawD164 * 600.0 / EncoderPulsesPerRevolution;
            bool reverse = speedRawD164 < 0 || GetCoil(coils, 8);
            bool running = GetCoil(coils, 11) || Math.Abs(speedRpm) > 0.5;

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "runId", "" },
                { "lessonId", "TH2" },
                { "userId", "" },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "action", "" },
                { "running", running },
                { "speedRpm", speedRpm },
                { "setSpeedRpm", GetUInt16(registers, 128) * 60.0 / EncoderPulsesPerRevolution },
                { "pulseFrequency", pulseFrequency },
                { "count", targetPulses },
                { "rotations", GetUInt16(registers, 124) },
                { "angle", NormalizeAngle(rotationsExact * 360.0) },
                { "encoderCount", encoderCount },
                { "rotationsExact", rotationsExact },
                { "pulsesPerSample", speedRawD164 },
                { "speedRawD164", speedRawD164 },
                { "motionMode", "telemetry" },
                { "direction", reverse ? "reverse" : "forward" },
                { "backendSynced", true },
                { "backendStatus", "SYNCED" }
            };

            if (includeRaw)
            {
                Dictionary<string, object> raw = new Dictionary<string, object>();
                int[] addresses = { 100, 104, 110, 112, 114, 120, 124, 128, 146, 164 };
                foreach (int address in addresses)
                    raw["D" + address] = GetUInt16(registers, address);
                for (int address = 0; address < coils.Length; address++)
                    raw["M" + address] = coils[address];
                payload["raw"] = raw;
            }

            return payload;
        }

        private ushort GetUInt16(ushort[] registers, int address)
        {
            return registers[address - RegisterStart];
        }

        private int GetInt16(ushort[] registers, int address)
        {
            return unchecked((short)GetUInt16(registers, address));
        }

        private int GetInt32(ushort[] registers, int address)
        {
            uint low = GetUInt16(registers, address);
            uint high = GetUInt16(registers, address + 1);
            return unchecked((int)(low | (high << 16)));
        }

        private static bool GetCoil(bool[] coils, int address)
        {
            return address >= 0 && address < coils.Length && coils[address];
        }

        private static double NormalizeAngle(double angle)
        {
            double result = angle % 360.0;
            return result < 0 ? result + 360.0 : result;
        }

        private static Dictionary<string, object> Error(string message)
        {
            return new Dictionary<string, object> { { "error", message } };
        }

        private void Respond(Stream stream, int status, object payload)
        {
            byte[] body = payload == null
                ? new byte[0]
                : Encoding.UTF8.GetBytes(json.Serialize(payload));
            string reason = status == 200 ? "OK"
                : status == 204 ? "No Content"
                : status == 400 ? "Bad Request"
                : status == 404 ? "Not Found"
                : status == 423 ? "Locked"
                : "Service Unavailable";
            string headers = "HTTP/1.1 " + status + " " + reason + "\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (body.Length > 0)
                stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        public void Test(ushort address)
        {
            Console.WriteLine("COM5 Modbus RTU read-only test");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1, slave=" + slaveId);
            Console.WriteLine("Request: FC03, holding register " + address + " (PLC D" + address + ")");
            ushort value = client.ReadHoldingRegisters(address, 1)[0];
            Console.WriteLine("D" + address + " unsigned=" + value + ", signed=" + unchecked((short)value));
            Console.WriteLine("MODBUS_RTU_READ=PASS");
        }

        public void Probe(ushort address, int attempts)
        {
            Console.WriteLine("COM5 Modbus RTU repeated read probe");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1, slave=" + slaveId);
            Console.WriteLine("Request: FC03, holding register " + address + " (PLC D" + address + ")");
            Console.WriteLine("Attempts=" + attempts + "; watch the DT-5119 TXD/RXD LEDs");

            Exception lastError = null;
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                Console.WriteLine("Attempt " + attempt + "/" + attempts);
                try
                {
                    ushort value = client.ReadHoldingRegisters(address, 1)[0];
                    Console.WriteLine("D" + address + " unsigned=" + value + ", signed=" + unchecked((short)value));
                    Console.WriteLine("MODBUS_RTU_READ=PASS");
                    return;
                }
                catch (Exception exception)
                {
                    lastError = exception;
                    Console.WriteLine("No response: " + exception.Message);
                    if (attempt < attempts)
                        Thread.Sleep(500);
                }
            }

            throw new IOException("No Modbus response after " + attempts + " attempts", lastError);
        }
    }

    internal static class Program
    {
        private static string Env(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static int EnvInt(string name, int fallback)
        {
            int parsed;
            return int.TryParse(Env(name, fallback.ToString()), out parsed) ? parsed : fallback;
        }

        private static int RunLoopback(string serialPort, int baudRate, int timeoutMs)
        {
            byte[] expected = Encoding.ASCII.GetBytes("PLC-LAB2-DTECH-LOOPBACK\r\n");
            byte[] received = new byte[expected.Length];

            Console.WriteLine("DTech DT-5119 isolated loopback test");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1");
            Console.WriteLine("Required wiring: T/R+ <-> RXD+, T/R- <-> RXD-");
            Console.WriteLine("PLC must be disconnected from the DTech terminal board.");

            using (SerialPort serial = new SerialPort(serialPort, baudRate, Parity.None, 8, StopBits.One))
            {
                serial.Handshake = Handshake.None;
                serial.ReadTimeout = timeoutMs;
                serial.WriteTimeout = timeoutMs;
                serial.Open();
                serial.DiscardInBuffer();
                serial.DiscardOutBuffer();

                Console.WriteLine("TX: " + BitConverter.ToString(expected).Replace('-', ' '));
                serial.Write(expected, 0, expected.Length);

                for (int index = 0; index < received.Length; index++)
                    received[index] = (byte)serial.ReadByte();
            }

            Console.WriteLine("RX: " + BitConverter.ToString(received).Replace('-', ' '));
            for (int index = 0; index < expected.Length; index++)
            {
                if (expected[index] != received[index])
                {
                    Console.Error.WriteLine("DTECH_LOOPBACK=FAIL (received data differs at byte " + index + ")");
                    return 3;
                }
            }

            Console.WriteLine("DTECH_LOOPBACK=PASS");
            return 0;
        }

        private static bool ContainsSequence(List<byte> received, byte[] expected)
        {
            if (received.Count < expected.Length)
                return false;

            for (int start = 0; start <= received.Count - expected.Length; start++)
            {
                bool matches = true;
                for (int index = 0; index < expected.Length; index++)
                {
                    if (received[start + index] != expected[index])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return true;
            }

            return false;
        }

        private static int RunListen(string serialPort, int baudRate, int timeoutMs, int listenSeconds)
        {
            byte[] expected = Encoding.ASCII.GetBytes("PLC2\r\n");
            List<byte> received = new List<byte>();
            DateTime deadline = DateTime.UtcNow.AddSeconds(listenSeconds);

            Console.WriteLine("FX3U-485-BD non-protocol receive-only test");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1");
            Console.WriteLine("Listening for PLC2 followed by CR/LF.");
            Console.WriteLine("Now pulse M500 OFF -> ON once in GX Works2.");

            using (SerialPort serial = new SerialPort(serialPort, baudRate, Parity.None, 8, StopBits.One))
            {
                serial.Handshake = Handshake.None;
                serial.ReadTimeout = Math.Max(100, Math.Min(timeoutMs, 500));
                serial.Open();
                serial.DiscardInBuffer();

                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        int value = serial.ReadByte();
                        if (value < 0)
                            continue;

                        received.Add((byte)value);
                        Console.WriteLine(
                            "RX byte: " + value.ToString("X2") +
                            "  " + (value >= 32 && value <= 126 ? ((char)value).ToString() : "."));

                        if (ContainsSequence(received, expected))
                        {
                            Console.WriteLine("RX HEX: " + BitConverter.ToString(received.ToArray()).Replace('-', ' '));
                            Console.WriteLine("FX3U_RS485_TX=PASS");
                            return 0;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Keep listening until the overall test deadline.
                    }
                }
            }

            if (received.Count > 0)
                Console.WriteLine("RX HEX: " + BitConverter.ToString(received.ToArray()).Replace('-', ' '));
            Console.Error.WriteLine(
                received.Count == 0
                    ? "FX3U_RS485_TX=FAIL: no bytes received"
                    : "FX3U_RS485_TX=FAIL: bytes received, but PLC2 CR/LF was not found");
            return 3;
        }

        private static ushort ReadFrameWord(byte[] frame, int wordIndex, bool littleEndian)
        {
            int offset = wordIndex * 2;
            return littleEndian
                ? (ushort)(frame[offset] | (frame[offset + 1] << 8))
                : (ushort)((frame[offset] << 8) | frame[offset + 1]);
        }

        private static int RunTelemetryFrameListen(
            string serialPort,
            int baudRate,
            int timeoutMs,
            int listenSeconds)
        {
            const int frameLength = 20;
            List<byte> pending = new List<byte>();
            DateTime deadline = DateTime.UtcNow.AddSeconds(listenSeconds);

            Console.WriteLine("FX3U-485-BD telemetry frame test");
            Console.WriteLine("Serial=" + serialPort + ", " + baudRate + "/8N1");
            Console.WriteLine("Expected: 10-word frame starting with HAA55.");
            Console.WriteLine("Waiting for the PLC telemetry frame (automatic 1-second trigger).");

            using (SerialPort serial = new SerialPort(serialPort, baudRate, Parity.None, 8, StopBits.One))
            {
                serial.Handshake = Handshake.None;
                serial.ReadTimeout = Math.Max(100, Math.Min(timeoutMs, 500));
                serial.Open();
                serial.DiscardInBuffer();

                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        int value = serial.ReadByte();
                        if (value < 0)
                            continue;
                        pending.Add((byte)value);
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }

                    while (pending.Count >= 2)
                    {
                        bool littleEndianHeader = pending[0] == 0x55 && pending[1] == 0xAA;
                        bool bigEndianHeader = pending[0] == 0xAA && pending[1] == 0x55;
                        if (!littleEndianHeader && !bigEndianHeader)
                        {
                            pending.RemoveAt(0);
                            continue;
                        }

                        if (pending.Count < frameLength)
                            break;

                        byte[] frame = pending.GetRange(0, frameLength).ToArray();
                        bool littleEndian = littleEndianHeader;
                        ushort version = ReadFrameWord(frame, 1, littleEndian);
                        uint encoderCount =
                            ReadFrameWord(frame, 2, littleEndian) |
                            ((uint)ReadFrameWord(frame, 3, littleEndian) << 16);
                        short pulsesPer100Ms = unchecked((short)ReadFrameWord(frame, 4, littleEndian));
                        ushort setRpm = ReadFrameWord(frame, 5, littleEndian);
                        ushort pulseFrequency = ReadFrameWord(frame, 6, littleEndian);
                        bool forward = ReadFrameWord(frame, 7, littleEndian) != 0;
                        bool reverse = ReadFrameWord(frame, 8, littleEndian) != 0;
                        bool running = ReadFrameWord(frame, 9, littleEndian) != 0;
                        double speedRpm = pulsesPer100Ms * 600.0 / 5000.0;
                        double rotations = encoderCount / 5000.0;
                        double angle = (encoderCount % 5000) * 360.0 / 5000.0;

                        Console.WriteLine("RX HEX: " + BitConverter.ToString(frame).Replace('-', ' '));
                        Console.WriteLine("version=" + version);
                        Console.WriteLine("encoderCount=" + encoderCount);
                        Console.WriteLine("pulsesPer100Ms=" + pulsesPer100Ms);
                        Console.WriteLine("speedRpm=" + speedRpm.ToString("F2"));
                        Console.WriteLine("setRpm=" + setRpm);
                        Console.WriteLine("pulseFrequency=" + pulseFrequency);
                        Console.WriteLine("running=" + running);
                        Console.WriteLine("direction=" + (reverse ? "reverse" : (forward ? "forward" : "unknown")));
                        Console.WriteLine("rotationsExact=" + rotations.ToString("F4"));
                        Console.WriteLine("angle=" + angle.ToString("F2"));
                        Console.WriteLine("FX3U_TELEMETRY_FRAME=PASS");
                        return version == 1 ? 0 : 4;
                    }
                }
            }

            if (pending.Count > 0)
                Console.WriteLine("RX HEX: " + BitConverter.ToString(pending.ToArray()).Replace('-', ' '));
            Console.Error.WriteLine("FX3U_TELEMETRY_FRAME=FAIL: complete HAA55 frame was not received");
            return 3;
        }

        public static int Main(string[] args)
        {
            string serialPort = Env("MODBUS_SERIAL_PORT", "COM5");
            int baudRate = EnvInt("MODBUS_BAUD_RATE", 38400);
            byte slaveId = (byte)EnvInt("MODBUS_SLAVE_ID", 3);
            int timeoutMs = EnvInt("MODBUS_TIMEOUT_MS", 1500);
            string host = Env("MODBUS_HTTP_HOST", "127.0.0.1");
            int httpPort = EnvInt("MODBUS_HTTP_PORT", 5002);
            bool isTest = args.Length > 0 && args[0].Equals("test", StringComparison.OrdinalIgnoreCase);
            bool isProbe = args.Length > 0 && args[0].Equals("probe", StringComparison.OrdinalIgnoreCase);
            bool isLoopback = args.Length > 0 && args[0].Equals("loopback", StringComparison.OrdinalIgnoreCase);
            bool isListen = args.Length > 0 && args[0].Equals("listen", StringComparison.OrdinalIgnoreCase);
            bool isTelemetryFrame = args.Length > 0 &&
                args[0].Equals("telemetry-frame", StringComparison.OrdinalIgnoreCase);
            bool debug = isTest || isProbe || Env("MODBUS_DEBUG", "0") == "1";

            if (isLoopback)
            {
                try
                {
                    return RunLoopback(serialPort, baudRate, timeoutMs);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("DTECH_LOOPBACK=FAIL: " + exception.Message);
                    return 2;
                }
            }

            if (isListen)
            {
                try
                {
                    int listenSeconds = 60;
                    if (args.Length > 1 &&
                        (!int.TryParse(args[1], out listenSeconds) || listenSeconds < 1 || listenSeconds > 600))
                        throw new ArgumentException("Listen seconds must be from 1 to 600");
                    return RunListen(serialPort, baudRate, timeoutMs, listenSeconds);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("FX3U_RS485_TX=FAIL: " + exception.Message);
                    return 2;
                }
            }

            if (isTelemetryFrame)
            {
                try
                {
                    int listenSeconds = 60;
                    if (args.Length > 1 &&
                        (!int.TryParse(args[1], out listenSeconds) || listenSeconds < 1 || listenSeconds > 600))
                        throw new ArgumentException("Listen seconds must be from 1 to 600");
                    return RunTelemetryFrameListen(serialPort, baudRate, timeoutMs, listenSeconds);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("FX3U_TELEMETRY_FRAME=FAIL: " + exception.Message);
                    return 2;
                }
            }

            Gateway gateway = new Gateway(host, httpPort, serialPort, baudRate, slaveId, timeoutMs, debug);
            try
            {
                if (isTest)
                {
                    ushort address = 500;
                    if (args.Length > 1 && !ushort.TryParse(args[1].TrimStart('D', 'd'), out address))
                        throw new ArgumentException("Invalid D register: " + args[1]);
                    gateway.Test(address);
                    return 0;
                }

                if (isProbe)
                {
                    ushort address = 500;
                    if (args.Length > 1 && !ushort.TryParse(args[1].TrimStart('D', 'd'), out address))
                        throw new ArgumentException("Invalid D register: " + args[1]);
                    int attempts = 10;
                    if (args.Length > 2 && (!int.TryParse(args[2], out attempts) || attempts < 1 || attempts > 100))
                        throw new ArgumentException("Attempts must be from 1 to 100");
                    gateway.Probe(address, attempts);
                    return 0;
                }

                gateway.Serve();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("MODBUS_RTU_ERROR: " + exception.Message);
                return 2;
            }
        }
    }
}
