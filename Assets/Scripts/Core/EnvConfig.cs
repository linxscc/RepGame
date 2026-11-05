using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RepGame.Core
{
    /// <summary>
    /// 读取 .env 配置文件（KEY=VALUE 格式）。
    /// 查找位置：编辑器下为 Unity 项目根目录；打包后为可执行文件所在目录。
    /// 同名系统环境变量优先于 .env 中的值。
    /// </summary>
    public static class EnvConfig
    {
        private const string EnvFileName = ".env";
        private static Dictionary<string, string> _values;

        private static Dictionary<string, string> Values
        {
            get
            {
                if (_values == null) Load();
                return _values;
            }
        }

        public static string EnvFilePath =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", EnvFileName);

        /// <summary>重新读取 .env 文件。</summary>
        public static void Load()
        {
            _values = new Dictionary<string, string>(StringComparer.Ordinal);
            string path = EnvFilePath;

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[EnvConfig] 未找到配置文件 {path}，将使用默认值。可复制 .env.example 为 .env 进行配置。");
                return;
            }

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("export ")) line = line.Substring(7).TrimStart();

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[value.Length - 1] == '"') ||
                     (value[0] == '\'' && value[value.Length - 1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                else
                {
                    // 去掉未加引号值后面的行内注释
                    int hash = value.IndexOf(" #", StringComparison.Ordinal);
                    if (hash >= 0) value = value.Substring(0, hash).TrimEnd();
                }

                _values[key] = value;
            }
        }

        public static string GetString(string key, string defaultValue = null)
        {
            string fromSystem = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(fromSystem)) return fromSystem;

            return Values.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value)
                ? value
                : defaultValue;
        }

        public static int GetInt(string key, int defaultValue)
        {
            string value = GetString(key);
            if (value != null && int.TryParse(value, out int result)) return result;
            if (value != null) Debug.LogWarning($"[EnvConfig] {key}={value} 不是有效整数，使用默认值 {defaultValue}");
            return defaultValue;
        }
    }
}
