using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGameModels
{
    [Serializable]
    public class DamageResult
    {
        public float TotalDamage;
        public List<CardModel> newCards;
        public List<CardModel> ProcessedCards;
        public DamageType Type;
        public List<BondModel> bonds;

        // 序列化方法
        public static string Serialize(DamageResult result)
        {
            return JsonUtility.ToJson(result);
        }

        // 反序列化方法
        public static DamageResult Deserialize(string json)
        {
            return JsonUtility.FromJson<DamageResult>(json);
        }
    }
}
