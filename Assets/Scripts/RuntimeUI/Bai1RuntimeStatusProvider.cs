using UnityEngine;

namespace PDTwin.RuntimeUI
{
    public sealed class Bai1RuntimeStatusProvider : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;
        private const float TelemetryFreshSeconds = 3f;

        private PDTwinStatusBar statusBar;
        private GatewayModeStatusClient modeStatus;
        private PLCController_v2 plc;
        private PLCController_v2.MotorTelemetry latestTelemetry;
        private float lastTelemetryAt = float.NegativeInfinity;
        private float nextRefreshAt;
        private string lastMode = "UNKNOWN";

        public static Bai1RuntimeStatusProvider Attach(GameObject host, PDTwinStatusBar bar)
        {
            Bai1RuntimeStatusProvider provider = host.GetComponent<Bai1RuntimeStatusProvider>();
            if (provider == null)
                provider = host.AddComponent<Bai1RuntimeStatusProvider>();
            provider.statusBar = bar;
            provider.modeStatus = GatewayModeStatusClient.Attach(host);
            provider.RefreshStatus();
            return provider;
        }

        private void Update()
        {
            BindPlc();
            if (Time.unscaledTime < nextRefreshAt)
                return;

            nextRefreshAt = Time.unscaledTime + RefreshInterval;
            RefreshStatus();
        }

        private void OnDestroy()
        {
            if (plc != null)
                plc.OnTelemetryUpdated -= OnTelemetryUpdated;
        }

        private void BindPlc()
        {
            if (plc != null)
                return;

            plc = PLCController_v2.Instance != null
                ? PLCController_v2.Instance
                : FindFirstObjectByType<PLCController_v2>(FindObjectsInactive.Include);
            if (plc == null)
                return;

            latestTelemetry = plc.LatestTelemetry;
            plc.OnTelemetryUpdated += OnTelemetryUpdated;
        }

        private void OnTelemetryUpdated(PLCController_v2.MotorTelemetry telemetry)
        {
            if (telemetry == null)
                return;
            latestTelemetry = telemetry;
            lastTelemetryAt = Time.unscaledTime;
        }

        private void RefreshStatus()
        {
            if (statusBar == null)
                return;

            bool gatewayOnline = plc != null && plc.IsPiOnline;
            RefreshCom3Status(gatewayOnline);

            bool hasTelemetry = lastTelemetryAt > float.NegativeInfinity;
            float telemetryAge = hasTelemetry ? Mathf.Max(0f, Time.unscaledTime - lastTelemetryAt) : float.PositiveInfinity;
            bool telemetryFresh = gatewayOnline && hasTelemetry && telemetryAge <= TelemetryFreshSeconds;
            bool backendSynced = latestTelemetry != null && latestTelemetry.backendSynced;
            RuntimeHealth plcHealth;
            string plcValue;
            string plcDetail;

            if (telemetryFresh && backendSynced)
            {
                plcHealth = RuntimeHealth.Online;
                plcValue = "ONLINE";
                plcDetail = $"Telemetry mới {telemetryAge:F1} giây";
            }
            else if (gatewayOnline)
            {
                plcHealth = RuntimeHealth.Warning;
                plcValue = backendSynced ? "CHỜ TELEMETRY" : "CHƯA ĐỒNG BỘ";
                plcDetail = hasTelemetry ? $"Phản hồi đã cũ {telemetryAge:F1} giây" : "Gateway online, chưa có frame PLC";
            }
            else
            {
                plcHealth = RuntimeHealth.Offline;
                plcValue = "OFFLINE";
                plcDetail = "Không xác nhận được PLC thật";
            }

            statusBar.SetStatus("PLC", "PLC FX3U", plcValue, plcDetail, plcHealth);

        }

