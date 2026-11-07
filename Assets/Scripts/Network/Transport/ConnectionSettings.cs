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

namespace RepGame.Network
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
}
