using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RepGame.Core;
using RepGame.Net;
using RepGame.Protocol;
using UnityEngine;

/// <summary>
/// 游戏服务器客户端（场景单例）。
/// 职责：连接生命周期（连接、断线重连、心跳）、把后台线程收到的消息切回主线程分发、
/// 对外提供类型安全的请求方法。传输细节见 <see cref="SecureConnection"/>，
/// 消息到领域事件的转换见 <see cref="ServerMessageRouter"/>。
/// </summary>
public class GameTcpClient : MonoBehaviour
{
    private static GameTcpClient _instance;
    public static GameTcpClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameTcpClient>();
                if (_instance == null)
                {
                    var go = new GameObject("GameTcpClient");
                    _instance = go.AddComponent<GameTcpClient>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }


    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private readonly Dictionary<uint, RequestKind> pendingRequests = new Dictionary<uint, RequestKind>();

    private ServerMessageRouter router;
    private SecureConnection connection;
    private bool connecting;
    private bool shuttingDown;
    private uint nextSeq;
    private int reconnectAttempts;
    private float nextHeartbeatAt;
    private string sessionToken;

    public bool IsConnected => connection != null && !connecting;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        router = new ServerMessageRouter();
        router.Authenticated += auth => sessionToken = auth.SessionToken;
        router.SessionInvalidated += () => sessionToken = null;

