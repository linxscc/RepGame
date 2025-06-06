using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace RepGame.UI
{
    /// <summary>
    /// 卡牌对象池管理器，负责预加载卡牌预制体并提供实例复制功能
    /// </summary>
    public class CardPoolManager : MonoBehaviour
    {
        private static CardPoolManager _instance;
        public static CardPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CardPoolManager");
                    _instance = go.AddComponent<CardPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("预制体路径配置")]
        [SerializeField] private string cardPrefabPath = "Assets/Prefabs/Cards/";
        [SerializeField] private string prefabExtension = ".prefab";        // 预制体存储
        private Dictionary<string, GameObject> cardPrefabs = new Dictionary<string, GameObject>();

        // 对象池存储
        private Dictionary<string, Queue<GameObject>> cardPools = new Dictionary<string, Queue<GameObject>>();

        // 已加载的预制体记录
        private HashSet<string> loadedPrefabs = new HashSet<string>();

        [Header("对象池配置")]
        [SerializeField] private int defaultPoolSize = 100;
        [SerializeField] private Transform poolContainer; private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePoolContainer();
            LoadAllCardPrefabs();
        }
        private void InitializePoolContainer()
        {
            if (poolContainer == null)
            {
                GameObject container = new GameObject("CardPoolContainer");
                container.transform.SetParent(transform);
                container.SetActive(false); // 隐藏对象池容器
                poolContainer = container.transform;
            }
        }

        /// <summary>
        /// 加载指定路径下的所有卡牌预制体并创建对象池
        /// </summary>
        private void LoadAllCardPrefabs()
        {
            Debug.Log("开始加载所有卡牌预制体...");

            // 使用Addressables加载指定路径下的所有预制体
            Addressables.LoadResourceLocationsAsync(cardPrefabPath, typeof(GameObject)).Completed += (locations) =>
            {
                if (locations.Status == AsyncOperationStatus.Succeeded)
                {
                    int totalPrefabs = locations.Result.Count;
                    int loadedCount = 0;

                    Debug.Log($"找到 {totalPrefabs} 个卡牌预制体");

                    if (totalPrefabs == 0)
                    {
                        Debug.LogWarning($"在路径 {cardPrefabPath} 下没有找到任何预制体");
                        return;
                    }

                    foreach (var location in locations.Result)
                    {
                        // 从location中提取卡牌名称
                        string cardName = System.IO.Path.GetFileNameWithoutExtension(location.PrimaryKey);

                        Addressables.LoadAssetAsync<GameObject>(location).Completed += (handle) =>
                        {
                            loadedCount++;

                            if (handle.Status == AsyncOperationStatus.Succeeded)
                            {
                                GameObject prefab = handle.Result;
                                cardPrefabs[cardName] = prefab;
                                loadedPrefabs.Add(cardName);

                                // 立即创建对象池
                                CreatePool(cardName, prefab);

                                Debug.Log($"成功加载并创建对象池: {cardName} ({loadedCount}/{totalPrefabs})");
                            }
                            else
                            {
                                Debug.LogError($"加载卡牌预制体失败: {cardName} - {handle.OperationException?.Message}");
                            }

                            // 检查是否所有预制体都已加载完成
                            if (loadedCount >= totalPrefabs)
                            {
                                Debug.Log($"所有卡牌预制体加载完成! 共加载 {loadedPrefabs.Count} 个预制体");
                                Debug.Log(GetPoolStatus());
                            }
                        };
                    }
                }
                else
                {
                    Debug.LogError($"无法加载卡牌预制体资源位置: {locations.OperationException?.Message}");
                }
            };
        }

        /// <summary>
        /// 为指定预制体创建对象池
        /// </summary>
        /// <param name="cardName">卡牌名称</param>
        /// <param name="prefab">预制体</param>
        private void CreatePool(string cardName, GameObject prefab)
        {
            if (cardPools.ContainsKey(cardName))
                return;

            Queue<GameObject> pool = new Queue<GameObject>();

            // 预创建指定数量的实例
            for (int i = 0; i < defaultPoolSize; i++)
            {
                GameObject instance = Instantiate(prefab, poolContainer);
                instance.name = $"Pool_{cardName}_{i}";
                instance.SetActive(false);
                pool.Enqueue(instance);
            }

            cardPools[cardName] = pool;
            Debug.Log($"为卡牌 {cardName} 创建对象池，预创建 {defaultPoolSize} 个实例");
        }        /// <summary>
                 /// 从对象池获取卡牌实例并设置属性
                 /// </summary>
                 /// <param name="cardName">卡牌名称</param>
                 /// <param name="cardID">卡牌ID</param>
                 /// <param name="parent">父级Transform</param>
                 /// <param name="onComplete">完成回调</param>
        public void GetCardInstance(string cardName, string cardID, Transform parent, Action<GameObject> onComplete)
        {
            // 检查是否已加载该卡牌
            if (!loadedPrefabs.Contains(cardName))
            {
                Debug.LogError($"卡牌预制体未加载: {cardName}，请确保在初始化时已加载所有预制体");
                onComplete?.Invoke(null);
                return;
            }

            GameObject prefab = cardPrefabs[cardName];
            GameObject cardInstance = GetInstanceFromPool(cardName, prefab);

            if (cardInstance != null)
            {
                // 设置父级和激活状态
                cardInstance.transform.SetParent(parent);
                cardInstance.transform.localScale = Vector3.one;
                cardInstance.transform.localPosition = Vector3.zero;
                cardInstance.SetActive(true);
                cardInstance.name = $"Card_{cardName}_{cardID.Substring(0, Math.Min(8, cardID.Length))}";

                // 设置CardItem组件属性
                SetupCardComponent(cardInstance, cardID, cardName);

                onComplete?.Invoke(cardInstance);
            }
            else
            {
                Debug.LogError($"无法从对象池获取卡牌实例: {cardName}");
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// 从对象池获取实例
        /// </summary>
        /// <param name="cardName">卡牌名称</param>
        /// <param name="prefab">预制体（用于创建新实例）</param>
        /// <returns>卡牌实例</returns>
        private GameObject GetInstanceFromPool(string cardName, GameObject prefab)
        {
            if (!cardPools.ContainsKey(cardName))
            {
                CreatePool(cardName, prefab);
            }

            Queue<GameObject> pool = cardPools[cardName];

            if (pool.Count > 0)
            {
                // 从池中获取现有实例
                GameObject instance = pool.Dequeue();
                return instance;
            }
            else
            {
                // 池为空，创建新实例
                GameObject newInstance = Instantiate(prefab);
                Debug.Log($"对象池为空，为 {cardName} 创建新实例");
                return newInstance;
            }
        }

        /// <summary>
        /// 设置CardItem组件的属性
        /// </summary>
        /// <param name="cardInstance">卡牌实例</param>
        /// <param name="cardID">卡牌ID</param>
        /// <param name="cardName">卡牌名称</param>
        private void SetupCardComponent(GameObject cardInstance, string cardID, string cardName)
        {
            try
            {
                // 确保有Button组件
                Button cardButton = cardInstance.GetComponent<Button>();
                if (cardButton == null)
                {
                    cardButton = cardInstance.AddComponent<Button>();
                }

                // 获取或添加CardItem组件
                MonoBehaviour cardComponent = cardInstance.GetComponent<MonoBehaviour>();
                Type cardItemType = Type.GetType("RepGame.UI.CardItem, Assembly-CSharp");

                if (cardComponent == null || cardComponent.GetType() != cardItemType)
                {
                    // 移除旧组件（如果存在且类型不匹配）
                    if (cardComponent != null && cardComponent.GetType() != cardItemType)
                    {
                        DestroyImmediate(cardComponent);
                    }

                    // 添加CardItem组件
                    if (cardItemType != null)
                    {
                        cardComponent = cardInstance.AddComponent(cardItemType) as MonoBehaviour;
                    }
                }

                if (cardComponent != null)
                {
                    // 设置CardID属性
                    var cardIDProperty = cardComponent.GetType().GetProperty("CardID");
                    if (cardIDProperty != null && cardIDProperty.CanWrite)
                    {
                        cardIDProperty.SetValue(cardComponent, cardID);
                    }

                    // 设置CardName属性
                    var cardNameProperty = cardComponent.GetType().GetProperty("CardName");
                    if (cardNameProperty != null && cardNameProperty.CanWrite)
                    {
                        cardNameProperty.SetValue(cardComponent, cardName);
                    }

                    // 调用Init方法
                    var initMethod = cardComponent.GetType().GetMethod("Init");
                    if (initMethod != null)
                    {
                        initMethod.Invoke(cardComponent, new object[] { cardID, cardName });
                    }

                    Debug.Log($"成功设置CardItem组件属性: {cardName} (ID: {cardID})");
                }
                else
                {
                    Debug.LogError($"无法找到或创建CardItem组件: {cardName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"设置CardItem组件时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 将卡牌实例归还到对象池
        /// </summary>
        /// <param name="cardName">卡牌名称</param>
        /// <param name="cardInstance">卡牌实例</param>
        public void ReturnToPool(string cardName, GameObject cardInstance)
        {
            if (cardInstance == null) return;

            // 重置实例状态
            cardInstance.SetActive(false);
            cardInstance.transform.SetParent(poolContainer);
            cardInstance.transform.localPosition = Vector3.zero;
            cardInstance.transform.localScale = Vector3.one;

            // 归还到对应的池
            if (!cardPools.ContainsKey(cardName))
            {
                cardPools[cardName] = new Queue<GameObject>();
            }

            cardPools[cardName].Enqueue(cardInstance);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in cardPools.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject instance = pool.Dequeue();
                    if (instance != null)
                    {
                        DestroyImmediate(instance);
                    }
                }
            }
            cardPools.Clear();
            Debug.Log("已清空所有卡牌对象池");
        }

        /// <summary>
        /// 获取对象池状态信息
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetPoolStatus()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 卡牌对象池状态 ===");
            sb.AppendLine($"已加载预制体数量: {loadedPrefabs.Count}");
            sb.AppendLine($"对象池数量: {cardPools.Count}");

            foreach (var kvp in cardPools)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value.Count} 个可用实例");
            }

            return sb.ToString();
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}
