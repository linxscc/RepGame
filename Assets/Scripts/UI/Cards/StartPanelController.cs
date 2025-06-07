using UnityEngine;
using UnityEngine.UI;
using RepGame.Core;
using TMPro;
using RepGameModels;
using System.Collections.Generic;
using RepGame.GameLogic;

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
            UIPanelController.Instance.ShowPanel(MAIN_PANEL_NAME);
        }

        void OnEnable()
        {
            // 订阅登录结果事件（带参数）
            EventManager.Subscribe<object>("InitializationCardGame", OnUserReadyResult);
            EventManager.Subscribe<object>("InitializationCardBonds", OnBondModelInit);
        }

        void OnDisable()
        {
            // 取消订阅登录结果事件
            EventManager.Unsubscribe<object>("InitializationCardGame", OnUserReadyResult);
            EventManager.Unsubscribe<object>("InitializationCardBonds", OnBondModelInit);
        }

        private void OnStartGameClicked()
        {

            // 使用GameTcpClient单例模式
            GameTcpClient.Instance.SendMessageToServer("UserReady", "");

            startGameButton.interactable = false;
            infoText.text = "等待其他玩家准备...";
        }
        private void OnUserReadyResult(object result)
        {
            if (result == null)
            {
                infoText.text = "对局已拒绝，请重新开始游戏！";
                startGameButton.interactable = true;
                return;
            }
            Debug.Log("收到信息: " + result);
            ResPlayerGameInfo playerGameInfo = TcpMessageHandler.Instance.ConvertJsonObject<ResPlayerGameInfo>(result);
            Debug.Log("玩家游戏信息: " + playerGameInfo.Username);

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
            Invoke(nameof(ClearStartPanel), 0.2f);

        }

        private void ClearStartPanel()
        {
            startGameButton.interactable = true;

            // 隐藏开始面板
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }

        private void OnBondModelInit(object bonds)
        {
            // 处理绑定模型初始化逻辑
            if (bonds != null)
            {
                List<BondModel> gameBondModelInfo = TcpMessageHandler.Instance.ConvertJsonObject<List<BondModel>>(bonds);
                BondManager.Instance.SetBonds(gameBondModelInfo);
            }
            else
            {
                Debug.Log("没有绑定模型需要初始化。");
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