        EventManager.Subscribe("AttemptReconnect", ReconnectNow);
    }

    void Start()
    {
        ConnectToServer();
    }

    void Update()
    {
        while (mainThreadQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                // 单个处理器出错不能影响后续消息
                Debug.LogException(e);
            }
        }

        // 半开连接（切换网络、NAT 超时）时读线程会一直阻塞：按"多久没收到任何消息"判定断线
        if (IsConnected && connection.SecondsSinceLastReceive > ConfigTables.Settings.network.receiveTimeoutSeconds)
        {
            connection.Abort(new TimeoutException($"{ConfigTables.Settings.network.receiveTimeoutSeconds:0} 秒未收到服务器消息"));
            return;
        }

        if (IsConnected && Time.unscaledTime >= nextHeartbeatAt)
        {
            nextHeartbeatAt = Time.unscaledTime + ConfigTables.Settings.network.heartbeatIntervalSeconds;
            Send(new ClientMessage { Ping = new RepGame.Protocol.Ping { ClientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } });
        }
    }

    void OnDestroy()
    {
        EventManager.Unsubscribe("AttemptReconnect", ReconnectNow);
        DisconnectFromServer();
        if (_instance == this) _instance = null;
    }

    // ------------------------------------------------------------------
    // 请求 API
    // ------------------------------------------------------------------

    public void Login(string username, string password) =>
        Send(new ClientMessage { Login = new LoginRequest { Username = username, Password = password } });

    public void Register(string username, string password) =>
        Send(new ClientMessage { Register = new RegisterRequest { Username = username, Password = password } });

    public void Ready() => Send(new ClientMessage { Ready = new ReadyRequest() });

    public void Restart() => Send(new ClientMessage { Restart = new RestartRequest() });

    public void PlayCards(string roomId, IEnumerable<string> cardUids)
    {
        var req = new PlayCardsRequest { RoomId = roomId ?? "" };
        req.CardUids.AddRange(cardUids);
        Send(new ClientMessage { PlayCards = req });
    }

    public void ComposeCards(string roomId, IEnumerable<string> cardUids)
    {
        var req = new ComposeCardsRequest { RoomId = roomId ?? "" };
        req.CardUids.AddRange(cardUids);
        Send(new ClientMessage { ComposeCards = req });
    }

    // ------------------------------------------------------------------
    // 连接生命周期
    // ------------------------------------------------------------------

    public void ConnectToServer()
    {
        if (connecting || connection != null || shuttingDown) return;
        connecting = true;

        var settings = new ConnectionSettings
        {
            Host = EnvConfig.GetString("SERVER_HOST", "127.0.0.1"),
            Port = EnvConfig.GetInt("SERVER_PORT", 9060),
            UseTls = !string.Equals(EnvConfig.GetString("SERVER_TLS", "true"), "false", StringComparison.OrdinalIgnoreCase),
            CertSha256 = EnvConfig.GetString("SERVER_CERT_SHA256", ""),
            ConnectTimeout = TimeSpan.FromSeconds(ConfigTables.Settings.network.connectTimeoutSeconds),
        };
        if (!settings.UseTls)
            Debug.LogWarning("SERVER_TLS=false：连接未加密，仅限本地调试使用");

        FrameCodec.MaxFrameSize = ConfigTables.Settings.network.maxFrameBytes;
        var conn = new SecureConnection(settings);
        conn.MessageReceived += msg => mainThreadQueue.Enqueue(() => router.Route(msg, TakePendingRequest(msg.ReplyTo)));
        conn.Closed += error => mainThreadQueue.Enqueue(() => OnConnectionClosed(conn, error));

        Debug.Log($"正在连接服务器 {settings.Host}:{settings.Port}（TLS: {settings.UseTls}）");
        Task.Run(conn.ConnectAsync).ContinueWith(t => mainThreadQueue.Enqueue(() => OnConnectCompleted(conn, t)));
    }

    private void OnConnectCompleted(SecureConnection conn, Task task)
    {
        connecting = false;

        if (task.IsFaulted || task.IsCanceled)
        {
            var error = task.Exception?.GetBaseException();
            string hint = conn.LastSeenCertSha256 != null && string.IsNullOrEmpty(EnvConfig.GetString("SERVER_CERT_SHA256", ""))
                ? $"（服务器证书指纹 {conn.LastSeenCertSha256}，确认无误后写入 .env 的 SERVER_CERT_SHA256）"
                : "";
            Debug.LogError($"连接服务器失败: {error?.Message}{hint}");
            conn.Dispose();
            EventManager.TriggerEvent(NetEvents.ConnectionFailed, ConfigTables.Text(UiText.ConnectionFailed));
            ScheduleReconnect();
            return;
        }

        connection = conn;
        reconnectAttempts = 0;
        nextHeartbeatAt = Time.unscaledTime + ConfigTables.Settings.network.heartbeatIntervalSeconds;
        Debug.Log("已连接到服务器");
        EventManager.TriggerEvent(NetEvents.ConnectedToServer, ConfigTables.Text(UiText.ConnectionConnected));

        // 断线前已登录：用会话令牌恢复身份（服务器会推送当前对局状态）
        if (!string.IsNullOrEmpty(sessionToken))
        {
            Send(new ClientMessage { ResumeSession = new ResumeSessionRequest { SessionToken = sessionToken } });
        }
    }

    private void OnConnectionClosed(SecureConnection conn, Exception error)
    {
        if (conn != connection) return; // 旧连接的迟到通知
        connection = null;
        pendingRequests.Clear();

        if (shuttingDown) return;
        Debug.LogWarning($"与服务器的连接已断开: {error?.Message ?? "服务器关闭了连接"}");
        EventManager.TriggerEvent(NetEvents.Disconnected, ConfigTables.Text(UiText.ConnectionLost));
        ScheduleReconnect();
    }

    private void ScheduleReconnect()
    {
        if (shuttingDown) return;
        reconnectAttempts++;
        float delay = Mathf.Min(ConfigTables.Settings.network.reconnectMaxDelaySeconds, Mathf.Pow(2, reconnectAttempts - 1));
        Debug.Log($"{delay:0} 秒后进行第 {reconnectAttempts} 次重连");
        CancelInvoke(nameof(ConnectToServer));
        Invoke(nameof(ConnectToServer), delay);
    }

    private void ReconnectNow()
    {
        if (connection != null || connecting) return;
        CancelInvoke(nameof(ConnectToServer));
        ConnectToServer();
    }

    public void DisconnectFromServer()
    {
        shuttingDown = true;
        CancelInvoke();
        connection?.Dispose();
        connection = null;
        pendingRequests.Clear();
    }

    // ------------------------------------------------------------------

    private void Send(ClientMessage message)
    {
        if (!IsConnected)
        {
            Debug.LogWarning($"未连接服务器，丢弃请求 {message.BodyCase}");
            EventManager.TriggerEvent(NetEvents.RequestFailed, new RequestFailure
            {
                Request = ProtocolMapper.ToRequestKind(message.BodyCase),
                Message = ConfigTables.Text(UiText.ConnectionNotConnected),
            });
            ReconnectNow();
            return;
        }

        message.Seq = ++nextSeq;
        if (message.BodyCase != ClientMessage.BodyOneofCase.Ping)
        {
            if (pendingRequests.Count > 256) pendingRequests.Clear(); // 防止异常情况下无限堆积
            pendingRequests[message.Seq] = ProtocolMapper.ToRequestKind(message.BodyCase);
        }

        try
        {
            connection.Send(message);
        }
        catch (Exception e)
        {
            Debug.LogError($"发送 {message.BodyCase} 失败: {e.Message}");
            connection.Abort(e); // 触发 Closed -> 重连
        }
    }

    private RequestKind TakePendingRequest(uint seq)
    {
        if (seq != 0 && pendingRequests.TryGetValue(seq, out var kind))
        {
            pendingRequests.Remove(seq);
            return kind;
        }
        return RequestKind.Unknown;
    }
}
