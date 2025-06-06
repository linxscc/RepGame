using System;


namespace RepGameModels
{
    [Serializable]
    public class DataEvent
    {
        /// <summary>
        /// 事件数据
        /// </summary>
        public string Data;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="data">事件数据</param>
        public DataEvent(string data)
        {
            Data = data;
        }
    }
}