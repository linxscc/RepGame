using System.Collections;
using UnityEngine;
using RepGame.Service;
using RepGame.Data;
using RepGame.UI.Chess;

namespace RepGame.UI
{
    /// <summary>
    /// UI启动引导类，用于初始化所有UI系统
    /// 请将此脚本放置在场景最早加载的对象上，确保UI系统正确初始化
    /// </summary>
    public class UIBootstrap : MonoBehaviour
    {
        [Tooltip("要在初始化时显示的默认面板（开始界面；未登录时会在其上弹出登录窗口）")]
        [SerializeField]
        private string defaultPanelName = "Panel_Start";

        private ConnectionStatusPresenter connectionStatus;
        private AccountSessionPresenter accountSession;

        private void Awake()
        {
            // 确保 UIInitializer 被初始化
            var initializer = UIInitializer.Instance;

            // 连接状态需要在所有面板隐藏时也能收到，所以挂在常驻的引导对象上
            connectionStatus = new ConnectionStatusPresenter();

            // 登录态也可能在任意界面下变化（会话过期、被顶号），同样挂在常驻对象上
            accountSession = new AccountSessionPresenter();

            StartCoroutine(Boot());
        }

        /// <summary>先通过 Addressables 预加载界面资源（ChessUISkin 等），再构建依赖它们的界面。</summary>
        private IEnumerator Boot()
        {
            yield return AssetLoader.Preload();
            ApplyLobbyFont();

            // 新版对局界面：代码构建，收到 MatchConfig 后自动显示
            ChessBattleScreen.Create();
            MatchmakingBar.Create();

            // 延迟显示默认面板，确保所有面板都已注册
            yield return new WaitForSeconds(ConfigTables.Settings.ui.defaultPanelDelay);
            ShowDefaultPanel();
        }

        /// <summary>
        /// 大厅场景里的文字不直接引用字体（避免字体既打进包体又打进 UI bundle），
        /// 预加载完成后统一换成 bundle 里的界面字体（含未激活的面板和输入框）。
        /// </summary>
        private static void ApplyLobbyFont()
        {
            var font = ChessUISkin.Instance.font;
            if (font == null) return;
            foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                t.font = font;
            foreach (var f in Object.FindObjectsByType<TMPro.TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                f.fontAsset = font;
        }

        private void OnDestroy()
        {
            connectionStatus?.Dispose();
            accountSession?.Dispose();
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
}
