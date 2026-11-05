using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.UI;
using RepGameModels;
using RepGame.Core;
using RepGame;
using RepGame.GameLogic;
using RepGame.Net;
using System.Text.RegularExpressions;

public class PanelLoginController : UIBase
{
    private TMP_InputField inputUserName;
    private TMP_InputField inputPassword;
    private Button btnLogin;
    private Button btnRegister;
    private Button btnExit;
    private TextMeshProUGUI infoText;

    // 面板名称常量，便于跨脚本引用
    public const string PANEL_NAME = "Panel_Login";
    public const string REGISTER_PANEL_NAME = "Panel_Register";
    public const string START_PANEL_NAME = "Panel_Start";
    public const string MAIN_PANEL_NAME = "Panel_Main";

    void Awake()
    {
        inputUserName = FindComponent<TMP_InputField>("UserName");
        inputPassword = FindComponent<TMP_InputField>("Password");
        btnLogin = FindComponent<Button>("LoginButton");
        btnRegister = FindComponent<Button>("RegisterButton");
        btnExit = FindComponent<Button>("ExitButton");
        infoText = FindComponent<TextMeshProUGUI>("Info");
        btnLogin.onClick.AddListener(OnLoginClick);
        btnRegister.onClick.AddListener(OnGoToRegisterClick);
        btnExit.onClick.AddListener(OnExitClick);
        // 限制用户名只能输入字母、数字、下划线和汉字
        inputUserName.onValueChanged.AddListener(OnUserNameChanged);
        // 限制密码只能输入字母、数字和@#&*特殊字符
        inputPassword.onValueChanged.AddListener(OnPasswordChanged);
    }

    void OnEnable()
    {
        EventManager.Subscribe<string>(NetEvents.LoginSucceeded, OnLoginSucceeded);
        EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        EventManager.Subscribe<ResPlayerGameInfo>(NetEvents.GameResumed, OnGameResumed);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe<string>(NetEvents.LoginSucceeded, OnLoginSucceeded);
        EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        EventManager.Unsubscribe<ResPlayerGameInfo>(NetEvents.GameResumed, OnGameResumed);
    }

    private void OnLoginClick()
    {
        string username = inputUserName.text;
        string password = inputPassword.text;

        if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 20)
        {
            infoText.text = "用户名长度需为3-20位";
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 6 || password.Length > 24)
        {
            infoText.text = "密码长度需为6-24位";
            return;
        }

        infoText.text = "登录中...";
        GameTcpClient.Instance.Login(username, password);
    }
    private void OnGoToRegisterClick()
    {
        // 使用UIPanelController隐藏登录面板并显示注册面板
        UIPanelController.Instance.HidePanel(PANEL_NAME);
        UIPanelController.Instance.ShowPanel(REGISTER_PANEL_NAME);
    }

    private void OnExitClick()
    {
        Application.Quit();
    }
    private void OnLoginSucceeded(string username)
    {
        UserAccountManager.Instance.SetUsername(username);
        // 登录成功后隐藏登录面板，显示开始面板
        Invoke(nameof(GotoStartPanel), 0.1f);
    }

    /// <summary>
    /// 客户端重启后登录，而该账号仍有进行中的对局：跳过开始面板，直接回到对局
    /// </summary>
    private void OnGameResumed(ResPlayerGameInfo state)
    {
        CancelInvoke(nameof(GotoStartPanel));
        inputPassword.text = string.Empty;
        infoText.text = "";

        UIPanelController.Instance.ShowPanel(MAIN_PANEL_NAME, hideOthers: true);
        // 主面板激活后已完成订阅，再转交给它重建对局
        EventManager.TriggerEvent(NetEvents.GameResumed, state);
    }

    private void OnRequestFailed(RequestFailure failure)
    {
        if (failure.Request == RequestKind.Login || failure.Request == RequestKind.ResumeSession)
        {
            infoText.text = failure.Message;
        }
    }

    private void GotoStartPanel()
    {
        // 初始化时隐藏登录面板
        UIPanelController.Instance.HidePanel(PANEL_NAME);
        UIPanelController.Instance.ShowPanel(START_PANEL_NAME);
        inputUserName.text = string.Empty;
        inputPassword.text = string.Empty;
        infoText.text = "";
    }

    private void OnUserNameChanged(string value)
    {
        // 只允许字母、数字、下划线和汉字
        string filtered = Regex.Replace(value, @"[^a-zA-Z0-9_\u4e00-\u9fa5]", "");

        // 限制用户名最多20位
        if (filtered.Length > 20)
        {
            filtered = filtered.Substring(0, 20);
        }

        if (filtered != value)
        {
            inputUserName.text = filtered;
        }
    }

    private void OnPasswordChanged(string value)
    {
        // 只允许字母、数字和@#&*特殊字符
        string filtered = Regex.Replace(value, @"[^a-zA-Z0-9@#&*]", "");

        // 限制密码最多24位
        if (filtered.Length > 24)
        {
            filtered = filtered.Substring(0, 24);
        }

        if (filtered != value)
        {
            inputPassword.text = filtered;
        }
    }
    private void ComeBackLoginClick()
    {
        // 使用面板控制器显示登录界面
        UIPanelController.Instance.ShowPanel(PANEL_NAME);
    }
}
