using System.Net.Sockets;
using System.Text;
using UnityEngine;
using RepGameModels;
using System.Collections.Generic;

public class GameTcpClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    private bool waitingResponse = false;
    private bool waitingResponse1 = false;

    void Start()
    {
        client = new TcpClient("127.0.0.1", 9060);
        // /telnet 52.62.195.43 9060
        //http://www.zspersonaldomain.it.com/
        //52.62.195.43
        // client = new TcpClient("52.62.195.43", 9060);

        stream = client.GetStream();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) && !waitingResponse)
        {

            string json = TcpRequest.Serialize(0, "startconnect", "");
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
            waitingResponse = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && !waitingResponse1)
        {
            // 构造请求对象
            HealthUpdate updateheath = new HealthUpdate();
            updateheath.AttackerHealth = 100;
            updateheath.ReceiverHealth = 100;
            List<HealthUpdate> healthUpdates = new List<HealthUpdate> { updateheath };

            string json = TcpRequest<List<HealthUpdate>>.Serialize(0, "healthUpdateList", healthUpdates);
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
            Debug.Log("TCP Send json: " + data);
            waitingResponse1 = true;
        }

        // 检查是否有返回数据
        if (waitingResponse && stream.DataAvailable)
        {
            int bytes = stream.Read(buffer, 0, buffer.Length);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, bytes);
            Debug.Log("TCP received json: " + jsonString);
            int start = jsonString.IndexOf('{');
            int end = jsonString.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                string jsonObject = jsonString.Substring(start, end - start + 1);
                TcpResponse response = JsonUtility.FromJson<TcpResponse>(jsonObject);
                Debug.Log("TCP received: " + response.data + ", Code: " + response.code + ", Message: " + response.message);
            }
            else
            {
                Debug.LogError("Invalid JSON received: " + jsonString);
            }
            waitingResponse = false;
        }
        // 检查是否有返回数据
        if (waitingResponse1 && stream.DataAvailable)
        {
            int bytes = stream.Read(buffer, 0, buffer.Length);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, bytes);
            Debug.Log("TCP received json: " + jsonString);
            int start = jsonString.IndexOf('{');
            int end = jsonString.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                string jsonObject = jsonString.Substring(start, end - start + 1);
                TcpResponse response = JsonUtility.FromJson<TcpResponse>(jsonObject);
                Debug.Log("TCP received: " + response.data + ", Code: " + response.code + ", Message: " + response.message);
            }
            else
            {
                Debug.LogError("Invalid JSON received: " + jsonString);
            }
            waitingResponse1 = false;
        }
    }

    void OnDestroy()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}




