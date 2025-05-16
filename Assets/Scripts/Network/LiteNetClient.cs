using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

public class LiteNetClient : MonoBehaviour, INetEventListener
{
    private NetManager client;
    private NetPeer serverPeer;

    void Start()
    {
        client = new NetManager(this);
        client.Start();
        client.Connect("127.0.0.1", 9050, "demo"); // IP, 端口, 密钥
        Debug.Log("Client started");
    }

    void Update()
    {
        client.PollEvents();

        // 按空格发送消息到服务器
        if (serverPeer != null && Input.GetKeyDown(KeyCode.Space))
        {
            var writer = new NetDataWriter();
            writer.Put("Hello from Client!");
            serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }

    void OnDestroy()
    {
        client.Stop();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("Connected to server!");
        serverPeer = peer;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("Disconnected from server: " + disconnectInfo.Reason);
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Debug.LogError($"Network error: {socketError} at {endPoint}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var msg = reader.GetString();
        Debug.Log("Received from server: " + msg);
        reader.Recycle();
    }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        var msg = reader.GetString();
        Debug.Log("Received from server: " + msg);
        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        Debug.Log($"Latency updated: {latency} ms");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        Debug.Log("Connection request received");
    }
}