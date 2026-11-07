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
            // 退出不可撤销：先二次确认
            EnsureQuitConfirm();
            quitConfirm.Show(Fmt.T("lobby.quit.confirm", "确定退出游戏吗？"), Fmt.T("lobby.quit", "退出"), () =>
            {
                Debug.Log("退出游戏...");
                Application.Quit();
            });
        }

        private ConfirmPopup quitConfirm;
        private GameObject quitCanvas;

        /// <summary>大厅没有现成的确认框：用局内的 ConfirmPopup 挂在一个独立的覆盖层画布上。</summary>
        private void EnsureQuitConfirm()
        {
            if (quitConfirm != null && quitCanvas != null) return;
            quitCanvas = new GameObject("QuitConfirmCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = quitCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = quitCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var stage = UIKit.Rect(quitCanvas.transform, "Stage");
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(1920, 1080);
            quitConfirm = new ConfirmPopup(stage);
        }

        private void OnDestroy()
        {
            if (quitCanvas != null) Destroy(quitCanvas);
        }
    }
}
