using UnityEngine;
using UnityEngine.UI;

public class MainStartUI : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel; // 要隐藏的 UI 面板
    [SerializeField] private Button hideButton; // 触发隐藏的按钮

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 确保按钮和面板已绑定
        if (hideButton != null)
        {
            hideButton.onClick.AddListener(HideUIPanelAndEnableScripts);
        }
    }

    private void HideUIPanelAndEnableScripts()
    {
        // 隐藏 UI 面板
        if (uiPanel != null)
        {
           
            StatusManage.Instance.SetGameState(StatusManage.GameStateType.Playing);

            uiPanel.SetActive(false);
        }
    }
}
