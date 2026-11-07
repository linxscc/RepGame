using System;
using RepGame.Core;
using RepGame.Network;
using RepGame.Service;

namespace RepGame.UI
{
    /// <summary>
    /// 维护当前登录账号状态，并在状态变化时通知界面。
    /// 由常驻的 UIBootstrap 持有：登录态可能在任何界面下变化（例如对局中会话过期、
    /// 账号在其他设备登录），而开始界面此时并不活跃、收不到事件。
    /// </summary>
    public sealed class AccountSessionPresenter : IDisposable
    {
        /// <summary>登录状态变化事件（无参数），界面据此刷新昵称并决定是否弹出登录窗口</summary>
        public const string AccountChanged = "UI.AccountChanged";

        public AccountSessionPresenter()
        {
            EventManager.Subscribe<string>(NetEvents.LoginSucceeded, OnLoggedIn);
            EventManager.Subscribe<string>(NetEvents.LoggedOut, OnLoggedOut);
        }

        /// <summary>当前是否已登录</summary>
        public static bool IsLoggedIn => UserAccountManager.Instance.IsUsernameValid();

        /// <summary>当前登录的玩家昵称；未登录时为空字符串</summary>
        public static string Nickname => UserAccountManager.Instance.GetUsername();

        private static void OnLoggedIn(string username)
        {
            UserAccountManager.Instance.SetUsername(username);
            EventManager.TriggerEvent(AccountChanged);
        }

        private static void OnLoggedOut(string reason)
        {
            UserAccountManager.Instance.ClearUsername();
            EventManager.TriggerEvent(AccountChanged);

            // 正停留在开始界面时立即弹出登录窗口；在其他界面时，回到开始界面会再弹出
            var ui = UIPanelController.Instance;
            if (ui != null && ui.IsPanelActive(StartPanelController.PANEL_NAME))
            {
                ui.ShowPanel(PanelLoginController.PANEL_NAME);
            }
        }

        public void Dispose()
        {
            EventManager.Unsubscribe<string>(NetEvents.LoginSucceeded, OnLoggedIn);
            EventManager.Unsubscribe<string>(NetEvents.LoggedOut, OnLoggedOut);
        }
    }
}
