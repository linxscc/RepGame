using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using RepGameModels;
using RepGame.Core;

public class GameClient : MonoBehaviour, INetEventListener
{
    private NetManager _netClient;
    private PlayerManager _playerManager;

    private void Start()
    {
        _netClient = new NetManager(this);
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.UpdateTime = 15;
        _netClient.Start();
        _netClient.Connect("127.0.0.1", 9050, "demo");

        _playerManager = FindFirstObjectByType<PlayerManager>();
        if (_playerManager == null)
        {
            Debug.LogError("PlayerManager not found in the scene!");
        }
    }
    private void OnEnable()
    {
        // Subscribe to the "StartCardGame" event
        EventManager.Subscribe("StartCardGame", SendStartCardGameRequest);
    }

    private void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        EventManager.Unsubscribe("StartCardGame", SendStartCardGameRequest);
    }

    private void Update()
    {
        _netClient.PollEvents();

        var peer = _netClient.FirstPeer;
        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            // 向服务器发送客户端位置
            // var writer = new NetDataWriter();
            // writer.Put("SendClientPositions");
            // peer.Send(writer, DeliveryMethod.ReliableOrdered);
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

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        // string responseType = reader.GetString();

        // if (responseType == "ClientPositions")
        // {
        //     // 接收 JSON 数据
        //     string json = reader.GetString();

        //     // 反序列化为 ClientPositionModel 列表
        //     List<ClientPositionModel> positions = ClientPositionModel.DeserializeList(json);

        //     // 更新玩家对象
        //     foreach (var positionModel in positions)
        //     {
        //         _playerManager.UpdatePlayer(positionModel.ClientId, positionModel.GetPosition());
        //     }
        // }
        // else if (responseType == "RemovePlayer")
        // {
        //     int clientId = reader.GetInt();
        //     _playerManager.RemovePlayer(clientId);
        // }
        string responseType = reader.GetString();

        if (responseType == "InitPlayerCards")
        {
            Debug.Log("Received InitPlayerCards message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            // 反序列化为 ClientPositionModel 列表
            List<CardModel> positions = CardModel.DeserializeList(json);
            Debug.Log($"Received InitPlayerCards {positions}.");
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
    }
    
    public void SendStartCardGameRequest()
    {
        if (_netClient == null)
        {
            Debug.LogError("NetManager is not initialized!");
            return;
        }

        NetDataWriter writer = new NetDataWriter();
        writer.Put("StartCardGame");
        _netClient.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }
}
