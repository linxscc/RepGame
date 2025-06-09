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
    private List<byte> messageBuffer = new List<byte>(); // 动态缓冲区
    private byte[] readBuffer = new byte[4096]; // 临时读取缓冲区
    private bool isReconnecting = false;
    private float reconnectInterval = 5f; // 重连间隔时间（秒）
    private int maxReconnectAttempts = 5; // 最大重连尝试次数
    private int reconnectAttempts = 0;
    private const int MAX_MESSAGE_SIZE = 1024 * 1024; // 最大消息大小1MB

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
            // client = new TcpClient("13.237.148.137", 9060);

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
                    // 读取数据到临时缓冲区
                    int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead > 0)
                    {
                        // 将读取的数据添加到消息缓冲区
                        for (int i = 0; i < bytesRead; i++)
                        {
                            messageBuffer.Add(readBuffer[i]);
                        }

                        // 检查缓冲区大小，防止内存溢出
                        if (messageBuffer.Count > MAX_MESSAGE_SIZE)
                        {
                            Debug.LogError("接收到的消息过大，清空缓冲区");
                            messageBuffer.Clear();
                            continue;
                        }

                        // 尝试从缓冲区中提取完整的JSON消息
                        ProcessCompleteMessages();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理服务器响应时发生错误: {ex.Message}");
                EventManager.TriggerEvent("NetworkError", "处理响应数据时发生错误");

                // 清空缓冲区以防止数据污染
                messageBuffer.Clear();
            }
            yield return null; // 每帧检测一次
        }
    }

    /// <summary>
    /// 从缓冲区中提取并处理完整的JSON消息
    /// </summary>
    private void ProcessCompleteMessages()
    {
        while (messageBuffer.Count > 0)
        {
            // 转换为字符串进行JSON解析
            string bufferString = Encoding.UTF8.GetString(messageBuffer.ToArray());

            // 查找第一个完整的JSON对象
            int jsonStart = bufferString.IndexOf('{');
            if (jsonStart == -1)
            {
                // 没有找到JSON开始标记，清空缓冲区
                messageBuffer.Clear();
                break;
            }

            // 从JSON开始位置查找完整的JSON对象
            int braceCount = 0;
            int jsonEnd = -1;
            bool inString = false;
            bool escapeNext = false;

            for (int i = jsonStart; i < bufferString.Length; i++)
            {
                char c = bufferString[i];

                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escapeNext = true;
                    continue;
                }

                if (c == '"' && !escapeNext)
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            jsonEnd = i;
                            break;
                        }
                    }
                }
            }

            if (jsonEnd != -1)
            {
                // 找到完整的JSON对象
                string jsonObject = bufferString.Substring(jsonStart, jsonEnd - jsonStart + 1);

                // 从缓冲区中移除已处理的数据
                int bytesProcessed = Encoding.UTF8.GetByteCount(bufferString.Substring(0, jsonEnd + 1));
                messageBuffer.RemoveRange(0, Math.Min(bytesProcessed, messageBuffer.Count));

                // 处理这个完整的JSON消息
                ProcessJsonMessage(jsonObject);
            }
            else
            {
                // 没有找到完整的JSON对象，等待更多数据
                break;
            }
        }
    }

    /// <summary>
    /// 处理单个完整的JSON消息
    /// </summary>
    private void ProcessJsonMessage(string jsonObject)
    {
        try
        {
            Debug.Log($"处理jsonObject数据: {jsonObject}");
            TcpResponse response = JsonUtility.FromJson<TcpResponse>(jsonObject);

            // 直接从JSON字符串中提取data部分                        
            string dataContent = TcpMessageHandler.Instance.ExtractDataContent(jsonObject);

            if (response.code != "200")
            {
                EventManager.TriggerEvent("NetworkError", response.message);
                return;
            }

            EventManager.TriggerEvent(response.responsekey, dataContent);
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析JSON消息时发生错误: {ex.Message}");
            Debug.LogError($"问题JSON: {jsonObject}");
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
    }    /// <summary>
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

            // 清空消息缓冲区
            messageBuffer.Clear();

            Debug.Log("已断开与服务器的连接");
        }
        catch (Exception e)
        {
            Debug.LogError($"断开连接时发生错误: {e.Message}");
        }
    }
}
