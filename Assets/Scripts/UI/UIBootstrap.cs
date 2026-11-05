using RepGame.Core;
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

    private ConnectionStatusPresenter connectionStatus;

    private void Awake()
    {
        // 确保 UIInitializer 被初始化
        var initializer = UIInitializer.Instance;

        // 连接状态需要在所有面板隐藏时也能收到，所以挂在常驻的引导对象上
        connectionStatus = new ConnectionStatusPresenter();
    }

    void Start()
    {
        // 延迟一帧显示默认面板，确保所有面板都已注册
        Invoke("ShowDefaultPanel", ConfigTables.Settings.ui.defaultPanelDelay);
    }

    private void OnDestroy()
    {
        connectionStatus?.Dispose();
    }

    private void ShowDefaultPanel()
    {
        if (!string.IsNullOrEmpty(defaultPanelName) && UIPanelController.Instance.IsPanelRegistered(defaultPanelName))
        {

            // 隐藏所有其他面板
            UIPanelController.Instance.HideAllPanels();

            // 显示默认面板
            UIPanelController.Instance.ShowPanel(defaultPanelName);

            // 启动阶段若已发生连接问题，面板注册完成后再显示
            connectionStatus.Refresh();
        }
        else
        {
            Debug.LogWarning($"UIBootstrap: 无法显示默认面板 {defaultPanelName}，面板未注册");
        }
    }
}
