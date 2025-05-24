using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using RepGame.Core;
using System.Linq;
using DG.Tweening;
using TMPro;
using RepGameModels;

namespace RepGame.UI
{
    public class CardUIManager
    {        private Transform cardContainer;
        private Transform enemyCardContainer;
        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, CardModel> cardModelsById; // Store original CardModel objects by ID
        private List<string> lockedCardIds = new List<string>(); // 新增：用于跟踪已锁定的卡牌ID

        // UI组件引用
        private Button playButton;
        private Button clrButton;
        private Button compButton;
        private Image playerBloodBar;
        private Image enemyBloodBar;
        private Image playerProfile;
        private Image enemyProfile;
        private GameObject panelMsg;
        private TextMeshProUGUI msgText;
        private MonoBehaviour coroutineRunner;
        public CardUIManager(Transform cardContainer, Transform enemyCardContainer)
        {
            this.cardContainer = cardContainer;
            this.enemyCardContainer = enemyCardContainer;
            this.instantiatedCards = new List<GameObject>();
            this.cardItemsById = new Dictionary<string, GameObject>();
            this.cardModelsById = new Dictionary<string, CardModel>();
            this.lockedCardIds = new List<string>();
        }

        // 初始化UI组件引用的方法
        public void InitializeUIComponents(
            Button playButton, Button clrButton, Button compButton,
            Image playerBloodBar, Image enemyBloodBar,
            Image playerProfile, Image enemyProfile,
            GameObject panelMsg, TextMeshProUGUI msgText,
            MonoBehaviour coroutineRunner)
        {
            this.playButton = playButton;
            this.clrButton = clrButton;
            this.compButton = compButton;
            this.playerBloodBar = playerBloodBar;
            this.enemyBloodBar = enemyBloodBar;
            this.playerProfile = playerProfile;
            this.enemyProfile = enemyProfile;
            this.panelMsg = panelMsg;
            this.msgText = msgText;
            this.coroutineRunner = coroutineRunner;
        }

        public void InitPlayerCards(List<CardModel> cardModels)
        {
            Debug.Log($"初始化玩家卡牌，数量：{cardModels.Count}");

            // 清空现有卡牌
            ClearCards();

            // 确保卡牌容器存在
            if (cardContainer == null)
            {
                Debug.LogError("卡牌容器不存在");
                return;
            }

            // 遍历卡牌模型，为每个卡牌实例化预制体
            foreach (var cardModel in cardModels)
            {
                InstantiateCardItem(cardModel);
            }

            // 初始化敌方卡牌的显示状态（与玩家卡牌数量相同）
            InitEnemyCardVisibility(cardModels.Count);

            Debug.Log($"卡牌实例化完成，共{instantiatedCards.Count}张卡牌");
        }

        // 初始化敌方卡牌显示状态
        private void InitEnemyCardVisibility(int cardCount)
        {
            if (enemyCardContainer == null)
            {
                Debug.LogError("敌方卡牌容器不存在");
                return;
            }

            // 获取所有敌方卡牌对象
            for (int i = 0; i < enemyCardContainer.childCount; i++)
            {
                // 设置卡牌显示状态
                enemyCardContainer.GetChild(i).gameObject.SetActive(i < cardCount);
            }
        }

