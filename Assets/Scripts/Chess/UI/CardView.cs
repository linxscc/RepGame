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
        public static RectTransform Build(Transform parent, ChessCard card, float w, float h, bool showPoints = true)
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

            var stars = UIKit.Text(frame.transform, "Stars", new string('★', card.Star), Mathf.Max(11, w * 0.11f), Theme.Red,
                TextAlignmentOptions.TopLeft, true);
            stars.rectTransform.offsetMin = new Vector2(8, 0);
            stars.rectTransform.offsetMax = new Vector2(0, -6);

            if (showPoints)
            {
                var pts = UIKit.Text(frame.transform, "Points", card.Points.ToString(), Mathf.Max(12, w * 0.12f), Theme.Ink,
                    TextAlignmentOptions.TopRight, true);
                pts.rectTransform.offsetMin = new Vector2(0, 0);
                pts.rectTransform.offsetMax = new Vector2(-8, -6);
                var badge = UIKit.Circle(frame.transform, "PointsBadge", Theme.Paper, Theme.Ink, 1.5f);
                badge.transform.SetSiblingIndex(pts.transform.GetSiblingIndex());
                var b = badge.rectTransform;
                b.anchorMin = b.anchorMax = new Vector2(1, 1);
                b.pivot = new Vector2(1, 1);
                b.sizeDelta = new Vector2(w * 0.26f, w * 0.26f);
                b.anchoredPosition = new Vector2(-3, -3);
                pts.rectTransform.anchorMin = pts.rectTransform.anchorMax = new Vector2(1, 1);
                pts.rectTransform.pivot = new Vector2(1, 1);
                pts.rectTransform.sizeDelta = b.sizeDelta;
                pts.rectTransform.anchoredPosition = b.anchoredPosition;
                pts.alignment = TextAlignmentOptions.Center;
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

        /// <summary>卡牌悬停说明。</summary>
        public static string Describe(ChessCard card, out string heading, out string sub)
        {
            var s = ChessSession.Instance;
            var line = s.Line(card.LineKey);
            heading = card.Name + "  " + new string('★', card.Star);
            var trade = s.Bond(line?.TradeKey)?.Name;
            var origin = s.Bond(line?.OriginKey)?.Name;
            sub = $"{line?.Name}系 · {trade} · {origin}";
            var text = $"点数 <b>{card.Points}</b>";
            if (card.Bonus != 0) text += $"（含永久加成 {card.Bonus:+#;-#;0}）";
            if (line != null && !string.IsNullOrEmpty(line.LegendDesc))
            {
                var legendName = line.StarNames.Count > 0 ? line.StarNames[line.StarNames.Count - 1] : "3★";
                var mark = card.Star >= 3 ? "" : "<color=#5A6A68>";
                var end = card.Star >= 3 ? "" : "</color>";
                text += $"\n{mark}3★ {legendName}：{line.LegendDesc}{end}";
            }
            text += $"\n出售可得 {card.SellPrice} 金";
            return text;
        }
    }
}
