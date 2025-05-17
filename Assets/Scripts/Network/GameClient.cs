using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

public class GameClient : MonoBehaviour, INetEventListener
{
    private NetManager _netClient;
    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>(); // 存储玩家对象

    [SerializeField] private GameObject playerPrefab; // 玩家预制体

    private void Start()
    {
        _netClient = new NetManager(this);
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.UpdateTime = 15;
        _netClient.Start();
        _netClient.Connect("127.0.0.1", 9050, "sample_app");
    }

    private void Update()
    {
        _netClient.PollEvents();

        var peer = _netClient.FirstPeer;
        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            // 向服务器请求客户端位置
            var writer = new NetDataWriter();
            writer.Put("RequestClientPositions");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            _netClient.SendBroadcast(new byte[] {1}, 5000);
        }
    }

    private void OnDestroy()
    {
        if (_netClient != null)
            _netClient.Stop();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[CLIENT] Connected to server!");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[CLIENT] Disconnected from server: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.LogError($"[CLIENT] Network error: {socketErrorCode}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader,byte channelNumber, DeliveryMethod deliveryMethod)
    {
        string responseType = reader.GetString();

        if (responseType == "ClientPositions")
        {
            int clientCount = reader.GetInt();
            for (int i = 0; i < clientCount; i++)
            {
                int clientId = reader.GetInt();
                float posX = reader.GetFloat();
                float posY = reader.GetFloat();
                float posZ = reader.GetFloat();

                Vector3 position = new Vector3(posX, posY, posZ);

                if (!players.ContainsKey(clientId))
                {
                    // 实例化新的玩家对象
                    GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
                    players[clientId] = player;
                }
                else
                {
                    // 更新已有玩家的位置
                    players[clientId].transform.position = position;
                }
            }
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.BasicMessage && _netClient.ConnectedPeersCount == 0 && reader.GetInt() == 1)
        {
            Debug.Log("[CLIENT] Received discovery response. Connecting to: " + remoteEndPoint);
            _netClient.Connect(remoteEndPoint, "sample_app");
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {

    }

    public void OnConnectionRequest(ConnectionRequest request)
    {

    }
}