        private void RefreshCom3Status(bool gatewayOnline)
        {
            if (modeStatus == null || !modeStatus.IsFresh)
            {
                statusBar.SetStatus(
                    "COM3",
                    "COM3 / GATEWAY",
                    gatewayOnline ? "SẴN SÀNG" : "CHƯA XÁC ĐỊNH",
                    gatewayOnline ? "Gateway HTTP đang phản hồi; chờ mode status" : "Không có mode status mới",
                    gatewayOnline ? RuntimeHealth.Online : RuntimeHealth.Warning);
                return;
            }

            GatewayModeDocument document = modeStatus.Current;
            string mode = modeStatus.Mode;
            RefreshModeBanner(mode);
            switch (mode)
            {
                case "GATEWAY":
                    bool ready = document.gatewayHealthy && gatewayOnline;
                    statusBar.SetStatus(
                        "COM3", "COM3 / GATEWAY",
                        ready ? "SẴN SÀNG" : "CHỜ GATEWAY",
                        ready ? "COM3 đang thuộc PLC Gateway" : "Mode Gateway nhưng health chưa sẵn sàng",
                        ready ? RuntimeHealth.Online : RuntimeHealth.Warning);
                    break;
                case "PREPARING_GX":
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "ĐANG NHẢ COM3",
                        "Đang chuyển quyền sang GX Works2", RuntimeHealth.Warning);
                    break;
                case "GXWORKS":
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "GX WORKS ĐANG DÙNG",
                        "Bài 1 chờ COM3 được trả về Gateway", RuntimeHealth.Warning);
                    break;
                case "CONFIRMING_END":
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "CHỜ XÁC NHẬN",
                        "GX Works2 đã đóng; đang chờ kết thúc Bài 2", RuntimeHealth.Warning);
                    break;
                case "RETURNING_GATEWAY":
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "ĐANG KHÔI PHỤC",
                        "Đang khởi động lại PLC Gateway", RuntimeHealth.Warning);
                    break;
                case "FAULT":
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "LỖI COM3",
                        string.IsNullOrWhiteSpace(document.message) ? "Prepare Mode Agent báo lỗi" : document.message,
                        RuntimeHealth.Offline);
                    break;
                default:
                    statusBar.SetStatus("COM3", "COM3 / GATEWAY", "CHƯA XÁC ĐỊNH",
                        "Không xác định được chủ sở hữu COM3", RuntimeHealth.Warning);
                    break;
            }
        }

        private void RefreshModeBanner(string mode)
        {
            if (string.Equals(mode, lastMode, System.StringComparison.Ordinal))
                return;

            string previous = lastMode;
            lastMode = mode;
            switch (mode)
            {
                case "PREPARING_GX":
                    statusBar.SetTransitionBanner(true, "ĐANG NHẢ COM3 CHO GX WORKS2",
                        "Bài 1 tạm thời chưa thể vận hành.", RuntimeHealth.Warning);
                    break;
                case "CONFIRMING_END":
                    statusBar.SetTransitionBanner(true, "GX WORKS2 ĐÃ ĐÓNG",
                        "Đang chờ sinh viên xác nhận kết thúc Bài 2.", RuntimeHealth.Warning);
                    break;
                case "RETURNING_GATEWAY":
                    statusBar.SetTransitionBanner(true, "ĐANG KHÔI PHỤC GATEWAY",
                        "Vui lòng chờ, chưa điều khiển hoặc bắt đầu chấm bài.", RuntimeHealth.Warning);
                    break;
                case "GATEWAY":
                    if (previous == "RETURNING_GATEWAY")
                        statusBar.ShowTransitionResult("COM3 / GATEWAY ĐÃ SẴN SÀNG",
                            "Bài 1 có thể tiếp tục vận hành.", RuntimeHealth.Online);
                    else
                        statusBar.SetTransitionBanner(false, string.Empty, string.Empty, RuntimeHealth.Unknown);
                    break;
                default:
                    statusBar.SetTransitionBanner(false, string.Empty, string.Empty, RuntimeHealth.Unknown);
                    break;
            }
        }
    }
}
