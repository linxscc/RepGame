using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using RepGamebackModels;

namespace Network
{
    public class ClientPositionHandler
    {
        public void BroadcastClientPositions(NetManager netServer, Dictionary<int, Vector3> clientPositions)
        {
            // Create a list of client position models
            List<ClientPositionModel> positions = new List<ClientPositionModel>();
            foreach (var kvp in clientPositions)
            {
                positions.Add(new ClientPositionModel(kvp.Key, kvp.Value));
            }

            // Serialize the list to JSON
            string json = ClientPositionModel.SerializeList(positions);

            // Use NetDataWriter to send JSON data
            var writer = new NetDataWriter();
            writer.Put("ClientPositions");
            writer.Put(json);

            foreach (var peer in netServer.ConnectedPeerList)
            {
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendClientPositions(NetPeer peer, Dictionary<int, Vector3> clientPositions)
        {
            var writer = new NetDataWriter();
            writer.Put("ClientPositions");

            foreach (var kvp in clientPositions)
            {
                writer.Put(kvp.Key); // Client ID
                writer.Put(kvp.Value.x);
                writer.Put(kvp.Value.y);
                writer.Put(kvp.Value.z);
            }

            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        // public void BroadcastPlayerRemoval(int clientId)
        // {
        //     var writer = new NetDataWriter();
        //     writer.Put("RemovePlayer");
        //     writer.Put(clientId);
    
        //     foreach (var peer in _netServer.ConnectedPeerList)
        //     {
        //         peer.Send(writer, DeliveryMethod.ReliableOrdered);
        //     }
        // }
    }
}
