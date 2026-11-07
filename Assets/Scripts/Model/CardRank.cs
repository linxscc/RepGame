namespace RepGame.Model
{
    /// <summary>
    /// 卡牌的星级、费用档与稀有度。服务器下发的都是整数，界面按这些常量区分显示，
    /// 不直接和数字比较。数值必须与服务器 chess_rules.yaml / chess_cards.yaml 一致。
    /// </summary>
    public static class CardRank
    {
        // ---- 星级（merge.max_star = 3）----

        /// <summary>1 星：商店买到的初始卡</summary>
        public const int StarOne = 1;

        /// <summary>2 星：3 张同系 1 星合成</summary>
        public const int StarTwo = 2;

        /// <summary>3 星：3 张同系 2 星合成，最高星级</summary>
        public const int StarThree = 3;

        // ---- 费用档 ----

        /// <summary>1 费：开局卡所在档位（start_card_cost）</summary>
        public const int CostLow = 1;

        /// <summary>2 费</summary>
        public const int CostMid = 2;

        /// <summary>3 费及以上：最高费用档</summary>
        public const int CostHigh = 3;

        // ---- 法宝稀有度（chess_rules.yaml 的 rarities 下标：common / rare / epic）----

        /// <summary>普通</summary>
        public const int RarityCommon = 0;

        /// <summary>稀有</summary>
        public const int RarityRare = 1;

        /// <summary>史诗及以上</summary>
        public const int RarityEpic = 2;
    }
}
