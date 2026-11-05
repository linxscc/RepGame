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

                Invoke(nameof(ClearStartPanel), 0.5f);

                infoText.text += "匹配成功，创建房间中...";
            }
            else
            {
                infoText.text = "游戏初始化失败，请稍后再试。";
            }



        }

        private void ClearStartPanel()
        {
            // 隐藏开始面板
            infoText.text = "游戏结束！";
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }


    }
}
