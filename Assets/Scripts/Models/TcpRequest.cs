using System;
using UnityEngine;
using System.Collections.Generic;

namespace RepGameModels
{
    [Serializable]
    public class TcpRequest<T>
    {
        public string message;
        public string code;
        public T Data;


        public static string Serialize(string code, string message, T data)
        {
            TcpRequest<T> request = new TcpRequest<T>
            {
                code = code,
                Data = data,
                message = message
            };
            return JsonUtility.ToJson(request);
        }

    }

    [Serializable]
    public class TcpRequest
    {
        public string message;
        public string code;
        public string Data;


        public static string Serialize(string code, string message, string data)
        {
            TcpRequest request = new TcpRequest
            {
                code = code,
                Data = data,
                message = message
            };
            return JsonUtility.ToJson(request);
        }

    }
}