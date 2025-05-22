using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using RepGameModels;
using RepGame.Core;
using RepGame.Network.GameClientLogic;

public class GameClient : MonoBehaviour, INetEventListener
{
    private NetManager _netClient;
    private PlayerManager _playerManager;
    private RepGame.Network.GameClientLogic.NetworkMessageHandler _messageHandler;

    private void Start()
    {
        _netClient = new NetManager(this);
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.UpdateTime = 15;
        _netClient.Start(0);
        _netClient.Connect("127.0.0.1", 9050, "demo");

        // 初始化消息处理器
        _messageHandler = new RepGame.Network.GameClientLogic.NetworkMessageHandler();

        // _playerManager = FindFirstObjectByType<PlayerManager>();
        // if (_playerManager == null)
        // {
        //     Debug.LogError("PlayerManager not found in the scene!");
        // }
    }
    private void OnEnable()
    {
        // Subscribe to the "StartCardGame" event
        EventManager.Subscribe("StartCardGame", SendStartCardGameRequest);
        EventManager.Subscribe<List<CardModel>>("PlayCards", SendPlayCardsRequest);
    }

    private void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        EventManager.Unsubscribe("StartCardGame", SendStartCardGameRequest);
        EventManager.Unsubscribe<List<CardModel>>("PlayCards", SendPlayCardsRequest);
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
        EventManager.TriggerEvent("ConnectedToServer","[CLIENT] Connected to server!");
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
        // 使用 NetworkMessageHandler 处理网络消息
        _messageHandler.HandleNetworkMessage(peer, reader, channelNumber, deliveryMethod);
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

    public void SendPlayCardsRequest(List<CardModel> cards)
    {
        if (_netClient == null)
        {
            Debug.LogError("NetManager is not initialized!");
            return;
        }

        try
        {
            // 直接使用 RepGameModels.CardModel 中的方法序列化卡牌数据
            string cardsJson = CardModel.SerializeList(cards);

            // 发送请求给服务器
            NetDataWriter writer = new NetDataWriter();
            writer.Put("PlayCards");
            writer.Put(cardsJson);
            _netClient.SendToAll(writer, DeliveryMethod.ReliableOrdered);
            
            Debug.Log($"Sent PlayCards request to server with {cards.Count} cards.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending play cards request: {ex.Message}");
        }
    }
    
}
