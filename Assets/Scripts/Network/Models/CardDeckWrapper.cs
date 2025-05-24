using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGamebackModels
{
    [System.Serializable]
    public class Card
    {
        public string name;
        public int value;
        public float damage;
        public string targetname;
        public int level;
    }

        [System.Serializable]
        public class CardDeckWrapper
        {
            public List<Card> cards;

            public Dictionary<string, int> ToDictionary()
            {
                Dictionary<string, int> cardDict = new Dictionary<string, int>();
                foreach (var card in cards)
                {
                    cardDict[card.name] = card.value;
                }
                return cardDict;
            }
        }
}
