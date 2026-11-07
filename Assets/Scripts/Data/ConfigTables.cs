using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace RepGame.Data
{
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
