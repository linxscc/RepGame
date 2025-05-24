using System;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGamebackModels;
using GameLogic;
using Network;

namespace GameLogic
{
    public class HandleNetworkRequest
    {
        // 保存卡牌数据的字典
        private Dictionary<NetPeer, string> pendingCardData = new Dictionary<NetPeer, string>();
        
        // 用于房间和玩家管理的引用
        private Dictionary<int, List<int>> rooms;
        private HashSet<int> readyPlayers;
        private int nextRoomId;
        private const int RequiredPlayersToStart = 2;
        
        // 卡牌管理器引用
        private CardManager cardManager;
          // 客户端位置处理器
        private ClientPositionHandler clientPositionHandler;
        
        // 保存网络实例的引用
        private NetManager netManager;
        
        public HandleNetworkRequest(
            NetManager netManager,
            Dictionary<int, List<int>> rooms, 
            HashSet<int> readyPlayers, 
            ref int nextRoomId, 
            CardManager cardManager,
            ClientPositionHandler clientPositionHandler,
            Dictionary<int, Vector3> clientPositions)
        {
            this.netManager = netManager;
            this.rooms = rooms;
            this.readyPlayers = readyPlayers;
            this.nextRoomId = nextRoomId;
            this.cardManager = cardManager;
            this.clientPositionHandler = clientPositionHandler;
            this.clientPositions = clientPositions;
        }

        public void HandleRequest(NetPeer peer, string requestType)
        {
            if (requestType == "RequestClientPositions")
            {
                HandleRequestClientPositions(peer);
            }
            else if (requestType == "StartCardGame")
            {
                HandleStartCardGame(peer);
            }
            else if (requestType == "PlayCards")
            {
                HandlePlayCards(peer);
            }
            else if (requestType == "SurrenderCardGame")
            {
                HandleSurrenderCardGame(peer);
            }
        }
        
        // 处理客户端位置请求
        private void HandleRequestClientPositions(NetPeer peer)
        {
            Debug.Log($"[SERVER] Received position request from client {peer.Id}");
            
            // 从GameServer获取clientPositions
            // 我们需要通过构造函数注入clientPositions
            Dictionary<int, Vector3> clientPositions = GetClientPositions();
            clientPositionHandler.SendClientPositions(peer, clientPositions);
        }
        
        // 客户端位置数据
        private Dictionary<int, Vector3> clientPositions;
        
        // 获取客户端位置
        private Dictionary<int, Vector3> GetClientPositions()
        {
            return clientPositions;
        }
        
