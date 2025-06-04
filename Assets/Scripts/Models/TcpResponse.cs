using System;

namespace RepGameModels
{
    [Serializable]
    public class TcpResponse
    {
        public object data;
        public string code;
        public string message;
        public string responsekey;
    }

    [Serializable]
    public class TcpResponse<T>
    {
        public string message;
        public string code;
        public string responsekey;
        public T Data;

    }
}