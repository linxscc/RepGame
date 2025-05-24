using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGameModels
{
    /// <summary>
    /// 客户端版本的通用网络请求响应类，用于统一处理网络请求结果
    /// </summary>
    /// <typeparam name="T">响应数据的类型</typeparam>
    [Serializable]
    public class ApiResponse<T>
    {
        /// <summary>
        /// 状态码：0=成功，其他值表示错误
        /// </summary>
        public int Code;

        /// <summary>
        /// 响应数据
        /// </summary>
        public T Data;

        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message;

        /// <summary>
        /// 创建一个成功的响应
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = "成功")
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// 创建一个失败的响应
        /// </summary>
        public static ApiResponse<T> Fail(int code, string message)
        {
            return new ApiResponse<T>
            {
                Code = code,
                Message = message,
                Data = default
            };
        }

        /// <summary>
        /// 将响应对象序列化为JSON字符串
        /// </summary>
        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// 从JSON字符串反序列化为响应对象
        /// </summary>        
         public static ApiResponse<T> Deserialize(string json)
        {
            return JsonUtility.FromJson<ApiResponse<T>>(json);
        }

        /// <summary>
        /// 检查响应是否成功
        /// </summary>
        public bool IsSuccess()
        {
            return Code == 200;
        }
    }

    /// <summary>
    /// 不带泛型参数的API响应类（用于不需要返回数据的场景）
    /// </summary>
    [Serializable]
    public class ApiResponse
    {
        /// <summary>
        /// 状态码：0=成功，其他值表示错误
        /// </summary>
        public int Code;

        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message;

        /// <summary>
        /// 创建一个成功的响应
        /// </summary>
        public static ApiResponse Success(string message = "成功")
        {
            return new ApiResponse
            {
                Code = 200,
                Message = message
            };
        }

        /// <summary>
        /// 创建一个失败的响应
        /// </summary>
        public static ApiResponse Fail(int code, string message)
        {
            return new ApiResponse
            {
                Code = code,
                Message = message
            };
        }

        /// <summary>
        /// 将响应对象序列化为JSON字符串
        /// </summary>
        public string Serialize()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// 从JSON字符串反序列化为响应对象
        /// </summary>
        public static ApiResponse Deserialize(string json)
        {
            return JsonUtility.FromJson<ApiResponse>(json);
        }

        /// <summary>
        /// 检查响应是否成功
        /// </summary>
        public bool IsSuccess()
        {
            return Code == 200;
        }
    }
}
