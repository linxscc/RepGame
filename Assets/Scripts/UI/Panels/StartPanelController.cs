using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Core;
using RepGame.Data;
using RepGame.Model;
using RepGame.Network;
using RepGame.UI.Chess;

namespace RepGame.UI
{
    public class StartPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Start";
        public const string REGISTER_PANEL_NAME = "Panel_Register";


        private Button startGameButton;
        private Button exitGameButton;
        private TextMeshProUGUI infoText;
        private TextMeshProUGUI playerNameText; // 右上角的玩家昵称

        private void Start()
        {
            startGameButton = FindButton("Start", OnStartGameClicked);
            exitGameButton = FindButton("Quit", OnExitGameClicked);
            infoText = FindText("Info");
            RefreshAccount();
        }

        void OnEnable()
        {
            EventManager.Subscribe(AccountSessionPresenter.AccountChanged, RefreshAccount);
            RefreshAccount();
            EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        void OnDisable()
        {
            EventManager.Unsubscribe(AccountSessionPresenter.AccountChanged, RefreshAccount);
            EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        /// <summary>
        /// 刷新右上角昵称；未登录时弹出登录窗口（开始界面是游戏入口，登录/注册以弹窗形式盖在它上面）
        /// </summary>
        private void RefreshAccount()
        {
            if (playerNameText == null)
            {
                playerNameText = FindText("PlayerName");
            }
            if (playerNameText != null)
            {
                playerNameText.text = AccountSessionPresenter.IsLoggedIn
                    ? ConfigTables.Text(UiText.Nickname, AccountSessionPresenter.Nickname)
                    : string.Empty;
            }

            if (!AccountSessionPresenter.IsLoggedIn)
            {
                ShowLoginPopup();
            }
        }

        private static void ShowLoginPopup()
        {
            var ui = UIPanelController.Instance;
            // 注册窗口打开时不打断；否则在开始界面之上弹出登录窗口
            if (ui.IsPanelActive(REGISTER_PANEL_NAME)) return;
            ui.ShowPanel(PanelLoginController.PANEL_NAME);
        }

        private void OnStartGameClicked()
        {
            // 未登录时先登录，登录后再开始匹配
            if (!AccountSessionPresenter.IsLoggedIn)
            {
                ShowLoginPopup();
                return;
            }

            // 匹配状态由屏幕上方的计时框显示（可点 × 取消），开始按钮保持可用
            infoText.text = string.Empty;
            MatchmakingBar.Instance?.Begin();
        }
        private void OnRequestFailed(RequestFailure failure)
        {
            if (failure.Request == RequestKind.Ready)
            {
                infoText.text = failure.Message;
                startGameButton.interactable = true;
            }
        }

        private void OnExitGameClicked()
        {
            // 退出游戏
            Debug.Log("退出游戏...");
            Application.Quit();
        }
    }
}
