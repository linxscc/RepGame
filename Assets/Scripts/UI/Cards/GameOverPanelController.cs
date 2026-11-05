using UnityEngine;
using TMPro;
using RepGame.Core;
using UnityEngine.UI;
using RepGameModels;
using RepGame.Net;

namespace RepGame.UI
{
    public class GameOverPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_GameOver";

        public const string PANEL_Main = "Panel_Main";


        private Button restartButton;
        private Button quitButton;
        private TextMeshProUGUI infoText;

        private void Start()
        {
            // 查找Panel和文本组件
            restartButton = FindButton("restart", OnRestartClicked);
            quitButton = FindButton("quit", OnQuitClicked);
            infoText = FindText("GameOverText");
        }

        private void OnQuitClicked()
        {
            Application.Quit();

        }

        void OnEnable()
        {
            EventManager.Subscribe<ResPlayerGameInfo>(NetEvents.GameStarted, OnUserReadyResult);
        }

        void OnDisable()
        {
            EventManager.Unsubscribe<ResPlayerGameInfo>(NetEvents.GameStarted, OnUserReadyResult);
        }

        private void OnRestartClicked()
        {
            GameTcpClient.Instance.Ready();
        }

        private void OnUserReadyResult(ResPlayerGameInfo playerGameInfo)
        {

            EventManager.TriggerEvent("InitGame", playerGameInfo);
            if (playerGameInfo != null)
            {
                // 更新玩家信息

                Invoke(nameof(ClearStartPanel), ConfigTables.Settings.ui.rematchPanelDelay);

                infoText.text += ConfigTables.Text(UiText.MatchFound);
            }
            else
            {
                infoText.text = ConfigTables.Text(UiText.MatchInitFailed);
            }



        }

        private void ClearStartPanel()
        {
            // 隐藏开始面板
            infoText.text = ConfigTables.Text(UiText.MatchGameOver);
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }


    }
}
