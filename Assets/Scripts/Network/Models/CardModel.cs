using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGamebackModels
{
    // Define the CardModel class
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
            return CardType.木匠学徒; // Default to a valid type if parsing fails
        }

        public static string SerializeList(List<CardModel> list)
        {
            return JsonUtility.ToJson(new Serialization<List<CardModel>> { Items = list });
        }

        public static List<CardModel> DeserializeList(string json)
        {
            return JsonUtility.FromJson<CardsList>(json).Items;
        }

        [System.Serializable]
        private class Serialization<T>
        {
            public T Items;
        }

        [Serializable]
        private class CardsList
        {
            public List<CardModel> Items;
        }
    }

}