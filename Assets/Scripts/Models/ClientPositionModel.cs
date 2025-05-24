using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGameModels
{
    [Serializable]
    public class ClientPositionModel
    {
        public int ClientId;
        public Vector3 Position;

        public ClientPositionModel(int clientId, Vector3 position)
        {
            ClientId = clientId;
            Position = position;
        }

        public Vector3 GetPosition()
        {
            return Position;
        }

        // 序列化单个对象为 JSON
        public static string Serialize(ClientPositionModel model)
        {
            return JsonUtility.ToJson(model);
        }

        // 反序列化单个对象
        public static ClientPositionModel Deserialize(string json)
        {
            return JsonUtility.FromJson<ClientPositionModel>(json);
        }

        // 序列化多个对象为 JSON
        public static string SerializeList(List<ClientPositionModel> models)
        {
            return JsonUtility.ToJson(new ClientPositionList { Items = models });
        }

        // 反序列化多个对象
        public static List<ClientPositionModel> DeserializeList(string json)
        {
            return JsonUtility.FromJson<ClientPositionList>(json).Items;
        }

        [Serializable]
        private class ClientPositionList
        {
            public List<ClientPositionModel> Items; // 字段名改为 Items，确保与 JSON 数据一致
        }
    }
}