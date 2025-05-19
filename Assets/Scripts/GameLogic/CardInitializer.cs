using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RepGamebackModels;

namespace GameLogic
{
    public class CardInitializer
    {
        private string jsonFilePath;

        public CardInitializer()
        {
            // 设置 JSON 文件路径
            jsonFilePath = Path.Combine(Application.streamingAssetsPath, "Config/CardDeck.json");
        }

        public Dictionary<string, int> LoadCardDeckFromJson()
        {
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    // 读取 JSON 文件内容
                    string jsonContent = File.ReadAllText(jsonFilePath);

                    // 使用 JsonUtility 解析 JSON 数据
                    CardDeckWrapper wrapper = JsonUtility.FromJson<CardDeckWrapper>(jsonContent);
                    return wrapper.ToDictionary();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error reading or parsing JSON file: {ex.Message}");
                    return new Dictionary<string, int>(); // 返回空字典作为回退
                }
            }
            else
            {
                Debug.LogError($"JSON file not found at path: {jsonFilePath}");
                return new Dictionary<string, int>(); // 返回空字典作为回退
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
