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
        #region 事件定义        // UI更新事件
        public event Action<List<Card>> OnHandCardsUpdated;
        public event Action<int> OnEnemyCardCountUpdated;
        public event Action<ResPlayerGameInfo> OnGameStateUpdated;
        public event Action<string, float> OnMessageRequested;
        public event Action<string, float, float> OnDelayedMessageRequested;
        public event Action<bool> OnGameOverRequested;
        public event Action OnClearCardSelection;
        public event Action OnButtonVisibilityUpdate;
        public event Action OnBondDisplayUpdate;
        public event Action OnGameDataClear; // 游戏数据清理事件

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

                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning($"尝试移除未选中的卡牌: {card.Name} (UID: {uid})");
                return false;
            }
        }        /// <summary>
                 /// 清除所有选中的卡牌并从卡牌数据字典中移除
                 /// </summary>
        public void ClearSelectedCards()
        {
            // 记录要移除的卡牌UID列表
            var cardUidsToRemove = selectedCards.Select(card => card.UID).ToList();

            // 从cardModelsById字典中移除选中的卡牌
            int removedCount = 0;
            foreach (var uid in cardUidsToRemove)
            {
                if (cardModelsById.ContainsKey(uid))
                {
                    cardModelsById.Remove(uid);
                    removedCount++;
                }
            }

            // 清空选中列表
            selectedCards.Clear();

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

            return playerGameInfo;
        }
        /// <summary>
        /// 处理出牌结果
        /// </summary>
        public void HandlePlayCardResult(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.5f);
                return;
            }

            // 出牌成功，清空选中的卡牌列表
            if (selectedCards.Count > 0)
            {
                selectedCards.Clear();
                OnClearCardSelection?.Invoke();
            }

            // 更新手牌数据（确保与服务器同步）
            if (result.SelfCards != null)
            {
                // 先更新内部数据，确保卡牌数据一致性
                UpdateCardModelsFromList(result.SelfCards);
                OnHandCardsUpdated?.Invoke(result.SelfCards);
            }

            // 处理伤害信息
            if (result.DamageInfo != null && result.DamageInfo.Count > 0)
            {
                // 处理伤害信息队列
                ProcessDamageQueue(result.DamageInfo);
            }

            // 更新游戏状态（不包括卡牌数据，因为上面已经处理过了）
            UpdateGameStateExcludingCards(result);
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

            return (playerGameInfo, cardsToCompose, cardsToRevert);
        }
        /// <summary>
        /// 处理合成结果
        /// </summary>
        public void HandleComposeCardResult(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.5f);
                return;
            }

            // 记录当前本地手牌数量，用于判断是谁进行了合成
            int currentLocalCardCount = cardModelsById.Count;

            // 标记是否显示了合成消息
            bool hasShownComposeMessage = false;

            // 1. 检查SelfCards更新并更新本地手牌
            if (result.SelfCards != null)
            {
                // 先更新内部数据，再通知UI
                UpdateCardModelsFromList(result.SelfCards);
                OnHandCardsUpdated?.Invoke(result.SelfCards);

                // 判断是否是自己的合成：本地手牌数量与服务器手牌数量不一致
                if (currentLocalCardCount != result.SelfCards.Count)
                {
                    // 自己进行了合成，显示合成成功消息
                    OnMessageRequested?.Invoke("卡牌合成成功！", 0.5f);
                    hasShownComposeMessage = true;
                }
            }

            // 2. 检查OtherPlayers数量更新并更新敌方卡牌容器显示
            if (result.OtherPlayers != null && result.OtherPlayers.Count > 0)
            {
                var enemyPlayer = result.OtherPlayers[0];
                OnEnemyCardCountUpdated?.Invoke(enemyPlayer.CardsCount);

                // 如果还没有显示合成消息，且敌方手牌数量发生了变化，则显示敌方合成消息
                if (!hasShownComposeMessage)
                {
                    // 延迟显示敌方合成消息 0.8 秒
                    OnDelayedMessageRequested?.Invoke("敌方完成卡牌合成！", 0.5f, 0.5f);
                    Debug.Log($"检测到敌方合成：敌方手牌数量 {enemyPlayer.CardsCount} 张");
                }
            }

            UpdateGameStateExcludingCards(result);
        }

        #endregion

        #region 游戏结束逻辑        /// <summary>
        /// 处理游戏结束逻辑
        /// </summary>
        public void HandleGameOver(ResPlayerGameInfo result)
        {
            if (result == null)
            {
                OnMessageRequested?.Invoke("网络连接错误！", 0.5f);
                return;
            }

            // 判断当前血量是否<=0，确定是失败还是胜利
            if (result.Health <= 0)
            {
                // 失败情况：血量<=0
                Debug.Log($"游戏失败 - 当前血量: {result.Health}");

                // 如果有伤害信息，先处理伤害信息
                if (result.DamageInfo != null && result.DamageInfo.Count > 0)
                {
                    ProcessDamageQueue(result.DamageInfo);
                    // 0.5秒后显示失败消息
                    OnDelayedMessageRequested?.Invoke("你失败了！", 0.5f, 3.0f);
                }
                else
                {
                    // 直接显示失败消息
                    OnMessageRequested?.Invoke("你失败了！", 3.0f);
                }

                OnGameOverRequested?.Invoke(false); // 失败
            }
            else
            {
                // 胜利情况：血量>0
                Debug.Log($"游戏胜利 - 当前血量: {result.Health}");

                // 直接显示胜利消息
                OnMessageRequested?.Invoke("你胜利了！", 3.0f);

                OnGameOverRequested?.Invoke(true); // 胜利
            }

            // 游戏结束后清理所有数据
            ClearAllGameData();

            // 更新最终游戏状态
            UpdateGameState(result);
        }

        /// <summary>
        /// 清理所有游戏数据（游戏结束时调用）
        /// </summary>
        private void ClearAllGameData()
        {
            try
            {
                Debug.Log("开始清理所有游戏数据...");

                // 1. 清除房间相关数据
                var previousRoomId = roomId;
                var previousUsername = username;
                roomId = string.Empty;
                username = string.Empty;
                currentRound = string.Empty;

                // 2. 清除玩家手牌卡牌对象
                var clearedCardCount = cardModelsById.Count;
                cardModelsById.Clear();

                // 3. 清除选中卡牌列表
                var clearedSelectedCount = selectedCards.Count;
                selectedCards.Clear();

                // 4. 隐藏所有敌方玩家手牌卡片（触发UI更新显示0张卡牌）
                OnEnemyCardCountUpdated?.Invoke(0);                // 5. 重置健康UI到初始状态
                var resetGameInfo = new ResPlayerGameInfo
                {
                    Health = maxHealth,
                    Room_Id = string.Empty,
                    Username = string.Empty,
                    Round = string.Empty,
                    SelfCards = new List<Card>(),
                    OtherPlayers = new List<OtherPlayerGameInfo>
                    {
                        new OtherPlayerGameInfo
                        {
                            Health = maxHealth,
                            CardsCount = 0,
                            Username = string.Empty,
                            Round = string.Empty
                        }
                    },
                    DamageInfo = new List<DamageInfo>()
                };
                OnGameStateUpdated?.Invoke(resetGameInfo);

                // 6. 清除消息内容（触发空手牌更新）
                OnHandCardsUpdated?.Invoke(new List<Card>());

                // 7. 清除卡牌选择和按钮状态
                OnClearCardSelection?.Invoke();
                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();

                // 8. 触发游戏数据清理事件通知UI组件
                OnGameDataClear?.Invoke();

                Debug.Log($"游戏数据清理完成 - 房间: {previousRoomId}, 用户: {previousUsername}, " +
                         $"清除卡牌: {clearedCardCount}张, 清除选中: {clearedSelectedCount}张");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"清理游戏数据时出现错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region 辅助方法        /// <summary>
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
                // 更新内部卡牌数据字典
                UpdateCardModelsFromList(gameInfo.SelfCards);

                OnHandCardsUpdated?.Invoke(gameInfo.SelfCards);
            }

            // 更新敌方玩家信息
            if (gameInfo.OtherPlayers != null && gameInfo.OtherPlayers.Count > 0)
            {
                // 目前只有一个敌方玩家，从第一个Index取值
                var enemyPlayer = gameInfo.OtherPlayers[0];
                OnEnemyCardCountUpdated?.Invoke(enemyPlayer.CardsCount);
            }

            // 通知UI更新
            OnGameStateUpdated?.Invoke(gameInfo);
            OnButtonVisibilityUpdate?.Invoke();
            OnBondDisplayUpdate?.Invoke();
        }

        /// <summary>
        /// 更新游戏状态（不包括卡牌数据）
        /// 用于避免重复更新卡牌数据的情况
        /// </summary>
        private void UpdateGameStateExcludingCards(ResPlayerGameInfo gameInfo)
        {
            // 更新基本信息
            if (!string.IsNullOrEmpty(gameInfo.Round))
            {
                currentRound = gameInfo.Round;
            }

            // 更新敌方玩家信息
            if (gameInfo.OtherPlayers != null && gameInfo.OtherPlayers.Count > 0)
            {
                // 目前只有一个敌方玩家，从第一个Index取值
                var enemyPlayer = gameInfo.OtherPlayers[0];
                OnEnemyCardCountUpdated?.Invoke(enemyPlayer.CardsCount);
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
        }        /// <summary>
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
        }        /// <summary>
                 /// 从卡牌列表更新内部卡牌数据字典
                 /// </summary>
        private void UpdateCardModelsFromList(List<Card> cards)
        {
            if (cards == null)
            {
                Debug.LogWarning("卡牌列表为空，无法更新卡牌数据");
                return;
            }

            // 验证数据一致性（在更新前）
            bool isConsistent = ValidateCardDataConsistency(cards);
            if (!isConsistent)
            {
                Debug.LogWarning("检测到卡牌数据不一致，将强制同步");
            }

            // 清空现有数据
            cardModelsById.Clear();

            // 重新构建卡牌数据字典
            foreach (var card in cards)
            {
                if (!string.IsNullOrEmpty(card.UID))
                {
                    cardModelsById[card.UID] = card;
                }
                else
                {
                    Debug.LogWarning($"发现UID为空的卡牌: {card.Name}，跳过添加");
                }
            }

            // 清理选中列表中不存在的卡牌
            CleanupSelectedCards();

            Debug.Log($"更新卡牌数据完成，当前卡牌数量：{cardModelsById.Count}");
        }

        /// <summary>
        /// 清理选中列表中不存在的卡牌
        /// </summary>
        private void CleanupSelectedCards()
        {
            var cardsToRemove = new List<Card>();

            foreach (var selectedCard in selectedCards)
            {
                // 检查选中的卡牌是否还在当前手牌中
                bool cardExists = cardModelsById.ContainsKey(selectedCard.UID);
                if (!cardExists)
                {
                    cardsToRemove.Add(selectedCard);
                }
            }

            // 移除不存在的卡牌
            foreach (var cardToRemove in cardsToRemove)
            {
                selectedCards.Remove(cardToRemove);
                Debug.Log($"从选中列表中移除不存在的卡牌: {cardToRemove.Name} (UID: {cardToRemove.UID})");
            }

            // 如果选中列表发生了变化，触发相关事件
            if (cardsToRemove.Count > 0)
            {
                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();
            }
        }

        #endregion

        #region 伤害处理逻辑

        /// <summary>
        /// 处理伤害信息队列
        /// </summary>
        private void ProcessDamageQueue(List<DamageInfo> damageInfoList)
        {
            if (damageInfoList == null || damageInfoList.Count == 0)
                return;

            // 如果有多条信息，排队显示
            for (int i = 0; i < damageInfoList.Count; i++)
            {
                var damageInfo = damageInfoList[i];
                string message = BuildDamageMessageFromInfo(damageInfo);

                if (i == 0)
                {
                    // 第一条消息立即显示
                    OnMessageRequested?.Invoke(message, 0.5f);
                }
                else
                {
                    // 后续消息延迟显示
                    float delay = i * 0.5f; // 每条消息间隔1秒
                    OnDelayedMessageRequested?.Invoke(message, delay, 0.5f);
                }
            }
        }
        /// <summary>
        /// 根据伤害信息构建消息文本
        /// </summary>
        private string BuildDamageMessageFromInfo(DamageInfo damageInfo)
        {
            string message = "";

            // 判断伤害来源是否为自己
            bool isSelfDamage = string.Equals(damageInfo.DamageSource, username, StringComparison.OrdinalIgnoreCase);

            switch (damageInfo.DamageType.ToUpper())
            {
                case "ATTACKED":
                    if (isSelfDamage)
                    {
                        message = $"造成了 {damageInfo.DamageValue} 点伤害";
                    }
                    else
                    {
                        message = $"受到了 {damageInfo.DamageValue} 点伤害";
                    }
                    break;
                case "RECOVER":
                    if (isSelfDamage)
                    {
                        message = $"回复了 {damageInfo.DamageValue} 点血量";
                    }
                    else
                    {
                        message = $"敌方回复了 {damageInfo.DamageValue} 点血量";
                    }
                    break;
                case "AOE":
                    if (isSelfDamage)
                    {
                        message = $"造成了 {damageInfo.DamageValue} 点AOE伤害";
                    }
                    else
                    {
                        message = $"受到了 {damageInfo.DamageValue} 点AOE伤害";
                    }
                    break;
                default:
                    if (isSelfDamage)
                    {
                        message = $"造成了 {damageInfo.DamageValue} 点伤害";
                    }
                    else
                    {
                        message = $"受到了 {damageInfo.DamageValue} 点伤害";
                    }
                    break;
            }

            // 添加触发的羁绊信息
            if (damageInfo.TriggeredBonds != null && damageInfo.TriggeredBonds.Count > 0)
            {
                var bondNames = damageInfo.TriggeredBonds.Select(bond => bond.Name).ToArray();
                string bondsText = string.Join("、", bondNames);
                message = $"触发{bondsText}羁绊，{message}！";
            }
            else
            {
                message += "！";
            }

            return message;
        }

        #endregion

        #region 卡牌数据验证逻辑        
        /// <summary>
        /// 验证卡牌数据一致性
        /// </summary>
        private bool ValidateCardDataConsistency(List<Card> serverCards)
        {
            if (serverCards == null)
            {
                Debug.LogWarning("服务器卡牌数据为空，跳过验证");
                return false;
            }

            int inconsistentCount = 0;

            // 检查服务器卡牌是否都在本地字典中
            foreach (var serverCard in serverCards)
            {
                if (string.IsNullOrEmpty(serverCard.UID))
                {
                    Debug.LogWarning($"发现无效的卡牌UID: {serverCard.Name}");
                    inconsistentCount++;
                    continue;
                }

                if (cardModelsById.ContainsKey(serverCard.UID))
                {
                    var localCard = cardModelsById[serverCard.UID];
                    // 验证关键属性是否一致
                    if (localCard.Name != serverCard.Name || localCard.Damage != serverCard.Damage || localCard.Level != serverCard.Level)
                    {
                        Debug.LogWarning($"卡牌数据不一致 - UID: {serverCard.UID}, 本地: {localCard.Name}(伤害:{localCard.Damage},等级:{localCard.Level}), 服务器: {serverCard.Name}(伤害:{serverCard.Damage},等级:{serverCard.Level})");
                        inconsistentCount++;
                    }
                }
            }

            // 检查本地字典中是否有服务器没有的卡牌
            foreach (var localCardPair in cardModelsById)
            {
                bool foundInServer = serverCards.Any(sc => sc.UID == localCardPair.Key);
                if (!foundInServer)
                {
                    Debug.LogWarning($"本地存在服务器没有的卡牌: {localCardPair.Value.Name} (UID: {localCardPair.Key})");
                    inconsistentCount++;
                }
            }

            if (inconsistentCount > 0)
            {
                Debug.LogError($"发现 {inconsistentCount} 个卡牌数据不一致问题");
                return false;
            }

            Debug.Log($"卡牌数据验证通过，本地: {cardModelsById.Count} 张，服务器: {serverCards.Count} 张");
            return true;
        }

        /// <summary>
        /// 强制同步卡牌数据（用于修复数据不一致问题）
        /// </summary>
        public void ForceCardDataSync(List<Card> authorativeCards)
        {
            if (authorativeCards == null)
            {
                Debug.LogWarning("权威卡牌数据为空，无法强制同步");
                return;
            }

            Debug.Log($"强制同步卡牌数据，清空现有 {cardModelsById.Count} 张卡牌，重建 {authorativeCards.Count} 张卡牌");

            // 清空并重建
            cardModelsById.Clear();
            selectedCards.Clear();

            foreach (var card in authorativeCards)
            {
                if (!string.IsNullOrEmpty(card.UID))
                {
                    cardModelsById[card.UID] = card;
                }
            }

            // 触发相关事件
            OnHandCardsUpdated?.Invoke(authorativeCards);
            OnClearCardSelection?.Invoke();
            OnButtonVisibilityUpdate?.Invoke();
            OnBondDisplayUpdate?.Invoke();

            Debug.Log($"强制同步完成，当前卡牌数量：{cardModelsById.Count}");
        }

        /// <summary>
        /// 获取卡牌数据统计信息（用于调试）
        /// </summary>
        public string GetCardDataStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine($"=== 卡牌数据统计 ===");
            stats.AppendLine($"总卡牌数量: {cardModelsById.Count}");
            stats.AppendLine($"选中卡牌数量: {selectedCards.Count}");

            // 按卡牌名称分组统计
            var cardGroups = cardModelsById.Values.GroupBy(card => card.Name);
            stats.AppendLine($"卡牌类型分布:");
            foreach (var group in cardGroups.OrderBy(g => g.Key))
            {
                stats.AppendLine($"  {group.Key}: {group.Count()}张");
            }

            // 按等级分组统计
            var levelGroups = cardModelsById.Values.GroupBy(card => card.Level);
            stats.AppendLine($"等级分布:");
            foreach (var group in levelGroups.OrderBy(g => g.Key))
            {
                stats.AppendLine($"  等级{group.Key}: {group.Count()}张");
            }

            // 选中卡牌详情
            if (selectedCards.Count > 0)
            {
                stats.AppendLine($"选中卡牌详情:");
                foreach (var card in selectedCards)
                {
                    stats.AppendLine($"  {card.Name} (UID: {card.UID})");
                }
            }

            return stats.ToString();
        }

        /// <summary>
        /// 检测卡牌数据完整性问题并尝试修复
        /// </summary>
        public bool DetectAndFixCardDataIssues()
        {
            bool hasIssues = false;
            var issuesFixed = new List<string>();

            // 检查选中卡牌列表中是否有无效的卡牌
            var invalidSelectedCards = new List<Card>();
            foreach (var selectedCard in selectedCards)
            {
                if (string.IsNullOrEmpty(selectedCard.UID) || !cardModelsById.ContainsKey(selectedCard.UID))
                {
                    invalidSelectedCards.Add(selectedCard);
                    hasIssues = true;
                }
            }

            // 移除无效的选中卡牌
            if (invalidSelectedCards.Count > 0)
            {
                foreach (var invalidCard in invalidSelectedCards)
                {
                    selectedCards.Remove(invalidCard);
                    issuesFixed.Add($"移除无效选中卡牌: {invalidCard.Name} (UID: {invalidCard.UID})");
                }
            }

            // 检查卡牌数据字典中是否有UID为空的卡牌
            var emptyUidCards = cardModelsById.Where(pair => string.IsNullOrEmpty(pair.Key)).ToList();
            if (emptyUidCards.Count > 0)
            {
                foreach (var pair in emptyUidCards)
                {
                    cardModelsById.Remove(pair.Key);
                    issuesFixed.Add($"移除UID为空的卡牌: {pair.Value.Name}");
                    hasIssues = true;
                }
            }

            if (hasIssues)
            {
                Debug.LogWarning($"检测到并修复了 {issuesFixed.Count} 个卡牌数据问题:");
                foreach (var fix in issuesFixed)
                {
                    Debug.LogWarning($"  - {fix}");
                }

                // 触发相关事件通知UI更新
                OnButtonVisibilityUpdate?.Invoke();
                OnBondDisplayUpdate?.Invoke();
            }

            return hasIssues;
        }

        #endregion
    }
}
