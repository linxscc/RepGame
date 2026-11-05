using UnityEngine;
using UnityEngine.UI;
using RepGame.Core;
using TMPro;
using RepGameModels;
using System.Collections.Generic;
using RepGame.GameLogic;
using RepGame.Net;

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
            EventManager.Subscribe<ResPlayerGameInfo>(NetEvents.GameStarted, OnUserReadyResult);
            EventManager.Subscribe<List<BondModel>>(NetEvents.BondsReceived, OnBondModelInit);
            EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        void OnDisable()
        {
            EventManager.Unsubscribe<ResPlayerGameInfo>(NetEvents.GameStarted, OnUserReadyResult);
            EventManager.Unsubscribe<List<BondModel>>(NetEvents.BondsReceived, OnBondModelInit);
            EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        private void OnStartGameClicked()
        {

            GameTcpClient.Instance.Ready();

            startGameButton.interactable = false;
            infoText.text = ConfigTables.Text(UiText.MatchWaiting);
        }
        private void OnRequestFailed(RequestFailure failure)
        {
            if (failure.Request == RequestKind.Ready)
            {
                infoText.text = failure.Message;
                startGameButton.interactable = true;
            }
        }

        private void OnUserReadyResult(ResPlayerGameInfo playerGameInfo)
        {

            EventManager.TriggerEvent("InitGame", playerGameInfo);
            if (playerGameInfo != null)
            {
                // 更新玩家信息

                infoText.text += ConfigTables.Text(UiText.MatchFound);
            }
            else
            {
                infoText.text = ConfigTables.Text(UiText.MatchInitFailed);
            }

            // 登录成功后隐藏登录面板，显示开始面板
            Invoke(nameof(ClearStartPanel), ConfigTables.Settings.ui.matchFoundPanelDelay);

        }

        private void ClearStartPanel()
        {
            startGameButton.interactable = true;

            // 隐藏开始面板
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }

        private void OnBondModelInit(List<BondModel> bonds)
        {
            BondManager.Instance.SetBonds(bonds);
        }


        private void OnExitGameClicked()
        {
            // 退出游戏
            Debug.Log("退出游戏...");
            Application.Quit();
        }
    }
}
