using System;

namespace RepGameModels
{
    [Serializable]
    public class TcpResponse
    {
        public object data;
        public int code;
        public string message;
    }

    [Serializable]
    public class TcpResponse<T>
    {
        public string message;
        public int code;
        public T Data;

    }
}