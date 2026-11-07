using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>左侧羁绊图标列（透明底），悬停或点击看效果。</summary>
    public sealed class BondBarView
    {
        private const float BarY = 170, RootH = 480, FullSize = 50, FullStep = 62, GroupGap = 22;
        private const float SmallSize = 36, SmallGap = 6;
        private const int SmallCols = 3;
        private const int MaxFull = 7; // 480px 高最多放 7 个完整图标
        private readonly RectTransform root;
        private readonly TooltipLayer tooltip;

        public BondBarView(RectTransform parent, TooltipLayer tooltip)
        {
            root = UIKit.Rect(parent, "Bonds");
            UIKit.Place(root, 32, BarY, 120, RootH);
            this.tooltip = tooltip;
        }

        public void Render(MatchState st)
        {
            UIKit.Clear(root);
            var s = ChessSession.Instance;
            // 只显示出阵卡牌身上带有的羁绊（未激活的也显示，但变暗）
            var fielded = new HashSet<string>();
            foreach (var c in st.Self.Board)
            {
                var line = s.Line(c.LineKey);
                if (line == null) continue;
                fielded.Add(line.TradeKey);
                fielded.Add(line.OriginKey);
            }
            var all = st.Self.TradeBonds.Concat(st.Self.OriginBonds)
                .Where(b => fielded.Contains(b.Key) && s.Bond(b.Key) != null)
                .OrderByDescending(b => b.Tier).ThenByDescending(b => b.Count).ToList();
            // 已激活的在前，间隔后是未激活的（变暗）；放不下时未激活的缩成小图标，再放不下收进「+N」
            var active = all.Where(b => b.Tier >= 0).ToList();
            var idle = all.Where(b => b.Tier < 0).ToList();
            var rest = new List<BondCount>();
            if (active.Count > MaxFull)
            {
                rest.AddRange(active.Skip(MaxFull - 1));
                active = active.Take(MaxFull - 1).ToList();
            }
            float y = 0;
            foreach (var bc in active)
            {
                Icon(bc, y, FullSize, true);
                y += FullStep;
            }
            if (active.Count > 0) y += GroupGap - (FullStep - FullSize);
            float usedFull = y + idle.Count * FullStep - (FullStep - FullSize);
            if (rest.Count == 0 && usedFull <= RootH)
            {
                foreach (var bc in idle)
                {
                    Icon(bc, y, FullSize, true);
                    y += FullStep;
                }
            }
            else
            {
                float remaining = RootH - y;
                int rows = Mathf.Max(1, Mathf.FloorToInt((remaining + SmallGap) / (SmallSize + SmallGap)));
                int capacity = rows * SmallCols;
                var queue = idle.ToList();
                bool overflow = rest.Count > 0 || queue.Count > capacity;
                int slots = overflow ? capacity - 1 : capacity;
                var shownIdle = queue.Take(slots).ToList();
                rest.AddRange(queue.Skip(slots));
                int i = 0;
                foreach (var bc in shownIdle)
                {
                    Icon(bc, y + (i / SmallCols) * (SmallSize + SmallGap), SmallSize, false, (i % SmallCols) * (SmallSize + SmallGap));
                    i++;
                }
                if (rest.Count > 0) MoreChip(rest, y + (i / SmallCols) * (SmallSize + SmallGap), (i % SmallCols) * (SmallSize + SmallGap));
            }
        }

        private void Icon(BondCount bc, float y, float size, bool withCount, float x = 0)
        {
            var s = ChessSession.Instance;
            var info = s.Bond(bc.Key);
            var top = bc.Tier >= 0 && bc.Tier == info.Tiers.Count - 1;
            var on = bc.Tier >= 0;
            var ring = top ? Theme.Gold : on ? Theme.Ink : Theme.WithAlpha(Theme.Ink, 0.35f);
            var fillC = top ? Theme.WithAlpha(Theme.Gold, 0.2f) : on ? Theme.WithAlpha(Theme.Cyan, 0.7f) : Theme.Clear;
            var icon = UIKit.Circle(root, "Bond_" + bc.Key, fillC, ring, size < FullSize ? 2f : 2.5f);
            UIKit.Place(icon.rectTransform, x, y, size, size);
            UIKit.Text(icon.transform, "Char", info.Name.Substring(0, 1), size < FullSize ? 16 : 21, Theme.Ink, bold: true);
            if (withCount)
            {
                var next = info.Tiers.Count > 0 ? info.Tiers[Mathf.Min(bc.Tier + 1, info.Tiers.Count - 1)].At + bc.AtDelta : 0;
                var count = UIKit.Live(UIKit.Text(root, "Count", $"{bc.Count}/{next}", 15, Theme.Ink, TextAlignmentOptions.Left, true), 17); // 「10/12」，60 宽
                UIKit.Place(count.rectTransform, x + size + 8, y + 13, 60, 24);
                if (!on) count.alpha = 0.55f;
            }
            if (!on) icon.color = Theme.WithAlpha(icon.color, 0.55f);
            var captured = bc;
            var iconY = BarY + y;
            var relay = UIKit.Hover(icon.gameObject, () => ShowTip(captured, info, iconY), () => tooltip.Hide(info));
            relay.Click = () => ShowTip(captured, info, iconY);
        }

        /// <summary>放不下的羁绊收成「+N」，悬停列出名称和进度。</summary>
        private void MoreChip(List<BondCount> more, float y, float x)
        {
            var s = ChessSession.Instance;
            var chip = UIKit.Circle(root, "BondMore", Theme.WithAlpha(Theme.Paper, 0.6f), Theme.WithAlpha(Theme.Ink, 0.35f), 2f);
            UIKit.Place(chip.rectTransform, x, y, SmallSize, SmallSize);
            UIKit.Text(chip.transform, "Char", $"+{more.Count}", 14, Theme.Ink, bold: true);
            var lines = string.Join("\n", more.Select(b => $"{s.Bond(b.Key).Name}  {b.Count}"));
            var iconY = BarY + y;
            UIKit.Hover(chip.gameObject, () => tooltip.Show(root, 130, iconY - 12, 240, Fmt.T("chess.bond.more", "其余羁绊"), null, lines), () => tooltip.Hide(root));
        }

        private void ShowTip(BondCount bc, BondInfo info, float y)
        {
            var s = ChessSession.Instance;
            var sb = new StringBuilder();
            for (int i = 0; i < info.Tiers.Count; i++)
            {
                var t = info.Tiers[i];
                var line = $"{t.At + bc.AtDelta} 个系：{t.Desc}";
                sb.AppendLine(i == bc.Tier ? $"<b><color=#2F7A72>{line}</color></b>" : $"<color=#5A6A68>{line}</color>");
            }
            var names = string.Join("、", info.LineKeys.Select(k => s.Line(k)?.Name ?? k));
            var bonus = s.Config != null && s.Config.BondTierMult > 0 ? $"\n<color=#5A6A68>每个生效档位另 +{Fmt.Num(s.Config.BondTierMult)} 气势</color>" : "";
            tooltip.Show(info, 130, y - 12, 300, info.Name, $"{info.Desc} · 包含：{names} · 当前 {bc.Count} 个系" + (bc.AtDelta > 0 ? $" · 失和：门槛 +{bc.AtDelta}" : ""), sb.ToString().TrimEnd() + bonus);
        }
    }
}
