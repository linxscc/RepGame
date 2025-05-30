﻿using System;
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
    private NetworkMessageHandler _messageHandler;

    private void Start()
    {
        _netClient = new NetManager(this);
        _netClient.UnconnectedMessagesEnabled = true;
        _netClient.UpdateTime = 15;

        // 设置超时参数
        _netClient.DisconnectTimeout = 60000; // 60 秒
        _netClient.ReconnectDelay = 500; // 重连延迟 0.5 秒
        _netClient.MaxConnectAttempts = 60; // 最大连接尝试次数
        _netClient.PingInterval = 1000; // ping 间隔 1 秒

        _netClient.Start(0);
        _netClient.Connect("127.0.0.1", 9060, "demo");

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
        EventManager.Subscribe<List<GameObject>>("CompCard", SendCompCardRequest);
        EventManager.Subscribe("AttemptReconnect", AttemptReconnect);
    }

    private void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        EventManager.Unsubscribe("StartCardGame", SendStartCardGameRequest);
        EventManager.Unsubscribe<List<CardModel>>("PlayCards", SendPlayCardsRequest);
        EventManager.Unsubscribe<List<GameObject>>("CompCard", SendCompCardRequest);
        EventManager.Unsubscribe("AttemptReconnect", AttemptReconnect);
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
        EventManager.TriggerEvent("ConnectedToServer", "[CLIENT] Connected to server!");
        Debug.Log("[CLIENT] Connected to server!");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[CLIENT] Disconnected from server: {disconnectInfo.Reason}");

        // 处理不同的断开连接原因
        switch (disconnectInfo.Reason)
        {
            case DisconnectReason.Timeout:
                Debug.LogWarning("[CLIENT] Connection timed out!");
                EventManager.TriggerEvent("ConnectionTimeout", "与服务器的连接超时，请检查网络并重试。");
                break;

            case DisconnectReason.ConnectionFailed:
                Debug.LogWarning("[CLIENT] Connection failed!");
                EventManager.TriggerEvent("ConnectionFailed", "无法连接到服务器，请稍后重试。");
                break;

            case DisconnectReason.RemoteConnectionClose:
                Debug.LogWarning("[CLIENT] Server closed the connection!");
                EventManager.TriggerEvent("ServerClosed", "服务器关闭了连接。");
                break;

            default:
                EventManager.TriggerEvent("Disconnected", $"与服务器断开连接: {disconnectInfo.Reason}");
                break;
        }
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
        try
        {
            // 直接使用 RepGameModels.CardModel 中的方法序列化卡牌数据
            string cardsJson = CardModel.SerializeList(cards);
            // 创建ApiResponse对象包装数据

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

    public void SendCompCardRequest(List<GameObject> cardObjects)
    {
        try
        {
            // 将GameObject列表转换为CardModel列表
            List<CardModel> cards = new List<CardModel>();
            foreach (var cardObj in cardObjects)
            {
                var cardItem = cardObj.GetComponent<RepGame.UI.CardItem>();
                if (cardItem != null)
                {
                    // 创建CardModel并添加到列表中
                    cards.Add(new CardModel
                    {
                        CardID = cardItem.CardID,
                        Type = cardItem.Type,
                        Damage = 0 // 合成操作不需要伤害值
                    });
                }
            }

            // 创建ApiResponse对象包装数据
            ApiResponse<List<CardModel>> response = new ApiResponse<List<CardModel>>
            {
                Code = 0, // 0表示成功
                Message = "Composition Request",
                Data = cards
            };

            // 序列化响应对象
            string jsonData = JsonUtility.ToJson(response);

            // 发送请求给服务器
            NetDataWriter writer = new NetDataWriter();
            writer.Put("CompCards");
            writer.Put(jsonData);
            _netClient.SendToAll(writer, DeliveryMethod.ReliableOrdered);

            Debug.Log($"Sent CompCards request to server with {cards.Count} cards.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending composition request: {ex.Message}");
            // 通知UI层处理错误
            EventManager.TriggerEvent("CompError", $"发送合成请求失败: {ex.Message}");
        }
    }

    // 尝试重新连接到服务器
    public void AttemptReconnect()
    {
        if (_netClient != null)
        {
            Debug.Log("[CLIENT] Attempting to reconnect to server...");
            _netClient.Connect("127.0.0.1", 9050, "demo");
        }
        else
        {
            Debug.LogError("[CLIENT] Cannot reconnect: NetManager is not initialized");
        }
    }
}
