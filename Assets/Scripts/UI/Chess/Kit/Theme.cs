using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Model;

namespace RepGame.UI.Chess
{
    /// <summary>配色（见 UIConcept/STYLE.md）</summary>
    public static class Theme
    {
        public static readonly Color Ink = Hex("#111111");
        public static readonly Color InkSoft = Hex("#3A4A48");
        public static readonly Color Paper = Hex("#F4F1EA");
        public static readonly Color Water = Hex("#DCEBE8");
        public static readonly Color Cyan = Hex("#BFE3DD");
        public static readonly Color CyanDeep = Hex("#2F7A72");
        public static readonly Color Red = Hex("#C8322A");
        public static readonly Color Gold = Hex("#C9A24A");
        public static readonly Color GoldText = Hex("#8A6A1E");
        public static readonly Color GoldPaper = Hex("#FFF7E0");
        public static readonly Color Dim = new Color(0.07f, 0.07f, 0.07f, 0.32f);
        public static readonly Color Clear = new Color(0, 0, 0, 0);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>稀有度颜色：普通（墨）、稀有（青）、史诗（金）</summary>
        public static Color Rarity(int r) => r >= CardRank.RarityEpic ? Gold : r == CardRank.RarityRare ? CyanDeep : Ink;

        /// <summary>费用档颜色（卡框与名牌）：1 费 黛灰、2 费 石绿、3 费 绛紫</summary>
        public static readonly Color Cost1 = Hex("#7E8A87");
        public static readonly Color Cost2 = Hex("#3E8C5E");
        public static readonly Color Cost3 = Hex("#7D3C98");
        public static Color CostColor(int cost) => cost >= CardRank.CostHigh ? Cost3 : cost == CardRank.CostMid ? Cost2 : Cost1;

        /// <summary>卡面底色：1★ 宣纸、2★ 浅青、3★ 金纸</summary>
        public static Color StarPaper(int star) => star >= CardRank.StarThree ? GoldPaper : star == CardRank.StarTwo ? Cyan : Paper;
    }
}
