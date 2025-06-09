using System;
using System.Collections.Generic;

namespace RepGameModels
{
    [Serializable]
    public class ResPlayerGameInfo
    {
        public string Room_Id; // 房间ID
        public string Username;  // 玩家用户名
        public string Round;  // 回合
        public float Health;    // 当前血量
        public List<Card> SelfCards;  // 玩家手牌列表
        public List<OtherPlayerGameInfo> OtherPlayers; // 其他玩家信息列表
        public List<DamageInfo> DamageInfo; // 伤害信息列表

    }

    [Serializable]
    public class OtherPlayerGameInfo
    {
        public string Username;   // 玩家用户名
        public string Round;      // 回合
        public float Health;      // 当前血量
        public int CardsCount;    // 手牌数量
    }

    [Serializable]
    public class DamageInfo
    {
        public string DamageSource;   // 伤害来源
        public string DamageTarget;   // 伤害目标
        public string DamageType;     // 伤害类型
        public float DamageValue;     // 伤害值
        public List<BondModel> TriggeredBonds; // 触发的羁绊列表
    }

}