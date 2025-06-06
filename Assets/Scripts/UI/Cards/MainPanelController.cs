using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.Core;
using System.Collections.Generic;
using RepGameModels;
using System;
using System.Linq;
using RepGame.GameLogic;

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

        //羁绊UI
        public GameObject bondButtonPrefab;
        private Transform bondContainer;


        private List<GameObject> instantiatedCards;
        private Dictionary<string, GameObject> cardItemsById;
        private Dictionary<string, Card> cardModelsById;

        private List<Card> selectedCards;

        private float MAX_HEALTH = 100f;
        private string Room_Id;
        private string username;

        private string round;

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

            // 初始化数据结构
            instantiatedCards = new List<GameObject>();
            cardItemsById = new Dictionary<string, GameObject>();
            cardModelsById = new Dictionary<string, Card>();
            selectedCards = new List<Card>();
        }

        private void SubscribeEvents()
        {
            EventManager.Subscribe<ResPlayerGameInfo>("InitGame", InitGame);
            EventManager.Subscribe<string>("CardSelected", OnCardSelected);
            EventManager.Subscribe<string>("CardDeselected", OnCardDeselected);


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
            EventManager.Unsubscribe<ResPlayerGameInfo>("InitGame", InitGame);
            EventManager.Unsubscribe<string>("CardSelected", OnCardSelected);
            EventManager.Unsubscribe<string>("CardDeselected", OnCardDeselected);

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
            bool hasSelectedCards = selectedCards.Count > 0;

            // CLR按钮：只要有选中的卡牌就激活并显示
            clrButton.gameObject.SetActive(hasSelectedCards);
            clrButton.interactable = hasSelectedCards;

            // Play按钮：有选中卡牌时显示，但只有在自己回合时才激活
            if (hasSelectedCards)
            {
                playButton.gameObject.SetActive(true);
                // 判断是否是自己的回合
                bool isMyTurn = string.Equals(round, "current", StringComparison.OrdinalIgnoreCase);
                playButton.interactable = isMyTurn;
            }
            else
            {
                playButton.gameObject.SetActive(false);
            }

            // COMP按钮：当选中卡牌中存在三张或以上同名卡牌时才激活并显示
            bool canCompose = CanCompose();
            compButton.gameObject.SetActive(canCompose);
            compButton.interactable = canCompose;
        }

        private void InitGame(ResPlayerGameInfo resPlayerGameInfo)
        {

            Init(resPlayerGameInfo);
            username = resPlayerGameInfo.Username;
            round = resPlayerGameInfo.Round;

        }
        private void OnCardSelected(string uid)
        {
            // 根据uid从cardModelsById中取出对应的card数据
            if (cardModelsById.ContainsKey(uid))
            {
                Card card = cardModelsById[uid];

                // 检查是否已在选中列表中
                if (!selectedCards.Contains(card))
                {
                    selectedCards.Add(card);
                    Debug.Log($"卡牌已选中: {card.Name} (UID: {uid}), 当前选中数量: {selectedCards.Count}");
                }
                else
                {
                    Debug.LogWarning($"卡牌已在选中列表中: {card.Name} (UID: {uid})");
                }
            }
            else
            {
                Debug.LogError($"未找到UID对应的卡牌数据: {uid}");
            }

            // 更新按钮显示状态
            UpdateButtonVisibility();

            // 更新羁绊显示
            UpdateBondDisplay();
        }
        private void OnCardDeselected(string uid)
        {
            // 根据uid从cardModelsById中取出对应的card数据
            if (cardModelsById.ContainsKey(uid))
            {
                Card card = cardModelsById[uid];

                // 从selectedCards中移除
                if (selectedCards.Remove(card))
                {
                    Debug.Log($"卡牌已取消选中: {card.Name} (UID: {uid}), 当前选中数量: {selectedCards.Count}");
                }
                else
                {
                    Debug.LogWarning($"尝试移除未选中的卡牌: {card.Name} (UID: {uid})");
                }
            }
            else
            {
                Debug.LogError($"未找到UID对应的卡牌数据: {uid}");
            }

            // 更新按钮显示状态
            UpdateButtonVisibility();

            // 更新羁绊显示
            UpdateBondDisplay();
        }
        private void OnPlayButtonClicked()
        {
            if (selectedCards.Count > 0)
            {
                Debug.Log($"出牌: {selectedCards.Count} 张卡牌");

                // 创建要发送的卡牌列表（复制一份选中的卡牌）
                var cardsToPlay = new List<Card>(selectedCards);

                // 创建出牌请求数据，使用ResPlayerGameInfo结构
                var playerGameInfo = new ResPlayerGameInfo
                {
                    Room_Id = Room_Id,
                    Username = username,
                    Round = round,
                    SelfCards = cardsToPlay  // 使用Card列表
                };

                Debug.Log($"发送出牌请求：房间ID {playerGameInfo.Room_Id}，用户名 {playerGameInfo.Username}，出牌数量 {cardsToPlay.Count}");

                // 使用GameTcpClient发送出牌数据到服务器
                GameTcpClient.Instance.SendMessageToServer("UserPlayCard", playerGameInfo);

                // 销毁已发送的卡牌对象
                foreach (var card in cardsToPlay)
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
                selectedCards.Clear();

                // 更新按钮状态
                UpdateButtonVisibility();

                // 更新羁绊状态
                UpdateBondDisplay();

                Debug.Log("出牌完成，已清除选中状态并销毁卡牌对象");
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
            if (selectedCards.Count < 3)
            {
                Debug.LogWarning("选中的卡牌数量不足3张，无法进行合成");
                return;
            }

            // 按卡牌名称分组
            var cardGroups = selectedCards.GroupBy(card => card.Name).ToList();
            var cardsToCompose = new List<Card>();
            var cardsToRevert = new List<Card>();

            // 找出可以合成的卡牌（同名且数量>=3的组合）
            foreach (var group in cardGroups)
            {
                var groupCards = group.ToList();
                int composableCount = (groupCards.Count / 3) * 3; // 取3的倍数

                if (composableCount >= 3)
                {
                    // 添加可合成的卡牌
                    for (int i = 0; i < composableCount; i++)
                    {
                        cardsToCompose.Add(groupCards[i]);
                    }

                    // 剩余的卡牌需要恢复状态
                    for (int i = composableCount; i < groupCards.Count; i++)
                    {
                        cardsToRevert.Add(groupCards[i]);
                    }
                }
                else
                {
                    // 数量不足3张的组合，全部恢复状态
                    cardsToRevert.AddRange(groupCards);
                }
            }

            if (cardsToCompose.Count == 0)
            {
                Debug.LogWarning("没有足够的同名卡牌进行合成（需要至少3张同名卡牌）");
                return;
            }

            Debug.Log($"准备合成 {cardsToCompose.Count} 张卡牌，恢复 {cardsToRevert.Count} 张卡牌状态");

            // 创建合成请求数据，使用ResPlayerGameInfo结构
            var playerGameInfo = new ResPlayerGameInfo
            {
                Room_Id = Room_Id,
                Username = username,
                Round = round,
                SelfCards = cardsToCompose  // 使用Card列表
            };

            Debug.Log($"发送卡牌合成请求：房间ID {playerGameInfo.Room_Id}，用户名 {playerGameInfo.Username}，合成卡牌数量 {cardsToCompose.Count}");


            GameTcpClient.Instance.SendMessageToServer("UserComposeCard", playerGameInfo);

            // 移除要合成的卡牌从UI中
            foreach (var card in cardsToCompose)
            {
                selectedCards.Remove(card);
                // 根据卡牌UID找到对应的GameObject并销毁
                if (cardItemsById.ContainsKey(card.UID))
                {
                    GameObject cardObject = cardItemsById[card.UID];
                    cardItemsById.Remove(card.UID);
                    cardModelsById.Remove(card.UID);
                    instantiatedCards.Remove(cardObject);
                    Destroy(cardObject);
                }
            }            // 恢复其他选中卡牌的状态
            foreach (var card in cardsToRevert)
            {
                selectedCards.Remove(card);
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

            // 更新按钮状态
            UpdateButtonVisibility();

            // 更新羁绊显示
            UpdateBondDisplay();
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
        private void Init(ResPlayerGameInfo resInfo)
        {
            List<Card> cards = resInfo.SelfCards;
            Room_Id = resInfo.Room_Id;

            // 确保游戏开始时血条是满的
            MAX_HEALTH = resInfo.Health;
            blood_Text.text = MAX_HEALTH.ToString();
            enemyBlood_Text.text = MAX_HEALTH.ToString();
            if (bloodBar != null) bloodBar.fillAmount = 1f;
            if (enemyBloodBar != null) enemyBloodBar.fillAmount = 1f;            // 清空现有卡牌
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
        }

        /// <summary>
        /// 清除所有选中的卡牌
        /// </summary>
        public void ClearSelectedCards()
        {
            // 发送清除所有卡牌选择的事件
            EventManager.TriggerEvent("ClearAllCardSelection");

            selectedCards.Clear();
            Debug.Log("已清除所有选中的卡牌");
            UpdateButtonVisibility();
            UpdateBondDisplay(); // 更新羁绊显示
        }

        /// <summary>
        /// 检查指定卡牌是否已选中
        /// </summary>
        /// <param name="uid">卡牌UID</param>
        /// <returns>是否已选中</returns>
        public bool IsCardSelected(string uid)
        {
            if (cardModelsById.ContainsKey(uid))
            {
                return selectedCards.Contains(cardModelsById[uid]);
            }
            return false;
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

            // 清空选中的卡牌
            selectedCards.Clear();

            // 更新按钮状态
            UpdateButtonVisibility();
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

            if (selectedCards.Count == 0)
            {
                return; // 没有选中的卡牌，不显示任何羁绊
            }

            // 获取所有可能的羁绊（包括激活的和潜在的）
            var allPossibleBonds = GetAllPossibleBonds();

            // 获取激活的羁绊
            var activeBonds = GetActivatedBonds();

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
        private List<BondModel> GetAllPossibleBonds()
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
        private List<BondModel> GetActivatedBonds()
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

        /// <summary>
        /// 检查是否可以合成：选中卡牌中是否存在三张或以上同名卡牌
        /// </summary>
        private bool CanCompose()
        {
            if (selectedCards.Count < 3)
                return false;

            // 统计每个卡牌名称的数量
            var cardNameCounts = new Dictionary<string, int>();
            foreach (var card in selectedCards)
            {
                if (cardNameCounts.ContainsKey(card.Name))
                {
                    cardNameCounts[card.Name]++;
                }
                else
                {
                    cardNameCounts[card.Name] = 1;
                }
            }

            // 检查是否有任何卡牌名称的数量达到3张或以上
            return cardNameCounts.Values.Any(count => count >= 3);
        }
    }
}
