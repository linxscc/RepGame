using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.UI;
using RepGameModels;
using RepGame.Core;
using RepGame;
using System;
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
        // 订阅登录结果事件（带参数）
        EventManager.Subscribe<string>("UserLogin", OnLoginResult);
    }

    void OnDisable()
    {
        // 取消订阅登录结果事件
        EventManager.Unsubscribe<string>("UserLogin", OnLoginResult);
    }

    private void OnLoginClick()
    {
        string username = inputUserName.text;
        string password = inputPassword.text;

        // 验证用户名不为空且长度不超过20位
        if (string.IsNullOrEmpty(username) || username.Length > 20)
        {
            infoText.text = "用户名不能为空且长度不能超过20位";
            return;
        }

        // 验证密码不为空且长度不超过24位
        if (string.IsNullOrEmpty(password) || password.Length > 24)
        {
            infoText.text = "密码不能为空且长度不能超过24位";
            return;
        }
        // 发送登录请求
        LoginData loginData = new LoginData { Username = username, Password = password };
        // 使用GameTcpClient单例模式
        GameTcpClient.Instance.SendMessageToServer("UserLogin", loginData);
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
    private void OnLoginResult(string data)
    {
        // 登录结果回调，data为服务器返回内容
        if (data == null)
        {
            infoText.text = "登录失败，服务器无响应";
            return;
        }
        infoText.text = data;
        if (data == "用户登录成功")
        {
            // 登录成功后隐藏登录面板，显示开始面板
            Invoke(nameof(GotoStartPanel), 0.5f);
        }
    }

    private void GotoStartPanel()
    {
        // 初始化时隐藏登录面板
        UIPanelController.Instance.HidePanel(PANEL_NAME);
        UIPanelController.Instance.ShowPanel(START_PANEL_NAME);
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
