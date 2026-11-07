using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace RepGame.Data
{
    /// <summary>ui_text.json 中的键（代码只通过这些常量引用文本）</summary>
    public static class UiText
    {
        public const string ConnectionFailed = "connection.failed";
        public const string ConnectionConnected = "connection.connected";
        public const string ConnectionLost = "connection.lost";
        public const string ConnectionNotConnected = "connection.notConnected";

        public const string UsernameLength = "account.usernameLength";
        public const string PasswordLength = "account.passwordLength";
        public const string PasswordMismatch = "account.passwordMismatch";
        public const string LoggingIn = "account.loggingIn";
        public const string Registering = "account.registering";
        public const string RegisterSucceeded = "account.registerSucceeded";
        public const string Nickname = "account.nickname";
    }
}
