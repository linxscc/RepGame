using UnityEngine;
using TMPro;
using RepGame.Core;

namespace RepGame.UI
{
    public class ServerPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Server";

        private TextMeshProUGUI serverStatusText;
        private GameObject panelServer;

        private void Start()
        {
            // 查找Panel和组件
            panelServer = FindGameObject(PANEL_NAME);
            serverStatusText = FindText($"{PANEL_NAME}/ServerStatus");

            // 订阅事件
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            EventManager.Subscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Subscribe<string>("ConnectionTimeout", OnConnectionTimeout);
            EventManager.Subscribe<string>("ConnectionFailed", OnConnectionFailed);
            EventManager.Subscribe<string>("ServerClosed", OnServerClosed);
            EventManager.Subscribe<string>("Disconnected", OnDisconnected);
        }

        private void OnDisable()
        {
            // 取消订阅事件
            EventManager.Unsubscribe<string>("ConnectedToServer", OnServerConnected);
            EventManager.Unsubscribe<string>("ConnectionTimeout", OnConnectionTimeout);
            EventManager.Unsubscribe<string>("ConnectionFailed", OnConnectionFailed);
            EventManager.Unsubscribe<string>("ServerClosed", OnServerClosed);
            EventManager.Unsubscribe<string>("Disconnected", OnDisconnected);
        }

        private void OnServerConnected(string serverStatus)
        {
            // 更新服务器状态
            serverStatusText.text = serverStatus;
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }

        private void OnConnectionTimeout(string message)
        {
            ShowConnectionError(message);
        }

        private void OnConnectionFailed(string message)
        {
            ShowConnectionError(message);
        }

        private void OnServerClosed(string message)
        {
            ShowConnectionError(message);
        }

        private void OnDisconnected(string message)
        {
            ShowConnectionError(message);
        }

        private void ShowConnectionError(string message)
        {
            if (serverStatusText != null)
            {
                serverStatusText.text = message;
            }
            UIPanelController.Instance.ShowPanel(PANEL_NAME);
        }
    }
}
