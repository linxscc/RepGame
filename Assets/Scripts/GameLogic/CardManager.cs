using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGamebackModels;
using GameLogic;

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

        public CardManager()
        {
            // 加载卡牌配置
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Config/CardDeck.json");
            string jsonContent = File.ReadAllText(jsonPath);
            cardDeckConfig = JsonUtility.FromJson<CardDeckWrapper>(jsonContent);
        }

        public void InitializeCardsForPlayers(int roomId, List<NetPeer> roomPeers)
        {
            // 创建房间的分级共享牌库
            var levelDecks = CreateLevelDecks(roomId);
            roomLevelDecks[roomId] = levelDecks;

            foreach (var peer in roomPeers)
            {
                // 记录玩家所在的房间
                playerRoomMap[peer.Id] = roomId;

                // 为玩家只从1级牌库中抽取6张卡牌
                List<CardModel> playerHand = DrawRandomCardsFromLevel(roomId, 1, 6);
                playerHands[peer.Id] = playerHand;

                // 发送卡牌给玩家
                SendCardsToPlayer(peer, playerHand);
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

        private void SendCardsToPlayer(NetPeer peer, List<CardModel> cards)
        {
            // 创建API响应
            var response = new ApiResponse<List<CardModel>>
            {
                Code = 0,
                Message = "初始化卡牌成功",
                Data = cards
            };

            // 序列化响应
            string json = JsonUtility.ToJson(response);
            Debug.Log($"Sending cards to player {peer.Id}: {json}");

            // 发送给客户端
            var writer = new NetDataWriter();
            writer.Put("InitPlayerCards");
            writer.Put(json);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
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
        public List<CardModel> HandleCardComposition(int playerId, List<CardModel> cardsToCompose)
        {
            if (!playerHands.ContainsKey(playerId) || !playerRoomMap.ContainsKey(playerId))
            {
                throw new Exception($"找不到玩家 {playerId} 的数据");
            }

            // 验证卡牌数量是否是3的倍数
            if (cardsToCompose.Count % 3 != 0)
            {
                throw new Exception("合成失败：卡牌数量必须是3的倍数");
            }

            int roomId = playerRoomMap[playerId];
            var levelDecks = roomLevelDecks[roomId];
            var playerHand = playerHands[playerId];

            // 按卡牌类型和等级分组
            var cardGroups = cardsToCompose.GroupBy(c => new { c.Type, c.Level }).ToList();
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

                // 所有组都验证通过，从玩家手牌中移除被合成的卡牌
                foreach (var card in cardsToCompose)
                {
                    playerHand.RemoveAll(c => c.CardID == card.CardID);
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

        // 处理出牌后从共享牌库中移除卡牌
        public void HandleCardsPlayed(int playerId, List<CardModel> playedCards)
        {
            if (!playerHands.ContainsKey(playerId) || !playerRoomMap.ContainsKey(playerId))
            {
                throw new Exception($"找不到玩家 {playerId} 的数据");
            }

            var playerHand = playerHands[playerId];
            
            // 从玩家手牌中移除打出的卡牌
            foreach (var card in playedCards)
            {
                playerHand.RemoveAll(c => c.CardID == card.CardID);
            }

            playerHands[playerId] = playerHand;

            // 从对应等级的牌库中移除这些卡牌
            int roomId = playerRoomMap[playerId];
            var levelDecks = roomLevelDecks[roomId];
            foreach (var card in playedCards)
            {
                if (levelDecks.ContainsKey(card.Level))
                {
                    levelDecks[card.Level].RemoveAll(c => c.CardID == card.CardID);
                }
            }
        }
    }
}
