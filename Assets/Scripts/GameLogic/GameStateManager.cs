using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepGameModels;
using RepGame.Core;

namespace RepGame.GameLogic
{
    /// <summary>
    /// 游戏状态管理器 - 处理游戏逻辑而不涉及UI操作
    /// </summary>
    public class GameStateManager
    {
        #region 事件定义

        // UI更新事件
        public event Action<List<Card>> OnHandCardsUpdated;
        public event Action<int> OnEnemyCardCountUpdated;
        public event Action<ResPlayerGameInfo> OnGameStateUpdated;
        public event Action<string, float> OnMessageRequested;
        public event Action<string, float, float> OnDelayedMessageRequested;
        public event Action<bool> OnGameOverRequested;
        public event Action OnClearCardSelection;
        public event Action OnButtonVisibilityUpdate;
        public event Action OnBondDisplayUpdate;

        #endregion

        #region 私有字段

        private List<Card> selectedCards;
        private Dictionary<string, Card> cardModelsById;
        private string roomId;
        private string username;
        private string currentRound;
        private float maxHealth = 100f;

        #endregion

        #region 构造函数

        public GameStateManager()
        {
            selectedCards = new List<Card>();
            cardModelsById = new Dictionary<string, Card>();
        }

        #endregion

        #region 公共属性

        public List<Card> SelectedCards => new List<Card>(selectedCards);
        public int SelectedCardCount => selectedCards.Count;
        public string RoomId => roomId;
        public string Username => username;
        public string CurrentRound => currentRound;

        #endregion

        #region 游戏初始化

        /// <summary>
        /// 初始化游戏状态
        /// </summary>
        public void InitializeGame(ResPlayerGameInfo gameInfo)
        {
            roomId = gameInfo.Room_Id;
            username = gameInfo.Username;
            currentRound = gameInfo.Round;
            maxHealth = gameInfo.Health;

            // 初始化卡牌数据
            cardModelsById.Clear();
            if (gameInfo.SelfCards != null)
            {
                foreach (var card in gameInfo.SelfCards)
                {
                    cardModelsById[card.UID] = card;
                }
            }

            Debug.Log($"游戏初始化完成 - 房间: {roomId}, 玩家: {username}, 手牌数量: {gameInfo.SelfCards?.Count ?? 0}");
        }

        #endregion

        #region 卡牌选择逻辑

        /// <summary>
        /// 处理卡牌选中
        /// </summary>
        public bool HandleCardSelected(string uid)
        {
            if (!cardModelsById.ContainsKey(uid))
            {
                Debug.LogError($"未找到UID对应的卡牌数据: {uid}");
                return false;
            }

            Card card = cardModelsById[uid];
            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
                Debug.Log($"卡牌已选中: {card.Name} (UID: {uid}), 当前选中数量: {selectedCards.Count}");

                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning($"卡牌已在选中列表中: {card.Name} (UID: {uid})");
                return false;
            }
        }

        /// <summary>
        /// 处理卡牌取消选中
        /// </summary>
        public bool HandleCardDeselected(string uid)
        {
            if (!cardModelsById.ContainsKey(uid))
            {
                Debug.LogError($"未找到UID对应的卡牌数据: {uid}");
                return false;
            }

            Card card = cardModelsById[uid];
            if (selectedCards.Remove(card))
            {
                Debug.Log($"卡牌已取消选中: {card.Name} (UID: {uid}), 当前选中数量: {selectedCards.Count}");

                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning($"尝试移除未选中的卡牌: {card.Name} (UID: {uid})");
                return false;
            }
        }

        /// <summary>
        /// 清除所有选中的卡牌
        /// </summary>
        public void ClearSelectedCards()
        {
            selectedCards.Clear();
            Debug.Log("已清除所有选中的卡牌");

            OnClearCardSelection?.Invoke();
            OnButtonVisibilityUpdate?.Invoke();
            OnBondDisplayUpdate?.Invoke();
        }

        /// <summary>
        /// 检查指定卡牌是否已选中
        /// </summary>
        public bool IsCardSelected(string uid)
        {
            if (cardModelsById.ContainsKey(uid))
            {
                return selectedCards.Contains(cardModelsById[uid]);
            }
            return false;
        }

        #endregion

        #region 按钮状态逻辑

        /// <summary>
        /// 检查是否可以出牌
        /// </summary>
        public bool CanPlayCards()
        {
            return selectedCards.Count > 0 && IsMyTurn();
        }

