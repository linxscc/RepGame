using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Collections.Generic;

public class LiteNetClient : MonoBehaviour, INetEventListener
{
    private NetManager client;
    private NetPeer serverPeer;

    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>(); // 玩家列表

    [SerializeField] private GameObject playerPrefab; // 玩家预制体
    private GameObject localPlayer;

    [SerializeField] private Vector3 playervec; // 玩家预制体
    void Start()
    {
        client = new NetManager(this);
        client.UnconnectedMessagesEnabled = true;
        client.UpdateTime = 15;
        client.Start();
        client.Connect("127.0.0.1", 8000, "demo"); // IP, 端口, 密钥
        Debug.Log("Client started");

        // 创建本地玩家
        localPlayer = Instantiate(playerPrefab, playervec, Quaternion.identity);
    }

    void Update()
    {
        client.PollEvents();

        // 发送本地玩家位置
        if (serverPeer != null && serverPeer.ConnectionState == ConnectionState.Connected)
        {
            var writer = new NetDataWriter();
            writer.Put(localPlayer.transform.position.x);
            writer.Put(localPlayer.transform.position.y);
            writer.Put(localPlayer.transform.position.z);
            serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            Debug.LogWarning("Server peer is not connected!");
        }
    }

    void OnDestroy()
    {
        if (client != null)
      {
        client.Stop(); // 停止客户端并释放端口
        Debug.Log("Client stopped and port released.");
      }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("Connected to server!");
        serverPeer = peer;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"Disconnected from server: {disconnectInfo.Reason}");
        if (peer == serverPeer)
        {
            serverPeer = null; // 清理无效的 serverPeer
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Debug.LogError($"Network error: {socketError} at {endPoint}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber,DeliveryMethod deliveryMethod)
    {
        // 接收其他玩家的位置
        int playerId = reader.GetInt();
        float x = reader.GetFloat();
        float y = reader.GetFloat();
        float z = reader.GetFloat();

        if (!players.ContainsKey(playerId))
        {
            // 创建新玩家
            var newPlayer = Instantiate(playerPrefab, new Vector3(x, y, z), Quaternion.identity);
            players[playerId] = newPlayer;
        }
        else
        {
            // 更新已有玩家的位置
            players[playerId].transform.position = new Vector3(x, y, z);
        }
    }

    public  void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
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