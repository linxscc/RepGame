using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using RepGame.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 透明背景的选择层：家世三选一、宝库三选一、选择行当、选择要卖掉的法宝。
    /// 只把选择结果发给服务器。
    /// </summary>
    public sealed class PickPopup
    {
        private readonly RectTransform root;
        private readonly TooltipLayer tooltip;
        private string shownKey; // 避免同一次选择被重复重建

        public PickPopup(RectTransform layer, TooltipLayer tooltip)
        {
            root = UIKit.Rect(layer, "Pick");
            UIKit.Fill(root);
            this.tooltip = tooltip;
            root.gameObject.SetActive(false);
        }

        public bool Visible => root.gameObject.activeSelf;

        public void Hide()
        {
            root.gameObject.SetActive(false);
            shownKey = null;
        }

        private sealed class Option
        {
            public string Title, Tag, Body;
            public Color Ring;
            public string RelicKey; // 显示法宝图标
            public Action Click;
        }

        private void Show(string key, string title, string subtitle, List<Option> options, string skipLabel, Action skip, bool dim = false)
        {
            if (shownKey == key && Visible) return;
            shownKey = key;
            UIKit.Clear(root);
            root.gameObject.SetActive(true);
            tooltip.Hide(null);
            if (dim)
            {
                var d = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
                UIKit.Fill(d.rectTransform);
            }
            var head = UIKit.Text(root, "Title", title, 44, Theme.Ink, TextAlignmentOptions.Center, true);
            head.outlineWidth = 0.22f;
            head.outlineColor = Theme.Paper;
            UIKit.PlaceCenter(head.rectTransform, 960, 190, 1200, 60);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = UIKit.Panel(root, "SubBg", Theme.WithAlpha(Theme.Paper, 0.88f), Theme.Clear, 0);
                UIKit.PlaceCenter(sub.rectTransform, 960, 236, 640, 30);
                UIKit.Text(sub.transform, "Sub", subtitle, 16, Theme.Ink);
            }
            float w = 320, gap = 40;
            float total = options.Count * w + (options.Count - 1) * gap;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                var card = UIKit.Panel(root, "Option" + i, Theme.WithAlpha(Theme.Paper, 0.96f), o.Ring, 3f);
                card.raycastTarget = true;
                UIKit.Place(card.rectTransform, 960 - total / 2 + i * (w + gap), 280, w, 420);
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                var click = o.Click;
                b.onClick.AddListener(() => click());
                var tag = UIKit.Text(card.transform, "Tag", o.Tag ?? "", 14, o.Ring, TextAlignmentOptions.TopRight, true);
                tag.rectTransform.offsetMax = new Vector2(-18, -16);
                if (o.RelicKey != null)
                {
                    var icon = RelicIcon.Build(card.transform, o.RelicKey, 132);
                    UIKit.PlaceCenter(icon, w / 2, 120, 132, 132);
                }
                var t = UIKit.Text(card.transform, "Title", o.Title, 28, Theme.Ink, TextAlignmentOptions.Center, true);
                UIKit.Place(t.rectTransform, 0, o.RelicKey != null ? 200 : 90, w, 40);
                var body = UIKit.Text(card.transform, "Body", o.Body ?? "", 17, Theme.Ink, TextAlignmentOptions.Top);
                UIKit.Place(body.rectTransform, 24, o.RelicKey != null ? 254 : 150, w - 48, 150);
            }
            if (skip != null)
            {
                var sb = UIKit.Button(root, "Skip", Theme.WithAlpha(Theme.Paper, 0.92f), Theme.Ink, skipLabel, 17, Theme.Ink, skip);
                UIKit.PlaceCenter((RectTransform)sb.transform, 960, 750, 240, 50);
            }
        }

        public void ShowFamilies(MatchState st)
        {
            var s = ChessSession.Instance;
            if (st.Self.Family >= 0)
            {
                Waiting("family-wait", Fmt.T("chess.family.waiting", "已选择：{0}，等待对手…", s.Family(st.Self.Family)?.Name));
                return;
            }
            var opts = st.FamilyOffer.Select((f, i) =>
            {
                var fam = s.Family(f);
                return new Option { Title = fam?.Name, Body = fam?.Desc, Ring = Theme.Ink, Click = () => ChessActions.PickFamily(i) };
            }).ToList();
            Show("family:" + string.Join(",", st.FamilyOffer), Fmt.T("chess.family.title", "选择家世"),
                Fmt.T("chess.family.subtitle", "整局生效 · 倒计时结束后随机选择"), opts, null, null);
        }

        public void ShowVault(MatchState st)
        {
            var s = ChessSession.Instance;
            if (st.Self.Ready || st.VaultOffer.Count == 0)
            {
                Waiting("vault-wait", Fmt.T("chess.vault.waiting", "等待对手选择…"));
                return;
            }
            var opts = st.VaultOffer.Select((key, i) =>
            {
                var info = s.Relic(key);
                return new Option
                {
                    Title = info?.Name, Body = info?.Desc, RelicKey = key, Tag = s.Rarity(info?.Rarity ?? 0)?.Name,
                    Ring = Theme.Rarity(info?.Rarity ?? 0), Click = () => PickVault(st, i, info),
                };
            }).ToList();
            Show("vault:" + st.Round + ":" + string.Join(",", st.VaultOffer), Fmt.T("chess.vault.title", "宝库 · 三选一"),
                Fmt.T("chess.vault.subtitle", "免费拿 1 件法宝 · 倒计时结束后随机选择"), opts, Fmt.T("chess.vault.skip", "放弃，换金币"), ChessActions.SkipVault);
        }

        private void PickVault(MatchState st, int index, RelicInfo info)
        {
            Action<int> withSell = trade =>
            {
                if (st.Self.Relics.Count >= st.Self.RelicSlots) ChooseRelicToSell(st, slot => ChessActions.PickVault(index, slot, trade));
                else ChessActions.PickVault(index, -1, trade);
            };
            if (info != null && info.NeedsTradeChoice) ChooseTrade(withSell);
            else withSell(-1);
        }

        public void ChooseTrade(Action<int> chosen)
        {
            var s = ChessSession.Instance;
            var opts = s.Config.Trades.Select((t, i) => new Option
            {
                Title = t.Name, Body = t.Desc, Ring = Theme.CyanDeep, Click = () => { Hide(); chosen(i); },
            }).ToList();
            Show("trade:" + Guid.NewGuid(), Fmt.T("chess.trade.title", "选择行当"), Fmt.T("chess.trade.subtitle", "这件法宝需要选定一个行当"), opts,
                Fmt.T("chess.cancel", "取消"), Hide, dim: true);
        }

        private void ChooseRelicToSell(MatchState st, Action<int> chosen)
        {
            var s = ChessSession.Instance;
            var opts = st.Self.Relics.Select((r, i) =>
            {
                var info = s.Relic(r.RelicKey);
                return new Option
                {
                    Title = info?.Name, Body = $"卖出可得 {r.SellPrice} 金", RelicKey = r.RelicKey, Ring = Theme.Rarity(info?.Rarity ?? 0),
                    Click = () => { Hide(); chosen(i); },
                };
            }).ToList();
            Show("sell:" + Guid.NewGuid(), Fmt.T("chess.sell.title", "法宝栏已满"), Fmt.T("chess.sell.subtitle", "选一件卖掉，给新法宝腾位置"), opts,
                Fmt.T("chess.cancel", "取消"), Hide, dim: true);
        }

        private void Waiting(string key, string text)
        {
            if (shownKey == key && Visible) return;
            shownKey = key;
            UIKit.Clear(root);
            root.gameObject.SetActive(true);
            var bg = UIKit.Panel(root, "WaitBg", Theme.WithAlpha(Theme.Paper, 0.9f), Theme.Ink, 1.5f);
            UIKit.PlaceCenter(bg.rectTransform, 960, 250, 520, 56);
            UIKit.Text(bg.transform, "Text", text, 20, Theme.Ink);
        }
    }

    /// <summary>主界面中央的结算弹窗：双方阵容、逐步计分、胜负与扣血。</summary>
    public sealed class SettlePopup
    {
        private readonly RectTransform root;
        private Sequence seq;

        public SettlePopup(RectTransform layer)
        {
            root = UIKit.Rect(layer, "Settle");
            UIKit.Fill(root);
            root.gameObject.SetActive(false);
        }

        public void Hide()
        {
            seq?.Kill();
            root.gameObject.SetActive(false);
        }

        public void Show(RoundResult rr)
        {
            seq?.Kill();
            UIKit.Clear(root);
            root.gameObject.SetActive(true);
            var dim = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Finish);

            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2.5f);
            UIKit.Place(panel.rectTransform, 400, 150, 1120, 780);
            var p = panel.rectTransform;
            var title = UIKit.Text(p, "Title", $"第 {rr.Round} 回合 · 结算", 28, Theme.Ink, TextAlignmentOptions.TopLeft, true);
            UIKit.Place(title.rectTransform, 34, 24, 600, 40);
            var hint = UIKit.Text(p, "Hint", "点击任意处跳过", 14, Theme.InkSoft, TextAlignmentOptions.TopRight);
            UIKit.Place(hint.rectTransform, 600, 32, 486, 24);

            Side(p, rr.Opponent, 80, "对手", false);
            Side(p, rr.Self, 610, "我方", true);

            // 中间：胜负 + 计分步骤
            var mid = UIKit.Rect(p, "Middle");
            UIKit.Place(mid, 34, 230, 1052, 360);
            var resultText = rr.Winner > 0 ? "胜" : rr.Winner < 0 ? "负" : "平";
            var big = UIKit.Text(mid, "Result", resultText, 72, rr.Winner >= 0 ? Theme.Red : Theme.InkSoft, TextAlignmentOptions.Center, true);
            UIKit.Place(big.rectTransform, 0, 60, 300, 90);
            var dmg = rr.Winner > 0 ? $"对手 −{rr.Opponent.DamageTaken} 血" : rr.Winner < 0 ? $"我方 −{rr.Self.DamageTaken} 血" : $"双方各 −{rr.Self.DamageTaken} 血";
            var dmgText = UIKit.Text(mid, "Damage", dmg, 20, Theme.Ink, TextAlignmentOptions.Center, true);
            UIKit.Place(dmgText.rectTransform, 0, 160, 300, 30);
            var next = rr.MatchOver ? "对局结束" : rr.VaultNext ? "接下来：宝库" : "即将进入下一回合";
            var nextText = UIKit.Text(mid, "Next", next, 14, Theme.InkSoft, TextAlignmentOptions.Center);
            UIKit.Place(nextText.rectTransform, 0, 200, 300, 24);

            var list = UIKit.Rect(mid, "Steps");
            UIKit.Place(list, 330, 0, 722, 360);
            var lines = Condense(rr.Self.Score);
            var total = UIKit.Text(p, "Total", "0", 40, Theme.Red, TextAlignmentOptions.Right, true);
            UIKit.Place(total.rectTransform, 700, 650, 386, 50);

            seq = DOTween.Sequence();
            float rowH = 30;
            int maxRows = Mathf.Min(lines.Count, 12);
            var start = Mathf.Max(0, lines.Count - maxRows);
            var dur = Mathf.Clamp(5f / Mathf.Max(1, lines.Count), 0.12f, 0.45f);
            for (int i = start; i < lines.Count; i++)
            {
                var l = lines[i];
                var row = UIKit.Text(list, "Step", $"{l.label}", 16, Theme.Ink, TextAlignmentOptions.Left);
                UIKit.Place(row.rectTransform, 10, (i - start) * rowH, 520, rowH);
                var val = UIKit.Text(list, "Val", l.value, 16, Theme.Ink, TextAlignmentOptions.Right, true);
                UIKit.Place(val.rectTransform, 520, (i - start) * rowH, 190, rowH);
                row.alpha = 0;
                val.alpha = 0;
                var score = l.runningScore;
                seq.Append(row.DOFade(1, dur));
                seq.Join(val.DOFade(1, dur));
                seq.AppendCallback(() => total.text = Fmt.Score(score));
            }
            var final = rr.Self.Score.Score;
            seq.AppendCallback(() => total.text = Fmt.Score(final));
        }

        private void Finish()
        {
            if (seq != null && seq.IsActive() && seq.IsPlaying()) seq.Complete(true);
            else Hide();
        }

        private static void Side(RectTransform p, SideResult side, float y, string who, bool self)
        {
            var label = UIKit.Text(p, who, $"{who}\n血量 {side.HpAfter + side.DamageTaken} → <b>{side.HpAfter}</b>", 15, Theme.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Place(label.rectTransform, 34, y + 20, 140, 60);
            float x = 180;
            foreach (var c in side.Board)
            {
                var rt = CardView.Build(p, c, 76, 108);
                UIKit.Place(rt, x, y, 76, 108);
                x += 84;
            }
            var sc = UIKit.Text(p, who + "Score", $"<size=13><color=#3A4A48>{Fmt.Num(side.Score.Points)} × {Fmt.Num(side.Score.Mult)}</color></size>\n{Fmt.Score(side.Score.Score)}",
                36, self ? Theme.Red : Theme.Ink, TextAlignmentOptions.TopRight, true);
            UIKit.Place(sc.rectTransform, 800, y + 10, 286, 90);
        }

        private struct Line
        {
            public string label, value;
            public double runningScore;
        }

        /// <summary>把逐步计分合并成易读的几行：同一来源的连续步骤合并。</summary>
        private static List<Line> Condense(ScoreBreakdown sb)
        {
            var phaseName = new[] { "", "①", "②", "③", "④", "⑤" };
            var lines = new List<Line>();
            int i = 0;
            while (i < sb.Steps.Count)
            {
                var s = sb.Steps[i];
                var j = i;
                double add = 0, times = 1;
                while (j < sb.Steps.Count && sb.Steps[j].Source == s.Source && sb.Steps[j].Kind == s.Kind && sb.Steps[j].Phase == s.Phase)
                {
                    if (sb.Steps[j].Kind == ScoreStepKind.TimesMult) times *= sb.Steps[j].Value;
                    else add += sb.Steps[j].Value;
                    j++;
                }
                var last = sb.Steps[j - 1];
                string value = s.Kind == ScoreStepKind.Points ? $"+{Fmt.Num(add)} 点"
                    : s.Kind == ScoreStepKind.AddMult ? $"+{Fmt.Num(add)} 倍" : $"×{Fmt.Num(times)}";
                var ph = s.Phase >= 0 && s.Phase < phaseName.Length ? phaseName[s.Phase] : "";
                lines.Add(new Line { label = $"{ph} {s.Source}", value = value, runningScore = Math.Floor(last.Points * last.Mult) });
                i = j;
            }
            return lines;
        }
    }

    /// <summary>对局结束弹窗。</summary>
    public sealed class GameOverPopup
    {
        private readonly RectTransform root;
        private readonly Action onRematch;
        private readonly Action onLobby;
        private TextMeshProUGUI status;

        public GameOverPopup(RectTransform layer, Action rematch, Action lobby)
        {
            root = UIKit.Rect(layer, "GameOver");
            UIKit.Fill(root);
            onRematch = rematch;
            onLobby = lobby;
            root.gameObject.SetActive(false);
        }

        public void Hide() => root.gameObject.SetActive(false);

        public void Show(MatchOver over)
        {
            UIKit.Clear(root);
            root.gameObject.SetActive(true);
            var dim = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2.5f);
            UIKit.PlaceCenter(panel.rectTransform, 960, 520, 640, 420);
            var word = over.Result > 0 ? Fmt.T("game.win", "你胜利了！") : over.Result < 0 ? Fmt.T("game.lose", "你失败了！") : Fmt.T("chess.draw", "平局");
            var t = UIKit.Text(panel.transform, "Result", word, 56, over.Result > 0 ? Theme.Red : Theme.Ink, TextAlignmentOptions.Top, true);
            UIKit.Place(t.rectTransform, 0, 50, 640, 80);
            var why = over.Surrendered ? (over.Result < 0 ? "你已认输 · " : "对手认输 · ") : "";
            var info = UIKit.Text(panel.transform, "Info", $"{why}共 {over.Rounds} 回合 · 我方血量 {over.Hp} · 对手血量 {over.OpponentHp}", 20, Theme.InkSoft, TextAlignmentOptions.Top);
            UIKit.Place(info.rectTransform, 0, 150, 640, 30);
            status = UIKit.Text(panel.transform, "Status", "", 16, Theme.CyanDeep, TextAlignmentOptions.Top);
            UIKit.Place(status.rectTransform, 0, 200, 640, 30);
            var again = UIKit.Button(panel.transform, "Rematch", Theme.Red, Theme.Ink, Fmt.T("chess.rematch", "再来一局"), 22, Theme.Paper, () =>
            {
                status.text = Fmt.T("match.waiting", "正在匹配对手...");
                onRematch();
            });
            UIKit.Place((RectTransform)again.transform, 120, 290, 180, 60);
            var lobby = UIKit.Button(panel.transform, "Lobby", Theme.Paper, Theme.Ink, Fmt.T("chess.lobby", "返回大厅"), 22, Theme.Ink, onLobby);
            UIKit.Place((RectTransform)lobby.transform, 340, 290, 180, 60);
        }
    }

    /// <summary>二次确认框（认输等不可撤销的操作）。</summary>
    public sealed class ConfirmPopup
    {
        private readonly RectTransform root;

        public ConfirmPopup(RectTransform layer)
        {
            root = UIKit.Rect(layer, "Confirm");
            UIKit.Fill(root);
            root.gameObject.SetActive(false);
        }

        public void Hide() => root.gameObject.SetActive(false);

        public void Show(string message, string okLabel, Action onOk)
        {
            UIKit.Clear(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var dim = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2.5f);
            panel.raycastTarget = true;
            UIKit.PlaceCenter(panel.rectTransform, 960, 520, 520, 240);
            var text = UIKit.Text(panel.transform, "Message", message, 22, Theme.Ink, TextAlignmentOptions.Center, true);
            UIKit.Place(text.rectTransform, 30, 30, 460, 100);
            var ok = UIKit.Button(panel.transform, "Ok", Theme.Red, Theme.Ink, okLabel, 20, Theme.Paper, () => { Hide(); onOk(); });
            UIKit.Place((RectTransform)ok.transform, 70, 160, 170, 54);
            var cancel = UIKit.Button(panel.transform, "Cancel", Theme.Paper, Theme.Ink, Fmt.T("chess.cancel", "取消"), 20, Theme.Ink, Hide);
            UIKit.Place((RectTransform)cancel.transform, 280, 160, 170, 54);
        }
    }

    /// <summary>点击对手头像：上回合阵容与得分。</summary>
    public sealed class OpponentPopover
    {
        private readonly RectTransform root;

        public OpponentPopover(RectTransform layer)
        {
            root = UIKit.Rect(layer, "OpponentBoard");
            root.gameObject.SetActive(false);
        }

        public void Toggle(MatchState st)
        {
            if (root.gameObject.activeSelf || st == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            UIKit.Clear(root);
            var o = st.Opponent;
            int n = Mathf.Max(1, o.LastBoard.Count);
            float w = 40 + n * 84;
            UIKit.Place(root, 32, 100, Mathf.Max(w, 360), 190);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2f);
            UIKit.Fill(panel.rectTransform);
            panel.raycastTarget = true;
            panel.gameObject.AddComponent<Button>().onClick.AddListener(() => root.gameObject.SetActive(false));
            var title = UIKit.Text(root, "Title", $"{o.Username} · 上回合得分 {Fmt.Score(o.LastScore)}", 16, Theme.Ink, TextAlignmentOptions.TopLeft, true);
            UIKit.Place(title.rectTransform, 20, 14, 600, 24);
            float x = 20;
            if (o.LastBoard.Count == 0)
            {
                var none = UIKit.Text(root, "None", "还没有结算过的阵容", 15, Theme.InkSoft, TextAlignmentOptions.TopLeft);
                UIKit.Place(none.rectTransform, 20, 60, 300, 24);
            }
            foreach (var c in o.LastBoard)
            {
                var rt = CardView.Build(root, c, 76, 108);
                UIKit.Place(rt, x, 56, 76, 108);
                x += 84;
            }
        }
    }

    /// <summary>屏幕上方的短提示（操作失败等）。</summary>
    public sealed class Toast
    {
        private readonly RectTransform root;
        private readonly TextMeshProUGUI text;
        private readonly CanvasGroup group;

        public Toast(RectTransform layer)
        {
            var bg = UIKit.Panel(layer, "Toast", Theme.Ink, Theme.Ink, 0);
            root = bg.rectTransform;
            UIKit.PlaceCenter(root, 960, 160, 520, 48);
            text = UIKit.Text(root, "Text", "", 18, Theme.Paper, bold: true);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0;
        }

        public void Show(string message)
        {
            text.text = message;
            DOTween.Kill(group);
            group.alpha = 1;
            group.DOFade(0, 0.4f).SetDelay(1.6f).SetTarget(group);
            root.SetAsLastSibling();
        }
    }
}