        /// <summary>
        /// 检查是否可以合成
        /// </summary>
        public bool CanCompose()
        {
            if (selectedCards.Count < 3)
                return false;

            // 统计每个卡牌名称的数量
            var cardNameCounts = new Dictionary<string, int>();
            foreach (var card in selectedCards)
            {
                if (cardNameCounts.ContainsKey(card.Name))
                {
                    cardNameCounts[card.Name]++;
                }
                else
                {
                    cardNameCounts[card.Name] = 1;
                }
            }

            // 检查是否有任何卡牌名称的数量达到3张或以上
            return cardNameCounts.Values.Any(count => count >= 3);
        }

        /// <summary>
        /// 检查是否有选中的卡牌
        /// </summary>
        public bool HasSelectedCards()
        {
            return selectedCards.Count > 0;
        }

        /// <summary>
        /// 检查是否是自己的回合
        /// </summary>
        public bool IsMyTurn()
        {
            return string.Equals(currentRound, "current", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 出牌逻辑

        /// <summary>
        /// 处理出牌逻辑
        /// </summary>
        public ResPlayerGameInfo CreatePlayCardRequest()
        {
            if (selectedCards.Count == 0)
            {
                Debug.LogWarning("没有选中任何卡牌");
                return null;
            }

            var cardsToPlay = new List<Card>(selectedCards);
            var playerGameInfo = new ResPlayerGameInfo
            {
                Room_Id = roomId,
                Username = username,
                Round = currentRound,
                SelfCards = cardsToPlay
            };

            Debug.Log($"创建出牌请求：房间ID {roomId}，用户名 {username}，出牌数量 {cardsToPlay.Count}");
            return playerGameInfo;
        }

        /// <summary>
        /// 处理出牌结果
        /// </summary>
        public void HandlePlayCardResult(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.8f);
                return;
            }

            // 检查是否造成伤害
            if (result.DamageDealt > 0)
            {
                string damageMessage = BuildDamageMessage(result.DamageDealt, result.TriggeredBonds, true);
                OnMessageRequested?.Invoke(damageMessage, 0.8f);
            }
            // 检查是否收到攻击伤害
            else if (result.DamageReceived > 0)
            {
                string damageMessage = BuildDamageMessage(result.DamageReceived, result.TriggeredBonds, false);
                OnMessageRequested?.Invoke(damageMessage, 0.8f);
            }

            // 更新游戏状态
            UpdateGameState(result);
        }

        #endregion

        #region 合成逻辑

        /// <summary>
        /// 处理卡牌合成逻辑
        /// </summary>
        public (ResPlayerGameInfo request, List<Card> cardsToCompose, List<Card> cardsToRevert) CreateComposeCardRequest()
        {
            if (selectedCards.Count < 3)
            {
                Debug.LogWarning("选中的卡牌数量不足3张，无法进行合成");
                return (null, null, null);
            }

            // 按卡牌名称分组
            var cardGroups = selectedCards.GroupBy(card => card.Name).ToList();
            var cardsToCompose = new List<Card>();
            var cardsToRevert = new List<Card>();

            // 找出可以合成的卡牌（同名且数量>=3的组合）
            foreach (var group in cardGroups)
            {
                var groupCards = group.ToList();
                int composableCount = (groupCards.Count / 3) * 3; // 取3的倍数

                if (composableCount >= 3)
                {
                    // 添加可合成的卡牌
                    for (int i = 0; i < composableCount; i++)
                    {
                        cardsToCompose.Add(groupCards[i]);
                    }

                    // 剩余的卡牌需要恢复状态
                    for (int i = composableCount; i < groupCards.Count; i++)
                    {
                        cardsToRevert.Add(groupCards[i]);
                    }
                }
                else
                {
                    // 数量不足3张的组合，全部恢复状态
                    cardsToRevert.AddRange(groupCards);
                }
            }

            if (cardsToCompose.Count == 0)
            {
                Debug.LogWarning("没有足够的同名卡牌进行合成（需要至少3张同名卡牌）");
                return (null, null, null);
            }

            var playerGameInfo = new ResPlayerGameInfo
            {
                Room_Id = roomId,
                Username = username,
                Round = currentRound,
                SelfCards = cardsToCompose
            };

            Debug.Log($"创建合成请求：房间ID {roomId}，用户名 {username}，合成卡牌数量 {cardsToCompose.Count}");
            return (playerGameInfo, cardsToCompose, cardsToRevert);
        }

