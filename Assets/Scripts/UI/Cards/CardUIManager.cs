using UnityEngine;
using System.Collections.Generic;
using RepGameModels;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using RepGame.Core;
using System.Linq;

namespace RepGame.UI
{
    public class CardUIManager
    {
        private Transform cardContainer;
        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, CardModel> cardModelsById; // Store original CardModel objects by ID

        public CardUIManager(Transform cardContainer)
        {
            this.cardContainer = cardContainer;
            this.instantiatedCards = new List<GameObject>();
            this.cardItemsById = new Dictionary<string, GameObject>();
            this.cardModelsById = new Dictionary<string, CardModel>();
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
    }
}
