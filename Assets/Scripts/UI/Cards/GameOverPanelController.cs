using UnityEngine;
using TMPro;
using RepGame.Core;
using UnityEngine.UI;
using RepGameModels;

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
            // 订阅登录结果事件（带参数）
            EventManager.Subscribe<string>("InitializationCardGame", OnUserReadyResult);
        }

        void OnDisable()
        {
            // 取消订阅登录结果事件
            EventManager.Unsubscribe<string>("InitializationCardGame", OnUserReadyResult);
        }

        private void OnRestartClicked()
        {
            // 使用GameTcpClient单例模式
            GameTcpClient.Instance.SendMessageToServer("UserReady", "");
        }

        private void OnUserReadyResult(string result)
        {
            if (result == null)
            {
                infoText.text = "对局已拒绝，请重新开始游戏！";
                return;
            }
            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);

            EventManager.TriggerEvent("InitGame", playerGameInfo);
            if (playerGameInfo != null)
            {
                // 更新玩家信息

                infoText.text += "匹配成功，创建房间中...";
            }
            else
            {
                infoText.text = "游戏初始化失败，请稍后再试。";
            }

            // 登录成功后隐藏登录面板，显示开始面板
            Invoke(nameof(ClearStartPanel), 0.5f);

        }

        private void ClearStartPanel()
        {
            // 隐藏开始面板
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }


    }
}