        // 处理出牌请求
        public void HandlePlayCards(NetPeer peer)
        {
            if (!pendingCardData.ContainsKey(peer))
            {
                Debug.LogError($"[SERVER] No pending card data for peer {peer.Id}");
                return;
            }
            try
            {
                string cardsJson = pendingCardData[peer];
                pendingCardData.Remove(peer); // 清理已处理的数据
                // 反序列化卡牌列表
                var playedCards = JsonUtility.FromJson<List<CardModel>>(cardsJson);
                // 使用DamageCalculator计算伤害，同时指定伤害类型为攻击者
                var damageResult = DamageCalculator.CalculateDamage(playedCards, DamageType.Attacker);
                // 使用RepGamebackModels命名空间下的ApiResponse封装结果
                var apiResponse = ApiResponse<DamageResult>.Success(damageResult, "出牌处理成功");
                string resultJson = apiResponse.Serialize();
                // 发送结果给当前出牌玩家
                var writer = new NetDataWriter();
                writer.Put("DamageResult");
                writer.Put(resultJson);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);

                // 给同一房间内的其他玩家发送相同数据，但伤害类型为承受者
                int? playerRoomId = null;
                foreach (var room in rooms)
                {
                    if (room.Value.Contains(peer.Id))
                    {
                        playerRoomId = room.Key;
                        break;
                    }
                }
                if (playerRoomId.HasValue)
                {
                    var roomPlayers = rooms[playerRoomId.Value];
                    foreach (var playerId in roomPlayers)
                    {
                        if (playerId == peer.Id) continue; // 跳过自己
                        var otherPeer = netManager.GetPeerById(playerId);
                        if (otherPeer != null)
                        {
                            // 复制伤害结果并设置为承受者
                            var receiverResult = new DamageResult
                            {
                                TotalDamage = damageResult.TotalDamage,
                                ProcessedCards = damageResult.ProcessedCards,
                                Type = DamageType.Receiver
                            };
                            var receiverResponse = ApiResponse<DamageResult>.Success(receiverResult, "你受到伤害");
                            var receiverJson = receiverResponse.Serialize();
                            var receiverWriter = new NetDataWriter();
                            receiverWriter.Put("DamageResult");
                            receiverWriter.Put(receiverJson);
                            otherPeer.Send(receiverWriter, DeliveryMethod.ReliableOrdered);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 发送错误响应
                var errorResponse = ApiResponse<DamageResult>.Fail(500, $"处理出牌请求出错: {ex.Message}");
                var writer = new NetDataWriter();
                writer.Put("DamageResult");
                writer.Put(errorResponse.Serialize());
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                Debug.LogError($"[SERVER] Error processing PlayCards request: {ex.Message}");
            }
        }
        
        // 储存卡牌数据
        public void StorePendingCardData(NetPeer peer, string cardData)
        {
            pendingCardData[peer] = cardData;
        }
        
        // 处理开始游戏请求
        private void HandleStartCardGame(NetPeer peer)
        {
            Debug.Log($"[SERVER] Player {peer.Id} is ready to start the card game.");
            readyPlayers.Add(peer.Id);

            if (readyPlayers.Count >= RequiredPlayersToStart)
            {
                CreateRoomAndInitializeCards();
            }
        }
        
        // 处理投降请求
        private void HandleSurrenderCardGame(NetPeer peer)
        {
            Debug.Log($"[SERVER] Player {peer.Id} has surrendered the card game.");
            RemovePlayerFromRoom(peer.Id);
        }

        // 从房间中移除玩家
        private void RemovePlayerFromRoom(int playerId)
        {
            foreach (var room in rooms)
            {
                if (room.Value.Contains(playerId))
                {
                    room.Value.Remove(playerId);
                    Debug.Log($"[SERVER] Player {playerId} removed from room {room.Key}.");

                    // 如果房间为空，删除房间
                    if (room.Value.Count == 0)
                    {
                        rooms.Remove(room.Key);
                        Debug.Log($"[SERVER] Room {room.Key} deleted as it is empty.");
                    }
                    break;
                }
            }
        }

        // 创建房间并初始化卡牌
        private void CreateRoomAndInitializeCards()
        {
            int roomId = nextRoomId++;
            List<int> roomPlayerIds = new List<int>(readyPlayers);
            rooms[roomId] = roomPlayerIds;

            Debug.Log($"[SERVER] Room {roomId} created with players {string.Join(", ", roomPlayerIds)}.");

            // 收集房间中所有玩家的NetPeer实例
            List<NetPeer> roomPeers = new List<NetPeer>();
            foreach (int playerId in roomPlayerIds)
            {
                NetPeer roomPeer = netManager.GetPeerById(playerId);
                if (roomPeer != null)
                {
                    roomPeers.Add(roomPeer);
                }
            }            // 为房间初始化卡牌
            try
            {
                cardManager.InitializeCardsForPlayers(roomPeers);
                
                // 随机选择一名玩家作为先手并发送回合通知
                if (roomPeers.Count > 0)
                {
                    // 随机选择一名玩家作为先手
                    int randomIndex = UnityEngine.Random.Range(0, roomPeers.Count);
                    NetPeer firstPlayer = roomPeers[randomIndex];
                    SendPlayerToPlay(roomPeers,firstPlayer);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SERVER] Error initializing cards for room {roomId}: {ex.Message}");
            }

            // 创建房间后清除准备玩家列表
            readyPlayers.Clear();
        }

        // 发送回合通知
        private void SendPlayerToPlay(List<NetPeer> roomPeers,NetPeer firstPlayer)
        {       
            // 向先手玩家发送回合开始消息，使用ApiResponse包装消息
            var firstPlayerResponse = ApiResponse.Success("你的回合开始，请出牌");
            var firstPlayerWriter = new NetDataWriter();
            firstPlayerWriter.Put("TurnNotification");
            firstPlayerWriter.Put(firstPlayerResponse.Serialize());
            firstPlayer.Send(firstPlayerWriter, DeliveryMethod.ReliableOrdered);
            
            // 向其他玩家发送等待消息，使用ApiResponse包装消息
            foreach (var peer in roomPeers)
            {
                if (peer.Id != firstPlayer.Id)
                {
                    var otherPlayerResponse = ApiResponse.Success("对方回合，请等待");
                    var otherPlayerWriter = new NetDataWriter();
                    otherPlayerWriter.Put("TurnNotification");
                    otherPlayerWriter.Put(otherPlayerResponse.Serialize());
                    peer.Send(otherPlayerWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }
    }
}
