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
using RepGame.UI;

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

        private CardUIManager cardManager;
        
        [Header("Main Panel Buttons")]
        private Button playButton;
        private Button clrButton;
        private Button compButton;

        private void Start()
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

            // 查找新按钮
            playButton = FindButton("Panel_Main/Play", OnPlayButtonClicked);
            clrButton = FindButton("Panel_Main/CLR", OnClrButtonClicked);
            compButton = FindButton("Panel_Main/COMP", OnCompButtonClicked);

            // 初始化面板
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);

            // 初始化 CardManager
            cardManager = new CardUIManager(cardContainer);

            // 初始化COMP按钮为不可选
            compButton.interactable = false;

            // 隐藏按钮
            playButton.gameObject.SetActive(false);
            clrButton.gameObject.SetActive(false);
            compButton.gameObject.SetActive(false);
        }
        
        private void OnEnable()
        {
            // 订阅事件
            EventManager.Subscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Subscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Subscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Subscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Subscribe<RepGameModels.DamageResult>("CardDamageResult", OnCardDamageResult);
        }
    
        private void OnDisable()
        {
            // 取消订阅事件
             EventManager.Unsubscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Unsubscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Unsubscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Unsubscribe<RepGameModels.DamageResult>("CardDamageResult", OnCardDamageResult);
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
            cardManager.InitPlayerCards(cardModels);
        }
        
        private void UpdateButtonVisibility()
        {
            int selectedCardCount = cardManager.GetSelectedCards().Count;
            bool hasSelectedCards = selectedCardCount > 0;

            playButton.gameObject.SetActive(hasSelectedCards);
            clrButton.gameObject.SetActive(hasSelectedCards);
            compButton.gameObject.SetActive(hasSelectedCards);
        }
        
        private void OnCardSelected(CardSelectionData data)
        {
            cardManager.HandleCardSelected(data);
            UpdateButtonVisibility();
        }
        
        private void OnCardDeselected(CardSelectionData data)
        {
            cardManager.HandleCardDeselected(data);
            UpdateButtonVisibility();
        }

        private void OnExitGameClicked()
        {
            // 退出游戏
            Debug.Log("退出游戏...");
            Application.Quit();
        }        private void OnPlayButtonClicked()
        {
            // 使用 CardUIManager 处理卡牌逻辑
            // CardUIManager.PlaySelectedCards() 会触发 PlayCards 事件，由 GameClient 处理并发送到服务器
            cardManager.PlaySelectedCards();
        }

        private void OnClrButtonClicked()
        {
            cardManager.ClearCardSelection();
        }

        private void OnCompButtonClicked()
        {
            cardManager.CompSelectedCards();
        }

        // 处理伤害结果
        private void OnCardDamageResult(DamageResult damageResult)
        {
            Debug.Log($"收到伤害结果：总伤害 {damageResult.TotalDamage}，处理卡牌数量 {damageResult.ProcessedCards.Count}");
            
            // 这里可以添加UI显示或其他处理逻辑
            // 例如：显示伤害动画，更新UI等
            
            // 如果需要将伤害信息显示在UI上，可以添加相应代码
            // damageText.text = $"伤害: {damageResult.TotalDamage}";
        }
    }
}
