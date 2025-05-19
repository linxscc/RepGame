using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using RepGamebackModels;
using GameLogic; // Correct namespace for CardManager
using Network;


public class GameServer : MonoBehaviour, INetEventListener
{
    private NetManager _netServer;
    private Dictionary<int, Vector3> clientPositions = new Dictionary<int, Vector3>(); // 客户端ID和位置映射
    private List<int> connectedPlayers = new List<int>();
    private CardManager cardManager = new CardManager();
    private ClientPositionHandler clientPositionHandler = new ClientPositionHandler();

    private Dictionary<int, List<int>> rooms = new Dictionary<int, List<int>>(); // Room ID to list of player IDs
    private int nextRoomId = 1; // Incremental room ID tracker

    private Queue<(NetPeer peer, string requestType)> requestQueue = new Queue<(NetPeer, string)>();
    private const int MaxRequestsPerFrame = 10; // Limit the number of requests processed per frame
    private const int RequiredPlayersToStart = 2; // Number of players required to start a card game
    private HashSet<int> readyPlayers = new HashSet<int>(); // Track players ready to start the game
    private const int MaxRequestsBeforeMultithreading = 20; // Threshold for enabling multithreading

    private void Start()
    {
        _netServer = new NetManager(this);
        _netServer.Start(9050);
        Debug.Log("[SERVER] Server started on port 9050");
    }

    private void Update()
    {
        _netServer.PollEvents();

        if (requestQueue.Count > MaxRequestsBeforeMultithreading)
        {
            // Process requests in a separate thread
            System.Threading.ThreadPool.QueueUserWorkItem(ProcessRequestsInBackground);
        }
        else
        {
            // Process requests in the main thread
            ProcessRequestsInMainThread();
        }
    }

    private void OnDestroy()
    {
        if (_netServer != null)
            _netServer.Stop();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log($"[SERVER] New peer connected: {peer.Id}");

        // Add the connected player to the list
        if (!connectedPlayers.Contains(peer.Id))
        {
            connectedPlayers.Add(peer.Id);
        }

        // 为新连接的客户端分配随机位置
        // if (!clientPositions.ContainsKey(peer.Id))
        // {
        //     Vector3 randomPosition = new Vector3(UnityEngine.Random.Range(40f, 50f), 1.5f, UnityEngine.Random.Range(5, 10f));
        //     clientPositions[peer.Id] = randomPosition;
        // }

        // 广播所有客户端的位置信息
        // clientPositionHandler.BroadcastClientPositions(_netServer, clientPositions);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[SERVER] Peer disconnected: {peer.Id}");

        // Remove the player from connected players and their room
        clientPositions.Remove(peer.Id);
        connectedPlayers.Remove(peer.Id);

        foreach (var room in rooms)
        {
            if (room.Value.Contains(peer.Id))
            {
                room.Value.Remove(peer.Id);
                Debug.Log($"[SERVER] Player {peer.Id} removed from room {room.Key}.");

                if (room.Value.Count == 0)
                {
                    rooms.Remove(room.Key);
                    Debug.Log($"[SERVER] Room {room.Key} deleted as it is empty.");
                }
                break;
            }
        }

        // 通知其他客户端移除该玩家
        // clientPositionHandler.BroadcastPlayerRemoval(peer.Id);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        string requestType = reader.GetString();

        // Enqueue the request for processing
        requestQueue.Enqueue((peer, requestType));
    }

    private void HandleRequest(NetPeer peer, string requestType)
    {
        if (requestType == "RequestClientPositions")
        {
            Debug.Log($"[SERVER] Received position request from client {peer.Id}");
            clientPositionHandler.SendClientPositions(peer, clientPositions);
        }
        else if (requestType == "StartCardGame")
        {
            Debug.Log($"[SERVER] Player {peer.Id} is ready to start the card game.");
            readyPlayers.Add(peer.Id);

            // Check if enough players are ready to start the game
            if (readyPlayers.Count >= RequiredPlayersToStart)
            {
                CreateRoomAndInitializeCards();
            }
        }
        else if (requestType == "SurrenderCardGame")
        {
            Debug.Log($"[SERVER] Player {peer.Id} has surrendered the card game.");
            RemovePlayerFromRoom(peer.Id);
        }
    }

    private void RemovePlayerFromRoom(int playerId)
    {
        foreach (var room in rooms)
        {
            if (room.Value.Contains(playerId))
            {
                room.Value.Remove(playerId);
                Debug.Log($"[SERVER] Player {playerId} removed from room {room.Key}.");

                // If the room is empty, remove the room
                if (room.Value.Count == 0)
                {
                    rooms.Remove(room.Key);
                    Debug.Log($"[SERVER] Room {room.Key} deleted as it is empty.");
                }
                break;
            }
        }
    }

    private void CreateRoomAndInitializeCards()
    {
        int roomId = nextRoomId++;
        List<int> roomPlayerIds = new List<int>(readyPlayers);
        rooms[roomId] = roomPlayerIds;

        Debug.Log($"[SERVER] Room {roomId} created with players {string.Join(", ", roomPlayerIds)}.");

        // Collect NetPeer instances for the room
        List<NetPeer> roomPeers = new List<NetPeer>();
        foreach (int playerId in roomPlayerIds)
        {
            NetPeer roomPeer = _netServer.GetPeerById(playerId);
            if (roomPeer != null)
            {
                roomPeers.Add(roomPeer);
            }
        }

        // Initialize cards for the room
        try
        {
            cardManager.InitializeCardsForPlayers(roomPeers);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SERVER] Error initializing cards for room {roomId}: {ex.Message}");
        }

        // Clear the ready players list after creating the room
        readyPlayers.Clear();
    }

    private void ProcessRequestsInMainThread()
    {
        int processedRequests = 0;
        while (requestQueue.Count > 0 && processedRequests < MaxRequestsPerFrame)
        {
            var (peer, requestType) = requestQueue.Dequeue();
            HandleRequest(peer, requestType);
            processedRequests++;
        }
    }

    private void ProcessRequestsInBackground(object state)
    {
        while (requestQueue.Count > 0)
        {
            var (peer, requestType) = requestQueue.Dequeue();
            HandleRequest(peer, requestType);
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"[SERVER] Network error: {socketError}");
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // 处理未连接的消息
        Debug.Log($"[SERVER] Unconnected message received from {remoteEndPoint}");
    }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        Debug.Log($"[SERVER] Latency updated for peer {peer.Id}: {latency} ms");
    }
    public void OnConnectionRequest(ConnectionRequest request)
    {
        Debug.Log("[SERVER] Connection request received");
        request.AcceptIfKey("demo"); // 验证连接密钥
    }

}
