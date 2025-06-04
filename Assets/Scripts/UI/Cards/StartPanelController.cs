using UnityEngine;
using UnityEngine.UI;
using RepGame.Core;
using TMPro;
using RepGameModels;

namespace RepGame.UI
{
    public class StartPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Start";
        public const string MAIN_PANEL_NAME = "Panel_Main";


        private Button startGameButton;
        private Button exitGameButton;
        private TextMeshProUGUI infoText;

        private void Start()
        {
            startGameButton = FindButton("Start", OnStartGameClicked);
            exitGameButton = FindButton("Quit", OnExitGameClicked);
            infoText = FindText("Info");
        }

        void OnEnable()
        {
            // 订阅登录结果事件（带参数）
            EventManager.Subscribe<object>("InitializationCardGame", OnStartGameResult);
        }

        void OnDisable()
        {
            // 取消订阅登录结果事件
            EventManager.Unsubscribe<object>("InitializationCardGame", OnStartGameResult);
        }

        private void OnStartGameClicked()
        {

            // 使用GameTcpClient单例模式
            GameTcpClient.Instance.SendMessageToServer("UserReady", "");

            startGameButton.interactable = false;
            infoText.text = "等待其他玩家准备...";
        }
        private void OnStartGameResult(object result)
        {
            if (result == null)
            {
                infoText.text = "对局已拒绝，请重新开始游戏！";
                startGameButton.interactable = true;
                return;
            }
            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);
            Debug.Log($"游戏初始化结果: {playerGameInfo?.Room_Id}");
            if (playerGameInfo != null)
            {
                // 更新玩家信息

                infoText.text = "游戏已开始！";
            }
            else
            {
                infoText.text = "游戏初始化失败，请稍后再试。";
            }
            startGameButton.interactable = true;

            // 隐藏开始面板，显示主面板
            UIPanelController.Instance.HidePanel(PANEL_NAME);
            UIPanelController.Instance.ShowPanel(MAIN_PANEL_NAME);
        }


        private void OnExitGameClicked()
        {
            // 退出游戏
            Debug.Log("退出游戏...");
            Application.Quit();
        }
    }
}
