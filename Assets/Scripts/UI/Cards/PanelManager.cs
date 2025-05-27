using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;


namespace RepGame.UI
{

    public class PanelManager : UIBase
    {
        [Header("Panels")]
        private GameObject panelServer;
        private GameObject panelStart;
        private GameObject panelMain;
        private GameObject panelMsg;
        private GameObject gameOverPanel;


        [Header("Server UI Elements")]
        private TextMeshProUGUI serverStatusText;
        private TextMeshProUGUI msgText;
        private TextMeshProUGUI gameOverText;
        private TextMeshProUGUI Blood_Text;
        private TextMeshProUGUI EnemyBlood_Text;

        [Header("Buttons")]
        private Button startGameButton;
        private Button exitGameButton;
        private Button reconnectButton;
        private Button playButton;
        private Button clrButton;
        private Button compButton;

        [Header("Card Management")]
        private Transform cardContainer;
        private Transform playContainer;
        private CardUIManager cardManager;

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
            gameOverPanel = FindGameObject("Panel_GameOver");

            // 查找按钮并添加监听器
            startGameButton = FindButton("Panel_Start/Start", OnStartGameClicked);
            exitGameButton = FindButton("Panel_Start/Quit", OnExitGameClicked);

            // 查找文本组件
            serverStatusText = FindText("Panel_Server/ServerStatus");
            msgText = FindText("Panel_Msg/Msg/Msg_text");
            Blood_Text = FindText("Panel_Main/BloodBar/Blood_Text");
            EnemyBlood_Text = FindText("Panel_Main/EnemyBloodBar/Blood_Text");

            // 查找卡牌容器
            cardContainer = FindGameObject("Panel_Main/CardContainer")?.transform;
            playContainer = FindGameObject("Panel_Main/PlayContainer")?.transform;

            // 查找新按钮
            playButton = FindButton("Panel_Main/Play", OnPlayButtonClicked);
            clrButton = FindButton("Panel_Main/CLR", OnClrButtonClicked);
            compButton = FindButton("Panel_Main/COMP", OnCompButtonClicked);

            BloodBar = FindGameObject("Panel_Main/BloodBar/Fill").GetComponent<Image>();

            Profile = FindGameObject("Panel_Main/Profile").GetComponent<Image>();
            EnemyCardContainer = FindGameObject("Panel_Main/EnemyCardContainer")?.transform;
            EnemyBloodBar = FindGameObject("Panel_Main/EnemyBloodBar/Fill").GetComponent<Image>();
            EnemyProfile = FindGameObject("Panel_Main/EnemyProfile").GetComponent<Image>();


            // 如果找到了游戏结束面板，获取其中的文本组件
            if (gameOverPanel != null)
            {
                gameOverText = gameOverPanel.transform.Find("GameOverText")?.GetComponent<TextMeshProUGUI>();
                gameOverPanel.SetActive(false); // 确保初始时是隐藏的
            }

            // 初始化面板
            panelServer.SetActive(true);
            panelStart.SetActive(false);
            panelMain.SetActive(false);

            // 初始化COMP按钮为不可选
            compButton.interactable = false;
            playButton.interactable = false;

            // 隐藏按钮
            playButton.gameObject.SetActive(false);
            clrButton.gameObject.SetActive(false);
            compButton.gameObject.SetActive(false);

            // 初始化 CardManager
            cardManager = new CardUIManager(cardContainer, EnemyCardContainer, playContainer);

            // 初始化卡牌管理器所需的所有UI组件
            cardManager.InitializeUIComponents(
                playButton, clrButton, compButton,
                BloodBar, EnemyBloodBar,
                Profile, EnemyProfile,
                panelMsg, msgText, Blood_Text, EnemyBlood_Text,
                this,
                gameOverPanel, gameOverText);
        }
        private void OnEnable()
        {
            // 订阅事件            
            EventManager.Subscribe<string>("ConnectedToServer", OnServerConnected);
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

            // 订阅网络连接相关事件
            EventManager.Subscribe<string>("ConnectionTimeout", OnConnectionTimeout);
            EventManager.Subscribe<string>("ConnectionFailed", OnConnectionFailed);
            EventManager.Subscribe<string>("ServerClosed", OnServerClosed);
            EventManager.Subscribe<string>("Disconnected", OnDisconnected);
        }

        private void OnDisable()
        {
            // 取消订阅事件
            EventManager.Unsubscribe<string>("ConnectedToServer", OnServerConnected);
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

            // 取消订阅网络连接相关事件
            EventManager.Unsubscribe<string>("ConnectionTimeout", OnConnectionTimeout);
            EventManager.Unsubscribe<string>("ConnectionFailed", OnConnectionFailed);
            EventManager.Unsubscribe<string>("ServerClosed", OnServerClosed);
            EventManager.Unsubscribe<string>("Disconnected", OnDisconnected);
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

            // 调用CardUIManager处理出牌错误
            cardManager.HandleDamageError(errorMessage);
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

        // 处理强制出牌请求
        private void OnForcePlayCards(string message)
        {
            Debug.Log($"收到强制出牌请求: {message}");

            // 调用CardUIManager自动出牌方法
            cardManager.AutoPlayFirstCard();
        }
        // 处理合成结果
        private void OnCompResult(CompResult compResult)
        {
            Debug.Log($"收到合成结果: 成功={compResult.Success}, 新卡牌数量={compResult.NewCards?.Count ?? 0}, 类型={compResult.ComposeType}");
            cardManager.HandleCompResult(compResult);
        }

        // 处理合成错误
        private void OnCompError(string errorMessage)
        {
            Debug.LogError($"合成处理错误: {errorMessage}");
            cardManager.HandleDamageError(errorMessage);
        }

        // 延迟执行操作的协程
        private System.Collections.IEnumerator DelayAction(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        // 处理游戏结束
        private void OnGameOver(object gameOverData)
        {
            Debug.Log($"收到游戏结束消息：{gameOverData}");
            cardManager.HandleGameOver(gameOverData);
        }

        // 处理初始血量
        private void OnInitHealth(int initialHealth)
        {
            Debug.Log($"收到初始血量：{initialHealth}");
            cardManager.UpdateInitHealthBars(initialHealth);

        }

        // 处理连接超时
        private void OnConnectionTimeout(string message)
        {
            ShowConnectionError(message);
        }

        // 处理连接失败
        private void OnConnectionFailed(string message)
        {
            ShowConnectionError(message);
        }

        // 处理服务器关闭
        private void OnServerClosed(string message)
        {
            ShowConnectionError(message);
        }

        // 处理断开连接
        private void OnDisconnected(string message)
        {
            ShowConnectionError(message);
        }

        // 显示连接错误
        private void ShowConnectionError(string errorMessage)
        {
            if (panelServer != null)
            {
                // 显示错误面板
                panelServer.SetActive(true);

                // 设置错误消息
                if (msgText != null)
                {
                    msgText.text = errorMessage;
                }
                // 尝试重新连接
                EventManager.TriggerEvent("AttemptReconnect");
                msgText.text += "\nAttempt to Reconnect...";
                // 隐藏其他面板
                if (panelStart != null) panelStart.SetActive(false);
                if (panelMain != null) panelMain.SetActive(false);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
            }
        }

    }
}
