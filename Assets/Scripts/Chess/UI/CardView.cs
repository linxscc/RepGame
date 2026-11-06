using RepGame.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 一张卡的显示：卡图（没有卡图时用卡名占位）、星级、点数、卡名。
    /// 数值全部来自服务器下发的 ChessCard。
    /// </summary>
    public static class CardView
    {
        /// <param name="cost">商店价格；大于 0 时在左上角显示金币价格。</param>
        public static RectTransform Build(Transform parent, ChessCard card, float w, float h, bool showPoints = true, int cost = 0)
        {
            var skin = ChessUISkin.Instance;
            var border = card.Star >= 3 ? Theme.Gold : Theme.Ink;
            var frame = UIKit.Panel(parent, "Card_" + card.Name, Theme.StarPaper(card.Star), border, card.Star >= 3 ? 3f : 2f);
            frame.raycastTarget = true;
            var rt = frame.rectTransform;
            rt.sizeDelta = new Vector2(w, h);

            var art = skin.Card(card.Name);
            var artImg = UIKit.Image(frame.transform, "Art", art != null ? Color.white : Theme.Water, art != null ? art : skin.Rounded);
            artImg.preserveAspect = false;
            var art_rt = artImg.rectTransform;
            art_rt.anchorMin = new Vector2(0, 0);
            art_rt.anchorMax = new Vector2(1, 1);
            art_rt.offsetMin = new Vector2(5, h * 0.2f);
            art_rt.offsetMax = new Vector2(-5, -5);
            if (art == null)
            {
                UIKit.Text(artImg.transform, "Placeholder", card.Name, Mathf.Max(13, w * 0.14f), Theme.InkSoft);
            }

            // 左上角：商店价格（金币）
            if (cost > 0)
            {
                var coin = UIKit.Circle(frame.transform, "Cost", Theme.GoldPaper, Theme.Gold, 2f);
                var ct = coin.rectTransform;
                ct.anchorMin = ct.anchorMax = new Vector2(0, 1);
                ct.pivot = new Vector2(0, 1);
                ct.sizeDelta = new Vector2(w * 0.24f, w * 0.24f);
                ct.anchoredPosition = new Vector2(5, -5);
                UIKit.Text(coin.transform, "Gold", cost.ToString(), w * 0.13f,
                    cost >= 3 ? Theme.Red : cost == 2 ? Theme.CyanDeep : Theme.GoldText, TextAlignmentOptions.Center, true);
            }

            // 右上角：星级，下面竖排羁绊标签
            float starH = Mathf.Max(14, w * 0.13f);
            var stars = UIKit.Text(frame.transform, "Stars", new string('★', card.Star), Mathf.Max(11, w * 0.11f), Theme.Red,
                TextAlignmentOptions.TopRight, true);
            stars.rectTransform.offsetMin = new Vector2(0, 0);
            stars.rectTransform.offsetMax = new Vector2(-6, -4);

            // 右上角：本卡的两个羁绊（行当、出身），竖排小标签
            var line = ChessSession.Instance.Line(card.LineKey);
            if (line != null)
            {
                float tagW = Mathf.Max(16, w * 0.17f), tagH = Mathf.Max(30, w * 0.34f), y = 6 + starH;
                foreach (var key in new[] { line.TradeKey, line.OriginKey })
                {
                    var bond = ChessSession.Instance.Bond(key);
                    if (bond == null) continue;
                    var tag = UIKit.Panel(frame.transform, "Bond_" + key, Theme.WithAlpha(Theme.Paper, 0.92f), Theme.Ink, 1.2f);
                    var t = tag.rectTransform;
                    t.anchorMin = t.anchorMax = new Vector2(1, 1);
                    t.pivot = new Vector2(1, 1);
                    t.sizeDelta = new Vector2(tagW, tagH);
                    t.anchoredPosition = new Vector2(-5, -y);
                    var label = UIKit.Text(tag.transform, "Name", string.Join("\n", bond.Name.ToCharArray()), tagW * 0.62f, Theme.Ink, TextAlignmentOptions.Center, true);
                    label.lineSpacing = -18;
                    y += tagH + 3;
                }
            }

            // 左下角（名字条上方）：点数
            if (showPoints)
            {
                var badge = UIKit.Circle(frame.transform, "PointsBadge", Theme.Paper, Theme.Ink, 1.5f);
                var bt = badge.rectTransform;
                bt.anchorMin = bt.anchorMax = new Vector2(0, 0);
                bt.pivot = new Vector2(0, 0);
                bt.sizeDelta = new Vector2(w * 0.26f, w * 0.26f);
                bt.anchoredPosition = new Vector2(4, h * 0.2f + 2);
                UIKit.Text(badge.transform, "Points", card.Points.ToString(), Mathf.Max(12, w * 0.12f), Theme.Ink, TextAlignmentOptions.Center, true);
            }

            var name = UIKit.Text(frame.transform, "Name", card.Name, Mathf.Max(12, w * 0.135f), Theme.Ink,
                TextAlignmentOptions.Bottom, true);
            name.rectTransform.offsetMin = new Vector2(4, 4);
            name.rectTransform.offsetMax = new Vector2(-4, 0);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            return rt;
        }

        /// <summary>空的出阵格 / 备战格。</summary>
        public static RectTransform Empty(Transform parent, float w, float h, bool locked = false)
        {
            var frame = UIKit.Panel(parent, locked ? "LockedSlot" : "EmptySlot",
                Theme.WithAlpha(Theme.Paper, locked ? 0.12f : 0.35f), Theme.WithAlpha(Theme.Ink, locked ? 0.15f : 0.35f));
            frame.raycastTarget = true;
            frame.rectTransform.sizeDelta = new Vector2(w, h);
            return frame.rectTransform;
        }

        /// <summary>卡牌悬停说明：只写这张卡自己的信息（羁绊已画在卡面上）。</summary>
        public static string Describe(ChessCard card, out string heading, out string sub)
        {
            var s = ChessSession.Instance;
            var line = s.Line(card.LineKey);
            heading = card.Name + "  " + new string('★', card.Star);
            sub = line != null ? line.Name + "系" : "";
            var text = $"点数 <b>{card.Points}</b>";
            if (card.Bonus != 0) text += $"（含永久加成 {card.Bonus:+#;-#;0}）";
            var maxStar = s.Config?.MaxStar ?? 3;
            if (card.Star >= maxStar && line != null && !string.IsNullOrEmpty(line.LegendDesc))
            {
                text += $"\n技能：{line.LegendDesc}";
            }
            text += $"\n出售可得 {card.SellPrice} 金";
            return text;
        }
    }
}
