using UnityEngine;
using TMPro;
using RepGame.Core;

namespace RepGame.UI
{
    public class GameOverPanelController : UIBase
    {
        // 面板名称常量
        public const string PANEL_NAME = "Panel_GameOver";

        private GameObject gameOverPanel;
        private TextMeshProUGUI gameOverText;

        private void Start()
        {
            // 查找Panel和文本组件
            gameOverPanel = FindGameObject(PANEL_NAME);
            gameOverText = FindText($"{PANEL_NAME}/GameOverText");

            // 订阅游戏结束事件
            EventManager.Subscribe<object>("GameOver", OnGameOver);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<object>("GameOver", OnGameOver);
        }

        private void OnGameOver(object gameOverData)
        {
            // 设置游戏结束文本
            if (gameOverText != null)
            {
                // 根据gameOverData判断胜负
                bool isWinner = gameOverData != null && gameOverData.ToString().Contains("Winner");
                gameOverText.text = isWinner ? "YOU WIN!" : "GAME OVER";
            }

            // 显示游戏结束面板
            UIPanelController.Instance.ShowPanel(PANEL_NAME);
        }
    }
}
