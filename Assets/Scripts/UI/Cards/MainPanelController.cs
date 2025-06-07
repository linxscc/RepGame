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
        private TextMeshProUGUI infoText;
        public GameObject bondButtonPrefab;
        private Transform bondContainer;

        // 游戏状态管理器
        private GameStateManager gameStateManager;

        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, Card> cardModelsById;

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
            enemyProfile = FindComponent<Image>("EnemyProfile"); blood_Text = FindText("BloodBar/Blood_Text");
            enemyBlood_Text = FindText("EnemyBloodBar/Blood_Text");
            infoImg = FindComponent<Image>("InfoImage");
            infoText = FindText("InfoImage/InfoText");

            // 初始化数据结构
            instantiatedCards = new List<GameObject>();
            cardItemsById = new Dictionary<string, GameObject>();
            cardModelsById = new Dictionary<string, Card>();
        }
        /// <summary>
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
            UpdateGameState(gameInfo);
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
            EventManager.Subscribe<object>("UserPlayCard", OnPlayCardResult);
            EventManager.Subscribe<object>("UserCompose", OnComposeCardResult);

            EventManager.Subscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Subscribe<object>("GameOver", OnGameOver);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<ResPlayerGameInfo>("InitGame", InitGame);
            EventManager.Unsubscribe<string>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<string>("CardDeselected", OnCardDeselected);
            EventManager.Unsubscribe<object>("UserPlayCard", OnPlayCardResult);
            EventManager.Unsubscribe<object>("UserCompose", OnComposeCardResult);


            EventManager.Unsubscribe<string>("ForcePlayCards", OnForcePlayCards);
            EventManager.Unsubscribe<object>("GameOver", OnGameOver);
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
                Debug.Log($"出牌: {gameStateManager.SelectedCards.Count} 张卡牌");

                // 使用游戏状态管理器处理出牌逻辑
                var playRequest = gameStateManager.CreatePlayCardRequest();
                if (playRequest != null)
                {
                    Debug.Log($"发送出牌请求：房间ID {playRequest.Room_Id}，用户名 {playRequest.Username}，出牌数量 {playRequest.SelfCards.Count}");

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

                    Debug.Log("出牌完成，已清除选中状态并销毁卡牌对象");
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
                Debug.Log($"准备合成 {composeResult.cardsToCompose.Count} 张卡牌，恢复 {composeResult.cardsToRevert.Count} 张卡牌状态");

                Debug.Log($"发送卡牌合成请求：房间ID {composeResult.request.Room_Id}，用户名 {composeResult.request.Username}，合成卡牌数量 {composeResult.cardsToCompose.Count}");

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
        private void OnGameOver(object result)
        {
            if (result == null)
            {
                ShowDamageMessage("网络连接错误！");
                return;
            }

            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            // 判断当前血量是否<=0，确定是失败还是胜利
            if (playerGameInfo.Health <= 0)
            {
                // 失败情况：血量<=0
                Debug.Log($"游戏失败 - 当前血量: {playerGameInfo.Health}");

                // 构建失败消息，包含触发的羁绊信息
                string defeatMessage = BuildDamageMessage(playerGameInfo.DamageReceived, playerGameInfo.TriggeredBonds, false);

                // 显示受到伤害的消息
                ShowTimedMessage(defeatMessage, 0.5f);

                // 0.5秒后显示失败消息
                StartCoroutine(ShowGameOverMessage("你失败了！", 0.5f));
            }
            else
            {
                // 胜利情况：血量>0
                Debug.Log($"游戏胜利 - 当前血量: {playerGameInfo.Health}");

                // 直接显示胜利消息
                ShowTimedMessage("你胜利了！", 2.0f);
            }

            // 更新最终游戏状态
            UpdateGameState(playerGameInfo);
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
            InitEnemyCardVisibility(resInfo.OtherCards.Count);

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

                        Debug.Log($"成功从对象池获取卡牌实例: {cardModel.Name} (ID: {cardModel.UID})");
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
        }        /// <summary>
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
        private void OnPlayCardResult(object result)
        {
            if (result == null)
            {
                ShowDamageMessage("网络连接错误！");
                return;
            }

            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            // 检查是否造成伤害
            if (playerGameInfo.DamageDealt > 0)
            {
                string damageMessage = BuildDamageMessage(playerGameInfo.DamageDealt, playerGameInfo.TriggeredBonds, true);
                ShowDamageMessageAndUpdateGame(damageMessage, playerGameInfo);
            }
            // 检查是否收到攻击伤害
            else if (playerGameInfo.DamageReceived > 0)
            {
                string damageMessage = BuildDamageMessage(playerGameInfo.DamageReceived, playerGameInfo.TriggeredBonds, false);
                ShowDamageMessageAndUpdateGame(damageMessage, playerGameInfo);
            }
            else
            {
                // 没有造成或受到伤害，直接更新游戏状态
                UpdateGameState(playerGameInfo);
            }
        }

        /// <summary>
        /// 构建伤害消息
        /// </summary>
        /// <param name="damageAmount">伤害数值</param>
        /// <param name="triggeredBonds">触发的羁绊</param>
        /// <param name="isDamageDealer">是否为造成伤害方</param>
        /// <returns>构建好的伤害消息</returns>
        private string BuildDamageMessage(float damageAmount, BondModel[] triggeredBonds, bool isDamageDealer)
        {
            string actionText = isDamageDealer ? "造成" : "受到";
            string damageText = isDamageDealer ? "点伤害" : "点伤害";

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
        /// 显示伤害消息（仅显示，不更新游戏状态）
        /// </summary>
        /// <param name="message">要显示的消息</param>
        private void ShowDamageMessage(string message)
        {
            infoImg.gameObject.SetActive(true);
            infoText.text = message;
            StartCoroutine(HideInfoAfterDelay(0.8f));
        }

        /// <summary>
        /// 显示伤害消息并更新游戏状态
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="playerGameInfo">游戏状态信息</param>
        private void ShowDamageMessageAndUpdateGame(string message, ResPlayerGameInfo playerGameInfo)
        {
            infoImg.gameObject.SetActive(true);
            infoText.text = message;

            // 0.8秒后隐藏消息并更新游戏状态
            StartCoroutine(HideInfoAndUpdateGame(0.8f));
            UpdateGameState(playerGameInfo);
        }

        /// <summary>
        /// 延迟隐藏信息面板的协程
        /// </summary>
        private IEnumerator HideInfoAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            infoImg.gameObject.SetActive(false);
        }

        /// <summary>
        /// 延迟隐藏信息面板并更新游戏状态的协程
        /// </summary>
        private IEnumerator HideInfoAndUpdateGame(float delay)
        {
            yield return new WaitForSeconds(delay);
            infoText.text = "";
            infoImg.gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示定时消息（指定显示时长）
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="duration">显示时长（秒）</param>
        private void ShowTimedMessage(string message, float duration)
        {
            if (infoImg != null && infoText != null)
            {
                infoImg.gameObject.SetActive(true);
                infoText.text = message;

                // 启动定时隐藏协程
                StartCoroutine(HideMessageAfterDelay(duration));
            }
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
        /// 延迟隐藏消息的协程
        /// </summary>
        /// <param name="duration">延迟时间（秒）</param>
        private IEnumerator HideMessageAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);

            if (infoImg != null)
            {
                infoImg.gameObject.SetActive(false);
            }
        }        /// <summary>
                 /// 更新游戏状态（血量、回合、手牌等）
                 /// </summary>
        private void UpdateGameState(ResPlayerGameInfo playerGameInfo)
        {
            // 更新血量
            if (playerGameInfo.Health >= 0)
            {
                MAX_HEALTH = playerGameInfo.Health;
                blood_Text.text = MAX_HEALTH.ToString();

                // 更新血条填充
                if (bloodBar != null)
                {
                    // 计算血条比例
                    float healthRatio = playerGameInfo.Health / MAX_HEALTH;
                    bloodBar.fillAmount = Mathf.Clamp01(healthRatio);
                }
            }

            // 更新手牌信息 - 增量更新
            if (playerGameInfo.SelfCards != null)
            {
                UpdateHandCards(playerGameInfo.SelfCards);
            }

            // 更新敌方手牌显示
            if (playerGameInfo.OtherCards != null)
            {
                InitEnemyCardVisibility(playerGameInfo.OtherCards.Count);
                Debug.Log($"敌方手牌更新：敌方手牌数量 {playerGameInfo.OtherCards.Count}");
            }

            // 通过事件处理器让游戏状态管理器处理状态更新
            HandleGameStateUpdated(playerGameInfo);// 更新手牌信息 - 增量更新
            if (playerGameInfo.SelfCards != null)
            {
                UpdateHandCards(playerGameInfo.SelfCards);
            }

            // 更新敌方手牌显示
            if (playerGameInfo.OtherCards != null)
            {
                InitEnemyCardVisibility(playerGameInfo.OtherCards.Count);
                Debug.Log($"敌方手牌更新：敌方手牌数量 {playerGameInfo.OtherCards.Count}");
            }

            // 更新按钮状态
            UpdateButtonVisibility();

            // 更新羁绊显示
            UpdateBondDisplay();
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

            // 1. 检查SelfCards更新并更新本地手牌
            if (playerGameInfo.SelfCards != null)
            {
                UpdateHandCards(playerGameInfo.SelfCards);
                Debug.Log($"卡牌合成成功，更新手牌数量：{playerGameInfo.SelfCards.Count}");

                // 显示合成成功消息 0.8 秒
                ShowTimedMessage("卡牌合成成功！", 0.8f);
            }

            // 2. 检查OtherCards数量更新并更新敌方卡牌容器显示
            if (playerGameInfo.OtherCards != null)
            {
                InitEnemyCardVisibility(playerGameInfo.OtherCards.Count);
                Debug.Log($"敌方完成卡牌合成，敌方手牌数量：{playerGameInfo.OtherCards.Count}");

                // 延迟显示敌方合成消息 0.8 秒（在己方消息后显示）
                StartCoroutine(DelayedMessage("敌方完成卡牌合成！", 1.0f, 0.8f));
            }

            // 3. 更新游戏状态
            UpdateGameState(playerGameInfo);
        }

        /// <summary>
        /// 延迟显示游戏结束消息的协程
        /// </summary>
        /// <param name="message">要显示的游戏结束消息</param>
        /// <param name="delay">延迟时间（秒）</param>
        private IEnumerator ShowGameOverMessage(string message, float delay)
        {
            // 等待指定的延迟时间
            yield return new WaitForSeconds(delay);

            // 显示游戏结束消息，持续时间更长
            ShowTimedMessage(message, 3.0f);

            Debug.Log($"游戏结束消息已显示: {message}");
        }

    }
}
