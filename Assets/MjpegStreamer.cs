using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class MjpegStreamer : MonoBehaviour
{
    private const string DefaultStreamUrl =
        "http://103.238.69.131:8080/cam/snapshot.jpg";

    [Header("Cấu hình kết nối")]
    public string streamUrl = DefaultStreamUrl;
    public RawImage displayImage;

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

        if (displayImage == null) displayImage = GetComponent<RawImage>();
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
                // Gửi yêu cầu lấy ảnh
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("<color=red>Lỗi kết nối Cam:</color> " + uwr.error);
                    // Đợi lâu hơn một chút nếu lỗi để tránh spam request
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    // Giải phóng bộ nhớ của tấm ảnh cũ trước khi nạp ảnh mới
                    if (displayImage.texture != null)
                    {
                        Destroy(displayImage.texture);
                    }

                    // Nạp ảnh mới vào
                    Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                    displayImage.texture = tex;
                }
            }
            // Nguồn camera hiện phát 5 FPS.
            yield return new WaitForSeconds(0.2f);
        }
    }
}
