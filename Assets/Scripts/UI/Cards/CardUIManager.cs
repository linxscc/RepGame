using UnityEngine;
using System.Collections.Generic;
using RepGameModels;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using RepGame.Core;
using System.Linq;
using DG.Tweening; // 添加 DOTween 命名空间引用
using TMPro;

namespace RepGame.UI
{
    public class CardUIManager
    {
        private Transform cardContainer;
        private Transform enemyCardContainer;
        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, CardModel> cardModelsById; // Store original CardModel objects by ID

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

            Debug.Log($"卡牌实例化完成，共{instantiatedCards.Count}张卡牌");
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
                        }                        // 将卡牌添加到列表和字典中
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
            // 返回所有选中的卡牌的ID和Type
            List<CardModel> selectedCards = new List<CardModel>();
            foreach (var card in instantiatedCards)
            {
                var cardItem = card.GetComponent<CardItem>();
                if (cardItem != null && cardItem.IsSelected)
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
                if (cardItem != null && cardItem.IsSelected)
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
                if (cardItem != null && cardItem.IsSelected)
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
                // 向服务器发送出牌消息
                EventManager.TriggerEvent("PlayCards", selectedCards);
            }
        }

        public void ClearCardSelection()
        {
            ClearSelectedCards();
        }

        public void CompSelectedCards()
        {
            var groupedCards = GroupSelectedCardsByType();

            foreach (var group in groupedCards)
            {
                if (group.Value.Count >= 3)
                {
                    int countToSend = (group.Value.Count / 3) * 3; // 按3的倍数取卡牌数量
                    var cardsToSend = group.Value.GetRange(0, countToSend);

                    // 向服务器发送消息
                    EventManager.TriggerEvent("CompCard", cardsToSend);

                    // 销毁已发送的卡牌
                    foreach (var card in cardsToSend)
                    {
                        UnityEngine.Object.Destroy(card);
                    }

                    // 其他卡牌状态回退
                    ClearSelectedCards();
                    break;
                }
            }
        }

        public void HandleCardSelected(CardSelectionData data)
        {
            Debug.Log($"卡牌选中：ID={data.CardID}, 类型={data.Type}");

            // Detect if there are three or more cards of the same type
            var groupedCards = GroupSelectedCardsByType();
            bool hasThreeOrMore = groupedCards.Values.Any(group => group.Count >= 3);

            // Trigger event to update COMP button state
            EventManager.TriggerEvent("UpdateCompButtonState", hasThreeOrMore);
        }

        public void HandleCardDeselected(CardSelectionData data)
        {
            Debug.Log($"卡牌取消选中：ID={data.CardID}, 类型={data.Type}");

            // Detect if there are three or more cards of the same type
            var groupedCards = GroupSelectedCardsByType();
            bool hasThreeOrMore = groupedCards.Values.Any(group => group.Count >= 3);

            // Trigger event to update COMP button state
            EventManager.TriggerEvent("UpdateCompButtonState", hasThreeOrMore);
        }

        public void InitEnemyCards(int cardCount)
        {
            if (enemyCardContainer == null)
            {
                Debug.LogError("敌方卡牌容器不存在");
                return;
            }

            // 先清空敌方卡牌容器下的所有卡牌
            foreach (Transform child in enemyCardContainer)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            // 实例化敌方卡牌（cardback）
            for (int i = 0; i < cardCount; i++)
            {
                string address = "Assets/Prefabs/Cards/cardback.prefab";
                Addressables.LoadAssetAsync<GameObject>(address).Completed += (handle) =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject cardBackPrefab = handle.Result;
                        if (cardBackPrefab == null)
                        {
                            Debug.LogError($"找不到敌方卡牌预制体：{address}");
                            return;
                        }
                        // 实例化并设置为隐藏
                        GameObject cardBackObj = UnityEngine.Object.Instantiate(cardBackPrefab, enemyCardContainer);
                        cardBackObj.name = $"EnemyCardBack_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    }
                    else
                    {
                        Debug.LogError($"加载敌方卡牌预制体失败: {address}");
                    }
                };
            }
        }        // 处理伤害结果展示效果
        public void HandleDamageResult(DamageResult damageResult)
        {
            if (damageResult.Type == DamageType.Attacker)
            {
                HandleAttackerDamage(damageResult);
            }
            else if (damageResult.Type == DamageType.Receiver)
            {
                HandleReceiverDamage(damageResult);
            }
        }        // 处理攻击者效果
        private void HandleAttackerDamage(DamageResult damageResult)
        {
            // 禁用出牌按钮 - 作为攻击者时
            SetCardButtonsState(false);
            
            // 1. 显示消息
            ShowDamageMessage($"你对敌人造成了 {damageResult.TotalDamage} 点伤害！");

            // 2. 延迟后执行扣血动画
            coroutineRunner.StartCoroutine(DelayAction(1f, () => {
                // 计算敌人扣血
                DecrementBloodBar(enemyBloodBar, damageResult.TotalDamage);
                
                // 销毁已使用的卡牌
                DestroyPlayedCards(damageResult.ProcessedCards);
            }));
        }        // 处理承受者效果
        private void HandleReceiverDamage(DamageResult damageResult)
        {
            // 启用出牌按钮 - 作为承受者时
            SetCardButtonsState(true);
            
            // 1. 显示消息
            ShowDamageMessage($"你受到了 {damageResult.TotalDamage} 点伤害！");

            // 2. 延迟后执行扣血动画
            coroutineRunner.StartCoroutine(DelayAction(1f, () => {
                // 计算玩家扣血
                DecrementBloodBar(playerBloodBar, damageResult.TotalDamage);
            }));
        }

        // 显示伤害消息
        private void ShowDamageMessage(string message)
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
        private System.Collections.IEnumerator DelayAction(float delayTime, System.Action action)
        {
            yield return new UnityEngine.WaitForSeconds(delayTime);
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
        private void DestroyPlayedCards(List<CardModel> processedCards)
        {
            foreach (var cardModel in processedCards)
            {
                if (cardItemsById.TryGetValue(cardModel.CardID, out GameObject cardObject))
                {
                    // 添加简单的消失动画
                    cardObject.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => {
                        // 动画结束后从列表中移除并销毁
                        instantiatedCards.Remove(cardObject);
                        cardItemsById.Remove(cardModel.CardID);
                        cardModelsById.Remove(cardModel.CardID);
                        UnityEngine.Object.Destroy(cardObject);
                    });
                }
            }
        }        // 控制出牌相关按钮的启用/禁用状态
        public void SetCardButtonsState(bool enabled)
        {
            // 设置按钮交互状态
            if (playButton != null) playButton.interactable = enabled;
            if (clrButton != null) clrButton.interactable = enabled;
            if (compButton != null) compButton.interactable = enabled;

            // 设置按钮颜色，enabled为false时变灰
            Color buttonColor = enabled ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
            if (playButton != null && playButton.image != null) playButton.image.color = buttonColor;
            if (clrButton != null && clrButton.image != null) clrButton.image.color = buttonColor;
            if (compButton != null && compButton.image != null) compButton.image.color = buttonColor;
        }// 处理自己回合开始通知
        public void HandleTurnStarted(string message)
        {           
            // 启用所有出牌相关按钮，允许玩家交互
            SetCardButtonsState(true);
            
            // 更新UI显示
            ShowTurnNotification(message, true);
            
            // 高亮自己的头像（添加动画效果）
            if (playerProfile != null)
            {
                playerProfile.transform.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }
        }
          // 处理等待对方回合通知
        public void HandleTurnWaiting(string message)
        {  
            // 禁用所有出牌按钮，阻止玩家交互
            SetCardButtonsState(false);
            
            // 更新UI显示
            ShowTurnNotification(message, false);
            
            // 高亮对方的头像（添加动画效果）
            if (enemyProfile != null)
            {
                enemyProfile.transform.DOScale(1.1f, 0.3f).SetLoops(2, LoopType.Yoyo);
            }
        }
          // 显示回合通知
        private void ShowTurnNotification(string message, bool isMyTurn)
        {
            // 显示通知面板
            if (panelMsg != null)
            {
                panelMsg.SetActive(true);
                
                // 设置消息文本
                if (msgText != null)
                {
                    msgText.text = message;
                    
                    // 设置文本颜色，自己回合为绿色，对方回合为红色
                    msgText.color = isMyTurn ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
                }
                
                // 短暂显示后自动隐藏（通过传入的MonoBehaviour组件启动协程）
                coroutineRunner.StartCoroutine(AutoHidePanel(panelMsg, 2.0f));
            }
        }
        
        // 自动隐藏面板的协程
        private System.Collections.IEnumerator AutoHidePanel(GameObject panel, float delay)
        {
            yield return new WaitForSeconds(delay);
            panel.SetActive(false);
        }
    }
}
