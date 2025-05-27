using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.Core;

namespace RepGame.UI
{
    public class GameOverPanel : UIBase
    {
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        
        private void Start()
        
        {
            // 查找新按钮
            restartButton = FindButton("Panel_GameOver/restart", OnRestartClicked);
            quitButton = FindButton("Panel_GameOver/quit", OnQuitClicked);
            // 初始时隐藏面板
            gameObject.SetActive(false);
            
        }
        
        public void Show(bool isWinner)
        {
            // 设置文本
            if (gameOverText != null)
            {
                gameOverText.text = isWinner ? "Victory!" : "Defeat!";
                gameOverText.color = isWinner ? Color.green : Color.red;
            }
            
            // 显示面板
            gameObject.SetActive(true);
        }
        
        private void OnRestartClicked()
        {
            // 触发重新开始游戏事件
            EventManager.TriggerEvent("RestartCardGame");
            
            // 隐藏面板
            gameObject.SetActive(false);
        }
        
        private void OnQuitClicked()
        {
            // 触发退出游戏事件
            EventManager.TriggerEvent("QuitCardGame");
            
            // 如果在编辑器中，停止播放
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            // 如果是构建版本，退出应用
            Application.Quit();
            #endif
        }
    }
}
