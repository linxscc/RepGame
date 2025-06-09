using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;
using System;
using System.Linq;
using RepGame.GameLogic;
using System.Collections;

namespace RepGame.UI
{
    public class MainPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Main";
        public const string Panel_GameOver = "Panel_GameOver";

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
        private Image infoImg;
        private Text infoText;
        public GameObject bondButtonPrefab;
        private Transform bondContainer;        // 游戏状态管理器
        private GameStateManager gameStateManager;

        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, Card> cardModelsById;

        // 协程管理
        private Coroutine currentInfoCoroutine;
        private bool isInfoDisplaying = false;

        private float MAX_HEALTH = 100f;
        private void Start()
        {
            // 初始化游戏状态管理器
            gameStateManager = new GameStateManager();
            SubscribeToGameStateEvents();

            // 查找Panel和组件
            panelMain = FindGameObject(PANEL_NAME);
            InitializeComponents();
            SubscribeEvents();
        }
        private void InitializeComponents()
        {
            // 查找容器
            cardContainer = FindGameObject("CardContainer")?.transform;
            playContainer = FindGameObject("PlayContainer")?.transform;
            enemyCardContainer = FindGameObject("EnemyCardContainer")?.transform;
            bondContainer = FindGameObject("BondListView/Viewport/BondContainer")?.transform;

            // 查找按钮
            playButton = FindButton("Play", OnPlayButtonClicked);
            clrButton = FindButton("CLR", OnClrButtonClicked);
            compButton = FindButton("COMP", OnCompButtonClicked);

            // 查找UI组件
            bloodBar = FindComponent<Image>("BloodBar/Fill");
            profile = FindComponent<Image>("Profile");
            enemyBloodBar = FindComponent<Image>("EnemyBloodBar/Fill");
            enemyProfile = FindComponent<Image>("EnemyProfile");
            blood_Text = FindText("BloodBar/Blood_Text");
            enemyBlood_Text = FindText("EnemyBloodBar/Blood_Text");
            infoImg = FindComponent<Image>("InfoImage");
            infoText = FindTextByNormal("InfoImage/InfoText");

            // 初始化数据结构
            instantiatedCards = new List<GameObject>();
            cardItemsById = new Dictionary<string, GameObject>();
            cardModelsById = new Dictionary<string, Card>();
        }        /// <summary>
                 /// 订阅游戏状态管理器的事件
                 /// </summary>        
        private void SubscribeToGameStateEvents()
        {
            gameStateManager.OnHandCardsUpdated += HandleHandCardsUpdated;
            gameStateManager.OnEnemyCardCountUpdated += HandleEnemyCardCountUpdated;
            gameStateManager.OnGameStateUpdated += HandleGameStateUpdated;
            gameStateManager.OnMessageRequested += ShowTimedMessage;
            gameStateManager.OnDelayedMessageRequested += ShowDelayedMessage;
            gameStateManager.OnGameOverRequested += HandleGameOverRequested;
            gameStateManager.OnClearCardSelection += ClearAllCardSelections;
            gameStateManager.OnButtonVisibilityUpdate += UpdateButtonVisibility;
            gameStateManager.OnBondDisplayUpdate += UpdateBondDisplay;
            gameStateManager.OnGameDataClear += HandleGameDataClear;
        }

        #region GameStateManager Event Handlers

        private void HandleHandCardsUpdated(List<Card> cards)
        {
            UpdateHandCards(cards);
        }
        private void HandleEnemyCardCountUpdated(int count)
        {
            InitEnemyCardVisibility(count);
        }
        private void HandleGameStateUpdated(ResPlayerGameInfo gameInfo)
        {
            UpdateHealthBars(gameInfo);
        }

