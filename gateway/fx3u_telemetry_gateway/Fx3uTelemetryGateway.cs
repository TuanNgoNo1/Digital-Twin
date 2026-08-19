using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace PlcLab.Fx3uTelemetry
{
    internal sealed class TelemetrySnapshot
    {
        public DateTime ReceivedUtc;
        public int EncoderCount;
        public short PulsesPer100Ms;
        public ushort SetRpm;
        public ushort PulseFrequency;
        public bool Forward;
        public bool Reverse;
        public bool Running;
        public double SpeedRpm;
        public string FrameHex;
    }

    internal sealed class Gateway : IDisposable
    {
        private const int FrameLength = 20;
        private const double EncoderPulsesPerRevolution = 5000.0;

        private readonly string host;
        private readonly int httpPort;
        private readonly string portName;
        private readonly int baudRate;
        private readonly int staleSeconds;
        private readonly object stateLock = new object();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private readonly List<byte> pending = new List<byte>();
        private readonly List<byte> recentBytes = new List<byte>();

        private SerialPort serial;
        private TcpListener listener;
        private Thread serialThread;
        private volatile bool running;
        private TelemetrySnapshot latest;
        private long receivedByteCount;
        private byte lastByte;
        private DateTime? lastByteUtc;
        private string serialError = "";
        private int? previousEncoderCount;
        private DateTime? previousFrameUtc;

        public Gateway(string host, int httpPort, string portName, int baudRate, int staleSeconds)
        {
            this.host = host;
            this.httpPort = httpPort;
            this.portName = portName;
            this.baudRate = baudRate;
            this.staleSeconds = staleSeconds;
        }

        public void Serve()
        {
            running = true;
            OpenSerial();

            serialThread = new Thread(ReadSerial);
            serialThread.IsBackground = true;
            serialThread.Name = "FX3U COM5 telemetry reader";
            serialThread.Start();

            listener = new TcpListener(IPAddress.Parse(host), httpPort);
            listener.Start();

            Console.WriteLine("FX3U non-protocol telemetry gateway");
            Console.WriteLine("Serial=" + portName + ", " + baudRate + "/8N1, receive-only=true");
            Console.WriteLine("HTTP=http://" + host + ":" + httpPort + "/");
            Console.WriteLine("Endpoints: /health, /telemetry, /debug");

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs args)
            {
                args.Cancel = true;
                Stop();
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
                    if (running)
                        throw;
                }
            }
        }

        private void OpenSerial()
        {
            serial = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serial.Handshake = Handshake.None;
            serial.ReadTimeout = 500;
            serial.Open();
            serial.DiscardInBuffer();
        }

        private void ReadSerial()
        {
            while (running)
            {
                try
                {
                    int value = serial.ReadByte();
                    if (value < 0)
                        continue;

                    lock (stateLock)
                    {
                        receivedByteCount++;
                        lastByte = (byte)value;
                        lastByteUtc = DateTime.UtcNow;
                        serialError = "";
                        pending.Add((byte)value);
                        recentBytes.Add((byte)value);
                        if (recentBytes.Count > 64)
                            recentBytes.RemoveAt(0);
                        ParseFrames();
                    }
                }
                catch (TimeoutException)
                {
                }
                catch (Exception exception)
                {
                    lock (stateLock)
                        serialError = exception.Message;
                    if (running)
                        Thread.Sleep(500);
                }
            }
        }

        private void ParseFrames()
        {
            while (pending.Count >= 2)
            {
                bool littleEndian = pending[0] == 0x55 && pending[1] == 0xAA;
                bool bigEndian = pending[0] == 0xAA && pending[1] == 0x55;
                bool compactHeader = pending[0] == 0x50 && pending[1] == 0x4C;
                if (!littleEndian && !bigEndian && !compactHeader)
                {
                    pending.RemoveAt(0);
                    continue;
                }

                if (compactHeader)
                {
                    if (TryParseCompactFrame())
                        continue;
                    return;
                }

                if (pending.Count < FrameLength)
                    return;

                byte[] frame = pending.GetRange(0, FrameLength).ToArray();
                ushort version = ReadWord(frame, 1, littleEndian);
                if (version != 1)
                {
                    pending.RemoveAt(0);
                    continue;
                }

                uint encoder =
                    ReadWord(frame, 2, littleEndian) |
                    ((uint)ReadWord(frame, 3, littleEndian) << 16);

                latest = new TelemetrySnapshot
                {
                    ReceivedUtc = DateTime.UtcNow,
                    EncoderCount = unchecked((int)encoder),
                    PulsesPer100Ms = unchecked((short)ReadWord(frame, 4, littleEndian)),
                    SetRpm = ReadWord(frame, 5, littleEndian),
                    PulseFrequency = ReadWord(frame, 6, littleEndian),
                    Forward = ReadWord(frame, 7, littleEndian) != 0,
                    Reverse = ReadWord(frame, 8, littleEndian) != 0,
                    Running = ReadWord(frame, 9, littleEndian) != 0,
                    SpeedRpm = Math.Abs(unchecked((short)ReadWord(frame, 4, littleEndian))) *
                        600.0 / EncoderPulsesPerRevolution,
                    FrameHex = BitConverter.ToString(frame).Replace('-', ' ')
                };

                Console.WriteLine(
                    "FRAME " + latest.ReceivedUtc.ToLocalTime().ToString("HH:mm:ss") +
                    " rpm=" + CalculateRpm(latest).ToString("F1") +
                    " encoder=" + latest.EncoderCount +
                    " direction=" + Direction(latest));
                pending.RemoveRange(0, FrameLength);
            }

            if (pending.Count > 256)
                pending.RemoveRange(0, pending.Count - 2);
        }

        private bool TryParseCompactFrame()
        {
            const int compactLength = 9;
            if (pending.Count < compactLength)
                return false;
            if (pending[7] != 0x0D || pending[8] != 0x0A)
                return false;

            byte[] frame = pending.GetRange(0, compactLength).ToArray();
            byte flags = frame[3];
            ushort rotations = frame[4];
            ushort residualPulses = (ushort)(frame[5] | (frame[6] << 8));
            int encoderCount = rotations * (int)EncoderPulsesPerRevolution + residualPulses;
            DateTime now = DateTime.UtcNow;
            double speedRpm = 0.0;

            if (previousEncoderCount.HasValue && previousFrameUtc.HasValue)
            {
                int delta = encoderCount - previousEncoderCount.Value;
                double elapsed = (now - previousFrameUtc.Value).TotalSeconds;
                if (elapsed > 0.05)
                    speedRpm = Math.Abs(delta) * 60.0 /
                        (EncoderPulsesPerRevolution * elapsed);
            }

            previousEncoderCount = encoderCount;
            previousFrameUtc = now;
            latest = new TelemetrySnapshot
            {
                ReceivedUtc = now,
                EncoderCount = encoderCount,
                PulsesPer100Ms = 0,
                SetRpm = frame[2],
                PulseFrequency = (ushort)Math.Min(
                    ushort.MaxValue,
                    Math.Round(speedRpm * EncoderPulsesPerRevolution / 60.0)),
                Forward = (flags & 0x01) != 0,
                Reverse = (flags & 0x02) != 0,
                Running = (flags & 0x04) != 0,
                SpeedRpm = speedRpm,
                FrameHex = BitConverter.ToString(frame).Replace('-', ' ')
            };

            Console.WriteLine(
                "COMPACT " + now.ToLocalTime().ToString("HH:mm:ss") +
                " rpm=" + speedRpm.ToString("F1") +
                " encoder=" + encoderCount +
                " direction=" + Direction(latest));
            pending.RemoveRange(0, compactLength);
            return true;
        }

        private static ushort ReadWord(byte[] frame, int wordIndex, bool littleEndian)
        {
            int offset = wordIndex * 2;
            return littleEndian
                ? (ushort)(frame[offset] | (frame[offset + 1] << 8))
                : (ushort)((frame[offset] << 8) | frame[offset + 1]);
        }

        private static double CalculateRpm(TelemetrySnapshot snapshot)
        {
            return snapshot.SpeedRpm;
        }

        private static string Direction(TelemetrySnapshot snapshot)
        {
            return snapshot.Reverse ? "reverse" : "forward";
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

            if (method != "GET")
            {
                Respond(stream, 423, Error("Gateway is receive-only"));
                return;
            }

            if (path == "/health")
                Respond(stream, 200, Health());
            else if (path == "/telemetry")
                RespondTelemetry(stream, false);
            else if (path == "/debug")
                RespondTelemetry(stream, true);
            else
                Respond(stream, 404, Error("Not found"));
        }

        private Dictionary<string, object> Health()
        {
            lock (stateLock)
            {
                return new Dictionary<string, object>
                {
                    { "gateway", "fx3u-non-protocol" },
                    { "serialPort", portName },
                    { "baudRate", baudRate },
                    { "dataBits", 8 },
                    { "parity", "None" },
                    { "stopBits", 1 },
                    { "receiveOnly", true },
                    { "receivedByteCount", receivedByteCount },
                    { "lastByteHex", receivedByteCount > 0 ? lastByte.ToString("X2") : "" },
                    { "lastByteAt", lastByteUtc.HasValue ? lastByteUtc.Value.ToString("o") : "" },
                    { "lastFrameAt", latest != null ? latest.ReceivedUtc.ToString("o") : "" },
                    { "recentHex", BitConverter.ToString(recentBytes.ToArray()).Replace('-', ' ') },
                    { "serialError", serialError }
                };
            }
        }

        private void RespondTelemetry(Stream stream, bool includeRaw)
        {
            lock (stateLock)
            {
                if (latest == null)
                {
                    Respond(stream, 503, Offline("Waiting for first HAA55 telemetry frame"));
                    return;
                }

                double ageSeconds = (DateTime.UtcNow - latest.ReceivedUtc).TotalSeconds;
                if (ageSeconds > staleSeconds)
                {
                    Respond(stream, 503, Offline("Telemetry stale: " + ageSeconds.ToString("F1") + " seconds"));
                    return;
                }

                Respond(stream, 200, BuildTelemetry(latest, includeRaw));
            }
        }

        private static Dictionary<string, object> BuildTelemetry(
            TelemetrySnapshot snapshot,
            bool includeRaw)
        {
            double rotations = snapshot.EncoderCount / EncoderPulsesPerRevolution;
            double angle = rotations * 360.0 % 360.0;
            if (angle < 0)
                angle += 360.0;

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "runId", "" },
                { "lessonId", "TH2" },
                { "userId", "" },
                { "timestamp", snapshot.ReceivedUtc.ToString("o") },
                { "action", "" },
                { "running", snapshot.Running || Math.Abs(CalculateRpm(snapshot)) > 0.5 },
                { "speedRpm", Math.Abs(CalculateRpm(snapshot)) },
                { "setSpeedRpm", snapshot.SetRpm },
                { "pulseFrequency", snapshot.PulseFrequency },
                { "count", snapshot.EncoderCount },
                { "rotations", rotations },
                { "angle", angle },
                { "encoderCount", snapshot.EncoderCount },
                { "rotationsExact", rotations },
                { "pulsesPerSample", snapshot.PulsesPer100Ms },
                { "speedRawD164", snapshot.PulsesPer100Ms },
                { "motionMode", "telemetry" },
                { "direction", Direction(snapshot) },
                { "backendSynced", true },
                { "backendStatus", "COM5 SYNCED" }
            };

            if (includeRaw)
            {
                payload["frameHex"] = snapshot.FrameHex;
                payload["m9"] = snapshot.Forward;
                payload["m10"] = snapshot.Reverse;
                payload["m11"] = snapshot.Running;
            }

            return payload;
        }

        private static Dictionary<string, object> Error(string message)
        {
            return new Dictionary<string, object> { { "error", message } };
        }

        private static Dictionary<string, object> Offline(string message)
        {
            return new Dictionary<string, object>
            {
                { "error", message },
                { "gateway", "fx3u-non-protocol" },
                { "backendSynced", false },
                { "backendStatus", "OFFLINE: " + message }
            };
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
                "Access-Control-Allow-Methods: GET, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: Content-Type\r\n" +
                "Cache-Control: no-store\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (body.Length > 0)
                stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private void Stop()
        {
            running = false;
            try
            {
                if (listener != null)
                    listener.Stop();
            }
            catch
            {
            }
            try
            {
                if (serial != null && serial.IsOpen)
                    serial.Close();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
            if (serial != null)
                serial.Dispose();
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

        public static int Main()
        {
            string portName = Env("FX3U_SERIAL_PORT", "COM5");
            int baudRate = EnvInt("FX3U_BAUD_RATE", 9600);
            string host = Env("FX3U_HTTP_HOST", "127.0.0.1");
            int httpPort = EnvInt("FX3U_HTTP_PORT", 5002);
            int staleSeconds = EnvInt("FX3U_STALE_SECONDS", 3);

            try
            {
                using (Gateway gateway = new Gateway(host, httpPort, portName, baudRate, staleSeconds))
                    gateway.Serve();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FX3U_TELEMETRY_GATEWAY_ERROR: " + exception.Message);
                return 2;
            }
        }
    }
}
