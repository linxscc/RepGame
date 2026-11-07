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
        private readonly RectTransform root;
        private readonly TooltipLayer tooltip;

        public BondBarView(RectTransform parent, TooltipLayer tooltip)
        {
            root = UIKit.Rect(parent, "Bonds");
            UIKit.Place(root, 32, 170, 120, 480);
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
                .Where(b => fielded.Contains(b.Key))
                .OrderByDescending(b => b.Tier).ThenByDescending(b => b.Count).ToList();
            float y = 0;
            foreach (var bc in all)
            {
                var info = s.Bond(bc.Key);
                if (info == null) continue;
                var top = bc.Tier >= 0 && bc.Tier == info.Tiers.Count - 1;
                var on = bc.Tier >= 0;
                var ring = top ? Theme.Gold : on ? Theme.Ink : Theme.WithAlpha(Theme.Ink, 0.35f);
                var fillC = top ? Theme.WithAlpha(Theme.Gold, 0.2f) : on ? Theme.WithAlpha(Theme.Cyan, 0.7f) : Theme.Clear;
                var icon = UIKit.Circle(root, "Bond_" + bc.Key, fillC, ring, 2.5f);
                UIKit.Place(icon.rectTransform, 0, y, 50, 50);
                UIKit.Text(icon.transform, "Char", info.Name.Substring(0, 1), 21, Theme.Ink, bold: true);
                var next = info.Tiers.Count > 0 ? info.Tiers[Mathf.Min(bc.Tier + 1, info.Tiers.Count - 1)].At : 0;
                var count = UIKit.Text(root, "Count", $"{bc.Count}/{next}", 15, Theme.Ink, TextAlignmentOptions.Left, true);
                UIKit.Place(count.rectTransform, 58, y + 13, 60, 24);
                if (!on)
                {
                    icon.color = Theme.WithAlpha(icon.color, 0.55f);
                    count.alpha = 0.55f;
                }
                var captured = bc;
                var iconY = 170 + y;
                var relay = UIKit.Hover(icon.gameObject, () => ShowTip(captured, info, iconY), () => tooltip.Hide(info));
                relay.Click = () => ShowTip(captured, info, iconY);
                y += 62;
            }
        }

        private void ShowTip(BondCount bc, BondInfo info, float y)
        {
            var s = ChessSession.Instance;
            var sb = new StringBuilder();
            for (int i = 0; i < info.Tiers.Count; i++)
            {
                var t = info.Tiers[i];
                var line = $"{t.At} 个系：{t.Desc}";
                sb.AppendLine(i == bc.Tier ? $"<b><color=#2F7A72>{line}</color></b>" : $"<color=#5A6A68>{line}</color>");
            }
            var names = string.Join("、", info.LineKeys.Select(k => s.Line(k)?.Name ?? k));
            var bonus = s.Config != null && s.Config.BondTierMult > 0 ? $"\n<color=#5A6A68>每个生效档位另 +{Fmt.Num(s.Config.BondTierMult)} 气势</color>" : "";
            tooltip.Show(info, 130, y - 12, 300, info.Name, $"{info.Desc} · 包含：{names} · 当前 {bc.Count} 个系", sb.ToString().TrimEnd() + bonus);
        }
    }
}