        /// <summary>
        /// 处理合成结果
        /// </summary>
        public void HandleComposeCardResult(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.8f);
                return;
            }

            // 1. 检查SelfCards更新并更新本地手牌
            if (result.SelfCards != null)
            {
                OnHandCardsUpdated?.Invoke(result.SelfCards);
                Debug.Log($"卡牌合成成功，更新手牌数量：{result.SelfCards.Count}");

                // 显示合成成功消息 0.8 秒
                OnMessageRequested?.Invoke("卡牌合成成功！", 0.8f);
            }

            // 2. 检查OtherCards数量更新并更新敌方卡牌容器显示
            if (result.OtherCards != null)
            {
                OnEnemyCardCountUpdated?.Invoke(result.OtherCards.Count);
                Debug.Log($"敌方完成卡牌合成，敌方手牌数量：{result.OtherCards.Count}");

                // 延迟显示敌方合成消息 0.8 秒（在己方消息后显示）
                OnDelayedMessageRequested?.Invoke("敌方完成卡牌合成！", 1.0f, 0.8f);
            }

            // 3. 更新游戏状态
            UpdateGameState(result);
        }

        #endregion

        #region 游戏结束逻辑

        /// <summary>
        /// 处理游戏结束逻辑
        /// </summary>
        public void HandleGameOver(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.8f);
                return;
            }

            // 判断当前血量是否<=0，确定是失败还是胜利
            if (result.Health <= 0)
            {
                // 失败情况：血量<=0
                Debug.Log($"游戏失败 - 当前血量: {result.Health}");

                // 构建失败消息，包含触发的羁绊信息
                string defeatMessage = BuildDamageMessage(result.DamageReceived, result.TriggeredBonds, false);

                // 显示受到伤害的消息
                OnMessageRequested?.Invoke(defeatMessage, 0.5f);

                // 0.5秒后显示失败消息
                OnDelayedMessageRequested?.Invoke("你失败了！", 0.5f, 3.0f);

                OnGameOverRequested?.Invoke(false); // 失败
            }
            else
            {
                // 胜利情况：血量>0
                Debug.Log($"游戏胜利 - 当前血量: {result.Health}");

                // 直接显示胜利消息
                OnMessageRequested?.Invoke("你胜利了！", 2.0f);

                OnGameOverRequested?.Invoke(true); // 胜利
            }

            // 更新最终游戏状态
            UpdateGameState(result);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        private void UpdateGameState(ResPlayerGameInfo gameInfo)
        {
            // 更新基本信息
            if (!string.IsNullOrEmpty(gameInfo.Round))
            {
                currentRound = gameInfo.Round;
            }

            // 更新手牌数据
            if (gameInfo.SelfCards != null)
            {
                OnHandCardsUpdated?.Invoke(gameInfo.SelfCards);
            }

            // 更新敌方卡牌数量
            if (gameInfo.OtherCards != null)
            {
                OnEnemyCardCountUpdated?.Invoke(gameInfo.OtherCards.Count);
            }

            // 通知UI更新
            OnGameStateUpdated?.Invoke(gameInfo);
            OnButtonVisibilityUpdate?.Invoke();
            OnBondDisplayUpdate?.Invoke();
        }

        /// <summary>
        /// 构建伤害消息
        /// </summary>
        private string BuildDamageMessage(float damageAmount, BondModel[] triggeredBonds, bool isDamageDealer)
        {
            string actionText = isDamageDealer ? "造成" : "受到";
            string damageText = "点伤害";

            // 检查是否有触发的羁绊
            if (triggeredBonds != null && triggeredBonds.Length > 0)
            {
                var bondNames = triggeredBonds.Select(bond => bond.Name).ToArray();
                string bondsText = string.Join("、", bondNames);
                return $"存在{bondsText} 羁绊伤害加成，{actionText}{damageAmount} {damageText}！";
            }
            else
            {
                return $"{actionText}伤害 {damageAmount} 点！";
            }
        }

        /// <summary>
        /// 更新卡牌数据
        /// </summary>
        public void UpdateCardModels(Dictionary<string, Card> newCardModels)
        {
            cardModelsById = new Dictionary<string, Card>(newCardModels);
        }

        /// <summary>
        /// 添加卡牌数据
        /// </summary>
        public void AddCardModel(string uid, Card card)
        {
            cardModelsById[uid] = card;
        }

        /// <summary>
        /// 移除卡牌数据
        /// </summary>
        public void RemoveCardModel(string uid)
        {
            cardModelsById.Remove(uid);

            // 如果这张卡牌在选中列表中，也要移除
            if (cardModelsById.ContainsKey(uid))
            {
                Card cardToRemove = cardModelsById[uid];
                selectedCards.Remove(cardToRemove);
            }
        }

        #endregion
    }
}
