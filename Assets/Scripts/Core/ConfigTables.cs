using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RepGame.Core
{
    // ============================================================
    // 客户端配置表
    // ------------------------------------------------------------
    // 配置文件位于 Assets/StreamingAssets/Config（打包后随游戏发布，可直接修改）：
    //   client_settings.json  数值配置（网络、账号限制、界面时长）
    //   ui_text.json          界面文本（键 -> 文本，支持 {0} 占位符）
    //   error_messages.json   服务器错误码 -> 玩家提示
    // 文件缺失或格式错误时使用代码中的默认值，并在控制台报错。
    // ============================================================

    /// <summary>client_settings.json 的结构；字段默认值即内置默认配置</summary>
    [Serializable]
    public class ClientSettings
    {
        public NetworkSettings network = new NetworkSettings();
        public AccountSettings account = new AccountSettings();
        public GameplaySettings gameplay = new GameplaySettings();
        public UiSettings ui = new UiSettings();

        [Serializable]
        public class NetworkSettings
        {
            public float heartbeatIntervalSeconds = 15f; // 心跳间隔
            public float connectTimeoutSeconds = 8f;     // 连接/握手超时
            public float reconnectMaxDelaySeconds = 30f; // 重连最大间隔
            public int maxFrameBytes = 1 << 20;          // 单条消息最大字节数
            public float requestTimeoutSeconds = 10f;    // 出牌/合成等待服务器结果的最长时间
        }

        [Serializable]
        public class AccountSettings
        {
            public int usernameMinLength = 3;
            public int usernameMaxLength = 20;
            public int passwordMinLength = 6;
            public int passwordMaxLength = 24;
        }

        [Serializable]
        public class GameplaySettings
        {
            public float fallbackMaxHealth = 100f; // 收到服务器数据前的血量上限
        }

        [Serializable]
        public class UiSettings
        {
            public float messageDuration = 0.5f;
            public float damageMessageDuration = 0.8f;
            public float damageMessageInterval = 0.5f;
            public float enemyComposeMessageDelay = 0.5f;
            public float gameOverMessageDelay = 0.5f;
            public float gameOverMessageDuration = 3f;
            public float gameOverPanelDelay = 0.5f;
            public float loginToStartPanelDelay = 0.1f;
            public float registerToLoginPanelDelay = 0.5f;
            public float matchFoundPanelDelay = 0.2f;
            public float rematchPanelDelay = 0.5f;
            public float defaultPanelDelay = 0.1f;
            public float bondTooltipDelay = 0.5f;
            public float bondTooltipOffset = 150f;
            public float cardShakeDuration = 0.5f;
            public int cardPoolSize = 17;
        }
    }

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

        public const string MatchWaiting = "match.waiting";
        public const string MatchFound = "match.found";
        public const string MatchInitFailed = "match.initFailed";
        public const string MatchGameOver = "match.gameOver";

        public const string NetworkError = "game.networkError";
        public const string ComposeSucceeded = "game.composeSucceeded";
        public const string EnemyComposed = "game.enemyComposed";
        public const string Win = "game.win";
        public const string Lose = "game.lose";
        public const string ForcedPlay = "game.forcedPlay";
        public const string RequestTimeout = "game.requestTimeout";

        public const string DamageDealt = "damage.dealt";
        public const string DamageTaken = "damage.taken";
        public const string DamageSummaryWithBonds = "damage.summaryWithBonds";
        public const string DamageSummary = "damage.summary";
        public const string DealtDamage = "damage.dealtDamage";
        public const string TakenDamage = "damage.takenDamage";
        public const string SelfHeal = "damage.selfHeal";
        public const string EnemyHeal = "damage.enemyHeal";
        public const string DealtAoe = "damage.dealtAoe";
        public const string TakenAoe = "damage.takenAoe";
        public const string BondTriggered = "damage.bondTriggered";

        public const string BondTooltip = "bond.tooltip";
        public const string BondActive = "bond.active";
        public const string BondInactive = "bond.inactive";
        public const string BondNone = "bond.none";
    }

    public static class ConfigTables
    {
        private const string ConfigFolder = "Config";
        private const string DefaultErrorCode = "Default";

        private static ClientSettings settings;
        private static Dictionary<string, string> uiTexts;
        private static Dictionary<string, string> errorTexts;

        /// <summary>数值配置</summary>
        public static ClientSettings Settings
        {
            get
            {
                if (settings == null) settings = LoadSettings();
                return settings;
            }
        }

        /// <summary>按键取界面文本，args 填充 {0}、{1}… 占位符；键不存在时返回键名并报错</summary>
        public static string Text(string key, params object[] args)
        {
            if (uiTexts == null) uiTexts = LoadTextTable("ui_text.json", e => e.key);

            if (!uiTexts.TryGetValue(key, out var text))
            {
                Debug.LogError($"[ConfigTables] ui_text.json 缺少键 {key}");
                return key;
            }
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        /// <summary>错误码名称（如 InvalidCredentials）对应的玩家提示</summary>
        public static string ErrorText(string errorCode)
        {
            if (errorTexts == null) errorTexts = LoadTextTable("error_messages.json", e => e.code);

            if (errorTexts.TryGetValue(errorCode, out var text)) return text;
            if (errorTexts.TryGetValue(DefaultErrorCode, out var fallback)) return fallback;
            return errorCode;
        }

        /// <summary>重新读取所有配置表（修改配置文件后可在运行时调用）</summary>
        public static void Reload()
        {
            settings = null;
            uiTexts = null;
            errorTexts = null;
        }

        private static ClientSettings LoadSettings()
        {
            string json = ReadConfigFile("client_settings.json");
            if (json == null) return new ClientSettings();
            try
            {
                return JsonUtility.FromJson<ClientSettings>(json) ?? new ClientSettings();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigTables] client_settings.json 格式错误，使用默认值: {e.Message}");
                return new ClientSettings();
            }
        }

#pragma warning disable 0649 // 以下字段由 JsonUtility 反序列化赋值
        [Serializable]
        private class TextEntry
        {
            public string key;
            public string code;
            public string text;
        }

        [Serializable]
        private class TextTable
        {
            public TextEntry[] entries;
        }
#pragma warning restore 0649

        private static Dictionary<string, string> LoadTextTable(string fileName, Func<TextEntry, string> keyOf)
        {
            var table = new Dictionary<string, string>();
            string json = ReadConfigFile(fileName);
            if (json == null) return table;

            try
            {
                var parsed = JsonUtility.FromJson<TextTable>(json);
                foreach (var entry in parsed?.entries ?? Array.Empty<TextEntry>())
                {
                    string key = keyOf(entry);
                    if (string.IsNullOrEmpty(key)) continue;
                    if (table.ContainsKey(key))
                        Debug.LogWarning($"[ConfigTables] {fileName} 中重复的键 {key}，使用后出现的值");
                    table[key] = entry.text ?? "";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigTables] {fileName} 格式错误: {e.Message}");
            }
            return table;
        }

        private static string ReadConfigFile(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, ConfigFolder, fileName);
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigTables] 读取配置表失败 {path}: {e.Message}");
                return null;
            }
        }
    }
}
