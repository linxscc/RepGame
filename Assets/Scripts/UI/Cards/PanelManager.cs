using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NUnit.Framework.Internal;
using TMPro;
using LiteNetLib;
using RepGame;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RepGame.UI
{
    
    public class PanelManager : UIBase
    {
        [Header("Panels")]
        private GameObject panelServer;
        private GameObject panelStart;
        private GameObject panelMain;

        [Header("Server UI Elements")]
        private TextMeshProUGUI serverStatusText;

        [Header("Start Panel Buttons")]
        private Button startGameButton;
        private Button exitGameButton;
        
        [Header("Card Management")]
        private Transform cardContainer; // 卡牌容器
        private List<GameObject> instantiatedCards = new List<GameObject>(); // 已实例化的卡牌列表
        private Dictionary<string, GameObject> cardItemsById = new Dictionary<string, GameObject>(); // 通过ID查找卡牌
        
        void Start()
        {
            // 查找面板
            panelServer = FindGameObject("Panel_Server");
            panelStart = FindGameObject("Panel_Start");
            panelMain = FindGameObject("Panel_Main");

            // 查找按钮并添加监听器
            startGameButton = FindButton("Panel_Start/Start", OnStartGameClicked);
            exitGameButton = FindButton("Panel_Start/Quit", OnExitGameClicked);

            // 查找文本组件
            serverStatusText = FindText("Panel_Server/ServerStatus");
            
            // 查找卡牌容器
            cardContainer = FindGameObject("Panel_Main/CardContainer")?.transform;
            if (cardContainer == null)
            {
                // 如果卡牌容器不存在，创建一个
                GameObject container = new GameObject("CardContainer");
                container.transform.SetParent(panelMain.transform, false);
                
                // 添加布局组件
                GridLayoutGroup layout = container.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(100, 150); // 卡牌大小
                layout.spacing = new Vector2(10, 10); // 卡牌间距
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;
                layout.childAlignment = TextAnchor.MiddleCenter;
                
                // 记录卡牌容器
                cardContainer = container.transform;
            }

            // 初始化面板
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);
        }
        
        private void OnEnable()
        {
            // 订阅事件
            EventManager.Subscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Subscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Subscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Subscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
        }
    
        private void OnDisable()
        {
            // 取消订阅事件
             EventManager.Unsubscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Unsubscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Unsubscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
        }

        public void OnServerConnected(string serverStatus)
        {
            // 更新服务器状态文本
            serverStatusText.text = serverStatus;

            // 切换到开始面板
            panelServer.SetActive(false);
            panelStart.SetActive(true);
        }

        public void OnStartGameClicked()
        {
            // 触发开始游戏事件
            EventManager.TriggerEvent("StartCardGame");

            // 切换到主面板
            panelStart.SetActive(false);
            panelMain.SetActive(true);
        }

        private void InitPlayerCards(List<CardModel> cardModels)
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
            
            // 初始化卡牌面板
            panelStart.SetActive(false);
            panelServer.SetActive(false);
            panelMain.SetActive(true);
            
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
                        GameObject cardObject = Instantiate(cardPrefab, cardContainer);
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
                    Destroy(cardObject);
                }
            }
            
            // 清空列表和字典
            instantiatedCards.Clear();
            cardItemsById.Clear();
        }
        
        private void OnCardSelected(CardSelectionData data)
        {
            Debug.Log($"卡牌选中：ID={data.CardID}, 类型={data.Type}");
            
            // 在这里处理卡牌选中逻辑
            // 例如高亮显示可用的目标、更新UI状态等
        }
        
        private void OnCardDeselected(CardSelectionData data)
        {
            Debug.Log($"卡牌取消选中：ID={data.CardID}, 类型={data.Type}");
            
            // 在这里处理卡牌取消选中逻辑
            // 例如清除高亮显示、重置UI状态等
        }

        private void OnExitGameClicked()
        {
            // 退出游戏
            Debug.Log("退出游戏...");
            Application.Quit();
        }
    }
}
