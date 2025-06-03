using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;

namespace RepGame.UI
{
    public class MainPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Main";

        private GameObject panelMain;

        // 按钮
        private Button playButton;
        private Button clrButton;
        private Button compButton;

        // 卡牌管理相关
        private Transform cardContainer;
        private Transform playContainer;
        private Transform enemyCardContainer;
        private CardUIManager cardManager;

        // UI组件
        private Image bloodBar;
        private Image profile;
        private Image enemyBloodBar;
        private Image enemyProfile;
        private TextMeshProUGUI blood_Text;
        private TextMeshProUGUI enemyBlood_Text;

        private void Start()
        {
            // 查找Panel和组件
            panelMain = FindGameObject(PANEL_NAME);
            InitializeComponents();
            SubscribeEvents();
        }

        private void InitializeComponents()
        {
            // 查找容器
            cardContainer = FindGameObject($"{PANEL_NAME}/CardContainer")?.transform;
            playContainer = FindGameObject($"{PANEL_NAME}/PlayContainer")?.transform;
            enemyCardContainer = FindGameObject($"{PANEL_NAME}/EnemyCardContainer")?.transform;

            // 查找按钮
            playButton = FindButton($"{PANEL_NAME}/Play", OnPlayButtonClicked);
            clrButton = FindButton($"{PANEL_NAME}/CLR", OnClrButtonClicked);
            compButton = FindButton($"{PANEL_NAME}/COMP", OnCompButtonClicked);

            // 查找UI组件
            bloodBar = FindComponent<Image>($"{PANEL_NAME}/BloodBar/Fill");
            profile = FindComponent<Image>($"{PANEL_NAME}/Profile");
            enemyBloodBar = FindComponent<Image>($"{PANEL_NAME}/EnemyBloodBar/Fill");
            enemyProfile = FindComponent<Image>($"{PANEL_NAME}/EnemyProfile");
            blood_Text = FindText($"{PANEL_NAME}/BloodBar/Blood_Text");
            enemyBlood_Text = FindText($"{PANEL_NAME}/EnemyBloodBar/Blood_Text");

            // 初始化卡牌管理器
            cardManager = new CardUIManager(cardContainer, enemyCardContainer, playContainer);
        }

        private void SubscribeEvents()
        {
            EventManager.Subscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Subscribe<int>("InitHealth", OnInitHealth);
            EventManager.Subscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Subscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Subscribe<DamageResult>("CardDamageResult", OnCardDamageResult);
            EventManager.Subscribe<string>("CardDamageError", OnCardDamageError);
            EventManager.Subscribe<string>("TurnStarted", OnTurnStarted);
            EventManager.Subscribe<string>("TurnWaiting", OnTurnWaiting);
            EventManager.Subscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Subscribe<CompResult>("CompResult", OnCompResult);
            EventManager.Subscribe<string>("CompError", OnCompError);
            EventManager.Subscribe<object>("GameOver", OnGameOver);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<List<CardModel>>("InitPlayerCards", InitPlayerCards);
            EventManager.Unsubscribe<int>("InitHealth", OnInitHealth);
            EventManager.Unsubscribe<CardSelectionData>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<CardSelectionData>("CardDeselected", OnCardDeselected);
            EventManager.Unsubscribe<DamageResult>("CardDamageResult", OnCardDamageResult);
            EventManager.Unsubscribe<string>("CardDamageError", OnCardDamageError);
            EventManager.Unsubscribe<string>("TurnStarted", OnTurnStarted);
            EventManager.Unsubscribe<string>("TurnWaiting", OnTurnWaiting);
            EventManager.Unsubscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Unsubscribe<CompResult>("CompResult", OnCompResult);
            EventManager.Unsubscribe<string>("CompError", OnCompError);
            EventManager.Unsubscribe<object>("GameOver", OnGameOver);
        }

        private void UpdateButtonVisibility()
        {
            int selectedCardCount = cardManager.GetSelectedCards().Count;
            bool hasSelectedCards = selectedCardCount > 0;

            playButton.gameObject.SetActive(hasSelectedCards);
            clrButton.gameObject.SetActive(hasSelectedCards);
            compButton.gameObject.SetActive(hasSelectedCards);
        }

        private void InitPlayerCards(List<CardModel> cardModels)
        {
            panelMain.SetActive(true);
            cardManager.InitPlayerCards(cardModels);
            cardManager.InitEnemyCards(cardModels.Count);
        }

        private void OnInitHealth(int initialHealth)
        {
            cardManager.UpdateInitHealthBars(initialHealth);
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

        private void OnPlayButtonClicked()
        {
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
            cardManager.HandleDamageError(errorMessage);
        }

        private void OnTurnStarted(string message)
        {
            cardManager.HandleTurnStarted(message);
        }

        private void OnTurnWaiting(string message)
        {
            cardManager.HandleTurnWaiting(message);
        }

        private void OnForcePlayCards(string message)
        {
            Debug.Log($"收到强制出牌请求: {message}");
            cardManager.AutoPlayFirstCard();
        }

        private void OnCompResult(CompResult compResult)
        {
            Debug.Log($"收到合成结果: 成功={compResult.Success}, 新卡牌数量={compResult.NewCards?.Count ?? 0}, 类型={compResult.ComposeType}");
            cardManager.HandleCompResult(compResult);
        }

        private void OnCompError(string errorMessage)
        {
            Debug.LogError($"合成处理错误: {errorMessage}");
            cardManager.HandleDamageError(errorMessage);
        }

        private void OnGameOver(object gameOverData)
        {
            Debug.Log($"收到游戏结束消息：{gameOverData}");
            cardManager.HandleGameOver(gameOverData);
        }
    }
}
