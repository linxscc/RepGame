using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGamebackModels;

namespace GameLogic
{
    public class CardManager
    {
        // 每个房间按等级分类的共享牌库
        private Dictionary<int, Dictionary<int, List<CardModel>>> roomLevelDecks = new Dictionary<int, Dictionary<int, List<CardModel>>>();
        // 每个玩家的手牌
        private Dictionary<int, List<CardModel>> playerHands = new Dictionary<int, List<CardModel>>();
        // 记录玩家所在的房间
        private Dictionary<int, int> playerRoomMap = new Dictionary<int, int>();
        // 卡牌配置
        private CardDeckWrapper cardDeckConfig;

        private NetManager _netManager;
        // 血量相关
        private const int INITIAL_HEALTH = 100;
        private Dictionary<int, int> playerHealth = new Dictionary<int, int>();
        private Dictionary<int, int> roomPlayerCount = new Dictionary<int, int>();
        private Dictionary<int, List<int>> rooms;

        // Expose certain properties to extension methods
        public Dictionary<int, List<CardModel>> GetPlayerHands() => playerHands;
        public Dictionary<int, Dictionary<int, List<CardModel>>> GetAllRoomLevelDecks() => roomLevelDecks;
        public Dictionary<int, Dictionary<int, List<CardModel>>> GetRoomLevelDecks(int roomId)
        {
            if (roomLevelDecks.ContainsKey(roomId))
            {
                return new Dictionary<int, Dictionary<int, List<CardModel>>> { { roomId, roomLevelDecks[roomId] } };
            }
            return null;
        }

        public void SetNetManager(NetManager netManager)
        {
            _netManager = netManager;
        }

        public CardManager(NetManager netManager)
        {
            _netManager = netManager;
            // 加载卡牌配置
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Config/CardDeck.json");
            string jsonContent = File.ReadAllText(jsonPath);
            cardDeckConfig = JsonUtility.FromJson<CardDeckWrapper>(jsonContent);
        }

        public void SetRooms(Dictionary<int, List<int>> gameRooms)
        {
            this.rooms = gameRooms;
        }
        public void CleanRooms(int playerId)
        {
            if (playerRoomMap.ContainsKey(playerId))
            {
                int gameRoomid = playerRoomMap[playerId];
                if (this.rooms.ContainsKey(gameRoomid))
                {
                    this.rooms.Remove(gameRoomid);
                }
                if (roomLevelDecks.ContainsKey(gameRoomid))
                {
                    roomLevelDecks.Remove(gameRoomid);
                }
                if (roomPlayerCount.ContainsKey(gameRoomid))
                {
                    roomPlayerCount.Remove(gameRoomid);
                }
                rooms.Remove(gameRoomid);
                playerRoomMap.Remove(playerId);
                playerHands.Remove(playerId);
                playerHealth.Remove(playerId);
            }
        }


        private void SendToPlayer(int playerId, string messageType, string json)
        {
            if (_netManager == null)
            {
                Debug.LogError("NetManager not set!");
                return;
            }

            var peer = _netManager.GetPeerById(playerId);
            if (peer != null)
            {
                var writer = new NetDataWriter();
                writer.Put(messageType);
                writer.Put(json);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                Debug.LogError($"Could not find peer with ID: {playerId}");
            }
        }

        public void InitializeCardsForPlayers(int roomId, List<NetPeer> roomPeers)
        {
            // 记录房间玩家数量
            roomPlayerCount[roomId] = roomPeers.Count;

            List<int> playerIds = roomPeers.Select(peer => peer.Id).ToList();
            SetRooms(new Dictionary<int, List<int>> { { roomId, playerIds } });

            // 创建房间的分级共享牌库
            var levelDecks = CreateLevelDecks(roomId);
            roomLevelDecks[roomId] = levelDecks;
            foreach (var peer in roomPeers)
            {
                // 记录玩家所在的房间
                playerRoomMap[peer.Id] = roomId;

                // 初始化玩家血量
                playerHealth[peer.Id] = INITIAL_HEALTH;

                // 为玩家只从1级牌库中抽取6张卡牌
                List<CardModel> playerHand = DrawRandomCardsFromLevel(roomId, 1, 6);
                playerHands[peer.Id] = playerHand;

                // 发送卡牌和血量信息给玩家
                SendInitialDataToPlayer(peer, playerHand);
            }
        }

