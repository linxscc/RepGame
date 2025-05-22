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
                // 可以在这里添加更多消息类型的处理
                default:
                    Debug.LogWarning($"未处理的消息类型: {responseType}");
                    break;
            }
        }

        private void HandleInitPlayerCards(NetPacketReader reader)
        {
            Debug.Log("Received InitPlayerCards message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            // 反序列化为卡牌模型列表
            List<CardModel> cards = CardModel.DeserializeList(json);
            EventManager.TriggerEvent("InitPlayerCards", cards);
        }

        private void HandleDamageResult(NetPacketReader reader)
        {
            Debug.Log("Received DamageResult message from server.");
            // 接收 JSON 数据
            string json = reader.GetString();

            // 反序列化为 DamageResult
            RepGameModels.DamageResult damageResult = DamageResult.Deserialize(json);
            
            // 通过事件系统广播伤害结果
            EventManager.TriggerEvent("CardDamageResult", damageResult);
            
            Debug.Log($"Total damage: {damageResult.TotalDamage}, Processed cards: {damageResult.ProcessedCards.Count}");
        }

        // 可以添加更多处理方法
    }
}
