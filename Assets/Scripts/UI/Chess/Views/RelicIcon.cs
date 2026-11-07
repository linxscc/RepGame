using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>法宝图标（暂无图时显示首字 + 稀有度色环）与说明。</summary>
    public static class RelicIcon
    {
        public static RectTransform Build(Transform parent, string key, float size)
        {
            var s = ChessSession.Instance;
            var info = s.Relic(key);
            var rarity = info?.Rarity ?? 0;
            var c = UIKit.Circle(parent, "Relic_" + key, Theme.Paper, Theme.Rarity(rarity), 2.5f);
            c.raycastTarget = true;
            var art = ChessUISkin.Instance.RelicIcon(key);
            if (art != null)
            {
                var img = UIKit.Image(c.transform, "Art", Color.white, art);
                UIKit.Fill(img.rectTransform, size * 0.1f);
            }
            else
            {
                UIKit.Text(c.transform, "Char", info != null ? info.Name.Substring(0, 1) : "?", size * 0.38f, Theme.Ink, bold: true);
            }
            c.rectTransform.sizeDelta = new Vector2(size, size);
            return c.rectTransform;
        }

        public static string SubTitle(RelicInfo info)
        {
            if (info == null) return null;
            var rarity = ChessSession.Instance.Rarity(info.Rarity)?.Name;
            return string.IsNullOrEmpty(info.CategoryName) ? rarity : $"{rarity} · {info.CategoryName}";
        }

        public static void ShowTip(TooltipLayer tooltip, RelicSlot r, float x, float y)
        {
            var s = ChessSession.Instance;
            var info = s.Relic(r.RelicKey);
            if (info == null) return;
            var text = info.Desc;
            if (r.Value != 0) text += $"\n当前成长：<b>+{Fmt.Num(r.Value)}</b>";
            if (r.TradeChoice >= 0 && s.Config != null && r.TradeChoice < s.Config.Trades.Count) text += $"\n选定行当：{s.Config.Trades[r.TradeChoice].Name}";
            if (r.SellPrice > 0) text += $"\n<color=#5A6A68>拖到金币上出售：{r.SellPrice} 金</color>";
            tooltip.Show(r, x, y, 280, info.Name, SubTitle(info), text);
        }

        public static void ShowInfoTip(TooltipLayer tooltip, object owner, string key, float x, float y)
        {
            var s = ChessSession.Instance;
            var info = s.Relic(key);
            if (info == null) return;
            tooltip.Show(owner, x, y, 280, info.Name, SubTitle(info), info.Desc);
        }
    }
}
