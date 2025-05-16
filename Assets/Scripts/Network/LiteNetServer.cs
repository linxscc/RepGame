using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;


public class LiteNetServer : MonoBehaviour, INetEventListener
{
    private NetManager server;

    void Start()
    {
        server = new NetManager(this);
        server.Start(9050); // 监听端口
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
        request.AcceptIfKey("demo"); // 连接密钥
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("Client connected: " + peer);

        // 向客户端发送欢迎消息
        var writer = new NetDataWriter();
        writer.Put("Welcome from Server!");
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        var msg = reader.GetString();
        Debug.Log("Received from client: " + msg);
        reader.Recycle();
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { }
    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod){}
    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType){}
}