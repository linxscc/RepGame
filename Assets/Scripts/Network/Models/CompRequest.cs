using System;
using System.Collections.Generic;
using UnityEngine;

namespace RepGamebackModels
{
    [Serializable]
    public class CompRequest
    {
        public int Code;
        public string Message;
        public List<CardModel> Data;

        // 反序列化方法
        public static CompRequest Deserialize(string json)
        {
            try
            {
                // 处理通用响应包装器
                var wrapper = JsonUtility.FromJson<ApiResponseWrapper>(json);
                
                // 手动解析Data部分的卡牌列表
                var request = new CompRequest
                {
                    Code = wrapper.Code,
                    Message = wrapper.Message,
                    Data = new List<CardModel>()
                };
                
                // 如果Data是一个JSON数组，需要手动解析
                if (!string.IsNullOrEmpty(wrapper.DataJson))
                {
                    // 解析卡牌数组
                    var cardsWrapper = JsonUtility.FromJson<CardListWrapper>(wrapper.DataJson);
                    if (cardsWrapper != null && cardsWrapper.Cards != null)
                    {
                        request.Data = cardsWrapper.Cards;
                    }
                }
                
                return request;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error deserializing CompRequest: {ex.Message}");
                return null;
            }
        }

        // 辅助类，用于反序列化通用ApiResponse包装器
        [Serializable]
        private class ApiResponseWrapper
        {
            public int Code;
            public string Message;
            public string DataJson;
        }

        // 辅助类，用于反序列化卡牌列表
        [Serializable]
        private class CardListWrapper
        {
            public List<CardModel> Cards;
        }
    }


}
