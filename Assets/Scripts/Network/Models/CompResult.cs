using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGamebackModels
{
    [Serializable]
    public class CompResult
    {
        public bool Success;
        public string ErrorMessage;
        public List<CardModel> UsedCards = new List<CardModel>();
        public List<CardModel> NewCards = new List<CardModel>();
        public ComposeType ComposeType; // 合成对象类型

        // 序列化方法
        public static string Serialize(CompResult result)
        {
            return JsonUtility.ToJson(result);
        }

        // 创建成功的合成结果
        public static CompResult CreateSuccess(List<CardModel> usedCards, List<CardModel> newCards)
        {
            return new CompResult
            {
                Success = true,
                UsedCards = usedCards,
                NewCards = newCards
            };
        }

        // 创建失败的合成结果
        public static CompResult CreateError(string errorMessage)
        {
            return new CompResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