        private void InstantiateCardItem(CardModel cardModel)
        {       
            try
            {
                // 使用Addressables加载卡牌预制体
                string address = $"Assets/Prefabs/Cards/{cardModel.Type}.prefab";
                Addressables.LoadAssetAsync<GameObject>(address).Completed += (AsyncOperationHandle<GameObject> handle) =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject cardPrefab = handle.Result;
                        if (cardPrefab == null)
                        {
                            Debug.LogError($"找不到卡牌预制体：{address}");
                            return;
                        }

                        // 实例化卡牌
                        GameObject cardObject = UnityEngine.Object.Instantiate(cardPrefab, cardContainer);
                        cardObject.name = $"Card_{cardModel.Type}_{cardModel.CardID.Substring(0, 8)}";

                        // 确保卡牌有Button组件
                        Button cardButton = cardObject.GetComponent<Button>();
                        if (cardButton == null)
                        {
                            cardButton = cardObject.AddComponent<Button>();
                        }

                        // 添加自定义卡牌脚本组件
                        MonoBehaviour cardComponent = cardObject.GetComponent(typeof(MonoBehaviour)) as MonoBehaviour;
                        if (cardComponent == null || cardComponent.GetType().Name != "CardItem")
                        {
                            // 动态添加CardItem脚本
                            Type cardItemType = Type.GetType("RepGame.UI.CardItem, Assembly-CSharp");
                            if (cardItemType != null)
                            {
                                cardComponent = cardObject.AddComponent(cardItemType) as MonoBehaviour;
                            }
                            else
                            {
                                Debug.LogError("无法找到CardItem类型");
                            }
                        }

                        // 反射设置卡牌属性
                        if (cardComponent != null)
                        {
                            // 获取CardID属性并设置
                            var cardIDProperty = cardComponent.GetType().GetProperty("CardID");
                            if (cardIDProperty != null)
                            {
                                cardIDProperty.SetValue(cardComponent, cardModel.CardID);
                            }

                            // 获取Type属性并设置
                            var typeProperty = cardComponent.GetType().GetProperty("Type");
                            if (typeProperty != null)
                            {
                                typeProperty.SetValue(cardComponent, cardModel.Type);
                            }

                            // 调用Init方法
                            var initMethod = cardComponent.GetType().GetMethod("Init");
                            if (initMethod != null)
                            {
                                initMethod.Invoke(cardComponent, new object[] { cardModel.CardID, cardModel.Type });
                            }
                        }
                        // 将卡牌添加到列表和字典中
                        instantiatedCards.Add(cardObject);
                        cardItemsById[cardModel.CardID] = cardObject;
                        cardModelsById[cardModel.CardID] = cardModel; // Store the original card model
                    }
                    else
                    {
                        Debug.LogError($"Failed to load Addressable asset: {address}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"实例化卡牌时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ClearCards()
        {
            // 销毁所有卡牌
            foreach (var cardObject in instantiatedCards)
            {
                if (cardObject != null)
                {
                    UnityEngine.Object.Destroy(cardObject);
                }
            }

            // 清空列表和字典
            instantiatedCards.Clear();
            cardItemsById.Clear();
            cardModelsById.Clear();
        }
          public List<CardModel> GetSelectedCards()
        {
            // 返回所有选中且未锁定的卡牌的ID和Type
            List<CardModel> selectedCards = new List<CardModel>();
            foreach (var card in instantiatedCards)
            {
                var cardItem = card.GetComponent<CardItem>();
                if (cardItem != null && cardItem.IsSelected && !cardItem.IsLocked)
                {
                    // 创建包含卡片ID和Type的CardModel对象
                    selectedCards.Add(new CardModel 
                    {
                        CardID = cardItem.CardID,
                        Type = cardItem.Type,
                        Damage = 0 
                    });
                }
            }
            return selectedCards;
        }
          // 获取选中的GameObject（用于内部实现）
        private List<GameObject> GetSelectedCardObjects()
        {
            List<GameObject> selectedCards = new List<GameObject>();
            foreach (var card in instantiatedCards)
            {
                var cardItem = card.GetComponent<CardItem>();
                if (cardItem != null && cardItem.IsSelected && !cardItem.IsLocked)
                {
                    selectedCards.Add(card);
                }
            }
            return selectedCards;
        }

        public void ClearSelectedCards()
        {
            // 清除所有选中的卡牌状态
            foreach (var card in instantiatedCards)
            {
                var cardItem = card.GetComponent<CardItem>();
                if (cardItem != null && cardItem.IsSelected)
                {
                    cardItem.Deselect();
                }
            }
        }
        public Dictionary<CardType, List<GameObject>> GroupSelectedCardsByType()
        {
            // 按卡牌类型分组选中的卡牌
            Dictionary<CardType, List<GameObject>> groupedCards = new Dictionary<CardType, List<GameObject>>();
            foreach (var card in instantiatedCards)
            {
                var cardItem = card.GetComponent<CardItem>();
                // 只选择未锁定且被选中的卡牌
                if (cardItem != null && cardItem.IsSelected && !cardItem.IsLocked)
                {
                    if (!groupedCards.ContainsKey(cardItem.Type))
                    {
                        groupedCards[cardItem.Type] = new List<GameObject>();
                    }
                    groupedCards[cardItem.Type].Add(card);
                }
            }
            return groupedCards;
        }
        public void PlaySelectedCards()
        {
            var selectedCards = GetSelectedCards();
            if (selectedCards.Count > 0)
            {
                // 锁定已选择的卡牌
                foreach (var card in selectedCards)
                {
                    LockCard(card.CardID);
                }
                
                // 向服务器发送出牌消息
                EventManager.TriggerEvent("PlayCards", selectedCards);
                
                // 禁用出牌按钮
                SetCardButtonsState(false);
                
                // 确保COMP按钮也被禁用
                UpdateCompButtonState(false);
            }
        }
        
        // 锁定指定ID的卡牌
        private void LockCard(string cardId)
        {
            if (!lockedCardIds.Contains(cardId))
            {
                lockedCardIds.Add(cardId);
                
                // 更新卡牌视觉状态
                if (cardItemsById.TryGetValue(cardId, out GameObject cardObject))
                {
                    var cardItem = cardObject.GetComponent<CardItem>();
                    if (cardItem != null)
                    {
                        cardItem.SetLockState(true);
                        Debug.Log($"已锁定卡牌: {cardId}");
                    }
                }
            }
        }
        
        // 解锁所有锁定的卡牌
        public void UnlockAllCards()
        {
            foreach (var cardId in lockedCardIds.ToArray()) // 使用ToArray创建副本，避免遍历时修改集合
            {
                UnlockCard(cardId);
            }
            
            lockedCardIds.Clear();
            Debug.Log("已解锁所有卡牌");
        }
        
        // 解锁指定ID的卡牌
        private void UnlockCard(string cardId)
        {
            if (lockedCardIds.Contains(cardId))
            {
                lockedCardIds.Remove(cardId);
                
                // 更新卡牌视觉状态
                if (cardItemsById.TryGetValue(cardId, out GameObject cardObject))
                {
                    var cardItem = cardObject.GetComponent<CardItem>();
                    if (cardItem != null)
                    {
                        cardItem.SetLockState(false);
                        Debug.Log($"已解锁卡牌: {cardId}");
                    }
                }
            }
        }

        public void ClearCardSelection()
        {
            ClearSelectedCards();
        }
        public void CompSelectedCards()
        {
            var groupedCards = GroupSelectedCardsByType();

            // Final validation before sending the request
            if (groupedCards.Count != 1)
            {
                Debug.LogWarning("合成失败：所选卡牌必须是相同类型");
                return;
            }

            var cardGroup = groupedCards.First();
            int cardCount = cardGroup.Value.Count;

            if (cardCount < 3)
            {
                Debug.LogWarning("合成失败：至少需要3张卡牌");
                return;
            }

            if (cardCount % 3 != 0)
            {
                Debug.LogWarning("合成失败：卡牌数量必须是3的倍数");
                return;
            }

            var cardsToSend = cardGroup.Value;

            // 先锁定要发送的卡牌，防止重复操作
            foreach (var cardObj in cardsToSend)
            {
                CardItem cardItem = cardObj.GetComponent<CardItem>();
                if (cardItem != null)
                {
                    LockCard(cardItem.CardID);
                }
            }

            // 向服务器发送消息
            EventManager.TriggerEvent("CompCard", cardsToSend);

            // 禁用COMP按钮
            UpdateCompButtonState(false);

            // 清除其他卡牌的选中状态
            ClearSelectedCards();
        }
        public void HandleCardSelected(CardSelectionData data)
        {
            Debug.Log($"卡牌选中：ID={data.CardID}, 类型={data.Type}");

            // Group all selected cards by type
            var groupedCards = GroupSelectedCardsByType();

            // Validate card composition rules:
            // 1. At least 3 cards must be selected
            // 2. All selected cards must be of the same type
            // 3. Number of cards must be a multiple of 3
            bool isValidComposition = false;

            if (groupedCards.Count == 1) // All cards are of the same type
            {
                var cardGroup = groupedCards.First();
                int cardCount = cardGroup.Value.Count;
                if (cardCount >= 3 && cardCount % 3 == 0)
                {
                    isValidComposition = true;
                }
            }

            // 更新COMP按钮状态
            UpdateCompButtonState(isValidComposition);
        }
        public void HandleCardDeselected(CardSelectionData data)
        {
            Debug.Log($"卡牌取消选中：ID={data.CardID}, 类型={data.Type}");

            // Group all selected cards by type
            var groupedCards = GroupSelectedCardsByType();

            // Validate card composition rules:
            // 1. At least 3 cards must be selected
            // 2. All selected cards must be of the same type
            // 3. Number of cards must be a multiple of 3
            bool isValidComposition = false;

            if (groupedCards.Count == 1) // All cards are of the same type
            {
                var cardGroup = groupedCards.First();
                int cardCount = cardGroup.Value.Count;
                if (cardCount >= 3 && cardCount % 3 == 0)
                {
                    isValidComposition = true;
                }
            }

            // 更新COMP按钮状态
            UpdateCompButtonState(isValidComposition);
        }

        // 初始化敌方卡牌UI
        public void InitEnemyCards(int cardCount)
        {
            if (enemyCardContainer == null)
            {
                Debug.LogError("敌方卡牌容器不存在");
                return;
            }

            // 获取所有卡牌对象
            var enemyCards = new List<GameObject>();
            for (int i = 0; i < enemyCardContainer.childCount; i++)
            {
                enemyCards.Add(enemyCardContainer.GetChild(i).gameObject);
            }

            // 设置初始显示状态
            int totalCards = enemyCards.Count;
            for (int i = 0; i < totalCards; i++)
            {
                enemyCards[i].SetActive(i < cardCount);
            }
        }

        // 处理伤害结果展示效果        
        public void HandleDamageResult(DamageResult damageResult)
        {
            // 处理成功的情况，销毁已使用的卡牌
            if (damageResult.Type == DamageType.Attacker)
            {
                HandleAttackerDamage(damageResult);
            }
            else if (damageResult.Type == DamageType.Receiver)
            {
                HandleReceiverDamage(damageResult);
            }
        }
        
        // 处理出牌错误，解锁所有锁定的卡牌
        public void HandleDamageError(string errorMessage)
        {
            // 显示错误消息
            ShowMessage($"出牌失败: {errorMessage}");
            
            // 解锁所有锁定的卡牌
            UnlockAllCards();
            
            // 重新启用出牌按钮
            SetCardButtonsState(true);
            
            // 更新COMP按钮状态（基于当前选择的卡牌）
            var groupedCards = GroupSelectedCardsByType();
            bool hasThreeOrMore = groupedCards.Values.Any(group => group.Count >= 3);
            UpdateCompButtonState(hasThreeOrMore);
        }
        
        // 处理合成结果
        public void HandleCompResult(CompResult compResult)
        {
            if (compResult.Success)
            {
                if (compResult.ComposeType == ComposeType.Self)
                {
                    // 显示成功消息
                    ShowMessage($"合成成功! 获得了 {compResult.NewCards.Count} 张新卡牌");
                    
                    // 清除卡牌选择
                    ClearCardSelection();
                    
                    // 销毁已使用的卡牌
                    DestroyPlayedCards(compResult.UsedCards);
                    
                    // 添加新合成的卡牌
                    if (compResult.NewCards != null && compResult.NewCards.Count > 0)
                    {
                        foreach (var cardModel in compResult.NewCards)
                        {
                            InstantiateCardItem(cardModel);
                        }
                    }
                    
                    // 重新启用按钮
                    SetCardButtonsState(true);
                    UpdateCompButtonState(false);
                    
                    // 解锁所有锁定的卡牌
                    UnlockAllCards();
                }
                else // ComposeType.Enemy
                {
                    // 处理敌方的合成结果
                    HandleEnemyCompose(compResult.NewCards.Count);
                }
            }
            else
            {
                // 处理合成失败的情况
                HandleDamageError(compResult.ErrorMessage ?? "未知错误");
            }
        }
        
        // 处理攻击者效果
        private void HandleAttackerDamage(DamageResult damageResult)
        {
            // 1. 显示消息
            ShowMessage($"你对敌人造成了 {damageResult.TotalDamage} 点伤害");


            // 2. 延迟后执行扣血动画
            coroutineRunner.StartCoroutine(DelayAction(1f, () =>
            {
                // 计算敌人扣血
                DecrementBloodBar(enemyBloodBar, damageResult.TotalDamage);

                // 销毁已使用的卡牌
                UnlockAllCards();
                DestroyPlayedCards(damageResult.ProcessedCards);
            }));
        }
        // 处理承受者效果
        private void HandleReceiverDamage(DamageResult damageResult)
        {
            // 1. 显示消息
            ShowMessage($"你受到了 {damageResult.TotalDamage} 点伤害！");

            // 2. 延迟后执行扣血动画
            coroutineRunner.StartCoroutine(DelayAction(1f, () =>
            {
                // 计算玩家扣血
                DecrementBloodBar(playerBloodBar, damageResult.TotalDamage);
                
                // 3. 根据对方使用的卡牌数量，销毁相应数量的敌方卡牌
                RemoveEnemyCards(damageResult.ProcessedCards.Count);
            }));
        }
        
        // 移除敌方卡牌显示
        private void RemoveEnemyCards(int count)
        {
            if (enemyCardContainer == null)
            {
                Debug.LogWarning("敌方卡牌容器不存在");
                return;
            }

            // 获取所有卡牌对象
            List<GameObject> enemyCards = new List<GameObject>();
            for (int i = 0; i < enemyCardContainer.childCount; i++)
            {
                enemyCards.Add(enemyCardContainer.GetChild(i).gameObject);
            }

            // 计算当前显示的卡牌数量
            int visibleCount = 0;
            foreach (var card in enemyCards)
            {
                if (card.activeSelf)
                {
                    visibleCount++;
                }
            }

            // 计算需要保持显示的卡牌数量
            int targetVisibleCount = Mathf.Max(0, visibleCount - count);

            // 从后向前设置卡牌的显示/隐藏状态
            for (int i = 0; i < enemyCards.Count; i++)
            {
                bool shouldBeVisible = i < targetVisibleCount;
                GameObject card = enemyCards[i];

                if (card.activeSelf != shouldBeVisible)
                {
                    if (!shouldBeVisible)
                    {
                        // 添加消失动画，动画结束后设置为隐藏
                        card.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => {
                            card.transform.localScale = Vector3.one; // 重置缩放
                            card.SetActive(false);
                        });
                    }
                    else
                    {
                        card.SetActive(true);
                    }
                }
            }
        }
        
        // 显示伤害消息
        private void ShowMessage(string message)
        {
            msgText.text = message;
            panelMsg.SetActive(true);
            
            // 1秒后隐藏消息面板并清除消息
            coroutineRunner.StartCoroutine(DelayAction(1f, () => {
                panelMsg.SetActive(false);
                msgText.text = string.Empty;
            }));
        }
        
        // 用于延迟执行的协程
        private System.Collections.IEnumerator DelayAction(float delayTime, Action action)
        {
            yield return new WaitForSeconds(delayTime);
            action?.Invoke();
        }

        // 血条减少动画
        private void DecrementBloodBar(Image bloodBar, float damage)
        {
            // 假设最大血量为100，计算伤害比例
            const float MAX_HEALTH = 100f;
            float damageRatio = damage / MAX_HEALTH;
            
            // 获取当前血量
            float currentFill = bloodBar.fillAmount;
            float targetFill = Mathf.Max(0, currentFill - damageRatio); // 不低于0
            
            // 使用DOTween制作过渡动画
            bloodBar.DOFillAmount(targetFill, 0.5f).SetEase(DG.Tweening.Ease.OutQuad);
        }
        // 销毁已使用的卡牌
        public void DestroyPlayedCards(List<CardModel> processedCards)
        {
            foreach (var cardModel in processedCards)
            {
                if (cardItemsById.TryGetValue(cardModel.CardID, out GameObject cardObject))
                {
                    // 添加简单的消失动画
                    cardObject.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() =>
                    {
                        // 动画结束后从列表中移除并销毁
                        instantiatedCards.Remove(cardObject);
                        cardItemsById.Remove(cardModel.CardID);
                        cardModelsById.Remove(cardModel.CardID);
                        UnityEngine.Object.Destroy(cardObject);
                    });
                }
            }
        }
        // 只控制出牌按钮的启用/禁用状态
        public void SetCardButtonsState(bool enabled)
        {
            // 设置按钮交互状态
            if (playButton != null) playButton.interactable = enabled;

            // 设置按钮颜色，enabled为false时变灰
            Color buttonColor = enabled ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
            if (playButton != null && playButton.image != null) playButton.image.color = buttonColor;
        }
        
        // 更新COMP按钮状态
        public void UpdateCompButtonState(bool enabled)
        {
            if (compButton != null)
            {
                compButton.interactable = enabled;
                
                // 设置按钮颜色，enabled为false时变灰
                Color buttonColor = enabled ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
                if (compButton.image != null)
                {
                    compButton.image.color = buttonColor;
                }
            }
        }
        // 处理自己回合开始通知
        public void HandleTurnStarted(string message)
        {
            // 启用出牌相关按钮，允许玩家交互
            SetCardButtonsState(true);

            // 高亮自己的头像（添加动画效果）
            if (playerProfile != null)
            {
                playerProfile.transform.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }
        }        // 处理等待对方回合通知
        public void HandleTurnWaiting(string message)
        {  
            // 禁用出牌按钮，阻止玩家交互
            SetCardButtonsState(false);
              
            // 高亮对方的头像（添加动画效果）
            if (enemyProfile != null)
            {
                enemyProfile.transform.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }
        }
        //   // 显示回合通知
        // private void ShowTurnNotification(string message, bool isMyTurn)
        // {
        //     // 显示通知面板
        //     if (panelMsg != null)
        //     {
        //         panelMsg.SetActive(true);
                
        //         // 设置消息文本
        //         if (msgText != null)
        //         {
        //             msgText.text = message;
                    
        //             // 设置文本颜色，自己回合为绿色，对方回合为红色
        //             msgText.color = isMyTurn ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
        //         }
                
        //         // 短暂显示后自动隐藏（通过传入的MonoBehaviour组件启动协程）
        //         coroutineRunner.StartCoroutine(AutoHidePanel(panelMsg, 2.0f));
        //     }
        // }
        
        // // 自动隐藏面板的协程
        // private System.Collections.IEnumerator AutoHidePanel(GameObject panel, float delay)
        // {
        //     yield return new WaitForSeconds(delay);
        //     panel.SetActive(false);
        // }        // 自动选择第一张卡牌并出牌
        public void AutoPlayFirstCard()
        {
            Debug.Log("执行自动出牌...");
            
            // 先清除已选择的卡牌
            ClearSelectedCards();
            
            // 禁用所有出牌按钮，包括COMP按钮
            SetCardButtonsState(false);
            UpdateCompButtonState(false);

            // 如果有可用卡牌
            if (instantiatedCards.Count > 0)
            {
                // 获取第一张卡牌
                GameObject firstCard = instantiatedCards[0];
                CardItem cardItem = firstCard.GetComponent<CardItem>();
                
                if (cardItem != null)
                {
                    // 选择第一张卡牌
                    Debug.Log($"自动选择卡牌: {cardItem.CardID}, 类型: {cardItem.Type}");
                    
                    // 通过反射调用Select方法
                    var selectMethod = cardItem.GetType().GetMethod("Select");
                    if (selectMethod != null)
                    {
                        selectMethod.Invoke(cardItem, null);
                        
                        // 显示一个通知
                        ShowMessage("回合超时，已自动选择第一张卡牌出牌");
                        
                        // 短暂延迟后出牌
                        coroutineRunner.StartCoroutine(DelayAction(1.0f, () => {
                            // 执行出牌操作
                            PlaySelectedCards();
                        }));
                    }
                    else
                    {
                        Debug.LogError("无法找到卡牌的Select方法");
                    }
                }
                else
                {
                    Debug.LogError("找不到CardItem组件");
                }
            }
            else
            {
                Debug.LogWarning("没有可用的卡牌可以自动出牌");
            }
        }

        // 处理敌方合成卡牌的UI逻辑
        public void HandleEnemyCompose(int newCardsCount)
        {
            if (enemyCardContainer == null)
            {
                Debug.LogError("Enemy card container is not initialized");
                return;
            }

            // 获取所有敌方卡牌GameObject
            List<GameObject> enemyCards = new List<GameObject>();
            for (int i = 0; i < enemyCardContainer.childCount; i++)
            {
                enemyCards.Add(enemyCardContainer.GetChild(i).gameObject);
            }

            // 计算需要隐藏的卡牌数量（3张合成1张）
            int cardsToHide = newCardsCount * 3;

            // 确保不会超出总卡牌数量
            int totalCards = enemyCards.Count;
            int visibleCards = 0;

            // 计算当前显示的卡牌数量
            foreach (var card in enemyCards)
            {
                if (card.activeSelf)
                {
                    visibleCards++;
                }
            }

            // 计算合成后应该显示的卡牌数量
            int targetVisibleCards = visibleCards - cardsToHide + newCardsCount;

            // 确保显示数量在合理范围内
            targetVisibleCards = Mathf.Clamp(targetVisibleCards, 0, totalCards);

            // 从后向前设置卡牌的显示/隐藏状态
            for (int i = 0; i < totalCards; i++)
            {
                enemyCards[i].SetActive(i < targetVisibleCards);
            }
        }
    }
}
