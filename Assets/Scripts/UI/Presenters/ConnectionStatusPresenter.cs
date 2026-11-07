using System;
using RepGame.Core;
using RepGame.Network;

namespace RepGame.UI
{
    /// <summary>
    /// 监听连接状态事件并控制服务器状态面板。
    /// 由常驻的 UIBootstrap 持有：面板本身默认隐藏（其脚本在隐藏时收不到事件），
    /// 所以订阅放在这里而不是面板脚本里。
    /// </summary>
    public sealed class ConnectionStatusPresenter : IDisposable
    {
        private const string PanelName = ServerPanelController.PANEL_NAME;

        // 最近一次未恢复的连接问题；UI 尚未就绪时先记下，就绪后再显示
        private string pendingProblem;

        public ConnectionStatusPresenter()
        {
            EventManager.Subscribe<string>(NetEvents.ConnectedToServer, OnConnected);
            EventManager.Subscribe<string>(NetEvents.ConnectionFailed, OnProblem);
            EventManager.Subscribe<string>(NetEvents.Disconnected, OnProblem);
        }

        /// <summary>UI 面板注册完成后调用，补显示启动阶段发生的连接问题。</summary>
        public void Refresh()
        {
            if (pendingProblem != null) Show(pendingProblem);
        }

        private void OnConnected(string message)
        {
            pendingProblem = null;
            var ui = UIPanelController.Instance;
            if (ui != null && ui.IsPanelRegistered(PanelName))
            {
                ui.HidePanel(PanelName);
            }
        }

        private void OnProblem(string message)
        {
            pendingProblem = message;
            Show(message);
        }

        private static void Show(string message)
        {
            var ui = UIPanelController.Instance;
            if (ui == null || !ui.IsPanelRegistered(PanelName)) return;

            ui.ShowPanel(PanelName);
            var view = ui.GetPanel(PanelName)?.GetComponent<ServerPanelController>();
            view?.SetStatus(message);
        }

        public void Dispose()
        {
            EventManager.Unsubscribe<string>(NetEvents.ConnectedToServer, OnConnected);
            EventManager.Unsubscribe<string>(NetEvents.ConnectionFailed, OnProblem);
            EventManager.Unsubscribe<string>(NetEvents.Disconnected, OnProblem);
        }
    }
}
