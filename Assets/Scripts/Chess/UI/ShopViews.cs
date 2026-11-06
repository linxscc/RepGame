using DG.Tweening;
using RepGame.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 右下角：金币按钮（即商店入口，也是出售区）与就绪按钮；商店抽屉从底部弹出。
    /// </summary>
    public sealed class ShopView
    {
        private const float DrawerX = 160, DrawerW = 1600, DrawerH = 360;

        private readonly RectTransform goldButton;
        private readonly TextMeshProUGUI goldText;
        private readonly TextMeshProUGUI interestText;
        private readonly Button readyButton;
        private readonly TextMeshProUGUI readyLabel;
        private readonly RectTransform dim;
        private readonly RectTransform drawer;
        private readonly RectTransform cardsRow;
        private readonly TextMeshProUGUI drawerGold;
        private readonly TextMeshProUGUI drawerInfo;
        private readonly TextMeshProUGUI refreshLabel;
        private readonly TextMeshProUGUI lockLabel;
        private readonly TextMeshProUGUI xpLabel;
        private readonly TooltipLayer tooltip;
        private readonly PickPopup picker;
        private MatchState state;
        private bool open;

        public ShopView(RectTransform baseLayer, RectTransform popupLayer, DragController drag, TooltipLayer tooltip, PickPopup picker)
        {
            this.tooltip = tooltip;
            this.picker = picker;

            // 金币按钮
            var gold = UIKit.Circle(baseLayer, "GoldButton", Theme.Paper, Theme.Gold, 3f);
            UIKit.PlaceCenter(gold.rectTransform, 1918 - 40 - 62, 1080 - 36 - 58 - 14 - 62, 124, 124);
            gold.raycastTarget = true;
            var goldBtn = gold.gameObject.AddComponent<Button>();
            goldBtn.targetGraphic = gold;
            goldBtn.onClick.AddListener(() => SetOpen(!open));
            goldButton = gold.rectTransform;
            goldText = UIKit.Text(gold.transform, "Gold", "", 40, Theme.GoldText, TextAlignmentOptions.Center, true);
            goldText.rectTransform.offsetMin = new Vector2(0, 18);
            interestText = UIKit.Text(gold.transform, "Interest", "", 14, Theme.GoldText, TextAlignmentOptions.Bottom);
            interestText.rectTransform.offsetMin = new Vector2(0, 22);
            drag.AddZone(goldButton, p => p.CardUid != null || p.RelicSlot >= 0, (p, _) => Sell(p), () => state?.Phase == MatchPhase.Prep);

            // 就绪
            readyButton = UIKit.Button(baseLayer, "Ready", Theme.Red, Theme.Ink, "", 24, Theme.Paper, ToggleReady);
            UIKit.Place((RectTransform)readyButton.transform, 1920 - 40 - 150, 1080 - 36 - 58, 150, 58);
            readyLabel = UIKit.Text(readyButton.transform, "Label", "", 24, Theme.Paper, bold: true);

            // 抽屉（第二层）
            var dimImg = UIKit.Image(popupLayer, "ShopDim", Theme.Dim, raycast: true);
            UIKit.Fill(dimImg.rectTransform);
            dimImg.gameObject.AddComponent<Button>().onClick.AddListener(() => SetOpen(false));
            dim = dimImg.rectTransform;

            var panel = UIKit.Panel(popupLayer, "ShopDrawer", Theme.Paper, Theme.Ink, 2.5f);
            panel.raycastTarget = true;
            drawer = panel.rectTransform;
            UIKit.Place(drawer, DrawerX, 1080 - DrawerH, DrawerW, DrawerH + 20);
            drag.AddZone(drawer, p => p.CardUid != null || p.RelicSlot >= 0, (p, _) => Sell(p), () => open && state?.Phase == MatchPhase.Prep);

            var title = UIKit.Text(drawer, "Title", Fmt.T("chess.shop.title", "商店"), 26, Theme.Ink, TextAlignmentOptions.TopLeft, true);
            UIKit.Place(title.rectTransform, 28, 20, 200, 40);
            drawerInfo = UIKit.Text(drawer, "Info", "", 14, Theme.InkSoft, TextAlignmentOptions.Left);
            UIKit.Place(drawerInfo.rectTransform, 120, 26, 900, 24);
            drawerGold = UIKit.Text(drawer, "Gold", "", 24, Theme.GoldText, TextAlignmentOptions.Right, true);
            UIKit.Place(drawerGold.rectTransform, DrawerW - 260, 20, 160, 36);
            var close = UIKit.Button(drawer, "Close", Theme.Paper, Theme.Ink, "×", 22, Theme.Ink, () => SetOpen(false));
            UIKit.Place((RectTransform)close.transform, DrawerW - 72, 16, 44, 44);

            cardsRow = UIKit.Rect(drawer, "Cards");
            UIKit.Place(cardsRow, 28, 76, 1200, 260);

            var refresh = UIKit.Button(drawer, "Refresh", Theme.Red, Theme.Ink, "", 18, Theme.Paper, ChessActions.Refresh);
            UIKit.Place((RectTransform)refresh.transform, DrawerW - 218, 86, 190, 58);
            refreshLabel = UIKit.Text(refresh.transform, "Label", "", 18, Theme.Paper, bold: true);
            var xp = UIKit.Button(drawer, "Xp", Theme.Paper, Theme.Ink, "", 18, Theme.Ink, ChessActions.BuyXp);
            UIKit.Place((RectTransform)xp.transform, DrawerW - 218, 156, 190, 58);
            xpLabel = UIKit.Text(xp.transform, "Label", "", 18, Theme.Ink, bold: true);
            var lockB = UIKit.Button(drawer, "Lock", Theme.Cyan, Theme.Ink, "", 18, Theme.Ink, () => ChessActions.Lock(!(state?.Self.ShopLocked ?? false)));
            UIKit.Place((RectTransform)lockB.transform, DrawerW - 218, 226, 190, 58);
            lockLabel = UIKit.Text(lockB.transform, "Label", "", 18, Theme.Ink, bold: true);

            dim.gameObject.SetActive(false);
            drawer.gameObject.SetActive(false);
        }

        private void Sell(DragPayload p)
        {
            if (p.CardUid != null) ChessActions.SellCard(p.CardUid);
            else if (p.RelicSlot >= 0) ChessActions.SellRelic(p.RelicSlot);
        }

        private void ToggleReady()
        {
            if (state == null) return;
            if (state.Phase == MatchPhase.Prep) ChessActions.Ready(!state.Self.Ready);
        }

        public void SetOpen(bool value)
        {
            if (value && state?.Phase != MatchPhase.Prep) value = false;
            if (open == value) return;
            open = value;
            tooltip.Hide(null);
            DOTween.Kill(drawer);
            if (open)
            {
                UIFx.In(dim.gameObject, UIFx.Style.Fade);
                drawer.gameObject.SetActive(true);
                drawer.anchoredPosition = new Vector2(DrawerX, -1080);
                drawer.DOAnchorPos(new Vector2(DrawerX, -(1080 - DrawerH)), 0.25f).SetEase(Ease.OutCubic).SetTarget(drawer);
            }
            else
            {
                UIFx.Out(dim.gameObject, UIFx.Style.Fade);
                drawer.DOAnchorPos(new Vector2(DrawerX, -1080), 0.2f).SetEase(Ease.InCubic).SetTarget(drawer)
                    .OnComplete(() => drawer.gameObject.SetActive(false));
            }
        }

        public void Render(MatchState st)
        {
            state = st;
            var self = st.Self;
            var cfg = ChessSession.Instance.Config;
            var prep = st.Phase == MatchPhase.Prep;
            goldText.text = self.Gold.ToString();
            interestText.text = self.Interest > 0 ? $"利息 +{self.Interest}" : "金";
            UIFx.Toggle(goldButton.gameObject, prep || st.Phase == MatchPhase.Vault);
            UIFx.Toggle(readyButton.gameObject, prep);
            readyLabel.text = self.Ready ? Fmt.T("chess.ready.cancel", "取消就绪") : Fmt.T("chess.ready", "就 绪");
            ((Image)readyButton.targetGraphic).transform.GetChild(0).GetComponent<Image>().color = self.Ready ? Theme.InkSoft : Theme.Red;
            if (!prep) SetOpen(false);

            drawerGold.text = $"{self.Gold} 金";
            drawerInfo.text = $"{self.Level} 级 · 拖卡或法宝到这里出售";
            refreshLabel.text = self.FreeRefreshes > 0 ? $"刷新 · 免费 ×{self.FreeRefreshes}" : $"刷新 · {cfg?.RefreshCost ?? 2} 金";
            xpLabel.text = self.XpToNext == 0 ? "已满级" : $"经验 · {cfg?.XpCost ?? 4} 金";
            lockLabel.text = self.ShopLocked ? "已锁定" : "锁定";

            UIKit.Clear(cardsRow);
            float x = 0;
            for (int i = 0; i < self.Shop.Count; i++)
            {
                var slot = self.Shop[i];
                var index = i;
                RectTransform rt;
                const float cardH = 222;
                if (string.IsNullOrEmpty(slot.LineKey))
                {
                    rt = CardView.Empty(cardsRow, 170, cardH);
                }
                else
                {
                    var line = ChessSession.Instance.Line(slot.LineKey);
                    var preview = new ChessCard { Name = line.StarNames[0], LineKey = slot.LineKey, Star = 1, Points = line.Points, SellPrice = slot.Cost };
                    rt = CardView.Build(cardsRow, preview, 170, cardH, true, slot.Cost); // 价格在卡面左上角
                    if (slot.Owned > 0)
                    {
                        var own = UIKit.Text(cardsRow, "Owned", $"已有 {slot.Owned}", 15, Theme.CyanDeep, TextAlignmentOptions.Center, true);
                        UIKit.Place(own.rectTransform, x, cardH + 4, 170, 24);
                    }
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = rt.GetComponent<Image>();
                    btn.onClick.AddListener(() => ChessActions.BuyCard(index));
                    var px = DrawerX + 28 + x;
                    UIKit.Hover(rt.gameObject, () =>
                    {
                        var text = CardView.Describe(preview, out var head, out var sub);
                        tooltip.Show(preview, px, 1080 - DrawerH - 150, 280, head, sub, text);
                    }, () => tooltip.Hide(preview));
                }
                UIKit.Place(rt, x, 0, 170, cardH);
                x += 186;
            }

            // 法宝位
            x += 20;
            var key = self.ShopRelicKey;
            var relicSlot = UIKit.Panel(cardsRow, "RelicSlot", string.IsNullOrEmpty(key) ? Theme.WithAlpha(Theme.Paper, 0.4f) : Theme.GoldPaper,
                string.IsNullOrEmpty(key) ? Theme.WithAlpha(Theme.Ink, 0.3f) : Theme.Rarity(ChessSession.Instance.Relic(key)?.Rarity ?? 0), 2.5f);
            UIKit.Place(relicSlot.rectTransform, x, 0, 200, 250);
            if (!string.IsNullOrEmpty(key))
            {
                var info = ChessSession.Instance.Relic(key);
                var price = ChessSession.Instance.RelicPrice(key);
                var head = UIKit.Text(relicSlot.transform, "Price", $"{price} 金 · {ChessSession.Instance.Rarity(info.Rarity)?.Name}", 14, Theme.GoldText, TextAlignmentOptions.Top, true);
                head.rectTransform.offsetMax = new Vector2(0, -12);
                var icon = RelicIcon.Build(relicSlot.transform, key, 88);
                UIKit.PlaceCenter(icon, 100, 112, 88, 88);
                var name = UIKit.Text(relicSlot.transform, "Name", info.Name, 19, Theme.Ink, TextAlignmentOptions.Bottom, true);
                name.rectTransform.offsetMin = new Vector2(0, 18);
                relicSlot.raycastTarget = true;
                var btn = relicSlot.gameObject.AddComponent<Button>();
                btn.targetGraphic = relicSlot;
                btn.onClick.AddListener(() =>
                {
                    if (info.NeedsTradeChoice) picker.ChooseTrade(ChessActions.BuyRelic);
                    else ChessActions.BuyRelic(-1);
                });
                var px = DrawerX + 28 + x;
                UIKit.Hover(relicSlot.gameObject, () => RelicIcon.ShowInfoTip(tooltip, relicSlot, key, px - 40, 1080 - DrawerH - 140), () => tooltip.Hide(relicSlot));
            }
        }
    }
}
