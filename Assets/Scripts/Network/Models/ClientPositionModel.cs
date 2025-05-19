using System;
using UnityEngine;
using System.Collections.Generic;


namespace RepGamebackModels
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

        public static string SerializeList(List<ClientPositionModel> list)
        {
            // 使用 JsonUtility 将列表序列化为 JSON 字符串
            return JsonUtility.ToJson(new Serialization<List<ClientPositionModel>> { Items = list });
        }

        [System.Serializable]
        private class Serialization<T>
        {
            public T Items;
        }
    }
}