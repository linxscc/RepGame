using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

public class GameServer : MonoBehaviour, INetEventListener, INetLogger
{
    private NetManager _netServer;
    private NetDataWriter _dataWriter;
    private Dictionary<int, Vector3> clientPositions = new Dictionary<int, Vector3>(); // 客户端ID和位置映射

    private void Start()
    {
        NetDebug.Logger = this;
        _dataWriter = new NetDataWriter();
        _netServer = new NetManager(this);
        _netServer.Start(5000);
        _netServer.BroadcastReceiveEnabled = true;
        _netServer.UpdateTime = 15;
        Debug.Log("[SERVER] Server started on port 5000");
    }

    private void Update()
    {
        _netServer.PollEvents();
    }

    private void OnDestroy()
    {
        NetDebug.Logger = null;
        if (_netServer != null)
            _netServer.Stop();
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        Debug.Log($"[SERVER] New peer connected: {peer.Id}");

        // 为新连接的客户端分配随机位置
        if (!clientPositions.ContainsKey(peer.Id))
        {
            Vector3 randomPosition = new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
            clientPositions[peer.Id] = randomPosition;
        }

        // 广播所有客户端的位置信息
        BroadcastClientPositions();
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[SERVER] Peer disconnected: {peer.Id}, Reason: {disconnectInfo.Reason}");
        clientPositions.Remove(peer.Id); // 移除断开连接的客户端
        BroadcastClientPositions(); // 更新其他客户端
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        string requestType = reader.GetString();

        if (requestType == "RequestClientPositions")
        {
            Debug.Log($"[SERVER] Received position request from client {peer.Id}");
            SendClientPositions(peer);
        }
    }

    private void BroadcastClientPositions()
    {
        _dataWriter.Reset();
        _dataWriter.Put("ClientPositions");
        _dataWriter.Put(clientPositions.Count);

        foreach (var kvp in clientPositions)
        {
            _dataWriter.Put(kvp.Key); // 客户端ID
            _dataWriter.Put(kvp.Value.x);
            _dataWriter.Put(kvp.Value.y);
            _dataWriter.Put(kvp.Value.z);
        }

        foreach (var peer in _netServer.ConnectedPeerList)
        {
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
        }
    }

    private void SendClientPositions(NetPeer peer)
    {
        _dataWriter.Reset();
        _dataWriter.Put("ClientPositions");
        _dataWriter.Put(clientPositions.Count);

        foreach (var kvp in clientPositions)
        {
            _dataWriter.Put(kvp.Key); // 客户端ID
            _dataWriter.Put(kvp.Value.x);
            _dataWriter.Put(kvp.Value.y);
            _dataWriter.Put(kvp.Value.z);
        }

        peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.Log($"[SERVER] Network error: {socketErrorCode}");
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.Broadcast)
        {
            Debug.Log("[SERVER] Received discovery request. Sending discovery response.");
            NetDataWriter resp = new NetDataWriter();
            resp.Put(1);
            _netServer.SendUnconnectedMessage(resp, remoteEndPoint);
        }
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("sample_app");
    }

    void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
    {
        Debug.LogFormat(str, args);
    }
}
