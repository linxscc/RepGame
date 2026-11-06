using System.Linq;
using System.Text;
using DG.Tweening;
using RepGame.Core;
using RepGame.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    public static class Fmt
    {
        /// <summary>得分显示：万 / 亿 缩写。</summary>
        public static string Score(double v)
        {
            if (v >= 1e12) return (v / 1e12).ToString("0.##") + "兆";
            if (v >= 1e8) return (v / 1e8).ToString("0.##") + "亿";
            if (v >= 1e5) return (v / 1e4).ToString("0.#") + "万";
            return v.ToString("#,0");
        }

        public static string Num(double v) => v == System.Math.Floor(v) ? v.ToString("0") : v.ToString("0.##");

        public static string T(string key, string fallback, params object[] args)
        {
            var s = ConfigTables.Text(key, args);
            return s == key ? (args.Length > 0 ? string.Format(fallback, args) : fallback) : s;
        }
    }

    /// <summary>血量条 + 一行文字（左上对手 / 左下我方共用）。</summary>
    public sealed class PlayerHud
    {
        private readonly Image fill;
        private readonly TextMeshProUGUI hpText;
        private readonly TextMeshProUGUI infoText;
        private readonly Image xpFill;

        public RectTransform Avatar { get; }

        public PlayerHud(RectTransform parent, float x, float y, bool self)
        {
            var skin = ChessUISkin.Instance;
            var avatar = UIKit.Circle(parent, self ? "SelfAvatar" : "EnemyAvatar", self ? Theme.Paper : Theme.Ink, Theme.Ink);
            UIKit.Place(avatar.rectTransform, x, y, 64, 64);
            var pic = self ? skin.avatarMe : skin.avatarEnemy;
            if (pic != null)
            {
                var img = UIKit.Image(avatar.transform, "Pic", Color.white, pic);
                UIKit.Fill(img.rectTransform, 4);
            }
            else
            {
                UIKit.Text(avatar.transform, "Char", self ? "我" : "敌", 24, self ? Theme.Ink : Theme.Paper, bold: true);
            }
            avatar.raycastTarget = true;
            Avatar = avatar.rectTransform;

            var block = UIKit.Rect(parent, self ? "SelfHp" : "EnemyHp");
            UIKit.Place(block, x + 78, y + 4, 240, 56);
            hpText = UIKit.Text(block, "Hp", "", 16, Theme.Ink, TextAlignmentOptions.TopLeft, true);
            infoText = UIKit.Text(block, "Info", "", 15, Theme.InkSoft, TextAlignmentOptions.TopRight);
            var bar = UIKit.Panel(block, "Bar", Theme.Paper, Theme.Ink, 1.5f);
            UIKit.Place(bar.rectTransform, 0, 24, 240, 14);
            fill = UIKit.Image(bar.transform.GetChild(0), "Fill", Theme.Red);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1, 1);
            fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            if (self)
            {
                var xpBar = UIKit.Image(block, "Xp", Theme.WithAlpha(Theme.Ink, 0.12f));
                UIKit.Place(xpBar.rectTransform, 0, 44, 240, 6);
                xpFill = UIKit.Image(xpBar.transform, "Fill", Theme.CyanDeep);
                xpFill.rectTransform.anchorMin = Vector2.zero;
                xpFill.rectTransform.anchorMax = new Vector2(0, 1);
                xpFill.rectTransform.pivot = new Vector2(0, 0.5f);
                xpFill.rectTransform.offsetMin = xpFill.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private float shownHp = -1;
        private int targetHp;
        private int max = 100;
        private bool frozen;

        public void Set(int hp, int maxHp, string info, float xp01 = -1)
        {
            infoText.text = info;
            max = maxHp;
            targetHp = hp;
            if (xpFill != null) xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(xp01 < 0 ? 1 : xp01), 1);
            if (shownHp < 0)
            {
                ShowHp(hp);
                return;
            }
            if (!frozen) AnimateHp();
        }

        /// <summary>过场动画期间保持旧血量，等攻击命中后再下降。</summary>
        public void Freeze() => frozen = true;

        public void Release()
        {
            frozen = false;
            AnimateHp();
        }

        /// <summary>受击：头像震动。</summary>
        public void Hit()
        {
            DOTween.Complete(Avatar);
            Avatar.DOShakeAnchorPos(0.45f, 14f, 30, 90, false, true).SetTarget(Avatar);
        }

        private void AnimateHp()
        {
            if (Mathf.Approximately(shownHp, targetHp)) return;
            DOTween.Kill(this);
            DOTween.To(() => shownHp, ShowHp, targetHp, ChessUISkin.Instance.hpDrainSeconds).SetEase(Ease.OutCubic).SetTarget(this);
        }

        private void ShowHp(float hp)
        {
            shownHp = hp;
            hpText.text = $"{Mathf.RoundToInt(hp)} / {max}";
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(max > 0 ? hp / max : 0), 1);
        }
    }

    /// <summary>顶部中央：倒计时圆 + 回合。</summary>
    public sealed class TimerView
    {
        private readonly TextMeshProUGUI seconds;
        private readonly TextMeshProUGUI caption;
        private readonly Image ring;

        public TimerView(RectTransform parent)
        {
            var c = UIKit.Circle(parent, "Timer", Theme.Paper, Theme.Red, 3f);
            UIKit.PlaceCenter(c.rectTransform, 960, 54, 72, 72);
            ring = c;
            seconds = UIKit.Text(c.transform, "Sec", "", 32, Theme.Red, bold: true);
            var cap = UIKit.Rect(parent, "Round");
            UIKit.PlaceCenter(cap, 960, 104, 260, 24);
            caption = UIKit.Text(cap, "Text", "", 15, Theme.InkSoft);
        }

        public void Tick(MatchState st)
        {
            // 结算阶段不显示倒计时：过场和扣血演完后会直接进入下一回合
            var left = st != null && st.Phase == MatchPhase.Settle ? -1 : ChessSession.Instance.SecondsLeft();
            seconds.text = left < 0 ? "" : left.ToString();
            ring.color = left >= 0 && left <= 5 ? Theme.Red : Theme.Ink;
            if (st == null) return;
            caption.text = st.Phase switch
            {
                MatchPhase.Family => Fmt.T("chess.phase.family", "选择家世"),
                MatchPhase.Vault => Fmt.T("chess.phase.vault", "第 {0} 回合 · 宝库", st.Round),
                MatchPhase.Settle => Fmt.T("chess.phase.settle", "第 {0} 回合 · 结算", st.Round),
                _ => Fmt.T("chess.phase.prep", "第 {0} 回合 · 阶段伤害 {1}", st.Round, st.StageDamage),
            };
        }
    }

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
            var all = st.Self.TradeBonds.Concat(st.Self.OriginBonds)
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

    /// <summary>出阵区上方的预计得分：固定位置，出现/消失时淡入淡出，分数变化时数字滚动到新值。</summary>
    public sealed class ScoreDockView
    {
        private readonly RectTransform root;
        private readonly TextMeshProUGUI score;
        private readonly TextMeshProUGUI formula;
        private readonly CanvasGroup group;
        private static readonly Vector2 Dock = new Vector2(960, 352);
        private double shown = -1;
        private float rolling; // 正在显示的数字（滚动动画用）

        public ScoreDockView(RectTransform parent)
        {
            root = UIKit.Rect(parent, "ScoreDock");
            UIKit.PlaceCenter(root, Dock.x, Dock.y, 520, 100);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            score = UIKit.Text(root, "Score", "", 56, Theme.Red, TextAlignmentOptions.Top, true);
            score.outlineWidth = 0.18f;
            score.outlineColor = Theme.Paper;
            var f = UIKit.Rect(root, "FormulaBox");
            f.anchorMin = new Vector2(0.5f, 0);
            f.anchorMax = new Vector2(0.5f, 0);
            f.pivot = new Vector2(0.5f, 0);
            f.sizeDelta = new Vector2(320, 26);
            var bg = UIKit.Panel(f, "Bg", Theme.WithAlpha(Theme.Paper, 0.92f), Theme.Ink, 1.5f);
            UIKit.Fill(bg.rectTransform);
            formula = UIKit.Text(f, "Formula", "", 15, Theme.Ink);
        }

        public void Render(ScoreBreakdown preview, bool visible)
        {
            if (!(visible && preview != null && preview.Score > 0))
            {
                    UIFx.Out(root.gameObject, UIFx.Style.Fade);
                shown = -1;
                return;
            }
            formula.text = $"战力 {Fmt.Num(preview.Points)} × 气势 {Fmt.Num(preview.Mult)}";
            if (!UIFx.IsShown(root.gameObject))
            {
                // 刚出现：直接显示最终值并淡入
                DOTween.Kill(score);
                shown = preview.Score;
                rolling = (float)preview.Score;
                score.text = Fmt.Score(preview.Score);
                UIFx.In(root.gameObject, UIFx.Style.Fade);
                return;
            }
            if (preview.Score == shown) return;
            shown = preview.Score;
            DOTween.Kill(score);
            DOTween.To(() => rolling, v => { rolling = v; score.text = Fmt.Score(v); }, (float)preview.Score, 0.35f)
                .SetEase(Ease.OutCubic).SetTarget(score);
        }
    }
}
