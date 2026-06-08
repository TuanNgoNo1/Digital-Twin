using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class MjpegStreamer : MonoBehaviour
{
    [Header("Cấu hình kết nối")]
    public string streamUrl = "http://192.168.137.67:8080/?action=stream";
    public RawImage displayImage;

    void Start()
    {
        if (displayImage == null) displayImage = GetComponent<RawImage>();
        StartCoroutine(GetStream());
    }

    IEnumerator GetStream()
    {
        // Chuyển sang url snapshot để lấy từng ảnh đơn lẻ
        string snapshotUrl = streamUrl.Replace("action=stream", "action=snapshot");

        while (true)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(snapshotUrl))
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
            // Tốc độ 20-25 fps là vừa đẹp cho Mobile Hotspot
            yield return new WaitForSeconds(0.05f);
        }
    }
}