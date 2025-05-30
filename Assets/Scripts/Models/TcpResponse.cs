using System;

namespace RepGameModels
{
    [Serializable]
    public class TcpResponse
    {
        public string data;
        public int code;
        public string message;
    }
}