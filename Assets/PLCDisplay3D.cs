using UnityEngine;
using TMPro;
using System;
using System.Net.Sockets;
using System.Collections;
using System.Text;

public class PLCDisplay3D : MonoBehaviour
{
    [Header("Kết nối PLC (ZeroTier/IP)")]
    public string ipAddress = "10.x.y.z";
    public int port = 2000;

    [Header("Cấu hình hiển thị")]
    public TextMeshProUGUI valueText;
    public float refreshRate = 0.5f;

    private TcpClient client;
    private NetworkStream stream;

    void Start()
    {
        StartCoroutine(ReadPLCRoutine());
    }

    IEnumerator ReadPLCRoutine()
    {
        while (true)
        {
            // 1. Kiểm tra và tạo kết nối (Để ngoài try-catch để tránh lỗi yield)
            if (client == null || !client.Connected)
            {
                client = new TcpClient();
                IAsyncResult result = client.BeginConnect(ipAddress, port, null, null);

                while (!result.IsCompleted)
                {
                    yield return null;
                }

                try
                {
                    client.EndConnect(result);
                    stream = client.GetStream();
                }
                catch
                {
                    client = null;
                }
            }

            // 2. Đọc dữ liệu
            if (client != null && client.Connected && stream != null)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                bool success = false;

                try
                {
                    // Lệnh ASCII Read thanh ghi D146
                    byte[] cmd = { 0x02, 0x30, 0x31, 0x31, 0x32, 0x34, 0x30, 0x32, 0x03, 0x35, 0x36 };
                    stream.Write(cmd, 0, cmd.Length);
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.Log("Lỗi truyền tin: " + e.Message);
                    client = null;
                }

                if (success && bytesRead > 0)
                {
                    try
                    {
                        string cleanData = Encoding.ASCII.GetString(buffer, 1, 4);
                        int val = Convert.ToInt32(cleanData, 16);
                        valueText.text = val.ToString() + " <size=60%>RPM</size>";
                        valueText.color = Color.green;
                    }
                    catch
                    {
                        valueText.text = "DATA ERR";
                    }
                }
            }
            else
            {
                valueText.text = "OFFLINE";
                valueText.color = Color.red;
            }

            yield return new WaitForSeconds(refreshRate);
        }
    }

    private void OnDisable()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}