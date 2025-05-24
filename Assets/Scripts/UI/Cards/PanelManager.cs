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
using DG.Tweening; // 添加DOTween命名空间引用

namespace RepGame.UI
{
    
    public class PanelManager : UIBase
    {
        [Header("Panels")]
        private GameObject panelServer;
        private GameObject panelStart;
        private GameObject panelMain;
        private GameObject panelMsg;

        [Header("Server UI Elements")]
        private TextMeshProUGUI serverStatusText;
        private TextMeshProUGUI msgText;

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
        
        private Image BloodBar;
        private Image Profile;
        private Transform EnemyCardContainer;
        private Image EnemyBloodBar;
        private Image EnemyProfile;
        private void Start()
        {
            // 查找面板
            panelServer = FindGameObject("Panel_Server");
            panelStart = FindGameObject("Panel_Start");
            panelMain = FindGameObject("Panel_Main");
            panelMsg = FindGameObject("Panel_Msg");

            // 查找按钮并添加监听器
            startGameButton = FindButton("Panel_Start/Start", OnStartGameClicked);
            exitGameButton = FindButton("Panel_Start/Quit", OnExitGameClicked);

            // 查找文本组件
            serverStatusText = FindText("Panel_Server/ServerStatus");
            msgText = FindText("Panel_Msg/Msg/Msg_Text");

            // 查找卡牌容器
            cardContainer = FindGameObject("Panel_Main/CardContainer")?.transform;

            // 查找新按钮
            playButton = FindButton("Panel_Main/Play", OnPlayButtonClicked);
            clrButton = FindButton("Panel_Main/CLR", OnClrButtonClicked);
            compButton = FindButton("Panel_Main/COMP", OnCompButtonClicked);

            BloodBar = FindGameObject("Panel_Main/BloodBar/Fill").GetComponent<Image>();
            Profile = FindGameObject("Panel_Main/Profile").GetComponent<Image>();
            EnemyCardContainer = FindGameObject("Panel_Main/EnemyCardContainer")?.transform;
            EnemyBloodBar = FindGameObject("Panel_Main/EnemyBloodBar/Fill").GetComponent<Image>();
            EnemyProfile = FindGameObject("Panel_Main/EnemyProfile").GetComponent<Image>();

            // 初始化面板
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);

            // 初始化 CardManager
            cardManager = new CardUIManager(cardContainer,EnemyCardContainer);

            // 初始化COMP按钮为不可选
            compButton.interactable = false;
            playButton.interactable = false;

            // 隐藏按钮
            playButton.gameObject.SetActive(false);
            clrButton.gameObject.SetActive(false);
            compButton.gameObject.SetActive(false);
        }        private void OnEnable()
        {
            // 订阅事件
            EventManager.Subscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Subscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Subscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Subscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Subscribe<RepGameModels.DamageResult>("CardDamageResult", OnCardDamageResult);
            EventManager.Subscribe<string>("CardDamageError", OnCardDamageError);
            EventManager.Subscribe<string>("TurnStarted", OnTurnStarted);
            EventManager.Subscribe<string>("TurnWaiting", OnTurnWaiting);
        }
    
        private void OnDisable()
        {
            // 取消订阅事件
             EventManager.Unsubscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Unsubscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Unsubscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Unsubscribe<RepGameModels.DamageResult>("CardDamageResult", OnCardDamageResult);
            EventManager.Unsubscribe<string>("CardDamageError", OnCardDamageError);
            EventManager.Unsubscribe<string>("TurnStarted", OnTurnStarted);
            EventManager.Unsubscribe<string>("TurnWaiting", OnTurnWaiting);
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
        // 初始化卡牌管理器所需的所有UI组件
        cardManager.InitializeUIComponents(
            playButton, clrButton, compButton,
            BloodBar, EnemyBloodBar,
            Profile, EnemyProfile,
            panelMsg, msgText,
            this
        );
        
        // 初始化卡牌
        cardManager.InitPlayerCards(cardModels);
        cardManager.InitEnemyCards(cardModels.Count);
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
        }
        private void OnPlayButtonClicked()
        {
            // 使用 CardUIManager 处理卡牌逻辑
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
        private void OnCardDamageResult(DamageResult damageResult)
        {
            Debug.Log($"收到伤害结果：总伤害 {damageResult.TotalDamage}，处理卡牌数量 {damageResult.ProcessedCards.Count}，伤害类型：{damageResult.Type}");

            cardManager.HandleDamageResult(damageResult);
        }
        private void OnCardDamageError(string errorMessage)
        {
            Debug.LogError($"出牌处理错误: {errorMessage}");
            
            // 这里可以添加错误提示UI，例如弹出对话框
            // ShowErrorDialog(errorMessage);
        }
        // 处理自己回合开始通知
        private void OnTurnStarted(string message)
        {
            // 委托给CardUIManager处理回合开始通知
            cardManager.HandleTurnStarted(message);
        }

        // 处理等待对方回合通知
        private void OnTurnWaiting(string message)
        {
            // 委托给CardUIManager处理等待回合通知
            cardManager.HandleTurnWaiting(message);
        }
    }
}
