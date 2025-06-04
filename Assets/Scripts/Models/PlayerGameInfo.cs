using System;
using System.Collections.Generic;

namespace RepGameModels
{
    [Serializable]
    public class ResPlayerGameInfo
    {
        public string Room_Id; // 房间ID
        public string Username;  // 玩家用户名
        public float Health;    // 当前血量
        public float DamageDealt;  // 造成的伤害总量

        public float DamageReceived; // 承受的伤害总量

        public BondModel[] BondModels; // 触发的羁绊列表
        public List<Card> SelfCards;  // 玩家手牌列表
        public List<Card> OtherCards;  // 敌方玩家手牌列表

    }

}