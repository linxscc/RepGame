using UnityEngine;
using System.Collections.Generic;

namespace RepGame.UI
{
    /// <summary>
    /// 全局UI面板控制器，用于管理所有UI面板的显示和隐藏
    /// 不依赖于GameObject的激活状态即可访问和控制任何面板
    /// </summary>
    public class UIPanelController : MonoBehaviour
    {
        // 单例实例
        private static UIPanelController _instance;
        public static UIPanelController Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 查找场景中是否已有实例
                    _instance = FindFirstObjectByType<UIPanelController>();

                    // 如果没有找到，创建一个新的
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIPanelController");
                        _instance = go.AddComponent<UIPanelController>();
                        DontDestroyOnLoad(go); // 保证控制器在场景切换时不被销毁
                    }
                }
                return _instance;
            }
        }

        // 存储所有已注册面板的字典
        private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

        // 当前激活的面板名称
        private string currentActivePanel = "";

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
        }

        /// <summary>
        /// 注册UI面板到控制器
        /// </summary>
        /// <param name="panelName">面板的唯一标识名称</param>
        /// <param name="panelObject">面板的GameObject引用</param>
        public void RegisterPanel(string panelName, GameObject panelObject)
        {
            if (panelObject == null)
            {
                Debug.LogError($"尝试注册空面板对象: {panelName}");
                return;
            }

            // 如果面板已存在，更新引用
            if (panels.ContainsKey(panelName))
            {
                panels[panelName] = panelObject;
                Debug.Log($"更新已存在的面板: {panelName}");
            }
            else
            {
                // 添加新面板
                panels.Add(panelName, panelObject);
            }
        }

        /// <summary>
        /// 显示指定的面板，可选隐藏其他面板
        /// </summary>
        /// <param name="panelName">要显示的面板名称</param>
        /// <param name="hideOthers">是否隐藏其他面板</param>
        public void ShowPanel(string panelName, bool hideOthers = false)
        {
            if (!panels.ContainsKey(panelName))
            {
                Debug.LogWarning($"尝试显示未注册的面板: {panelName}");
                return;
            }

            if (hideOthers)
            {
                // 隐藏所有其他面板
                foreach (var panel in panels)
                {
                    if (panel.Key != panelName)
                    {
                        panel.Value.SetActive(false);
                    }
                }
            }

            // 显示目标面板
            panels[panelName].SetActive(true);
            currentActivePanel = panelName;

        }

        /// <summary>
        /// 隐藏指定的面板
        /// </summary>
        /// <param name="panelName">要隐藏的面板名称</param>
        public void HidePanel(string panelName)
        {
            if (!panels.ContainsKey(panelName))
            {
                Debug.LogWarning($"尝试隐藏未注册的面板: {panelName}");
                return;
            }

            panels[panelName].SetActive(false);

            // 如果隐藏的是当前激活的面板，清除当前面板记录
            if (currentActivePanel == panelName)
            {
                currentActivePanel = "";
            }
        }

        /// <summary>
        /// 隐藏所有面板
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var panel in panels)
            {
                panel.Value.SetActive(false);
            }
            currentActivePanel = "";
        }

        /// <summary>
        /// 获取面板的GameObject引用
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>面板的GameObject，如果不存在则返回null</returns>
        public GameObject GetPanel(string panelName)
        {
            if (panels.ContainsKey(panelName))
            {
                return panels[panelName];
            }

            Debug.LogWarning($"尝试获取未注册的面板: {panelName}");
            return null;
        }

        /// <summary>
        /// 检查面板是否已注册
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>是否已注册</returns>
        public bool IsPanelRegistered(string panelName)
        {
            return panels.ContainsKey(panelName);
        }

        /// <summary>
        /// 检查面板当前是否处于激活状态
        /// </summary>
        /// <param name="panelName">面板名称</param>
        /// <returns>是否激活</returns>
        public bool IsPanelActive(string panelName)
        {
            if (!panels.ContainsKey(panelName))
            {
                return false;
            }

            return panels[panelName].activeSelf;
        }

        /// <summary>
        /// 获取当前激活的面板名称
        /// </summary>
        /// <returns>当前激活的面板名称</returns>
        public string GetCurrentActivePanel()
        {
            return currentActivePanel;
        }
    }
}