        private void HandleGameDataClear()
        {
            try
            {
                Debug.Log("MainPanelController: 开始清理UI数据...");

                // 1. 清空所有卡牌UI对象
                ClearCards();

                // 2. 清空羁绊按钮
                ClearBondButtons();

                // 3. 重置按钮状态
                if (playButton != null) playButton.gameObject.SetActive(false);
                if (clrButton != null) clrButton.gameObject.SetActive(false);
                if (compButton != null) compButton.gameObject.SetActive(false);

                // 4. 清除任何显示中的消息
                if (currentInfoCoroutine != null)
                {
                    StopCoroutine(currentInfoCoroutine);
                    currentInfoCoroutine = null;
                }
                isInfoDisplaying = false;
                if (infoImg != null) infoImg.gameObject.SetActive(false);

                UIPanelController.Instance.ShowPanel(Panel_GameOver);

            }
            catch (Exception ex)
            {
                Debug.LogError($"MainPanelController: 清理UI数据时出现错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowDelayedMessage(string message, float delay, float duration)
        {
            StartCoroutine(DelayedMessage(message, delay, duration));
        }

        private void HandleGameOverRequested(bool isWinner)
        {
            ShowTimedMessage(isWinner ? "你胜利了！" : "你失败了！", 3.0f);
        }

        private void ClearAllCardSelections()
        {
            EventManager.TriggerEvent("ClearAllCardSelection");
        }

        #endregion

        private void SubscribeEvents()
        {
            EventManager.Subscribe<ResPlayerGameInfo>("InitGame", InitGame);
            EventManager.Subscribe<string>("CardSelected", OnCardSelected);
            EventManager.Subscribe<string>("CardDeselected", OnCardDeselected);
            EventManager.Subscribe<string>("UserPlayCard", OnPlayCardResult);
            EventManager.Subscribe<object>("UserCompose", OnComposeCardResult);

            EventManager.Subscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Subscribe<string>("GameOver", OnGameOver);
        }
        private void OnDisable()
        {
            // 停止当前信息显示协程
            if (currentInfoCoroutine != null)
            {
                StopCoroutine(currentInfoCoroutine);
                currentInfoCoroutine = null;
            }

            // 重置状态
            isInfoDisplaying = false;

            EventManager.Unsubscribe<ResPlayerGameInfo>("InitGame", InitGame);
            EventManager.Unsubscribe<string>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<string>("CardDeselected", OnCardDeselected);
            EventManager.Unsubscribe<string>("UserPlayCard", OnPlayCardResult);
            EventManager.Unsubscribe<object>("UserCompose", OnComposeCardResult);


            EventManager.Unsubscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Unsubscribe<string>("GameOver", OnGameOver);
        }
        private void UpdateButtonVisibility()
        {
            bool hasSelectedCards = gameStateManager.HasSelectedCards();

            // CLR按钮：只要有选中的卡牌就激活并显示
            clrButton.gameObject.SetActive(hasSelectedCards);
            clrButton.interactable = hasSelectedCards;

            // Play按钮：有选中卡牌时显示，但只有在自己回合时才激活
            if (hasSelectedCards)
            {
                playButton.gameObject.SetActive(true);
                // 判断是否是自己的回合
                bool isMyTurn = gameStateManager.IsMyTurn();
                playButton.interactable = isMyTurn;
            }
            else
            {
                playButton.gameObject.SetActive(false);
            }

            // COMP按钮：当选中卡牌中存在三张或以上同名卡牌时才激活并显示
            bool canCompose = gameStateManager.CanCompose();
            compButton.gameObject.SetActive(canCompose);
            compButton.interactable = canCompose;
        }
        private void InitGame(ResPlayerGameInfo resPlayerGameInfo)
        {
            Init(resPlayerGameInfo);
            // 使用游戏状态管理器初始化游戏
            gameStateManager.InitializeGame(resPlayerGameInfo);
        }
        private void OnCardSelected(string uid)
        {
            // 委托给游戏状态管理器处理卡牌选择
            gameStateManager.HandleCardSelected(uid);

            // 更新羁绊显示
            UpdateBondDisplay();
        }
        private void OnCardDeselected(string uid)
        {
            // 委托给游戏状态管理器处理卡牌取消选择
            gameStateManager.HandleCardDeselected(uid);

            // 更新羁绊显示
            UpdateBondDisplay();
        }
        private void OnPlayButtonClicked()
        {
            if (gameStateManager.HasSelectedCards())
            {
                // 使用游戏状态管理器处理出牌逻辑
                var playRequest = gameStateManager.CreatePlayCardRequest();
                if (playRequest != null)
                {
                    // 使用GameTcpClient发送出牌数据到服务器
                    GameTcpClient.Instance.SendMessageToServer("UserPlayCard", playRequest);

                    // 销毁已发送的卡牌对象
                    foreach (var card in playRequest.SelfCards)
                    {
                        // 根据卡牌UID找到对应的GameObject并销毁
                        if (cardItemsById.ContainsKey(card.UID))
                        {
                            GameObject cardObject = cardItemsById[card.UID];
                            cardItemsById.Remove(card.UID);
                            cardModelsById.Remove(card.UID);
                            instantiatedCards.Remove(cardObject);
                            Destroy(cardObject);
                        }
                    }

                    // 清除选择列表
                    gameStateManager.ClearSelectedCards();

                }
            }
            else
            {
                Debug.LogWarning("没有选中任何卡牌");
            }
        }
        private void OnClrButtonClicked()
        {
            ClearSelectedCards();
        }
        private void OnCompButtonClicked()
        {
            // 使用游戏状态管理器处理合成逻辑
            var composeResult = gameStateManager.CreateComposeCardRequest();
            if (composeResult.request != null)
            {
                GameTcpClient.Instance.SendMessageToServer("UserComposeCard", composeResult.request);

                // 移除要合成的卡牌从UI中
                foreach (var card in composeResult.cardsToCompose)
                {
                    // 根据卡牌UID找到对应的GameObject并销毁
                    if (cardItemsById.ContainsKey(card.UID))
                    {
                        GameObject cardObject = cardItemsById[card.UID];
                        cardItemsById.Remove(card.UID);
                        cardModelsById.Remove(card.UID);
                        instantiatedCards.Remove(cardObject);
                        Destroy(cardObject);
                    }
                }

                // 恢复其他选中卡牌的状态
                foreach (var card in composeResult.cardsToRevert)
                {
                    // 通知CardItem取消选中状态
                    if (cardItemsById.ContainsKey(card.UID))
                    {
                        var cardItem = cardItemsById[card.UID].GetComponent<CardItem>();
                        if (cardItem != null)
                        {
                            // 使用Deselect方法取消选中状态
                            cardItem.Deselect();
                        }
                    }
                }

                // 从游戏状态管理器中清除选中的卡牌
                gameStateManager.ClearSelectedCards();
            }
        }

        private void OnForcePlayCards(string message)
        {
            Debug.Log($"收到强制出牌请求: {message}");
            cardManager.AutoPlayFirstCard();
        }
        private void OnGameOver(string result)
        {
            if (result == null)
            {
                ShowDamageMessage("网络连接错误！");
                return;
            }

            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            // 委托给 GameStateManager 处理游戏结束的业务逻辑
            // GameStateManager 会通过事件系统自动更新UI和游戏状态
            gameStateManager.HandleGameOver(playerGameInfo);
        }
        private void Init(ResPlayerGameInfo resInfo)
        {
            List<Card> cards = resInfo.SelfCards;

            // 确保游戏开始时血条是满的
            MAX_HEALTH = resInfo.Health;
            blood_Text.text = MAX_HEALTH.ToString();
            enemyBlood_Text.text = MAX_HEALTH.ToString();
            if (bloodBar != null) bloodBar.fillAmount = 1f;
            if (enemyBloodBar != null) enemyBloodBar.fillAmount = 1f;
            // 清空现有卡牌
            ClearCards();

            // 确保卡牌容器存在
            if (cardContainer == null)
            {
                Debug.LogError("卡牌容器不存在");
                return;
            }

            // 直接实例化卡牌（因为 CardPoolManager 已在启动时预加载了所有预制体）
            foreach (var cardModel in cards)
            {
                InstantiateCardItem(cardModel);
            }

            // 初始化敌方卡牌的显示状态
            InitEnemyCardVisibility(resInfo.OtherPlayers[0].CardsCount);

        }

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
        private void InstantiateCardItem(Card cardModel)
        {
            try
            {
                // 使用CardPoolManager获取卡牌实例
                CardPoolManager.Instance.GetCardInstance(cardModel.Name, cardModel.UID, cardContainer, (cardObject) =>
                {
                    if (cardObject != null)
                    {
                        // 将卡牌添加到列表和字典中
                        instantiatedCards.Add(cardObject);
                        cardItemsById[cardModel.UID] = cardObject;
                        cardModelsById[cardModel.UID] = cardModel;

                    }
                    else
                    {
                        Debug.LogError($"从对象池获取卡牌实例失败: {cardModel.Name}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"实例化卡牌时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        /// <summary>
        /// 清除所有选中的卡牌
        /// </summary>
        public void ClearSelectedCards()
        {
            // 使用游戏状态管理器清除选中的卡牌
            gameStateManager.ClearSelectedCards();
            Debug.Log("已清除所有选中的卡牌");
        }
        /// <summary>
        /// 检查指定卡牌是否已选中
        /// </summary>
        /// <param name="uid">卡牌UID</param>
        /// <returns>是否已选中</returns>
        public bool IsCardSelected(string uid)
        {
            return gameStateManager.IsCardSelected(uid);
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
            cardModelsById.Clear();

            // 清空选中的卡牌 - 使用游戏状态管理器
            gameStateManager.ClearSelectedCards();
        }
        /// <summary>
        /// 更新羁绊显示
        /// </summary>
        private void UpdateBondDisplay()
        {
            if (bondContainer == null)
            {
                Debug.LogWarning("BondContainer 未找到，无法更新羁绊显示");
                return;
            }

            if (bondButtonPrefab == null)
            {
                Debug.LogWarning("BondButtonPrefab 未设置，无法更新羁绊显示");
                return;
            }

            // 清除现有的羁绊按钮
            ClearBondButtons();

            if (!gameStateManager.HasSelectedCards())
            {
                return; // 没有选中的卡牌，不显示任何羁绊
            }

            // 获取选中的卡牌列表
            var selectedCards = gameStateManager.SelectedCards;

            // 获取所有可能的羁绊（包括激活的和潜在的）
            var allPossibleBonds = GetAllPossibleBonds(selectedCards);

            // 获取激活的羁绊
            var activeBonds = GetActivatedBonds(selectedCards);

            // 合并并排序羁绊（激活的羁绊优先，然后按等级和伤害排序）
            var sortedBonds = SortBonds(allPossibleBonds, activeBonds);

            // 实例化羁绊按钮并配置
            foreach (var bond in sortedBonds)
            {
                CreateBondButton(bond, activeBonds.Contains(bond));
            }
        }

        /// <summary>
        /// 清除所有羁绊按钮
        /// </summary>
        private void ClearBondButtons()
        {
            foreach (Transform child in bondContainer)
            {
                if (child.gameObject != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        /// <summary>
        /// 获取所有可能的羁绊（基于选中的卡牌）
        /// </summary>
        private List<BondModel> GetAllPossibleBonds(List<Card> selectedCards)
        {
            var allBonds = new HashSet<BondModel>();

            foreach (var card in selectedCards)
            {
                var bonds = BondManager.Instance.GetBondsByCardName(card.Name);
                foreach (var bond in bonds)
                {
                    allBonds.Add(bond);
                }
            }

            return allBonds.ToList();
        }

        /// <summary>
        /// 获取激活的羁绊
        /// </summary>
        private List<BondModel> GetActivatedBonds(List<Card> selectedCards)
        {
            var selectedCardNames = selectedCards.Select(card => card.Name).ToList();
            return BondManager.Instance.GetActiveBonds(selectedCardNames);
        }
        /// <summary>
        /// 排序羁绊：激活的羁绊优先，然后按等级和伤害排序
        /// </summary>
        private List<BondModel> SortBonds(List<BondModel> allBonds, List<BondModel> activeBonds)
        {
            return allBonds.OrderBy(bond => !activeBonds.Contains(bond))  // 激活的排在前面
                          .ThenByDescending(bond => bond.Level)              // 按等级降序
                          .ThenByDescending(bond => bond.Damage)             // 按伤害降序
                          .ToList();
        }

        /// <summary>
        /// 创建羁绊按钮
        /// </summary>
        private void CreateBondButton(BondModel bond, bool isActivated)
        {
            GameObject bondButton = Instantiate(bondButtonPrefab, bondContainer);

            // 配置BondItem组件
            BondItem bondItem = bondButton.GetComponent<BondItem>();
            if (bondItem == null)
            {
                bondItem = bondButton.AddComponent<BondItem>();
            }

            bondItem.IsActived = isActivated;
            bondItem.Name = bond.Name;
            bondItem.Level = bond.Level;
            bondItem.CardNames = bond.CardNames;
            bondItem.Damage = bond.Damage;
            bondItem.Description = bond.Description;
            bondItem.Skill = bond.Skill;

            // 可以根据是否激活设置不同的视觉效果
            SetBondButtonVisualState(bondButton, isActivated);
        }

        /// <summary>
        /// 设置羁绊按钮的视觉状态
        /// </summary>
        private void SetBondButtonVisualState(GameObject bondButton, bool isActivated)
        {
            // 可以在这里设置不同的颜色、透明度等来区分激活和未激活的羁绊
            var image = bondButton.GetComponent<Image>();
            if (image != null)
            {
                if (isActivated)
                {
                    image.color = Color.yellow; // 激活的羁绊用黄色
                }
                else
                {
                    image.color = Color.gray; // 未激活的羁绊用灰色
                }
            }
        }
        private void OnPlayCardResult(string result)
        {
            if (result == null)
            {
                ShowDamageMessage("网络连接错误！");
                return;
            }

            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            gameStateManager.HandlePlayCardResult(playerGameInfo);
        }
        /// <summary>
        /// 显示伤害消息（仅显示，不更新游戏状态）
        /// </summary>
        /// <param name="message">要显示的消息</param>
        private void ShowDamageMessage(string message)
        {
            ShowInfoMessage(message, 0.8f);
        }
        /// <summary>
        /// 统一的信息显示方法，防止协程冲突
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="duration">显示时长</param>
        private void ShowInfoMessage(string message, float duration)
        {
            if (infoImg == null || infoText == null)
            {
                Debug.LogWarning("InfoImg 或 InfoText 为空，无法显示消息");
                return;
            }

            // 如果当前有信息正在显示，先停止当前协程
            if (currentInfoCoroutine != null)
            {
                StopCoroutine(currentInfoCoroutine);
                currentInfoCoroutine = null;
            }

            // 重置状态
            isInfoDisplaying = true;

            // 显示信息
            infoImg.gameObject.SetActive(true);
            infoText.text = message;

            // 启动新的隐藏协程
            currentInfoCoroutine = StartCoroutine(HideInfoAfterDelay(duration));
        }
        /// <summary>
        /// 延迟隐藏信息面板的协程
        /// </summary>
        private IEnumerator HideInfoAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 安全地隐藏信息面板
            if (infoImg != null)
            {
                infoImg.gameObject.SetActive(false);
            }

            // 重置状态
            isInfoDisplaying = false;
            currentInfoCoroutine = null;
        }

        /// <summary>
        /// 显示定时消息（指定显示时长）
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="duration">显示时长（秒）</param>
        private void ShowTimedMessage(string message, float duration)
        {
            ShowInfoMessage(message, duration);
        }
        /// <summary>
        /// 延迟显示消息的协程
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="duration">显示时长（秒）</param>
        private IEnumerator DelayedMessage(string message, float delay, float duration)
        {
            // 等待指定的延迟时间
            yield return new WaitForSeconds(delay);

            // 显示消息
            ShowTimedMessage(message, duration);
        }

        /// <summary>
        /// 增量更新手牌信息 - 只更新变化的卡牌
        /// </summary>
        /// <param name="serverCards">服务器返回的手牌列表</param>
        private void UpdateHandCards(List<Card> serverCards)
        {
            if (serverCards == null)
            {
                // 如果服务器返回空列表，清空所有手牌
                ClearCards();
                Debug.Log("服务器返回空手牌列表，已清空所有本地手牌");
                return;
            }

            // 创建服务器卡牌UID集合用于快速查找
            var serverCardUIDs = new HashSet<string>(serverCards.Select(card => card.UID));

            // 1. 找出需要删除的本地卡牌（本地有但服务器没有的）
            var cardsToRemove = new List<string>();
            foreach (var localUID in cardItemsById.Keys.ToList())
            {
                if (!serverCardUIDs.Contains(localUID))
                {
                    cardsToRemove.Add(localUID);
                }
            }

            // 删除不存在的卡牌
            foreach (var uidToRemove in cardsToRemove)
            {
                RemoveCardByUID(uidToRemove);
                Debug.Log($"删除本地卡牌: UID {uidToRemove}");
            }

            // 2. 找出需要添加的新卡牌（服务器有但本地没有的）
            var cardsToAdd = new List<Card>();
            foreach (var serverCard in serverCards)
            {
                if (!cardItemsById.ContainsKey(serverCard.UID))
                {
                    cardsToAdd.Add(serverCard);
                }
            }

            // 添加新卡牌
            foreach (var newCard in cardsToAdd)
            {
                InstantiateCardItem(newCard);
                Debug.Log($"添加新卡牌: {newCard.Name} (UID: {newCard.UID})");
            }

            Debug.Log($"手牌增量更新完成 - 删除: {cardsToRemove.Count}, 添加: {cardsToAdd.Count}, 当前手牌数量: {cardItemsById.Count}");
        }
        /// <summary>
        /// 根据UID删除单个卡牌
        /// </summary>
        /// <param name="uid">要删除的卡牌UID</param>
        private void RemoveCardByUID(string uid)
        {
            if (cardItemsById.ContainsKey(uid))
            {
                // 获取卡牌对象
                GameObject cardObject = cardItemsById[uid];
                Card cardModel = cardModelsById[uid];

                // 如果卡牌被选中，从选中列表中移除 - 使用游戏状态管理器
                if (gameStateManager.IsCardSelected(uid))
                {
                    gameStateManager.HandleCardDeselected(uid);
                }

                // 从各个数据结构中移除
                cardItemsById.Remove(uid);
                cardModelsById.Remove(uid);
                instantiatedCards.Remove(cardObject);

                // 销毁GameObject
                if (cardObject != null)
                {
                    Destroy(cardObject);
                }
            }
        }
        private void OnComposeCardResult(object result)
        {
            if (result == null)
            {
                ShowDamageMessage("网络连接错误！");
                return;
            }

            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            // 委托给 GameStateManager 处理合成结果的业务逻辑
            // GameStateManager 会通过事件系统自动更新UI和游戏状态
            gameStateManager.HandleComposeCardResult(playerGameInfo);
        }

        /// <summary>
        /// 更新双方血条
        /// </summary>
        /// <param name="gameInfo">游戏状态信息</param>
        private void UpdateHealthBars(ResPlayerGameInfo gameInfo)
        {
            if (gameInfo == null)
            {
                Debug.LogWarning("游戏状态信息为空，无法更新血条");
                return;
            }

            // 更新己方血条
            if (bloodBar != null && blood_Text != null)
            {
                float playerHealthRatio = gameInfo.Health / MAX_HEALTH;
                bloodBar.fillAmount = Mathf.Clamp01(playerHealthRatio);
                blood_Text.text = gameInfo.Health.ToString("F0");
            }

            // 更新敌方血条
            if (gameInfo.OtherPlayers != null && gameInfo.OtherPlayers.Count > 0)
            {
                var enemyPlayer = gameInfo.OtherPlayers[0];
                if (enemyBloodBar != null && enemyBlood_Text != null)
                {
                    float enemyHealthRatio = enemyPlayer.Health / MAX_HEALTH;
                    enemyBloodBar.fillAmount = Mathf.Clamp01(enemyHealthRatio);
                    enemyBlood_Text.text = enemyPlayer.Health.ToString("F0");
                }
            }
        }

    }
}
