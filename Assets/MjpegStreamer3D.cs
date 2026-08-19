using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MjpegStreamer3D : MonoBehaviour
{
    private const string DefaultStreamUrl =
        "http://103.238.69.131:8080/cam/snapshot.jpg";

    [Header("Cấu hình kết nối camera")]
    public string streamUrl = DefaultStreamUrl;

    [Header("Tốc độ làm mới (giây)")]
    public float updateInterval = 0.2f;

    private Renderer screenRenderer;

    void Start()
    {
        if (
            string.IsNullOrWhiteSpace(streamUrl)
            || streamUrl.IndexOf(
                "unacquiescent-quiana-excepable.ngrok-free.dev",
                System.StringComparison.OrdinalIgnoreCase
            ) >= 0
        )
        {
            streamUrl = DefaultStreamUrl;
        }

        updateInterval = Mathf.Max(0.2f, updateInterval);

        // Tự động lấy Renderer của vật thể (Plane/Quad/Cube)
        screenRenderer = GetComponent<Renderer>();

        if (screenRenderer == null)
        {
            Debug.LogError("Lỗi: Script này phải được gán vào một vật thể 3D có Mesh Renderer!");
            return;
        }

        StartCoroutine(GetStream());
    }

    IEnumerator GetStream()
    {
        // Hỗ trợ cả URL cũ action=stream và URL snapshot trực tiếp.
        string snapshotUrl = streamUrl.Replace("action=stream", "action=snapshot");

        while (true)
        {
            string separator = snapshotUrl.Contains("?") ? "&" : "?";
            string requestUrl =
                snapshotUrl
                + separator
                + "t="
                + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(requestUrl))
            {
                uwr.SetRequestHeader("ngrok-skip-browser-warning", "true");
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("<color=red>Cam Error:</color> " + uwr.error);
                    yield return new WaitForSeconds(2.0f); // Lỗi thì đợi 2s mới thử lại
                }
                else
                {
                    // 1. Lấy Texture mới về
                    Texture2D newTexture = DownloadHandlerTexture.GetContent(uwr);

                    // 2. Xóa Texture cũ trong bộ nhớ để tránh tràn RAM (Rất quan trọng!)
                    if (screenRenderer.material.mainTexture != null)
                    {
                        Destroy(screenRenderer.material.mainTexture);
                    }

                    // 3. Dán Texture mới lên vật thể 3D
                    screenRenderer.material.mainTexture = newTexture;
                }
            }

            // Đợi một khoảng thời gian trước khi lấy khung hình tiếp theo
            yield return new WaitForSeconds(updateInterval);
        }
    }
}
