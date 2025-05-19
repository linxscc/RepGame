using System;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using RepGamebackModels;
using GameLogic;

namespace GameLogic
{
    public class CardManager
    {
        private Dictionary<string, int> cardDeck;

        private Dictionary<int, List<string>> playerHands = new Dictionary<int, List<string>>();

        private CardInitializer cardInitializer = new CardInitializer();

        public CardManager()
        {
            // Load the card deck
            cardDeck = cardInitializer.LoadCardDeckFromJson();
        }

        public void InitializeCardsForPlayers(List<NetPeer> roomPeers)
        {
            // Create a shared deck for the room
            List<string> sharedDeck = cardInitializer.CreateSharedDeck(cardDeck);

            foreach (var peer in roomPeers)
            {
                // Draw 6 random cards for each player
                List<string> playerHand = cardInitializer.DrawRandomCards(sharedDeck, 6);
                playerHands[peer.Id] = playerHand;

                // Send the cards to the player
                SendCardsToPlayer(peer, playerHand);
            }
        }

        private void SendCardsToPlayer(NetPeer peer, List<string> cards)
        {
            // Convert card names to CardModel objects
            List<CardModel> cardModels = new List<CardModel>();
            foreach (var cardName in cards)
            {
                cardModels.Add(new CardModel
                {
                    CardID = Guid.NewGuid().ToString(), // Generate a unique ID for each card
                    Type = GetCardType(cardName)
                });
            }

            // Serialize the card models to JSON
            string json = JsonUtility.ToJson(new { Cards = cardModels });

            // Send the JSON to the player
            var writer = new NetDataWriter();
            writer.Put("InitPlayerCards");
            writer.Put(json);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        // Move the method inside the CardModel class
        public CardType GetCardType(string cardName)
        {
            if (Enum.TryParse(cardName, out CardType cardType))
            {
                return cardType;
            }
            return CardType.木匠学徒; // Default to a valid type if parsing fails
        }

    }
}
