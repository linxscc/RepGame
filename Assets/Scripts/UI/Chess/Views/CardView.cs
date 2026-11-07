using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 一张卡的显示：卡图（没有卡图时用卡名占位）、星级、战力、卡名。
    /// 数值全部来自服务器下发的 ChessCard。
    /// </summary>
    public static class CardView
    {
        public static RectTransform Build(Transform parent, ChessCard card, float w, float h, bool showPoints = true)
        {
            var skin = ChessUISkin.Instance;
            var line = ChessSession.Instance.Line(card.LineKey);
            // 卡框颜色 = 费用档（一眼区分 1/2/3 费）；星级看卡名字色（底色统一宣纸色，便于辨认卡图）
            var tier = Theme.CostColor(line?.Cost ?? 1);
            var frame = UIKit.Panel(parent, "Card_" + card.Name, Theme.Paper, tier, 4f);
            frame.raycastTarget = true;
            var rt = frame.rectTransform;
            rt.sizeDelta = new Vector2(w, h);

            var art = skin.Card(card.Name);
            // 卡图区域：按原图比例铺满并裁掉多余部分（不拉伸），任何卡牌尺寸下都不变形
            var artArea = UIKit.Rect(frame.transform, "ArtArea");
            artArea.anchorMin = new Vector2(0, 0);
            artArea.anchorMax = new Vector2(1, 1);
            artArea.offsetMin = new Vector2(5, h * 0.2f);
            artArea.offsetMax = new Vector2(-5, -5);
            artArea.gameObject.AddComponent<RectMask2D>();
            var artImg = UIKit.Image(artArea, "Art", art != null ? Color.white : Theme.Water, art != null ? art : skin.Rounded);
            artImg.preserveAspect = false;
            var art_rt = artImg.rectTransform;
            UIKit.Fill(art_rt);
            if (art != null && art.rect.height > 0)
            {
                var fit = artImg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.rect.width / art.rect.height;
            }
            if (art == null)
            {
                UIKit.Text(artImg.transform, "Placeholder", card.Name, Mathf.Max(13, w * 0.14f), Theme.InkSoft);
            }

            // 右上角：本卡的两个羁绊（行当、出身），竖排小标签。星级看卡名字色
            if (line != null)
            {
                float tagW = Mathf.Max(16, w * 0.17f), tagH = Mathf.Max(30, w * 0.34f), y = 6f;
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

            // 左下角（名字条上方）：战力
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

            // 花色：战力徽章上方一个字（神/鬼），红/黑
            var suitGlyph = SuitGlyph(card);
            if (suitGlyph != "")
            {
                var suitColor = card.Color == CardColor.Red ? Theme.Red : Theme.Ink;
                var disc = UIKit.Circle(frame.transform, "SuitBadge", Theme.WithAlpha(Theme.Paper, 0.9f), suitColor, 1.2f);
                var dt = disc.rectTransform;
                dt.anchorMin = dt.anchorMax = new Vector2(0, 0);
                dt.pivot = new Vector2(0, 0);
                var d = w * 0.22f;
                dt.sizeDelta = new Vector2(d, d);
                dt.anchoredPosition = new Vector2(4 + w * 0.02f, h * 0.2f + 2 + w * 0.26f + 3);
                Sprite suitSprite = null;
                suitSprite = skin.SuitIcon((card.Color == CardColor.Red ? "red_" : "black_") + (suitGlyph == "神" ? "god" : "ghost"));
                if (suitSprite != null)
                {
                    var si = UIKit.Image(disc.transform, "Suit", Color.white, suitSprite);
                    si.preserveAspect = true;
                    var sr = si.rectTransform;
                    sr.anchorMin = new Vector2(0.12f, 0.12f);
                    sr.anchorMax = new Vector2(0.88f, 0.88f);
                    sr.offsetMin = sr.offsetMax = Vector2.zero;
                }
                else
                {
                    UIKit.Text(disc.transform, "Suit", suitGlyph, Mathf.Max(11, w * 0.15f), suitColor, TextAlignmentOptions.Center, true);
                }
            }

            // 名牌底色带：费用档颜色
            var plate = UIKit.Image(frame.transform, "NamePlate", Theme.WithAlpha(tier, 0.22f));
            var prt = plate.rectTransform;
            prt.anchorMin = new Vector2(0, 0);
            prt.anchorMax = new Vector2(1, 0);
            prt.pivot = new Vector2(0.5f, 0);
            prt.offsetMin = new Vector2(4, 4);
            prt.offsetMax = new Vector2(-4, h * 0.2f - 4);

            var name = UIKit.Text(frame.transform, "Name", card.Name, Mathf.Max(12, w * 0.135f), Theme.StarText(card.Star),
                TextAlignmentOptions.Bottom, true);
            name.rectTransform.offsetMin = new Vector2(4, 4);
            name.rectTransform.offsetMax = new Vector2(-4, 0);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            return rt;
        }

        static string SuitGlyph(ChessCard c)
        {
            if (c.Color == CardColor.Unspecified) return "";
            var line = ChessSession.Instance.Line(c.LineKey);
            if (line == null) return "";
            return line.Kind == CardKind.God ? "神" : line.Kind == CardKind.Ghost ? "鬼" : "";
        }

        /// <summary>花色名：红神 / 黑神 / 红鬼 / 黑鬼；未知返回空串。</summary>
        public static string SuitName(ChessCard c)
        {
            var g = SuitGlyph(c);
            if (g == "") return "";
            return (c.Color == CardColor.Red ? "红" : "黑") + g;
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
            var suit = SuitName(card);
            if (suit != "") sub += " · " + suit;
            var text = $"战力 <b>{card.Points}</b>";
            if (card.Bonus != 0) text += $"（含永久加成 {card.Bonus:+#;-#;0}）";
            var maxStar = s.Config?.MaxStar ?? 3;
            if (line != null && !string.IsNullOrEmpty(line.LegendDesc))
            {
                // 未到 3★ 时也提示技能（灰色），让玩家知道合成目标
                text += card.Star >= maxStar
                    ? $"\n<color=#C9A24A>{maxStar}★ 技能</color>：{line.LegendDesc}"
                    : $"\n<color=#8A9290>{maxStar}★ 解锁：{line.LegendDesc}</color>";
            }
            text += $"\n出售可得 {card.SellPrice} 金";
            return text;
        }
    }
}
