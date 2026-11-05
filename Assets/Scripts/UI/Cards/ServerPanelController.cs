using TMPro;

namespace RepGame.UI
{
    /// <summary>
    /// 服务器连接状态面板（纯视图）。何时显示/隐藏由 <see cref="ConnectionStatusPresenter"/> 决定。
    /// </summary>
    public class ServerPanelController : UIBase
    {
        public const string PANEL_NAME = "Panel_Server";

        private TextMeshProUGUI serverStatusText;

        public void SetStatus(string message)
        {
            if (serverStatusText == null)
            {
                serverStatusText = FindText("ServerStatus");
            }
            if (serverStatusText != null)
            {
                serverStatusText.text = message;
            }
        }
    }
}
