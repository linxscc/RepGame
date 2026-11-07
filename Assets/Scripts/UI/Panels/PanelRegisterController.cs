using System.Text.RegularExpressions;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Core;
using RepGame.Data;
using RepGame.Model;
using RepGame.Network;

namespace RepGame.UI
{
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
            EventManager.Subscribe<string>(NetEvents.RegisterSucceeded, OnRegisterSucceeded);
            EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        void OnDisable()
        {
            EventManager.Unsubscribe<string>(NetEvents.RegisterSucceeded, OnRegisterSucceeded);
            EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        private void OnRegisterClick()
        {
            string username = inputUserName.text;
            string password = inputPassword.text;
            string repeatPassword = inputRepeatPassword.text;

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
            }        // 验证两次输入的密码是否一致
            if (password != repeatPassword)
            {
                infoText.text = ConfigTables.Text(UiText.PasswordMismatch);
                return;
            }
            infoText.text = ConfigTables.Text(UiText.Registering);
            GameTcpClient.Instance.Register(username, password);
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

        private void OnRegisterSucceeded(string username)
        {
            infoText.text = ConfigTables.Text(UiText.RegisterSucceeded);
            Invoke(nameof(OnGoToLoginClick), ConfigTables.Settings.ui.registerToLoginPanelDelay);
        }

        private void OnRequestFailed(RequestFailure failure)
        {
            if (failure.Request == RequestKind.Register)
            {
                infoText.text = failure.Message;
            }
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

            TMP_InputField inputField = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            if (inputField != null && filtered != value)
            {
                inputField.text = filtered;
            }
        }
    }
}
