using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGameModels;
using RepGame.Core;
using UnityEngine;

namespace RepGame.Network.GameClientLogic
{
    public class NetworkMessageHandler
    {
        // 成功状态码
        private const int SUCCESS_CODE = 200;
        public void HandleNetworkMessage(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            string responseType = reader.GetString();

            switch(responseType)
            {
                case "InitPlayerCards":
                    HandleInitPlayerCards(reader);
                    break;
                case "DamageResult":
                    HandleDamageResult(reader);
                    break;
                case "TurnNotification":
                    HandleTurnNotification(reader);
                    break;
                // 可以在这里添加更多消息类型的处理
                default:
                    Debug.LogWarning($"未处理的消息类型: {responseType}");
                    break;
            }
        }        private void HandleInitPlayerCards(NetPacketReader reader)
        {
            string requestType = "InitPlayerCards";
            Debug.Log($"Received {requestType} message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            try
            {                // 尝试反序列化为ApiResponse
                var apiResponse = RepGamebackModels.ApiResponse.Deserialize(json);
                  // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    // 成功处理，解析消息
                    string cardJson = apiResponse.Message;
                  // 反序列化为卡牌模型列表
                    List<CardModel> cards = CardModel.DeserializeList(cardJson);
                    EventManager.TriggerEvent(requestType, cards);
                }
                else
                {
                    // 处理错误，抛出异常
                    throw new NetworkResponseException(
                        apiResponse.Message,
                        apiResponse.Code,
                        requestType
                    );
                }
            }
            catch (NetworkResponseException ex)
            {
                // 处理网络响应异常
                ExceptionHandler.HandleNetworkResponseException(ex);
            }
            catch (Exception innerEx)
            {
                // 处理一般异常
                ExceptionHandler.HandleGeneralException(innerEx, requestType);
            }
        }
        private void HandleDamageResult(NetPacketReader reader)
        {
            string requestType = "DamageResult";
            Debug.Log($"Received {requestType} message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            try
            {                // 尝试使用新的客户端 ApiResponse 格式
                ApiResponse<DamageResult> apiResponse = ApiResponse<DamageResult>.Deserialize(json);

                // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    // 成功处理
                    DamageResult damageResult = apiResponse.Data;

                    // 通过事件系统广播伤害结果
                    EventManager.TriggerEvent("CardDamageResult", damageResult);
                    Debug.Log($"处理成功: {apiResponse.Message}, 总伤害: {damageResult.TotalDamage}, 已处理卡牌: {damageResult.ProcessedCards.Count}, 伤害类型: {damageResult.Type}");
                }
                else
                {
                    // 处理错误，抛出异常
                    throw new NetworkResponseException(
                        apiResponse.Message,
                        apiResponse.Code,
                        requestType
                    );
                }
            }
            catch (NetworkResponseException ex)
            {
                // 处理网络响应异常
                ExceptionHandler.HandleNetworkResponseException(ex);
            }
            catch (Exception)
            {
                // 尝试旧格式
                try
                {
                    DamageResult damageResult = DamageResult.Deserialize(json);

                    // 通过事件系统广播伤害结果
                    EventManager.TriggerEvent("CardDamageResult", damageResult);
                    Debug.Log($"使用旧格式处理，总伤害: {damageResult.TotalDamage}, 已处理卡牌: {damageResult.ProcessedCards.Count}, 伤害类型: {damageResult.Type}");
                }
                catch (Exception innerEx)
                {
                    // 处理一般异常
                    ExceptionHandler.HandleGeneralException(innerEx, requestType);
                }
            }
        }
        private void HandleTurnNotification(NetPacketReader reader)
        {
            string requestType = "TurnNotification";
            Debug.Log($"Received {requestType} message from server.");
            // 接收JSON数据
            string json = reader.GetString();

            try
            {
                // 反序列化ApiResponse
                var apiResponse = RepGamebackModels.ApiResponse.Deserialize(json);

                // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    string message = apiResponse.Message;
                    Debug.Log($"回合通知: {message}");

                    // 判断是自己回合还是对方回合
                    bool isMyTurn = message.Contains("你的回合");

                    if (isMyTurn)
                    {
                        // 如果是自己回合，触发开始回合事件
                        EventManager.TriggerEvent("TurnStarted", message);
                        Debug.Log("你的回合开始，出牌按钮已启用");
                    }
                    else
                    {
                        // 如果是对方回合，触发等待回合事件
                        EventManager.TriggerEvent("TurnWaiting", message);
                        Debug.Log("对方回合，请等待");
                    }
                }
                else
                {
                    // 处理错误，抛出异常
                    throw new NetworkResponseException(
                        apiResponse.Message,
                        apiResponse.Code,
                        requestType
                    );
                }
            }
            catch (NetworkResponseException ex)
            {
                // 处理网络响应异常
                ExceptionHandler.HandleNetworkResponseException(ex);
            }
            catch (Exception innerEx)
            {
                // 处理一般异常
                ExceptionHandler.HandleGeneralException(innerEx, requestType);

            }
        }

        // 可以添加更多处理方法
    }
}
