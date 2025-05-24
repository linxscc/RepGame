using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGameModels
{
    [Serializable]
    public class CardModel
    {
        public string CardID;
        public CardType Type;
        public float Damage;
        public string TargetName;
        public int Level;

        public CardType GetCardType(string cardName)
        {
            if (Enum.TryParse(cardName, out CardType cardType))
            {
                return cardType;
            }
            return CardType.木匠学徒;
        }

        // 反序列化多个对象
        public static List<CardModel> DeserializeList(string json)
        {
            return JsonUtility.FromJson<CardsList>(json).Items;
        }

        // 序列化卡牌列表
        public static string SerializeList(List<CardModel> list)
        {
            return JsonUtility.ToJson(new CardsList { Items = list });
        }

        [Serializable]
        private class CardsList
        {
            public List<CardModel> Items;
        }
    }

}