using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGamebackModels;
using GameLogic;
using Network;
using System.Collections;
using System.Threading.Tasks;

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

        // 回合计时器
        private Dictionary<int, System.Threading.CancellationTokenSource> turnTimers = new Dictionary<int, System.Threading.CancellationTokenSource>();
        private const int TurnTimeoutSeconds = 120; // 2分钟超时
        
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
            else if (requestType == "CompCards")
            {
                if (!pendingCardData.ContainsKey(peer))
                {
                    Debug.LogError($"[SERVER] No pending composition data for peer {peer.Id}");
                    return;
                }
                string compData = pendingCardData[peer];
                pendingCardData.Remove(peer); // 清理已处理的数据
                HandleCompCards(peer, compData);
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
            // 停止该玩家的回合计时器
            StopTurnTimer(peer.Id);
            
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
                var cardsWrapper = JsonUtility.FromJson<ApiResponse<List<CardModel>>>(cardsJson);
                var playedCards = cardsWrapper.Data;
                
                // 处理共享牌库中的卡牌
                cardManager.HandleCardsPlayed(peer.Id, playedCards);
                
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
                  // 交换回合，向双方发送回合通知
                if (playerRoomId.HasValue)
                {
                    var roomPlayers = rooms[playerRoomId.Value];
                    List<NetPeer> roomPeers = new List<NetPeer>();
                    
                    foreach (var playerId in roomPlayers)
                    {
                        var roomPeer = netManager.GetPeerById(playerId);
                        if (roomPeer != null)
                        {
                            roomPeers.Add(roomPeer);
                        }
                    }
                    
                    // 找出除攻击者外的玩家（即承受者）作为下一个出牌者
                    NetPeer nextPlayer = null;
                    foreach (var roomPeer in roomPeers)
                    {
                        if (roomPeer.Id != peer.Id)
                        {
                            nextPlayer = roomPeer;
                            break;
                        }
                    }
                    
                    // 如果找到下一个玩家，发送回合通知
                    if (nextPlayer != null)
                    {
                        // 使用SendPlayerToPlay方法复用回合通知逻辑
                        SendPlayerToPlay(roomPeers, nextPlayer);
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
                cardManager.InitializeCardsForPlayers(roomId, roomPeers);

                // 随机选择一名玩家作为先手并发送回合通知
                if (roomPeers.Count > 0)
                {
                    // 随机选择一名玩家作为先手
                    int randomIndex = UnityEngine.Random.Range(0, roomPeers.Count);
                    NetPeer firstPlayer = roomPeers[randomIndex];
                    SendPlayerToPlay(roomPeers, firstPlayer);
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
        private void SendPlayerToPlay(List<NetPeer> roomPeers, NetPeer firstPlayer)
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

            // 为当前玩家设置回合计时器
            StartTurnTimer(firstPlayer);
        }

        // 开始回合计时器
        private void StartTurnTimer(NetPeer player)
        {
            // 如果已有计时器，先取消
            if (turnTimers.ContainsKey(player.Id))
            {
                turnTimers[player.Id].Cancel();
                turnTimers.Remove(player.Id);
            }

            // 创建新的计时器
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            turnTimers[player.Id] = cancellationTokenSource;

            // 开始异步计时任务
            StartTurnTimerTask(player, cancellationTokenSource.Token);
        }

        // 异步回合计时任务
        private async void StartTurnTimerTask(NetPeer player, System.Threading.CancellationToken cancellationToken)
        {
            try 
            {
                // 等待指定的超时时间
                await Task.Delay(TimeSpan.FromSeconds(TurnTimeoutSeconds), cancellationToken);
                
                // 如果没有被取消，则执行超时处理
                if (!cancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"[SERVER] Player {player.Id} turn timeout. Forcing play.");
                    SendForcePlayRequest(player);
                }
            }
            catch (TaskCanceledException)
            {
                // 计时器被取消，正常情况下玩家已出牌
                Debug.Log($"[SERVER] Turn timer for player {player.Id} was cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SERVER] Error in turn timer: {ex.Message}");
            }
        }

        // 发送强制出牌请求
        private void SendForcePlayRequest(NetPeer player)
        {
            try
            {
                // 创建强制出牌的消息
                var forcePlayResponse = ApiResponse.Success("回合超时，即将自动出牌");
                var writer = new NetDataWriter();
                writer.Put("ForcePlayRequest");
                writer.Put(forcePlayResponse.Serialize());
                
                // 发送给超时的玩家
                player.Send(writer, DeliveryMethod.ReliableOrdered);
                
                Debug.Log($"[SERVER] Sent force play request to player {player.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SERVER] Error sending force play request: {ex.Message}");
            }
        }

        // 停止回合计时器
        private void StopTurnTimer(int playerId)
        {
            if (turnTimers.ContainsKey(playerId))
            {
                turnTimers[playerId].Cancel();
                turnTimers.Remove(playerId);
                Debug.Log($"[SERVER] Turn timer stopped for player {playerId}");
            }
        }

        // 处理卡牌合成请求
        public void HandleCompCards(NetPeer peer, string requestData)
        {
            try
            {
                Debug.Log($"[SERVER] Processing composition request from player {peer.Id}");
                
                // 反序列化客户端的请求数据，使用服务器端的模型
                var clientRequest = JsonUtility.FromJson<ApiResponse<List<CardModel>>>(requestData);

                // 将客户端的卡牌数据转换为服务器端的CardModel
                var cardsToCompose = clientRequest.Data;
                
                // 使用CardManager处理卡牌合成逻辑
                var newCards = cardManager.HandleCardComposition(peer.Id, cardsToCompose);
                
                // 创建合成结果响应
                var compResult = new ApiResponse<CompResult>
                {
                    Code = 200,
                    Message = "合成成功",
                    Data = new CompResult
                    {
                        Success = true,
                        UsedCards = clientRequest.Data, 
                        NewCards = newCards,
                        ComposeType = ComposeType.Self // 设置为我方合成
                    }
                };
                
                // 向合成请求者发送结果
                string resultJson = JsonUtility.ToJson(compResult);
                var writer = new NetDataWriter();
                writer.Put("CompResult");
                writer.Put(resultJson);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                
                // 获取玩家所在房间
                int? roomId = null;
                foreach (var room in rooms)
                {
                    if (room.Value.Contains(peer.Id))
                    {
                        roomId = room.Key;
                        break;
                    }
                }

                // 向房间内其他玩家发送敌方合成结果
                if (roomId.HasValue)
                {
                    var roomPlayers = rooms[roomId.Value];
                    foreach (var playerId in roomPlayers)
                    {
                        if (playerId != peer.Id) // 跳过发送请求的玩家
                        {
                            var otherPeer = netManager.GetPeerById(playerId);
                            if (otherPeer != null)
                            {
                                var enemyResult = new ApiResponse<CompResult>
                                {
                                    Code = 200,
                                    Message = "敌方合成卡牌",
                                    Data = new CompResult
                                    {
                                        Success = true,
                                        NewCards = newCards,
                                        ComposeType = ComposeType.Enemy // 设置为敌方合成
                                    }
                                };
                                
                                string enemyJson = JsonUtility.ToJson(enemyResult);
                                var enemyWriter = new NetDataWriter();
                                enemyWriter.Put("CompResult");
                                enemyWriter.Put(enemyJson);
                                otherPeer.Send(enemyWriter, DeliveryMethod.ReliableOrdered);
                            }
                        }
                    }
                }
                
                Debug.Log($"[SERVER] Composition successful for player {peer.Id}, created {newCards.Count} new cards");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SERVER] Error processing composition request: {ex.Message}");
                // 发送错误响应
                SendCompErrorResponse(peer, $"服务器处理合成请求时出错: {ex.Message}");
            }
        }
        
        // 发送合成错误响应
        private void SendCompErrorResponse(NetPeer peer, string errorMessage)
        {
            var errorResponse = new ApiResponse<CompResult>
            {
                Code = 500,
                Message = errorMessage,
                Data = new CompResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    UsedCards = null,
                    NewCards = null
                }
            };

            // 序列化并发送错误响应
            string errorJson = JsonUtility.ToJson(errorResponse);
            var writer = new NetDataWriter();
            writer.Put("CompResult");
            writer.Put(errorJson);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }
    }
}