        private Dictionary<int, List<CardModel>> CreateLevelDecks(int roomId)
        {
            var levelDecks = new Dictionary<int, List<CardModel>>();

            // 根据配置创建分级牌库
            foreach (var cardConfig in cardDeckConfig.cards)
            {
                // 确保等级对应的列表已创建
                if (!levelDecks.ContainsKey(cardConfig.level))
                {
                    levelDecks[cardConfig.level] = new List<CardModel>();
                }

                // 根据value创建对应数量的卡牌
                for (int i = 0; i < cardConfig.value; i++)
                {
                    var card = new CardModel
                    {
                        CardID = $"{roomId}-{Guid.NewGuid()}",
                        Type = GetCardType(cardConfig.name),
                        Damage = cardConfig.damage,
                        TargetName = cardConfig.targetname,
                        Level = cardConfig.level
                    };
                    levelDecks[cardConfig.level].Add(card);
                }
            }

            // 打乱每个等级牌库的顺序
            foreach (var level in levelDecks.Keys.ToList())
            {
                levelDecks[level] = levelDecks[level].OrderBy(x => Guid.NewGuid()).ToList();
            }

            return levelDecks;
        }

        private List<CardModel> DrawRandomCardsFromLevel(int roomId, int level, int count)
        {
            var levelDecks = roomLevelDecks[roomId];
            if (!levelDecks.ContainsKey(level))
            {
                throw new Exception($"找不到{level}级牌库");
            }

            var deck = levelDecks[level];
            var drawnCards = deck.Take(count).ToList();
            deck.RemoveRange(0, Math.Min(count, deck.Count));
            return drawnCards;
        }
        // 从共享牌库中抽取随机卡牌（仅从1级牌库中抽取）
        public List<CardModel> DrawRandomCardsFromSharedDeck(int playerId, int count)
        {
            if (!playerRoomMap.ContainsKey(playerId))
            {
                Debug.LogError($"找不到玩家 {playerId} 的房间数据");
                return new List<CardModel>();
            }

            int roomId = playerRoomMap[playerId];
            var levelDecks = roomLevelDecks[roomId];

            // 确保1级牌库存在
            if (!levelDecks.ContainsKey(1) || levelDecks[1] == null || levelDecks[1].Count == 0)
            {
                Debug.LogWarning($"房间 {roomId} 的1级牌库不存在或为空");
                return new List<CardModel>();
            }

            // 仅从1级牌库中获取卡牌
            var level1Cards = levelDecks[1];

            // 随机洗牌
            level1Cards = level1Cards.OrderBy(x => Guid.NewGuid()).ToList();

            // 抽取指定数量的卡牌
            int cardsToTake = Math.Min(count, level1Cards.Count);
            var selectedCards = level1Cards.Take(cardsToTake).ToList();

            // 从1级牌库中移除这些卡牌
            foreach (var card in selectedCards)
            {
                level1Cards.Remove(card);
            }

            playerHands[playerId].AddRange(selectedCards);

            Debug.Log($"为玩家 {playerId} 从1级共享牌库中抽取了 {selectedCards.Count} 张卡牌");

            return selectedCards;
        }

        private void SendInitialDataToPlayer(NetPeer peer, List<CardModel> cards)
        {
            // 创建包含卡牌和血量的响应数据
            var initData = new InitPlayerData
            {
                Cards = cards,
                Health = INITIAL_HEALTH
            };            // 创建API响应
            var response = new ApiResponse<InitPlayerData>
            {
                Code = 200,
                Message = "游戏初始化成功",
                Data = initData
            };

            // 序列化响应
            string json = JsonUtility.ToJson(response);
            Debug.Log($"Sending initial data to player {peer.Id}: {json}");

            // 发送给客户端
            SendToPlayer(peer.Id, "InitPlayerData", json);
        }

