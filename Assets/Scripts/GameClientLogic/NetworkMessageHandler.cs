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

            switch (responseType)
            {
                case "InitPlayerData":
                    HandleInitPlayerData(reader);
                    break;
                case "DamageResult":
                    HandleDamageResult(reader);
                    break;
                case "TurnNotification":
                    HandleTurnNotification(reader);
                    break;
                case "ForcePlayRequest":
                    HandleForcePlayRequest(reader);
                    break;
                case "CompResult":
                    HandleCompResult(reader);
                    break;
                case "GameOver":
                    HandleGameOver(reader);
                    break;
                // 可以在这里添加更多消息类型的处理
                default:
                    Debug.LogWarning($"未处理的消息类型: {responseType}");
                    break;
            }
        }
        private void HandleDamageResult(NetPacketReader reader)
        {
            string requestType = "DamageResult";
            Debug.Log($"Received {requestType} message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            try
            {
                // 尝试使用新的客户端 ApiResponse 格式
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
                    // 通过事件系统广播伤害结果
                    EventManager.TriggerEvent("CardDamageError", "处理错误: " + apiResponse.Message);
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
        private void HandleTurnNotification(NetPacketReader reader)
        {
            string requestType = "TurnNotification";
            Debug.Log($"Received {requestType} message from server.");
            // 接收JSON数据
            string json = reader.GetString();

            try
            {
                // 反序列化ApiResponse
                var apiResponse = ApiResponse.Deserialize(json);

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
        private void HandleForcePlayRequest(NetPacketReader reader)
        {
            try
            {
                var message = reader.GetString();
                Debug.Log($"收到强制出牌请求: {message}");

                // 通知游戏逻辑强制出牌
                EventManager.TriggerEvent("ForcePlay", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"处理强制出牌请求时出错: {ex.Message}");
            }
        }

        private void HandleCompResult(NetPacketReader reader)
        {
            try
            {
                string jsonData = reader.GetString();
                Debug.Log($"收到合成结果: {jsonData}");

                var apiResponse = ApiResponse<CompResult>.Deserialize(jsonData);

                // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    // 解析合成结果
                    CompResult compResult = apiResponse.Data;

                    // 通知UI更新
                    EventManager.TriggerEvent("CompResult", compResult);

                }
                else
                {
                    // 处理错误，抛出异常
                    throw new NetworkResponseException(
                        apiResponse.Message,
                        apiResponse.Code,
                        "CompResult"
                    );
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"处理合成结果时出错: {ex.Message}");

                // 通知UI处理错误
                EventManager.TriggerEvent("CompError", "服务器响应解析失败");
            }
        }

        private void HandleGameOver(NetPacketReader reader)
        {
            string requestType = "GameOver";
            Debug.Log($"Received {requestType} message from server.");
            // 接收 JSON 数据
            string json = reader.GetString(); try
            {
                // 反序列化为ApiResponse
                var apiResponse = ApiResponse<GameOverResponse>.Deserialize(json);

                // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    // 成功处理，转发游戏结束事件
                    EventManager.TriggerEvent("GameOver", apiResponse.Data);
                    Debug.Log($"Game over: {apiResponse.Message}, 是否获胜: {apiResponse.Data.IsWinner}");
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
        private void HandleInitPlayerData(NetPacketReader reader)
        {
            string requestType = "InitPlayerData";
            // 接收 JSON 数据
            string json = reader.GetString();

            try
            {
                // 尝试反序列化为ApiResponse
                var apiResponse = ApiResponse<InitPlayerData>.Deserialize(json);

                // 检查请求是否成功
                if (apiResponse.Code == SUCCESS_CODE)
                {
                    // 成功处理，获取数据
                    var initData = apiResponse.Data;

                    // 存储初始血量数据
                    EventManager.TriggerEvent("InitHealth", initData.Health);

                    // 触发卡牌初始化事件
                    EventManager.TriggerEvent("InitPlayerCards", initData.Cards);

                    Debug.Log($"初始化数据接收成功: 卡牌数量: {initData.Cards.Count}, 初始血量: {initData.Health}");
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
