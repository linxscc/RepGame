using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGamebackModels
{
    
    [Serializable]
    public class DamageResult
    {
        public float TotalDamage;
        public List<CardModel> ProcessedCards;
        public DamageType Type;
        public List<BondModel> bonds;

        public static string Serialize(DamageResult result)
        {
            return JsonUtility.ToJson(result);
        }

        public static DamageResult Deserialize(string json)
        {
            return JsonUtility.FromJson<DamageResult>(json);
        }
    }
}
