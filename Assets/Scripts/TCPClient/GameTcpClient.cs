using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using RepGameModels;
using RepGame.Core;
using RepGame;

public class GameTcpClient : MonoBehaviour
{
    // 单例实例
    private static GameTcpClient _instance;
    public static GameTcpClient Instance
    {
        get
        {
            if (_instance == null)
            {
                // 查找场景中是否已有实例
                _instance = FindFirstObjectByType<GameTcpClient>();

                // 如果没有找到，创建一个新的
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameTcpClient");
                    _instance = go.AddComponent<GameTcpClient>();
                    DontDestroyOnLoad(go); // 保证控制器在场景切换时不被销毁
                }
            }
            return _instance;
        }
    }

    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    private bool isReconnecting = false;
    private float reconnectInterval = 5f; // 重连间隔时间（秒）
    private int maxReconnectAttempts = 5; // 最大重连尝试次数
    private int reconnectAttempts = 0;

    void Start()
    {
        // 确保只有一个实例
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化连接
        ConnectToServer();
    }

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public void ConnectToServer()
    {
        try
        {
            if (isReconnecting)
            {
                Debug.Log("正在重连中，忽略重复连接请求");
                return;
            }

            if (client != null && client.Connected)
            {
                Debug.Log("已经连接到服务器，忽略重复连接请求");
                return;
            }

            client = new TcpClient("127.0.0.1", 9060); // 本地测试
            // client = new TcpClient("52.62.195.43", 9060);

            stream = client.GetStream();
            StartCoroutine(CheckServerResponse());
            Debug.Log("连接到服务器成功");

            // 重置重连计数器
            reconnectAttempts = 0;

            // 触发连接成功事件
            EventManager.TriggerEvent("ConnectionEstablished");
        }
        catch (Exception e)
        {
            Debug.LogError($"连接服务器失败: {e.Message}");
            EventManager.TriggerEvent("ConnectionFailed", "无法连接到服务器，请检查网络连接");

            // 启动自动重连
            if (!isReconnecting && reconnectAttempts < maxReconnectAttempts)
            {
                StartCoroutine(ReconnectCoroutine());
            }
        }
    }

    /// <summary>
    /// 自动重连协程
    /// </summary>
    private IEnumerator ReconnectCoroutine()
    {
        isReconnecting = true;
        reconnectAttempts++;

        Debug.Log($"尝试重连，第 {reconnectAttempts} 次尝试，将在 {reconnectInterval} 秒后重试...");
        EventManager.TriggerEvent("ConnectionRetrying", $"尝试重连 ({reconnectAttempts}/{maxReconnectAttempts})...");

        yield return new WaitForSeconds(reconnectInterval);

        // 尝试重新连接
        ConnectToServer();

        isReconnecting = false;
    }

    private IEnumerator CheckServerResponse()
    {
        while (client != null && client.Connected)
        {
            try
            {
                if (stream != null && stream.DataAvailable)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    string jsonString = Encoding.UTF8.GetString(buffer, 0, bytes);
                    // 解析 message 和 data 字段
                    int start = jsonString.IndexOf('{');
                    int end = jsonString.LastIndexOf('}');
                    if (start >= 0 && end > start)
                    {
                        string jsonObject = jsonString.Substring(start, end - start + 1);
                        TcpResponse response = JsonUtility.FromJson<TcpResponse>(jsonObject);
                        Debug.Log($"收到响应: {jsonObject}");

                        // 直接从JSON字符串中提取data部分                        
                        string dataContent = TcpMessageHandler.Instance.ExtractDataContent(jsonObject);

                        if (response.code != "200")
                        {
                            EventManager.TriggerEvent("NetworkError", response.message);
                            continue;
                        }

                        EventManager.TriggerEvent(response.responsekey, dataContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理服务器响应时发生错误: {ex.Message}");
                EventManager.TriggerEvent("NetworkError", "处理响应数据时发生错误");
            }
            yield return null; // 每帧检测一次
        }
    }

    // 通用消息发送方法
    public void SendMessageToServer<T>(string messageType, T data)
    {
        // 检查连接状态
        if (client == null || stream == null || !client.Connected)
        {
            Debug.LogWarning("客户端未连接，尝试重新连接...");
            ConnectToServer();

            // 如果重连失败，返回
            if (client == null || stream == null || !client.Connected)
            {
                Debug.LogError("发送消息失败：客户端未连接");
                EventManager.TriggerEvent("ConnectionFailed", "连接已断开，无法发送消息");
                return;
            }
        }

        string json;
        if (data == null || (data is string s && string.IsNullOrEmpty(s)))
        {
            json = TcpRequest.Serialize("0", messageType, "");
        }
        else
        {
            json = TcpRequest<T>.Serialize("0", messageType, data);
        }

        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            Debug.Log($"发送消息: {messageType}, 数据: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"发送消息失败: {e.Message}");
            EventManager.TriggerEvent("ConnectionFailed", "发送消息失败，连接可能已断开");

            // 尝试重新连接并重发消息
            if (!isReconnecting)
            {
                Debug.Log("尝试重新连接并重发消息...");
                ConnectToServer();

                // 如果重连成功，重发消息
                if (client != null && client.Connected && stream != null)
                {
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        stream.Write(bytes, 0, bytes.Length);
                        Debug.Log($"重发消息成功: {messageType}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"重发消息失败: {ex.Message}");
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        DisconnectFromServer();

        // 如果当前实例是单例实例，重置单例引用
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 断开与服务器的连接
    /// </summary>
    public void DisconnectFromServer()
    {
        try
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }

            Debug.Log("已断开与服务器的连接");
        }
        catch (Exception e)
        {
            Debug.LogError($"断开连接时发生错误: {e.Message}");
        }
    }
}