        public bool ProcessDamage(int attackerId, int receiverId, DamageResult damageResult)
        {
            if (!playerHealth.ContainsKey(receiverId))
            {
                throw new Exception($"找不到玩家 {receiverId} 的血量数据");
            }

            // 检查游戏是否结束
            if (playerHealth[receiverId] <= 0)
            {
                int roomId = playerRoomMap[receiverId];
                HandleGameOver(roomId, attackerId, receiverId);
                return true; // 游戏结束
            }

            return false; // 游戏继续
        }

        private void HandleGameOver(int roomId, int winnerId, int loserId)
        {
            // 发送游戏结束消息给胜利者
            var winnerResponse = new ApiResponse<GameOverResponse>
            {
                Code = 200,
                Message = "游戏胜利！",
                Data = new GameOverResponse { IsWinner = true }
            };
            SendToPlayer(winnerId, "GameOver", JsonUtility.ToJson(winnerResponse));            // 发送游戏结束消息给失败者
            var loserResponse = new ApiResponse<GameOverResponse>
            {
                Code = 200,
                Message = "游戏失败！",
                Data = new GameOverResponse { IsWinner = false }
            };
            SendToPlayer(loserId, "GameOver", JsonUtility.ToJson(loserResponse));

            // 清理房间数据
            CleanupRoom(roomId);
        }

        private void CleanupRoom(int roomId)
        {
            // 获取房间内的所有玩家
            var roomPlayers = playerRoomMap.Where(kvp => kvp.Value == roomId).Select(kvp => kvp.Key).ToList();

            // 清理所有相关数据
            foreach (var playerId in roomPlayers)
            {
                playerHands.Remove(playerId);
                playerHealth.Remove(playerId);
                playerRoomMap.Remove(playerId);
            }

            roomLevelDecks.Remove(roomId);
            roomPlayerCount.Remove(roomId);
        }

        public CardType GetCardType(string cardName)
        {
            if (Enum.TryParse(cardName, out CardType cardType))
            {
                return cardType;
            }
            return CardType.木匠学徒;
        }
        // 处理卡牌合成，返回合成后的新卡牌数据
        public List<CardModel> HandleCardComposition(int playerId, List<CardModel> validatedCards)
        {
            if (!playerRoomMap.ContainsKey(playerId))
            {
                throw new Exception($"找不到玩家 {playerId} 的房间数据");
            }

            // 验证卡牌数量是否是3的倍数
            if (validatedCards.Count % 3 != 0)
            {
                throw new Exception("合成失败：卡牌数量必须是3的倍数");
            }

            int roomId = playerRoomMap[playerId];
            var levelDecks = roomLevelDecks[roomId];
            var playerHand = playerHands[playerId]; // 获取玩家手牌引用

            // 按卡牌类型和等级分组
            var cardGroups = validatedCards.GroupBy(c => new { c.Type, c.Level }).ToList();
            var resultCards = new List<CardModel>();

            try
            {
                foreach (var group in cardGroups)
                {
                    // 验证每组卡牌数量是否是3的倍数
                    if (group.Count() % 3 != 0)
                    {
                        throw new Exception($"合成失败：类型 {group.Key.Type} 等级 {group.Key.Level} 的卡牌数量不是3的倍数");
                    }

                    // 验证卡牌等级
                    if (group.Any(c => c.Level != group.Key.Level))
                    {
                        throw new Exception("合成失败：同组卡牌等级必须相同");
                    }

                    // 计算这个类型需要多少张升级卡牌
                    int upgradeCount = group.Count() / 3;

                    // 获取目标卡牌类型和等级
                    var targetCardType = GetCardType(group.First().TargetName);
                    int targetLevel = group.Key.Level + 1;

                    // 检查目标等级牌库是否存在
                    if (!levelDecks.ContainsKey(targetLevel))
                    {
                        throw new Exception($"合成失败：不存在{targetLevel}级牌库");
                    }

                    // 从目标等级牌库中查找升级卡牌
                    var availableUpgradeCards = levelDecks[targetLevel]
                        .Where(c => c.Type == targetCardType)
                        .ToList();

                    // 检查是否有足够的升级卡牌
                    if (availableUpgradeCards.Count < upgradeCount)
                    {
                        throw new Exception($"合成失败：{targetLevel}级牌库中没有足够的 {targetCardType} 卡牌");
                    }

                    // 获取需要的升级卡牌
                    var upgradeCards = availableUpgradeCards.Take(upgradeCount).ToList();

                    // 从目标等级牌库中移除这些升级卡牌
                    foreach (var card in upgradeCards)
                    {
                        levelDecks[targetLevel].Remove(card);
                    }

                    // 添加到结果列表
                    resultCards.AddRange(upgradeCards);
                }
                // 从玩家手牌中移除已合成的卡牌
                foreach (var card in validatedCards)
                {
                    playerHand.RemoveAll(c => c.CardID == card.CardID);
                    Debug.Log($"从玩家 {playerId} 手牌中移除卡牌 {card.CardID}");
                }
                // 将新卡牌添加到玩家手牌中
                playerHand.AddRange(resultCards);
                playerHands[playerId] = playerHand;

                return resultCards;
            }
            catch (Exception)
            {
                // 合成失败，确保牌库恢复原状
                roomLevelDecks[roomId] = levelDecks;
                throw;
            }
        }
        public List<CardModel> ValidateAndRemovePlayedCards(int playerId, List<CardModel> clientCards)
        {
            if (!playerHands.ContainsKey(playerId))
            {
                Debug.LogError($"找不到玩家 {playerId} 的手牌数据");
                return null;
            }

            var playerHand = playerHands[playerId];
            var validatedCards = new List<CardModel>();

            // 验证所有卡牌是否都在玩家手牌中，并收集完整的卡牌模型
            foreach (var clientCard in clientCards)
            {
                // 寻找手牌中匹配ID的完整卡牌数据
                var serverCard = playerHand.FirstOrDefault(c => c.CardID == clientCard.CardID);
                if (serverCard == null)
                {
                    Debug.LogError($"卡牌 {clientCard.CardID} 不在玩家 {playerId} 的手牌中");
                    return null;
                }

                // 添加服务器端的完整卡牌数据到结果列表
                validatedCards.Add(serverCard);
            }

            // 所有卡牌验证通过，从手牌中移除这些卡牌
            foreach (var card in clientCards)
            {
                playerHand.RemoveAll(c => c.CardID == card.CardID);
                Debug.Log($"从玩家 {playerId} 手牌中移除卡牌 {card.CardID}");
            }
            Debug.Log($"验证成功，返回 {validatedCards.Count} 张完整卡牌数据用于伤害计算");
            return validatedCards;
        }

