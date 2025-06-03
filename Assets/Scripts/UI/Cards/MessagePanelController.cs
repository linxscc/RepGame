using UnityEngine;
using TMPro;

namespace RepGame.UI
{
    public class MessagePanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_Msg";

        private GameObject panelMsg;
        private TextMeshProUGUI msgText;

        private void Start()
        {
            // 查找Panel和文本组件
            panelMsg = FindGameObject(PANEL_NAME);
            msgText = FindText($"{PANEL_NAME}/Msg/Msg_text");
        }

        public void ShowMessage(string message)
        {
            if (msgText != null)
            {
                msgText.text = message;
                UIPanelController.Instance.ShowPanel(PANEL_NAME);
            }
        }

        public void HideMessage()
        {
            UIPanelController.Instance.HidePanel(PANEL_NAME);
        }
    }
}
