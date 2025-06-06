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

            // 如果已经是字符串，直接返回
            if (data is string jsonString)
            {
                // 检查字符串是否已经是有效的JSON格式
                jsonString = jsonString.Trim();
                if (IsValidJsonFormat(jsonString))
                {
                    return jsonString;
                }
                else
                {
                    // 如果不是有效的JSON，将其作为字符串值处理
                    return JsonUtility.ToJson(new { value = jsonString });
                }
            }

            try
            {
                // 对于复杂对象、列表、字典等，使用JsonUtility转换
                string result = JsonUtility.ToJson(data);

                // 如果转换结果为空或无效，尝试其他方法
                if (string.IsNullOrEmpty(result) || result == "{}")
                {
                    // 对于一些特殊类型，可能需要特殊处理
                    if (data is System.Collections.IList list)
                    {
                        // 处理列表类型
                        return ConvertListToJson(list);
                    }
                    else if (data is System.Collections.IDictionary dict)
                    {
                        // 处理字典类型
                        return ConvertDictionaryToJson(dict);
                    }
                    else
                    {
                        // 其他类型，尝试ToString()
                        return JsonUtility.ToJson(new { value = data.ToString() });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"将对象转换为JSON字符串失败: {ex.Message}");
                Debug.LogError($"数据类型: {data.GetType().Name}");
                Debug.LogError($"数据内容: {data}");

                // 最后的备用方案
                try
                {
                    return JsonUtility.ToJson(new { value = data.ToString() });
                }
                catch
                {
                    return "{}";
                }
            }
        }

        /// <summary>
        /// 将列表转换为JSON字符串
        /// </summary>
        private string ConvertListToJson(System.Collections.IList list)
        {
            try
            {
                var items = new List<object>();
                foreach (var item in list)
                {
                    items.Add(item);
                }
                return JsonUtility.ToJson(new { items = items });
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换列表为JSON失败: {ex.Message}");
                return "[]";
            }
        }

        /// <summary>
        /// 将字典转换为JSON字符串
        /// </summary>
        private string ConvertDictionaryToJson(System.Collections.IDictionary dict)
        {
            try
            {
                var pairs = new List<object>();
                foreach (var key in dict.Keys)
                {
                    pairs.Add(new { key = key, value = dict[key] });
                }
                return JsonUtility.ToJson(new { pairs = pairs });
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换字典为JSON失败: {ex.Message}");
                return "{}";
            }
        }        /// <summary>
                 /// 直接处理JSON对象并转换为指定类型
                 /// </summary>
                 /// <typeparam name="T">目标类型</typeparam>
                 /// <param name="data">JSON对象或原始数据</param>
                 /// <returns>转换后的对象，如果转换失败则返回默认值</returns>        
        public T ConvertJsonObject<T>(object data) where T : class, new()
        {
            try
            {
                if (data == null)
                {
                    Debug.LogWarning("传入的数据为null，返回默认对象");
                    return new T();
                }

                // 检查目标类型是否是泛型List
                Type targetType = typeof(T);
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // 获取List的元素类型
                    Type elementType = targetType.GetGenericArguments()[0];

                    // 获取数据的JSON字符串表示
                    string arrayJsonString = data is string str ? str : ObjectToJsonString(data);                    // 使用反射调用泛型方法
                    var method = GetType().GetMethod("ConvertJsonArrayToList").MakeGenericMethod(elementType);
                    var arrayResult = method.Invoke(this, new object[] { arrayJsonString });

                    return (T)arrayResult;
                }

                string jsonString = string.Empty;

                // 根据数据类型进行不同的处理
                if (data is string stringData)
                {
                    jsonString = stringData.Trim();

                    // 如果是空字符串，返回默认对象
                    if (string.IsNullOrEmpty(jsonString))
                    {
                        Debug.LogWarning("JSON字符串为空，返回默认对象");
                        return new T();
                    }

                    // 检查是否是有效的JSON格式
                    if (!IsValidJsonFormat(jsonString))
                    {
                        Debug.LogWarning($"无效的JSON格式: {jsonString}，尝试作为简单字符串处理");
                        // 如果不是有效的JSON，尝试将其包装为JSON对象
                        jsonString = JsonUtility.ToJson(new { value = stringData });
                    }
                }
                else
                {
                    // 对于其他类型（包括复杂对象、列表、字典等），使用智能转换
                    jsonString = ObjectToJsonString(data);

                    if (string.IsNullOrEmpty(jsonString) || jsonString == "{}")
                    {
                        Debug.LogWarning($"复杂对象转换为JSON字符串失败，数据类型: {data.GetType().Name}");

                        // 尝试直接序列化
                        try
                        {
                            jsonString = JsonUtility.ToJson(data);
                        }
                        catch (Exception serializeEx)
                        {
                            Debug.LogError($"直接序列化失败: {serializeEx.Message}");

                            // 最后的备用方案：将对象转换为字符串表示
                            jsonString = JsonUtility.ToJson(new { value = data.ToString(), type = data.GetType().Name });
                        }

                        if (string.IsNullOrEmpty(jsonString))
                        {
                            Debug.LogWarning("所有转换方式都失败，返回默认对象");
                            return new T();
                        }
                    }
                }
                Debug.Log($"目标类型: {typeof(T).Name}");

                // 尝试将JSON字符串转换为目标类型
                T result = JsonUtility.FromJson<T>(jsonString);

                if (result == null)
                {
                    Debug.LogWarning($"JSON转换结果为null，返回默认对象。JSON: {jsonString}");
                    return new T();
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换JSON对象失败: {ex.Message}");

                // 提供更详细的错误信息
                if (data is System.Collections.IList)
                {
                    Debug.LogError("检测到列表类型，请确保目标类型支持列表结构");
                }
                else if (data is System.Collections.IDictionary)
                {
                    Debug.LogError("检测到字典类型，请确保目标类型支持字典结构");
                }

                return new T();
            }
        }

        /// <summary>
        /// 检查字符串是否是有效的JSON格式
        /// </summary>
        /// <param name="jsonString">要检查的字符串</param>
        /// <returns>是否是有效的JSON格式</returns>
        private bool IsValidJsonFormat(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return false;

            jsonString = jsonString.Trim();

            // 检查是否是对象格式
            if (jsonString.StartsWith("{") && jsonString.EndsWith("}"))
                return true;

            // 检查是否是数组格式
            if (jsonString.StartsWith("[") && jsonString.EndsWith("]"))
                return true;

            // 检查是否是字符串格式
            if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                return true;

            // 检查是否是数字、布尔值或null
            if (jsonString == "null" || jsonString == "true" || jsonString == "false")
                return true;

            // 检查是否是数字
            if (double.TryParse(jsonString, out _))
                return true;

            return false;
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
                if (string.IsNullOrEmpty(jsonString))
                {
                    Debug.LogWarning("JSON字符串为空");
                    return string.Empty;
                }

                // 查找"data":的位置，支持不同的空白字符组合
                int dataIndex = jsonString.IndexOf("\"data\"");
                if (dataIndex == -1)
                {
                    Debug.LogWarning("未找到data字段");
                    return string.Empty;
                }

                // 查找冒号位置
                int colonIndex = jsonString.IndexOf(':', dataIndex);
                if (colonIndex == -1)
                {
                    Debug.LogWarning("data字段格式错误，未找到冒号");
                    return string.Empty;
                }

                // 移动到冒号后面
                int startIndex = colonIndex + 1;

                // 跳过空白字符
                while (startIndex < jsonString.Length && char.IsWhiteSpace(jsonString[startIndex]))
                {
                    startIndex++;
                }

                if (startIndex >= jsonString.Length)
                {
                    Debug.LogWarning("data字段后没有内容");
                    return string.Empty;
                }

                string result = ExtractJsonValue(jsonString, startIndex);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning("提取的data内容为空");
                    return string.Empty;
                }

                // 如果是字符串类型（被引号包围），需要去除引号并处理转义
                if (result.StartsWith("\"") && result.EndsWith("\"") && result.Length >= 2)
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
                Debug.LogError($"问题JSON: {jsonString}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从指定位置提取完整的JSON值（支持嵌套对象和数组）
        /// </summary>
        /// <param name="jsonString">JSON字符串</param>
        /// <param name="startIndex">开始位置</param>
        /// <returns>提取的JSON值</returns>
        private string ExtractJsonValue(string jsonString, int startIndex)
        {
            if (startIndex >= jsonString.Length)
                return string.Empty;

            char firstChar = jsonString[startIndex];

            // 处理字符串值
            if (firstChar == '"')
            {
                return ExtractStringValue(jsonString, startIndex);
            }
            // 处理对象值
            else if (firstChar == '{')
            {
                return ExtractObjectValue(jsonString, startIndex);
            }
            // 处理数组值
            else if (firstChar == '[')
            {
                return ExtractArrayValue(jsonString, startIndex);
            }
            // 处理基本类型值（数字、布尔值、null）
            else
            {
                return ExtractPrimitiveValue(jsonString, startIndex);
            }
        }

        /// <summary>
        /// 提取字符串值
        /// </summary>
        private string ExtractStringValue(string jsonString, int startIndex)
        {
            int endIndex = startIndex + 1;
            bool isEscaped = false;

            while (endIndex < jsonString.Length)
            {
                char c = jsonString[endIndex];

                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (c == '\\')
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    endIndex++; // 包含结束引号
                    break;
                }

                endIndex++;
            }

            return jsonString.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// 提取对象值
        /// </summary>
        private string ExtractObjectValue(string jsonString, int startIndex)
        {
            int braceCount = 0;
            int endIndex = startIndex;
            bool inString = false;
            bool isEscaped = false;

            while (endIndex < jsonString.Length)
            {
                char c = jsonString[endIndex];

                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (c == '\\' && inString)
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{')
                    {
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            endIndex++; // 包含结束大括号
                            break;
                        }
                    }
                }

                endIndex++;
            }

            return jsonString.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// 提取数组值
        /// </summary>
        private string ExtractArrayValue(string jsonString, int startIndex)
        {
            int bracketCount = 0;
            int endIndex = startIndex;
            bool inString = false;
            bool isEscaped = false;

            while (endIndex < jsonString.Length)
            {
                char c = jsonString[endIndex];

                if (isEscaped)
                {
                    isEscaped = false;
                }
                else if (c == '\\' && inString)
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '[')
                    {
                        bracketCount++;
                    }
                    else if (c == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            endIndex++; // 包含结束方括号
                            break;
                        }
                    }
                }

                endIndex++;
            }

            return jsonString.Substring(startIndex, endIndex - startIndex);
        }        /// <summary>
                 /// 提取基本类型值（数字、布尔值、null）
                 /// </summary>
        private string ExtractPrimitiveValue(string jsonString, int startIndex)
        {
            int endIndex = startIndex;

            while (endIndex < jsonString.Length)
            {
                char c = jsonString[endIndex];

                // 遇到分隔符或结束符就停止
                if (c == ',' || c == '}' || c == ']' || char.IsWhiteSpace(c))
                {
                    break;
                }

                endIndex++;
            }

            // 如果到达字符串末尾，也是有效的结束
            if (endIndex == startIndex)
            {
                return string.Empty;
            }

            return jsonString.Substring(startIndex, endIndex - startIndex);
        }

        /// <summary>
        /// 将JSON数组字符串转换为List<T>类型
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="jsonArrayString">JSON数组字符串</param>
        /// <returns>转换后的List<T>对象</returns>
        public List<T> ConvertJsonArrayToList<T>(string jsonArrayString) where T : class, new()
        {
            try
            {
                if (string.IsNullOrEmpty(jsonArrayString))
                {
                    Debug.LogWarning("JSON数组字符串为空，返回空列表");
                    return new List<T>();
                }

                jsonArrayString = jsonArrayString.Trim();

                // 检查是否是有效的JSON数组格式
                if (!jsonArrayString.StartsWith("[") || !jsonArrayString.EndsWith("]"))
                {
                    Debug.LogError($"不是有效的JSON数组格式: {jsonArrayString}");
                    return new List<T>();
                }

                // Unity的JsonUtility不支持直接反序列化数组，需要包装在对象中
                string wrappedJson = $"{{\"items\":{jsonArrayString}}}";

                Debug.Log($"包装后的JSON: {wrappedJson}");

                // 创建包装类实例
                var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);

                if (wrapper?.items != null)
                {
                    Debug.Log($"成功转换JSON数组，包含 {wrapper.items.Length} 个元素");
                    return new List<T>(wrapper.items);
                }
                else
                {
                    Debug.LogWarning("JSON数组转换失败，wrapper或items为null");
                    return new List<T>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"转换JSON数组失败: {ex.Message}");
                Debug.LogError($"JSON内容: {jsonArrayString}");
                Debug.LogError($"目标类型: List<{typeof(T).Name}>");
                return new List<T>();
            }
        }

        /// <summary>
        /// JSON数组包装类，用于Unity JsonUtility反序列化
        /// </summary>
        [System.Serializable]
        private class JsonArrayWrapper<T>
        {
            public T[] items;
        }
    }

}