        // 获取房间玩家列表
        public (int roomId, List<int> playerIds)? GetPlayerRoomInfo(int playerId)
        {
            if (rooms == null)
            {
                Debug.LogError("Rooms dictionary not set");
                return null;
            }

            foreach (var room in rooms)
            {
                if (room.Value.Contains(playerId))
                {
                    return (room.Key, room.Value);
                }
            }

            Debug.LogError($"Player {playerId} not found in any room");
            return null;
        }

        // 获取房间中除指定玩家外的其他玩家ID
        public int? GetOpponentId(int playerId)
        {
            var roomInfo = GetPlayerRoomInfo(playerId);
            if (!roomInfo.HasValue)
            {
                return null;
            }

            var (_, playerIds) = roomInfo.Value;
            foreach (var id in playerIds)
            {
                if (id != playerId)
                {
                    return id;
                }
            }

            return null;
        }

        // 获取房间内所有玩家的NetPeer对象
        public List<NetPeer> GetRoomPeers(int playerId)
        {
            var roomInfo = GetPlayerRoomInfo(playerId);
            if (!roomInfo.HasValue)
            {
                return new List<NetPeer>();
            }

            var (_, playerIds) = roomInfo.Value;
            List<NetPeer> peers = new List<NetPeer>();

            foreach (var id in playerIds)
            {
                var peer = _netManager.GetPeerById(id);
                if (peer != null)
                {
                    peers.Add(peer);
                }
            }

            return peers;
        }

        // 获取下一个出牌玩家
        public NetPeer GetNextPlayer(int currentPlayerId)
        {
            var opponentId = GetOpponentId(currentPlayerId);
            if (!opponentId.HasValue)
            {
                return null;
            }

            return _netManager.GetPeerById(opponentId.Value);
        }
    }
}
