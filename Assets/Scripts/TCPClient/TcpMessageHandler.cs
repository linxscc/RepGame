using System;
using System.Collections.Generic;
using UnityEngine;
using RepGameModels;
using RepGame.Core;

namespace RepGame
{

    /// <summary>
    /// TCP消息处理器，负责将JSON数据转换为对应的对象并通过事件系统发送
    /// </summary>
    public class TcpMessageHandler
    {
        // 单例实例
        private static TcpMessageHandler _instance;
        public static TcpMessageHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TcpMessageHandler();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 辅助方法：将接收的对象数据转换为JSON字符串
        /// </summary>
        public string ObjectToJsonString(object data)
        {
            if (data == null)
                return "";

            if (data is string jsonString)
                return jsonString;

            try
            {
                return JsonUtility.ToJson(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"将对象转换为JSON字符串失败: {ex.Message}");
                return "{}";
            }
        }
        /// <summary>
        /// 直接处理JSON对象并转换为指定类型
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">JSON对象</param>
        /// <returns>转换后的对象，如果转换失败则返回默认值</returns>
        public T ConvertJsonObject<T>(object data) where T : class, new()
        {
            try
            {
                string jsonString = ObjectToJsonString(data);
                return JsonUtility.FromJson<T>(jsonString) ?? new T();
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换JSON对象失败: {ex.Message}");
                return new T();
            }
        }

        /// <summary>
        /// 通用方法：将JSON数据转换为指定类型并触发事件
        /// </summary>
        public void ConvertAndTrigger<T>(string eventName, object data) where T : class
        {
            try
            {
                // 将对象数据转换为JSON字符串
                string jsonString = ObjectToJsonString(data);

                // 从JSON解析为指定类型
                T typedData = JsonUtility.FromJson<T>(jsonString);

                // 通过事件系统发送处理后的对象
                EventManager.TriggerEvent(eventName, typedData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"解析数据失败 ({eventName}): {ex.Message}");
            }
        }        /// <summary>
                 /// 从JSON字符串中提取data字段的内容
                 /// </summary>
                 /// <param name="jsonString">完整的JSON字符串</param>
                 /// <returns>提取出的data内容</returns>
        public string ExtractDataContent(string jsonString)
        {
            try
            {
                // 使用正则表达式匹配data字段的内容
                // (?<="data":) 表示匹配"data":后面的内容
                // (?:"|{|\[) 表示匹配一个引号、大括号或方括号（非捕获组）
                // (.*?) 表示匹配任意字符（非贪婪模式）
                // (?:"|}|\]) 表示匹配一个引号、大括号或方括号（非捕获组）
                // (?=,|}) 表示后面是逗号或大括号（正向预查）
                var match = System.Text.RegularExpressions.Regex.Match(
                    jsonString,
                    @"(?<=""data"":)\s*(""[^""\\]*(?:\\.[^""\\]*)*""|{[^{}]*}|\[[^\[\]]*\]|[\d.]+|true|false|null)(?=,|})"
                );

                if (!match.Success)
                {
                    Debug.LogWarning("未能成功匹配data字段内容");
                    return string.Empty;
                }

                string result = match.Value.Trim();

                // 如果是字符串类型（被引号包围），需要去除引号并处理转义
                if (result.StartsWith("\"") && result.EndsWith("\""))
                {
                    // 去除首尾引号并处理转义字符
                    result = result.Substring(1, result.Length - 2);
                    result = System.Text.RegularExpressions.Regex.Unescape(result);
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"提取data内容时出错: {ex.Message}");
                return string.Empty;
            }
        }
    }

}
