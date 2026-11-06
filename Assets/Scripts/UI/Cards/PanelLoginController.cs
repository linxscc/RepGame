using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.UI;
using RepGame.Core;
using RepGame;
using RepGame.Net;
using System.Text.RegularExpressions;

public class PanelLoginController : UIBase
{
    private TMP_InputField inputUserName;
    private TMP_InputField inputPassword;
    private Button btnLogin;
    private Button btnRegister;
    private TextMeshProUGUI infoText;

    // 面板名称常量，便于跨脚本引用
    public const string PANEL_NAME = "Panel_Login";
    public const string REGISTER_PANEL_NAME = "Panel_Register";
    public const string START_PANEL_NAME = "Panel_Start";

    void Awake()
    {
        inputUserName = FindComponent<TMP_InputField>("UserName");
        inputPassword = FindComponent<TMP_InputField>("Password");
        btnLogin = FindComponent<Button>("LoginButton");
        btnRegister = FindComponent<Button>("RegisterButton"); // 登录窗口左下角的“注册”（原“退出”位置）
        infoText = FindComponent<TextMeshProUGUI>("Info");
        btnLogin.onClick.AddListener(OnLoginClick);
        btnRegister.onClick.AddListener(OnGoToRegisterClick);
        // 限制用户名只能输入字母、数字、下划线和汉字
        inputUserName.onValueChanged.AddListener(OnUserNameChanged);
        // 限制密码只能输入字母、数字和@#&*特殊字符
        inputPassword.onValueChanged.AddListener(OnPasswordChanged);
    }

    void OnEnable()
    {
        EventManager.Subscribe<string>(NetEvents.LoginSucceeded, OnLoginSucceeded);
        EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        RepGame.Chess.ChessSession.Instance.ConfigReceived += OnMatchConfig;
    }

    void OnDisable()
    {
        EventManager.Unsubscribe<string>(NetEvents.LoginSucceeded, OnLoginSucceeded);
        EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        RepGame.Chess.ChessSession.Instance.ConfigReceived -= OnMatchConfig;
    }

    private void OnLoginClick()
    {
        string username = inputUserName.text;
        string password = inputPassword.text;

        var account = ConfigTables.Settings.account;
        if (string.IsNullOrEmpty(username) || username.Length < account.usernameMinLength || username.Length > account.usernameMaxLength)
        {
            infoText.text = ConfigTables.Text(UiText.UsernameLength, account.usernameMinLength, account.usernameMaxLength);
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < account.passwordMinLength || password.Length > account.passwordMaxLength)
        {
            infoText.text = ConfigTables.Text(UiText.PasswordLength, account.passwordMinLength, account.passwordMaxLength);
            return;
        }

        infoText.text = ConfigTables.Text(UiText.LoggingIn);
        GameTcpClient.Instance.Login(username, password);
    }
    private void OnGoToRegisterClick()
    {
        // 使用UIPanelController隐藏登录面板并显示注册面板
        UIPanelController.Instance.HidePanel(PANEL_NAME);
        UIPanelController.Instance.ShowPanel(REGISTER_PANEL_NAME);
    }

    private void OnLoginSucceeded(string username)
    {
        // 登录态由 AccountSessionPresenter 记录；这里只关闭登录弹窗，回到下面的开始界面
        Invoke(nameof(GotoStartPanel), ConfigTables.Settings.ui.loginToStartPanelDelay);
    }

    /// <summary>
    /// 登录后该账号仍有进行中的对局：服务器直接下发对局数据，由对局界面接管，不再跳到开始面板
    /// </summary>
    private void OnMatchConfig(RepGame.Protocol.MatchConfig _)
    {
        CancelInvoke(nameof(GotoStartPanel));
        inputPassword.text = string.Empty;
        infoText.text = "";
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

        // 限制用户名最大长度（client_settings.json）
        int maxUsername = ConfigTables.Settings.account.usernameMaxLength;
        if (filtered.Length > maxUsername)
        {
            filtered = filtered.Substring(0, maxUsername);
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

        // 限制密码最大长度（client_settings.json）
        int maxPassword = ConfigTables.Settings.account.passwordMaxLength;
        if (filtered.Length > maxPassword)
        {
            filtered = filtered.Substring(0, maxPassword);
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
