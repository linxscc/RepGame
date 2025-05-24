using System;
using UnityEngine;
using RepGame.Core;
using RepGame.Network;

namespace RepGame.Network.GameClientLogic
{
    /// <summary>
    /// 网络响应异常类，用于处理服务器响应错误
    /// </summary>
    public class NetworkResponseException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode { get; private set; }

        /// <summary>
        /// 请求类型
        /// </summary>
        public string RequestType { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="requestType">请求类型</param>
        public NetworkResponseException(string message, int errorCode, string requestType) 
            : base(message)
        {
            ErrorCode = errorCode;
            RequestType = requestType;
        }
    }

    /// <summary>
    /// 网络异常处理器，用于集中处理网络通信中发生的异常
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// 处理网络响应异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        public static void HandleNetworkResponseException(NetworkResponseException ex)
        {
            Debug.LogError($"网络响应错误 [请求: {ex.RequestType}, 代码: {ex.ErrorCode}]: {ex.Message}");
            
            // 根据请求类型和错误代码处理不同的异常            
            switch (ex.RequestType)
            {
                case "InitPlayerCards":
                    // 处理初始化卡牌错误
                    EventManager.TriggerEvent("InitCardsError", ex.Message);
                    break;
                case "DamageResult":
                    // 处理伤害结果错误
                    EventManager.TriggerEvent("CardDamageError", ex.Message);
                    break;
                case "TurnNotification":
                    // 处理回合通知错误
                    EventManager.TriggerEvent("TurnError", ex.Message);
                    break;
                default:
                    // 处理其他未知类型的错误
                    EventManager.TriggerEvent("NetworkError", ex.Message);
                    break;
            }
        }

        /// <summary>
        /// 处理一般网络异常（如解析错误等）
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="requestType">请求类型</param>
        public static void HandleGeneralException(Exception ex, string requestType)
        {
            Debug.LogError($"处理网络消息时出错 [请求: {requestType}]: {ex.Message}");
            
            // 触发一般错误事件
            EventManager.TriggerEvent("NetworkError", $"{requestType}处理失败: {ex.Message}");
        }
    }
}
