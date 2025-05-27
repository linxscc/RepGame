using System;

namespace RepGameModels
{
    // 强类型的血量更新数据类
    [Serializable]
    public class HealthUpdate
    {
        public int AttackerHealth;
        public int ReceiverHealth;
    }
}