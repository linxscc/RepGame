using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGameModels
{
    // 定义伤害类型枚举
    [Serializable]
    public enum DamageType
    {
        Attacker,  // 攻击者
        Receiver   // 承受者
    }
    
    [Serializable]
    public class DamageResult
    {
        public float TotalDamage;
        public List<CardModel> ProcessedCards;
        public DamageType Type; // 添加伤害类型属性

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
