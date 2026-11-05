using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using RepGame.Protocol;

namespace RepGame.Net
{
    /// <summary>连接参数。</summary>
    public sealed class ConnectionSettings
    {
        public string Host;
        public int Port;
        public bool UseTls = true;
        /// <summary>服务器证书 SHA-256 指纹（十六进制，大小写和冒号均可）。为空时只接受系统信任的证书。</summary>
        public string CertSha256;
        public TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);
    }

    /// <summary>
    /// 一条到服务器的连接：负责 TCP/TLS 建立、证书固定、按帧收发 protobuf 消息。
    /// 接收在后台线程进行，回调也在后台线程触发，由调用方切回主线程。
    /// 不包含重连、心跳等策略。
    /// </summary>
    public sealed class SecureConnection : IDisposable
    {
        public event Action<ServerMessage> MessageReceived;
        public event Action<Exception> Closed;

        /// <summary>最近一次握手时看到的服务器证书指纹，便于首次配置。</summary>
        public string LastSeenCertSha256 { get; private set; }

        private readonly ConnectionSettings settings;
        private readonly object writeLock = new object();
        private TcpClient tcp;
        private Stream stream;
        private Thread receiveThread;
        private int closed;

        public SecureConnection(ConnectionSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task ConnectAsync()
        {
            tcp = new TcpClient { NoDelay = true };
            var connectTask = tcp.ConnectAsync(settings.Host, settings.Port);
            if (await Task.WhenAny(connectTask, Task.Delay(settings.ConnectTimeout)) != connectTask)
                throw new TimeoutException($"connect to {settings.Host}:{settings.Port} timed out");
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

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "GameServerReceive" };
            receiveThread.Start();
        }

        public void Send(ClientMessage message)
        {
            var frame = FrameCodec.Encode(message.ToByteArray());
            lock (writeLock)
            {
                if (stream == null) throw new InvalidOperationException("not connected");
                stream.Write(frame, 0, frame.Length);
                stream.Flush();
            }
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
            try { stream?.Dispose(); } catch { /* 忽略关闭时的异常 */ }
            try { tcp?.Close(); } catch { /* 忽略关闭时的异常 */ }
            lock (writeLock) { stream = null; }
            Closed?.Invoke(error);
        }

        public void Dispose()
        {
            Close(null);
        }
    }
}
