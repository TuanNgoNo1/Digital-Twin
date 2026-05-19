using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PLCController_v2 : MonoBehaviour
{
    // Đặt Header ở đây thì mới đúng (trên đầu một biến)
    [Header("Cấu hình kết nối")]
    public string url = "http://192.168.137.67:5000/control";

    // Không được đặt [Header] ở đây!
    public void TurnOn()
    {
        StartCoroutine(PostData("ON"));
    }

    public void TurnOff()
    {
        StartCoroutine(PostData("OFF"));
    }

    IEnumerator PostData(string actionValue)
    {
        string jsonData = "{\"action\":\"" + actionValue + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Lỗi: " + request.error);
            }
            else
            {
                Debug.Log("✅ Phản hồi: " + request.downloadHandler.text);
            }
        }
    }
}