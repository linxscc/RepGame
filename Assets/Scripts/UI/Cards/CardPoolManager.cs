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
        [SerializeField] private string cardPrefabLabel = "CardPrefabs"; // Addressables标签
        [SerializeField] private string cardPrefabPath = "Assets/Prefabs/Cards/"; // 备用路径（用于调试）
        [SerializeField] private string prefabExtension = ".prefab";        // 预制体存储
        private Dictionary<string, GameObject> cardPrefabs = new Dictionary<string, GameObject>();

        // 对象池存储
        private Dictionary<string, Queue<GameObject>> cardPools = new Dictionary<string, Queue<GameObject>>();

        // 已加载的预制体记录
        private HashSet<string> loadedPrefabs = new HashSet<string>();

        [Header("对象池配置")]
        [SerializeField] private int defaultPoolSize = 17;
        [SerializeField] private Transform poolContainer;
        private void Awake()
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
        }        /// <summary>
                 /// 加载指定路径下的所有卡牌预制体并创建对象池
                 /// </summary>
        private void LoadAllCardPrefabs()
        {
            Debug.Log("开始加载所有卡牌预制体...");

            // 方法1: 使用标签加载 (推荐)
            LoadCardPrefabsByLabel();

            // 方法2: 使用路径加载 (备用)
            // LoadCardPrefabsByPath();
        }

        /// <summary>
        /// 通过Addressables标签加载卡牌预制体
        /// </summary>
        private void LoadCardPrefabsByLabel()
        {

            // 使用标签加载所有相关的GameObject资源
            Addressables.LoadResourceLocationsAsync(cardPrefabLabel, typeof(GameObject)).Completed += (locations) =>
            {
                if (locations.Status == AsyncOperationStatus.Succeeded)
                {
                    int totalPrefabs = locations.Result.Count;
                    int loadedCount = 0;

                    if (totalPrefabs == 0)
                    {
                        Debug.LogWarning($"标签 '{cardPrefabLabel}' 下没有找到任何预制体，尝试使用路径加载...");
                        LoadCardPrefabsByPath();
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


                            }
                            else
                            {
                                Debug.LogError($"加载卡牌预制体失败: {cardName} - {handle.OperationException?.Message}");
                            }

                        };
                    }
                }
                else
                {
                    Debug.LogError($"无法通过标签加载卡牌预制体资源位置: {locations.OperationException?.Message}");
                    Debug.Log("尝试使用路径加载...");
                    LoadCardPrefabsByPath();
                }
            };
        }

        /// <summary>
        /// 通过路径加载卡牌预制体 (备用方法)
        /// </summary>
        private void LoadCardPrefabsByPath()
        {
            Debug.Log($"通过路径加载卡牌预制体: {cardPrefabPath}");

            // 使用Addressables加载指定路径下的所有预制体
            Addressables.LoadResourceLocationsAsync(cardPrefabPath, typeof(GameObject)).Completed += (locations) =>
            {
                if (locations.Status == AsyncOperationStatus.Succeeded)
                {
                    int totalPrefabs = locations.Result.Count;
                    int loadedCount = 0;

                    Debug.Log($"通过路径找到 {totalPrefabs} 个卡牌预制体");

                    if (totalPrefabs == 0)
                    {
                        Debug.LogWarning($"在路径 {cardPrefabPath} 下没有找到任何预制体");
                        Debug.LogWarning("请检查以下事项:");
                        Debug.LogWarning("1. 预制体是否已添加到Addressables组中");
                        Debug.LogWarning("2. 预制体的Addressables地址是否正确");
                        Debug.LogWarning("3. 是否已构建Addressables资源");
                        return;
                    }

                    foreach (var location in locations.Result)
                    {
                        // 从location中提取卡牌名称
                        string cardName = System.IO.Path.GetFileNameWithoutExtension(location.PrimaryKey);

                        Debug.Log($"正在加载预制体: {cardName} (地址: {location.PrimaryKey})");

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
                    Debug.LogError("Addressables加载失败，可能的原因:");
                    Debug.LogError("1. 资源没有正确添加到Addressables组");
                    Debug.LogError("2. Addressables资源没有构建");
                    Debug.LogError("3. 路径或标签配置错误");
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
        }
        /// <summary>
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
                Debug.Log($"已加载的预制体列表: {string.Join(", ", loadedPrefabs)}");
                onComplete?.Invoke(null);
                return;
            }

            GameObject prefab = cardPrefabs[cardName];
            if (prefab == null)
            {
                Debug.LogError($"预制体为空: {cardName}");
                onComplete?.Invoke(null);
                return;
            }

            GameObject cardInstance = GetInstanceFromPool(cardName, prefab);

            if (cardInstance != null)
            {

                // 设置父级和激活状态
                cardInstance.transform.SetParent(parent);
                cardInstance.transform.localScale = Vector3.one;
                cardInstance.transform.localPosition = Vector3.zero;
                cardInstance.transform.localRotation = Quaternion.identity;

                // 重要：确保卡牌实例被激活
                cardInstance.SetActive(true);

                // 设置卡牌名称
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
            if (cardInstance == null)
            {
                Debug.LogError("卡牌实例为空，无法设置组件");
                return;
            }

            try
            {
                // 检查Image组件是否存在（因为您的预制体是基于Image的）
                Image cardImage = cardInstance.GetComponent<Image>();
                if (cardImage == null)
                {
                    Debug.LogWarning($"卡牌 {cardName} 缺少Image组件，这可能导致显示问题");
                }

                // 确保有Button组件
                Button cardButton = cardInstance.GetComponent<Button>();
                if (cardButton == null)
                {
                    cardButton = cardInstance.AddComponent<Button>();
                    Debug.Log($"为卡牌 {cardName} 添加了Button组件");
                }

                // 尝试获取现有的CardItem组件
                CardItem cardComponent = cardInstance.GetComponent<CardItem>();

                // 如果没有CardItem组件，添加一个
                if (cardComponent == null)
                {
                    cardComponent = cardInstance.AddComponent<CardItem>();
                    Debug.Log($"为卡牌 {cardName} 添加了CardItem组件");
                }

                if (cardComponent != null)
                {
                    // 直接设置属性，避免反射操作
                    cardComponent.CardID = cardID;
                    cardComponent.CardName = cardName;

                    // 直接调用Init方法，指定具体的参数类型
                    cardComponent.Init(cardID, cardName);

                    Debug.Log($"成功设置CardItem组件属性: {cardName} (ID: {cardID})");
                }
                else
                {
                    Debug.LogError($"无法找到或创建CardItem组件: {cardName}");
                }
            }
            catch (System.Reflection.AmbiguousMatchException ex)
            {
                Debug.LogError($"设置CardItem组件时发生歧义匹配错误: {cardName}\n错误详情: {ex.Message}");
                Debug.LogError("这通常是由于存在多个重载方法导致的，请检查CardItem类的Init方法定义");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"设置CardItem组件时出错: {cardName}\n错误类型: {ex.GetType().Name}\n错误消息: {ex.Message}");

                // 提供更详细的调试信息
                if (ex.InnerException != null)
                {
                    Debug.LogError($"内部异常: {ex.InnerException.Message}");
                }
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
