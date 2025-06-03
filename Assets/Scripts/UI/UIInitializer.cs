using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RepGame.UI
{
    /// <summary>
    /// UI 初始化管理器，确保 UIPanelController 在所有面板之前正确初始化
    /// 并提供安全的面板注册机制
    /// </summary>
    public class UIInitializer : MonoBehaviour
    {
        // 单例实例
        private static UIInitializer _instance;
        public static UIInitializer Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试查找现有实例
                    _instance = FindFirstObjectByType<UIInitializer>();

                    // 如果没有找到，创建一个新的
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIInitializer");
                        _instance = go.AddComponent<UIInitializer>();
                    }
                }
                return _instance;
            }
        }

        // 是否已完成初始化
        private bool _initialized = false;
        public bool Initialized => _initialized;

        // 待注册的面板队列（初始化完成前的注册请求会被加入队列）
        private Queue<PanelRegistration> _panelRegistrationQueue = new Queue<PanelRegistration>();

        // 面板注册信息
        private struct PanelRegistration
        {
            public string panelName;
            public GameObject panelObject;

            public PanelRegistration(string name, GameObject obj)
            {
                panelName = name;
                panelObject = obj;
            }
        }

        /// <summary>
        /// 确保场景中存在UIBootstrap组件
        /// </summary>
        public static void EnsureUIBootstrapExists()
        {
            UIBootstrap bootstrap = FindFirstObjectByType<UIBootstrap>();
            if (bootstrap == null)
            {
                GameObject bootstrapObject = new GameObject("UIBootstrap");
                bootstrap = bootstrapObject.AddComponent<UIBootstrap>();
                Debug.Log("UIBootstrap自动创建成功");
            }
        }

        private void Awake()
        {
            // 确保场景中只有一个实例
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 确保 UIPanelController 已初始化
            InitializeUIPanelController();

            // 确保 UIBootstrap 存在
            EnsureUIBootstrapExists();
        }

        private void Start()
        {
            // 自动查找并注册所有UI面板
            AutoRegisterAllPanels();

            // 等待一帧后处理所有排队的面板注册请求
            StartCoroutine(ProcessPanelRegistrationQueue());
        }
        /// <summary>
        /// 自动查找并注册所有UI面板
        /// </summary>
        private void AutoRegisterAllPanels()
        {
            // 查找场景中的Canvas对象
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                // 查找Canvas下的所有面板
                Transform panelsParent = canvas.transform;
                foreach (Transform child in panelsParent)
                {
                    if (child.name.StartsWith("Panel_"))
                    {
                        string panelName = child.name;
                        GameObject panelObject = child.gameObject;

                        // 注册面板（无论是否激活）
                        RegisterPanel(panelName, panelObject);
                    }
                }
            }
        }

        /// <summary>
        /// 初始化 UIPanelController
        /// </summary>
        private void InitializeUIPanelController()
        {
            // 强制创建 UIPanelController 实例
            var controller = UIPanelController.Instance;

            if (controller != null)
            {
                Debug.Log("UIPanelController 初始化成功");
                _initialized = true;
            }
            else
            {
                Debug.LogError("UIPanelController 初始化失败");
            }
        }

        /// <summary>
        /// 注册面板到 UIPanelController
        /// 如果 UIPanelController 尚未初始化，将请求加入队列
        /// </summary>
        public void RegisterPanel(string panelName, GameObject panelObject)
        {
            if (panelObject == null)
            {
                Debug.LogError($"尝试注册空面板对象: {panelName}");
                return;
            }

            if (_initialized)
            {
                // 如果已初始化，直接注册
                UIPanelController.Instance.RegisterPanel(panelName, panelObject);
            }
            else
            {
                // 如果未初始化，加入队列
                _panelRegistrationQueue.Enqueue(new PanelRegistration(panelName, panelObject));
            }
        }

        /// <summary>
        /// 处理面板注册队列
        /// </summary>
        private IEnumerator ProcessPanelRegistrationQueue()
        {
            // 等待一帧，确保所有面板的 Awake 和 Start 方法都已执行
            yield return null;

            // 确保 UIPanelController 已初始化
            if (!_initialized)
            {
                InitializeUIPanelController();
            }

            // 处理队列中的所有注册请求
            while (_panelRegistrationQueue.Count > 0)
            {
                var registration = _panelRegistrationQueue.Dequeue();

                if (registration.panelObject != null)
                {
                    UIPanelController.Instance.RegisterPanel(registration.panelName, registration.panelObject);
                }
                else
                {
                    Debug.LogWarning($"UIInitializer: 面板对象为空，无法注册 {registration.panelName}");
                }
            }
        }
    }
}
