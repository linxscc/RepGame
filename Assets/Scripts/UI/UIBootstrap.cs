using UnityEngine;
using RepGame.UI;

/// <summary>
/// UI启动引导类，用于初始化所有UI系统
/// 请将此脚本放置在场景最早加载的对象上，确保UI系统正确初始化
/// </summary>
public class UIBootstrap : MonoBehaviour
{
    [Tooltip("要在初始化时显示的默认面板")]
    [SerializeField]
    private string defaultPanelName = "Panel_Login";

    private void Awake()
    {
        // 确保 UIInitializer 被初始化
        var initializer = UIInitializer.Instance;
        Debug.Log("UI系统初始化开始");
    }

    void Start()
    {
        // 延迟一帧显示默认面板，确保所有面板都已注册
        Invoke("ShowDefaultPanel", 0.1f);
    }

    private void ShowDefaultPanel()
    {
        if (!string.IsNullOrEmpty(defaultPanelName) && UIPanelController.Instance.IsPanelRegistered(defaultPanelName))
        {
            Debug.Log($"UIBootstrap: 显示默认面板 {defaultPanelName}");

            // 隐藏所有其他面板
            UIPanelController.Instance.HideAllPanels();

            // 显示默认面板
            UIPanelController.Instance.ShowPanel(defaultPanelName);
        }
        else
        {
            Debug.LogWarning($"UIBootstrap: 无法显示默认面板 {defaultPanelName}，面板未注册");
        }
    }
}
