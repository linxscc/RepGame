using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

public class LiteNetServer : MonoBehaviour, INetEventListener, INetLogger
{
    private NetManager server;
    private NetDataWriter _dataWriter;

    void Start()
    {
        NetDebug.Logger = this;
        _dataWriter = new NetDataWriter();
        server = new NetManager(this);
        server.Start(9050); // 监听端口
        server.BroadcastReceiveEnabled = true;
        server.UpdateTime = 15;
        Debug.Log("Server started on port 9050");
    }

    void Update()
    {
        server.PollEvents();
    }

    void OnDestroy()
    {
        server.Stop();
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("demo"); // 验证连接密钥
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log($"Client connected: {peer.Id}");

        // 向客户端发送欢迎消息
        var writer = new NetDataWriter();
        writer.Put("Welcome from Server!");
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // 接收客户端消息
        var msg = reader.GetString();
        Debug.Log($"Received from client {peer.Id}: {msg}");
        reader.Recycle();

        // 示例：广播消息给所有客户端
        var writer = new NetDataWriter();
        writer.Put($"Client {peer.Id} says: {msg}");
        foreach (var connectedPeer in server.ConnectedPeerList)
        {
            connectedPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"Client disconnected: {peer.Id}, Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Debug.LogError($"Network error: {socketError}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        Debug.Log($"Latency updated for client {peer.Id}: {latency} ms");
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // 处理未连接的消息
        var msg = reader.GetString();
        Debug.Log($"Received unconnected message: {msg}");
        reader.Recycle();
    }
    
    void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
    {
        Debug.LogFormat(str, args);
    }
}