using System;
using System.Collections.Generic;
using UnityEngine;
using RepGamebackModels;

namespace GameLogic
{
    public class CardInitializer
    {
        public Dictionary<string, int> LoadCardDeckFromJson()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Config/CardDeck"); // Load from Resources folder
            if (jsonFile != null)
            {
                CardDeckWrapper wrapper = JsonUtility.FromJson<CardDeckWrapper>(jsonFile.text);
                return wrapper.ToDictionary();
            }
            else
            {
                Debug.LogError("CardDeck.json not found in Resources/Config.");
                return new Dictionary<string, int>(); // Fallback to an empty deck
            }
        }

        public List<string> CreateSharedDeck(Dictionary<string, int> cardDeck)
        {
            List<string> deck = new List<string>();

            foreach (var card in cardDeck)
            {
                for (int i = 0; i < card.Value; i++)
                {
                    deck.Add(card.Key);
                }
            }

            ShuffleDeck(deck);
            return deck;
        }

        public List<string> DrawRandomCards(List<string> deck, int count)
        {
            List<string> hand = new List<string>();
            System.Random random = new System.Random();

            for (int i = 0; i < count; i++)
            {
                if (deck.Count == 0) break;

                int index = random.Next(deck.Count);
                hand.Add(deck[index]);
                deck.RemoveAt(index);
            }

            return hand;
        }

        private void ShuffleDeck(List<string> deck)
        {
            System.Random random = new System.Random();
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }
    }
}
