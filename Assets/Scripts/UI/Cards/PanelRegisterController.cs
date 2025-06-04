using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RepGame.UI;
using RepGameModels;
using RepGame.Core;
using RepGame;
using System;
using System.Text.RegularExpressions;

public class PanelRegisterController : UIBase
{
    private TMP_InputField inputUserName;
    private TMP_InputField inputPassword;
    private TMP_InputField inputRepeatPassword;
    private Button btnRegister;
    private Button btnReturn;
    private TextMeshProUGUI infoText;

    // 面板名称常量，便于跨脚本引用
    public const string PANEL_NAME = "Panel_Register";
    public const string LOGIN_PANEL_NAME = "Panel_Login";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inputUserName = FindComponent<TMP_InputField>("UserName");
        inputPassword = FindComponent<TMP_InputField>("Password");
        inputRepeatPassword = FindComponent<TMP_InputField>("RepeatPassword");
        btnRegister = FindComponent<Button>("RegisterButton");
        btnReturn = FindComponent<Button>("ReturnButton");
        infoText = FindComponent<TextMeshProUGUI>("Info");
        btnRegister.onClick.AddListener(OnRegisterClick);
        btnReturn.onClick.AddListener(OnGoToLoginClick);
        // 限制用户名只能输入字母、数字、下划线和汉字
        inputUserName.onValueChanged.AddListener(OnUserNameChanged);
        // 限制密码只能输入字母、数字和@#&*特殊字符
        inputPassword.onValueChanged.AddListener(OnPasswordChanged);
        inputRepeatPassword.onValueChanged.AddListener(OnPasswordChanged);
    }

    void OnEnable()
    {
        // 订阅注册结果事件（带参数）
        EventManager.Subscribe<string>("UserRegister", OnRegisterResult);
    }

    void OnDisable()
    {
        // 取消订阅注册结果事件
        EventManager.Unsubscribe<string>("UserRegister", OnRegisterResult);
    }

    private void OnRegisterClick()
    {
        string username = inputUserName.text;
        string password = inputPassword.text;
        string repeatPassword = inputRepeatPassword.text;

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
        }        // 验证两次输入的密码是否一致
        if (password != repeatPassword)
        {
            infoText.text = "两次输入的密码不一致";
            return;
        }
        // 发送注册请求，使用LoginData类
        UserAccount loginData = new UserAccount { Username = username, Password = password };
        // 使用GameTcpClient单例模式
        GameTcpClient.Instance.SendMessageToServer("UserRegister", loginData);
    }
    private void OnGoToLoginClick()
    {
        // 使用UIPanelController隐藏注册面板并显示登录面板
        UIPanelController.Instance.HidePanel(PANEL_NAME);
        UIPanelController.Instance.ShowPanel(LOGIN_PANEL_NAME);
        inputUserName.text = string.Empty;
        inputPassword.text = string.Empty;
        inputRepeatPassword.text = string.Empty;
    }

    private void OnRegisterResult(object data)
    {
        if (data == null)
        {
            infoText.text = "注册失败，服务器无响应";
            return;
        }
        UserRegisterResponse userRegisterResponse = TcpMessageHandler.Instance.ConvertJsonObject<UserRegisterResponse>(data);
        // 注册结果回调，data为服务器返回内容
        if (userRegisterResponse == null)
        {
            infoText.text = "注册失败，服务器无响应";
            return;
        }
        infoText.text = userRegisterResponse.message;
        if (userRegisterResponse.message == "用户创建成功")
        {
            Invoke(nameof(OnGoToLoginClick), 0.5f);

        }
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

        TMP_InputField inputField = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
        if (inputField != null && filtered != value)
        {
            inputField.text = filtered;
        }
    }
}

public class UserRegisterResponse
{
    public string username;
    public string message;
}
