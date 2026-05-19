using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MjpegStreamer3D : MonoBehaviour
{
    [Header("Cấu hình kết nối Pi")]
    public string streamUrl = "http://10.38.100.214:8080/?action=stream";

    [Header("Tốc độ làm mới (giây)")]
    public float updateInterval = 0.05f;

    private Renderer screenRenderer;

    void Start()
    {
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
        // Chuyển đổi sang URL snapshot để Unity xử lý mượt hơn
        string snapshotUrl = streamUrl.Replace("action=stream", "action=snapshot");

        while (true)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(snapshotUrl))
            {
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