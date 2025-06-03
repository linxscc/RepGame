using System;
using System.Collections.Generic;

namespace RepGameModels
{
    [Serializable]
    public class PlayerGameInfo
    {
        public string Username;  // 玩家用户名
        public List<Card> Cards;  // 玩家手牌列表

        public float Health;    // 当前血量
        public float DamageDealt;  // 造成的伤害总量

        public float DamageReceived; // 承受的伤害总量

        public BondModel[] bondModels; // 触发的羁绊列表
    }

    [Serializable]
    public class ResPlayerGameInfo
    {
        public string room_id; // 房间ID
        public PlayerGameInfo self_info;

        public Dictionary<string, PlayerGameInfo> otherPlayersInfo;

    }

}