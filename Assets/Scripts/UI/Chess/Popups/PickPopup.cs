using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
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

        // 可收起的选择（宝库三选一）：点空白处收到右上角的小标签，点标签再展开
        private const float ChipW = 200, ChipH = 44, ChipRight = 1888, ChipY = 152, CollapseSeconds = 0.28f;
        private RectTransform content, chip;
        private Image catcher;
        private bool collapsed;
        private Sequence collapseSeq;

        public PickPopup(RectTransform layer, TooltipLayer tooltip)
        {
            root = UIKit.Rect(layer, "Pick");
            UIKit.Fill(root);
            this.tooltip = tooltip;
            root.gameObject.SetActive(false);
        }

        public bool Visible => UIFx.IsShown(root.gameObject);

        public void Hide()
        {
            KillCollapse();
            UIFx.Out(root.gameObject, UIFx.Style.Fade);
            shownKey = null;
            collapsed = false;
        }

        private void KillCollapse()
        {
            if (collapseSeq != null && collapseSeq.IsActive()) collapseSeq.Kill();
            collapseSeq = null;
        }

        private Vector2 ChipCenter => new Vector2(ChipRight - ChipW / 2, ChipY + ChipH / 2);

        /// <summary>收起：选择面板缩小飞向右上角并淡出，换成一个可点击的小标签。</summary>
        private void Collapse()
        {
            if (collapsed || content == null) return;
            collapsed = true;
            tooltip.Hide(null);
            KillCollapse();
            var group = UIKit.GetOrAdd<CanvasGroup>(content.gameObject);
            group.blocksRaycasts = false;
            catcher.raycastTarget = false;
            // content 铺满全屏、中心在 (960,540)；把中心移到标签处并缩小
            var to = new Vector2(ChipCenter.x - 960, 540 - ChipCenter.y);
            var q = DOTween.Sequence().SetLink(root.gameObject);
            q.Join(content.DOAnchorPos(to, CollapseSeconds).SetEase(Ease.InCubic));
            q.Join(content.DOScale(0.12f, CollapseSeconds).SetEase(Ease.InCubic));
            q.Join(group.DOFade(0, CollapseSeconds).SetEase(Ease.InQuad));
            q.OnComplete(() => content.gameObject.SetActive(false));
            collapseSeq = q;
            UIFx.In(chip.gameObject, UIFx.Style.Pop);
        }

        /// <summary>展开：从右上角标签处放大回原位。</summary>
        private void Expand()
        {
            if (!collapsed || content == null) return;
            collapsed = false;
            KillCollapse();
            UIFx.HideNow(chip.gameObject);
            content.gameObject.SetActive(true);
            var group = UIKit.GetOrAdd<CanvasGroup>(content.gameObject);
            catcher.raycastTarget = true;
            var q = DOTween.Sequence().SetLink(root.gameObject);
            q.Join(content.DOAnchorPos(Vector2.zero, CollapseSeconds).SetEase(Ease.OutCubic));
            q.Join(content.DOScale(1f, CollapseSeconds).SetEase(Ease.OutBack));
            q.Join(group.DOFade(1, CollapseSeconds * 0.8f));
            q.OnComplete(() => group.blocksRaycasts = true);
            collapseSeq = q;
        }

        private sealed class Option
        {
            public string Title, Tag, Body;
            public Color Ring;
            public string RelicKey; // 显示法宝图标
            public Action Click;
        }

        private void Show(string key, string title, string subtitle, List<Option> options, string skipLabel, Action skip, bool dim = false,
            string chipLabel = null)
        {
            if (shownKey == key && Visible) return;
            shownKey = key;
            KillCollapse();
            collapsed = false;
            UIKit.Clear(root);
            content = chip = null;
            catcher = null;
            UIFx.In(root.gameObject, UIFx.Style.Fade);
            tooltip.Hide(null);
            if (dim)
            {
                var d = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
                UIKit.FillScreen(d.rectTransform);
            }
            var parent = root;
            if (chipLabel != null)
            {
                // 透明接收层：点选项以外的地方就收起
                catcher = UIKit.Image(root, "Catcher", Theme.Clear, raycast: true);
                UIKit.FillScreen(catcher.rectTransform);
                catcher.gameObject.AddComponent<Button>().onClick.AddListener(Collapse);
                content = UIKit.Rect(root, "Content");
                UIKit.Fill(content);
                parent = content;
                var c = UIKit.Button(root, "Chip", Theme.WithAlpha(Theme.Paper, 0.95f), Theme.Gold, chipLabel, 18, Theme.Ink, Expand);
                chip = (RectTransform)c.transform;
                UIKit.Place(chip, ChipRight - ChipW, ChipY, ChipW, ChipH);
                chip.gameObject.SetActive(false);
            }
            var head = UIKit.Text(parent, "Title", title, 44, Theme.Ink, TextAlignmentOptions.Center, true);
            head.outlineWidth = 0.22f;
            head.outlineColor = Theme.Paper;
            UIKit.PlaceCenter(head.rectTransform, 960, 190, 1200, 60);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = UIKit.Panel(parent, "SubBg", Theme.WithAlpha(Theme.Paper, 0.88f), Theme.Clear, 0);
                UIKit.PlaceCenter(sub.rectTransform, 960, 236, 640, 30);
                UIKit.Text(sub.transform, "Sub", subtitle, 16, Theme.Ink);
            }
            float w = 320, gap = 40;
            float total = options.Count * w + (options.Count - 1) * gap;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                var card = UIKit.Panel(parent, "Option" + i, Theme.WithAlpha(Theme.Paper, 0.96f), o.Ring, 3f);
                card.raycastTarget = true;
                UIKit.Place(card.rectTransform, 960 - total / 2 + i * (w + gap), 280, w, 420);
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                var click = o.Click;
                b.onClick.AddListener(() => click());
                // 选项卡依次弹出
                card.rectTransform.DOScale(1f, 0.32f).From(0.82f).SetEase(Ease.OutBack).SetDelay(0.05f + i * 0.07f);
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
                var sb = UIKit.Button(parent, "Skip", Theme.WithAlpha(Theme.Paper, 0.92f), Theme.Ink, skipLabel, 17, Theme.Ink, skip);
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
                    Title = info?.Name, Body = info?.Desc, RelicKey = key, Tag = RelicIcon.SubTitle(info),
                    Ring = Theme.Rarity(info?.Rarity ?? 0), Click = () => PickVault(st, i, info),
                };
            }).ToList();
            Show("vault:" + st.Round + ":" + string.Join(",", st.VaultOffer), Fmt.T("chess.vault.title", "宝库 · 三选一"),
                Fmt.T("chess.vault.subtitle", "免费拿 1 件法宝 · 倒计时结束后随机选择"), opts, Fmt.T("chess.vault.skip", "放弃，换金币"), ChessActions.SkipVault,
                chipLabel: Fmt.T("chess.vault.chip", "宝库 · 点击展开"));
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
            UIFx.In(root.gameObject, UIFx.Style.Fade);
            var bg = UIKit.Panel(root, "WaitBg", Theme.WithAlpha(Theme.Paper, 0.9f), Theme.Ink, 1.5f);
            UIKit.PlaceCenter(bg.rectTransform, 960, 250, 520, 56);
            UIKit.Text(bg.transform, "Text", text, 20, Theme.Ink);
            bg.rectTransform.DOScale(1f, 0.28f).From(0.85f).SetEase(Ease.OutBack);
        }
    }
}
