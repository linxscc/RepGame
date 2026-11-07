using Google.Protobuf;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;
using RepGame.Protocol;

namespace RepGame.Network
{
    /// <summary>
    /// 一条到服务器的连接：负责 TCP/TLS 建立、证书固定、按帧收发 protobuf 消息。
    /// 接收和发送各在一个后台线程进行：<see cref="Send"/> 只是入队，不会阻塞调用方（主线程）；
    /// 回调在后台线程触发，由调用方切回主线程。
    /// 不包含重连、心跳等策略；<see cref="SecondsSinceLastReceive"/> 供上层判断连接是否已失效。
    /// </summary>
    public sealed class SecureConnection : IDisposable
    {
        public event Action<ServerMessage> MessageReceived;
        public event Action<Exception> Closed;

        /// <summary>最近一次握手时看到的服务器证书指纹，便于首次配置。</summary>
        public string LastSeenCertSha256 { get; private set; }

        /// <summary>待发送队列上限；写满说明网络已经卡住，按断线处理</summary>
        private const int SendQueueCapacity = 256;

        private readonly ConnectionSettings settings;
        private readonly BlockingCollection<byte[]> sendQueue = new BlockingCollection<byte[]>(SendQueueCapacity);
        private TcpClient tcp;
        private Stream stream;
        private Thread receiveThread;
        private Thread sendThread;
        private int closed;
        private long lastReceiveTicks;

        /// <summary>距离最近一次收到服务器消息（或连接建立）的秒数</summary>
        public double SecondsSinceLastReceive =>
            TimeSpan.FromTicks(DateTime.UtcNow.Ticks - Interlocked.Read(ref lastReceiveTicks)).TotalSeconds;

        public SecureConnection(ConnectionSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>主机名 → 地址：IP 字面量直接用；域名解析后优先取 IPv4。</summary>
        private static async Task<IPAddress> ResolveAsync(string host)
        {
            if (IPAddress.TryParse(host, out var literal)) return literal;
            var all = await Dns.GetHostAddressesAsync(host);
            foreach (var a in all) if (a.AddressFamily == AddressFamily.InterNetwork) return a;
            if (all.Length > 0) return all[0];
            throw new SocketException((int)SocketError.HostNotFound);
        }

        public async Task ConnectAsync()
        {
            // 先解析成具体地址，按地址族建 socket（IPv4 地址用纯 IPv4 socket）。
            // 默认构造的 TcpClient 在部分平台（iOS IL2CPP）是 IPv6 双栈 socket，连局域网 IPv4 地址可能一直超时。
            var address = await ResolveAsync(settings.Host);
            tcp = new TcpClient(address.AddressFamily) { NoDelay = true, SendTimeout = (int)settings.ConnectTimeout.TotalMilliseconds * 2 };
            // 用阻塞式 Connect 放在后台线程里，而不是 TcpClient.ConnectAsync：
            // iOS（IL2CPP）上异步连接在部分系统版本会一直挂起直到超时，阻塞连接走的是最直接的 BSD connect
            var client = tcp;
            var endpoint = new IPEndPoint(address, settings.Port);
            var connectTask = Task.Run(() => client.Connect(endpoint));
            if (await Task.WhenAny(connectTask, Task.Delay(settings.ConnectTimeout)) != connectTask)
                throw new TimeoutException($"connect to {address}:{settings.Port} timed out ({address.AddressFamily})");
            await connectTask; // 传播连接异常

            Stream netStream = tcp.GetStream();
            if (settings.UseTls)
            {
                var ssl = new SslStream(netStream, false, ValidateServerCertificate);
                var authTask = ssl.AuthenticateAsClientAsync(settings.Host);
                if (await Task.WhenAny(authTask, Task.Delay(settings.ConnectTimeout)) != authTask)
                    throw new TimeoutException("TLS handshake timed out");
                await authTask;
                netStream = ssl;
            }
            stream = netStream;
            Interlocked.Exchange(ref lastReceiveTicks, DateTime.UtcNow.Ticks);

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "GameServerReceive" };
            receiveThread.Start();
            sendThread = new Thread(SendLoop) { IsBackground = true, Name = "GameServerSend" };
            sendThread.Start();
        }

        /// <summary>消息入队，由发送线程写出。连接已关闭或队列已满时抛出异常。</summary>
        public void Send(ClientMessage message)
        {
            var frame = FrameCodec.Encode(message.ToByteArray());
            bool queued;
            try
            {
                queued = sendQueue.TryAdd(frame);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("not connected");
            }
            if (!queued) throw new IOException("send queue full");
        }

        /// <summary>主动断开（例如上层判定连接已失效），reason 会随 <see cref="Closed"/> 传出。</summary>
        public void Abort(Exception reason)
        {
            Close(reason);
        }

        private void SendLoop()
        {
            Exception error = null;
            try
            {
                foreach (var frame in sendQueue.GetConsumingEnumerable())
                {
                    stream.Write(frame, 0, frame.Length);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null) Close(error);
        }

        private void ReceiveLoop()
        {
            Exception error = null;
            try
            {
                while (true)
                {
                    byte[] payload = FrameCodec.Read(stream);
                    if (payload == null) break;
                    Interlocked.Exchange(ref lastReceiveTicks, DateTime.UtcNow.Ticks);
                    MessageReceived?.Invoke(ServerMessage.Parser.ParseFrom(payload));
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            Close(error);
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (certificate == null) return false;

            using (var sha = SHA256.Create())
            {
                LastSeenCertSha256 = ToHex(sha.ComputeHash(certificate.GetRawCertData()));
            }

            string pinned = NormalizeFingerprint(settings.CertSha256);
            if (!string.IsNullOrEmpty(pinned))
            {
                // 证书固定：只认指定指纹，忽略 CA/域名校验（服务器使用自签名证书）
                return string.Equals(pinned, LastSeenCertSha256, StringComparison.Ordinal);
            }

            // 未配置指纹时，只接受系统信任链校验通过的证书
            return errors == SslPolicyErrors.None;
        }

        public static string NormalizeFingerprint(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Replace(":", "").Replace(" ", "").Trim().ToLowerInvariant();
        }

        private static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[i * 2 + 1] = hex[bytes[i] & 0xF];
            }
            return new string(chars);
        }

        private void Close(Exception error)
        {
            if (Interlocked.Exchange(ref closed, 1) == 1) return;
            sendQueue.CompleteAdding(); // 发送线程随之退出，之后的 Send 抛出 not connected
            try { stream?.Dispose(); } catch { /* 忽略关闭时的异常 */ }
            try { tcp?.Close(); } catch { /* 忽略关闭时的异常 */ }
            Closed?.Invoke(error);
        }

        public void Dispose()
        {
            Close(null);
        }
    }
}
