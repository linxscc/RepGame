using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGameModels
{
    // 合成对象类型枚举
    [Serializable]
    public enum ComposeType
    {
        Self,   // 我方合成
        Enemy   // 敌方合成
    }

    [Serializable]
    public class CompResult
    {
        public bool Success;
        public string ErrorMessage;
        public List<CardModel> UsedCards;
        public List<CardModel> NewCards;
        public ComposeType ComposeType; // 新增：合成对象类型

        // 序列化方法
        public static string Serialize(CompResult result)
        {
            return JsonUtility.ToJson(result);
        }

        // 反序列化方法
        public static CompResult Deserialize(string json)
        {
            return JsonUtility.FromJson<CompResult>(json);
        }
    }
}
