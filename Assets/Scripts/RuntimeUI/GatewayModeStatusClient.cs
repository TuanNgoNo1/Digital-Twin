using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PDTwin.RuntimeUI
{
    [Serializable]
    public sealed class GatewayModeDocument
    {
        public int schemaVersion;
        public string mode;
        public bool gatewayHealthy;
        public string plcComPort;
        public bool plcPortAvailable;
        public bool gxWorksRunning;
        public string sessionState;
        public int sessionCount;
        public string message;
        public string updatedAt;
    }

    /// <summary>Read-only client for the local Prepare Mode status published by Caddy.</summary>
    public sealed class GatewayModeStatusClient : MonoBehaviour
    {
        private const string DefaultStatusUrl =
            "http://103.238.69.131:8080/gateway_status/mode.json";
        private const float PollIntervalSeconds = 2f;
        private const float StaleAfterSeconds = 8f;

        private GatewayModeDocument current;
        private string lastRevision = string.Empty;
        private float lastRevisionAt = float.NegativeInfinity;

        public GatewayModeDocument Current => current;
        public bool IsFresh => current != null &&
                               Time.unscaledTime - lastRevisionAt <= StaleAfterSeconds;
        public string Mode => IsFresh && !string.IsNullOrWhiteSpace(current.mode)
            ? current.mode.Trim().ToUpperInvariant()
            : "UNKNOWN";
        public string LastError { get; private set; } = string.Empty;

        public static GatewayModeStatusClient Attach(GameObject host)
        {
            GatewayModeStatusClient client = host.GetComponent<GatewayModeStatusClient>();
            return client != null ? client : host.AddComponent<GatewayModeStatusClient>();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                yield return Poll();
                yield return new WaitForSecondsRealtime(PollIntervalSeconds);
            }
        }

        private IEnumerator Poll()
        {
            string statusUrl = ResolveStatusUrl();
            string separator = statusUrl.Contains("?") ? "&" : "?";
            string url = statusUrl + separator + "t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 4;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LastError = request.error ?? "Không đọc được gateway status.";
                    yield break;
                }

                GatewayModeDocument parsed = null;
                try
                {
                    parsed = JsonUtility.FromJson<GatewayModeDocument>(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    LastError = exception.Message;
                }

                if (parsed == null || parsed.schemaVersion != 1 || string.IsNullOrWhiteSpace(parsed.mode))
                {
                    LastError = "Gateway status JSON không hợp lệ.";
                    yield break;
                }

                current = parsed;
                if (!string.Equals(lastRevision, parsed.updatedAt, StringComparison.Ordinal))
                {
                    lastRevision = parsed.updatedAt ?? string.Empty;
                    lastRevisionAt = Time.unscaledTime;
                }
                LastError = string.Empty;
            }
        }

        private static string ResolveStatusUrl()
        {
            if (!string.IsNullOrWhiteSpace(Application.absoluteURL) &&
                Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out Uri pageUri))
            {
                return $"{pageUri.Scheme}://{pageUri.Authority}/gateway_status/mode.json";
            }
            return DefaultStatusUrl;
        }
    }
}
