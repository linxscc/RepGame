using DG.Tweening;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 右下角：金币按钮（即商店入口，也是出售区）与就绪按钮；商店抽屉从顶部落下（不挡手牌）。
    /// </summary>
    public sealed class ShopView
    {
        // 抽屉宽度为原来的 2/3，水平居中；只卖卡（法宝只能从宝库获得）
        private const float DrawerW = 1330, DrawerX = (1920 - DrawerW) / 2, DrawerH = 400;
        private const float CardW = 200, CardH = 261, CardStep = 214; // 放大约 1/3
        private const float ButtonW = 180;
        private const float Pad = 20; // 抽屉上沿藏在屏幕外的部分，内容整体下移这么多

        private readonly RectTransform goldButton;
        private readonly TextMeshProUGUI goldText;
        private readonly TextMeshProUGUI interestText;
        private readonly Button readyButton;
        private readonly TextMeshProUGUI readyLabel;
        private readonly RectTransform dim;
        private readonly RectTransform drawer;
        private readonly RectTransform cardsRow;
        private readonly TextMeshProUGUI refreshLabel;
        private readonly TextMeshProUGUI lockLabel;
        private readonly TextMeshProUGUI xpLabel;
        private readonly TooltipLayer tooltip;
        private MatchState state;
        private bool open;

        public ShopView(RectTransform baseLayer, RectTransform popupLayer, DragController drag, TooltipLayer tooltip)
        {
            this.tooltip = tooltip;

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
            UIKit.FillScreen(dimImg.rectTransform);
            dimImg.gameObject.AddComponent<Button>().onClick.AddListener(() => SetOpen(false));
            dim = dimImg.rectTransform;

            var panel = UIKit.Panel(popupLayer, "ShopDrawer", Theme.Paper, Theme.Ink, 2.5f);
            panel.raycastTarget = true;
            drawer = panel.rectTransform;
            UIKit.Place(drawer, DrawerX, -Pad, DrawerW, DrawerH + Pad);
            // 平板、折叠屏上 Stage 上方还有一条背景：抽屉向上延伸盖住它，看起来仍是从屏幕顶端落下
            var bleed = UIKit.Panel(drawer, "TopBleed", Theme.Paper, Theme.Ink, 2.5f);
            UIKit.Place(bleed.rectTransform, 0, -BleedH, DrawerW, BleedH + Pad + 40);
            bleed.raycastTarget = true; // 点在延伸部分不算点空白
            bleed.rectTransform.SetAsFirstSibling();
            drag.AddZone(drawer, p => p.CardUid != null || p.RelicSlot >= 0, (p, _) => Sell(p), () => open && state?.Phase == MatchPhase.Prep);

            var title = UIKit.Text(drawer, "Title", Fmt.T("chess.shop.title", "商店"), 26, Theme.Ink, TextAlignmentOptions.TopLeft, true);
            UIKit.Place(title.rectTransform, 28, Pad + 20, 200, 40);
            var close = UIKit.Button(drawer, "Close", Theme.Paper, Theme.Ink, "×", 22, Theme.Ink, () => SetOpen(false));
            UIKit.Place((RectTransform)close.transform, DrawerW - 72, Pad + 16, 44, 44);

            cardsRow = UIKit.Rect(drawer, "Cards");
            UIKit.Place(cardsRow, 28, Pad + 80, CardStep * 5, CardH + 30);

            var refresh = UIKit.Button(drawer, "Refresh", Theme.Red, Theme.Ink, "", 18, Theme.Paper, ChessActions.Refresh);
            UIKit.Place((RectTransform)refresh.transform, DrawerW - ButtonW - 28, Pad + 86, ButtonW, 56);
            refreshLabel = UIKit.Text(refresh.transform, "Label", "", 18, Theme.Paper, bold: true);
            var xp = UIKit.Button(drawer, "Xp", Theme.Paper, Theme.Ink, "", 18, Theme.Ink, ChessActions.BuyXp);
            UIKit.Place((RectTransform)xp.transform, DrawerW - ButtonW - 28, Pad + 154, ButtonW, 56);
            xpLabel = UIKit.Text(xp.transform, "Label", "", 18, Theme.Ink, bold: true);
            var lockB = UIKit.Button(drawer, "Lock", Theme.Cyan, Theme.Ink, "", 18, Theme.Ink, () => ChessActions.Lock(!(state?.Self.ShopLocked ?? false)));
            UIKit.Place((RectTransform)lockB.transform, DrawerW - ButtonW - 28, Pad + 222, ButtonW, 56);
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

        /// <summary>抽屉打开时刷新按钮（主按钮是抽屉里的刷新）收起，避免两个红色主按钮竞争。</summary>
        private void UpdateReady() => UIFx.Toggle(readyButton.gameObject, state != null && state.Phase == MatchPhase.Prep && !open);

        private const float BleedH = 600;
        private static readonly Vector3[] corners = new Vector3[4];

        /// <summary>收起时整个抽屉在屏幕（不只是 Stage）上沿之外。</summary>
        private float Hidden()
        {
            float band = 0;
            var parent = drawer.parent as RectTransform;
            var canvas = drawer.GetComponentInParent<Canvas>(true);
            if (parent != null && canvas != null)
            {
                ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
                band = Mathf.Max(0, parent.InverseTransformPoint(corners[1]).y - parent.rect.yMax);
            }
            return DrawerH + Pad + 20 + band;
        }

        public void SetOpen(bool value)
        {
            if (value && state?.Phase != MatchPhase.Prep) value = false;
            if (open == value) return;
            open = value;
            UpdateReady();
            tooltip.Hide(null);
            DOTween.Kill(drawer);
            if (open)
            {
                UIFx.In(dim.gameObject, UIFx.Style.Fade);
                drawer.gameObject.SetActive(true);
                drawer.anchoredPosition = new Vector2(DrawerX, Hidden());
                drawer.DOAnchorPos(new Vector2(DrawerX, Pad), 0.25f).SetEase(Ease.OutCubic).SetTarget(drawer);
            }
            else
            {
                UIFx.Out(dim.gameObject, UIFx.Style.Fade);
                drawer.DOAnchorPos(new Vector2(DrawerX, Hidden()), 0.2f).SetEase(Ease.InCubic).SetTarget(drawer)
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
            UpdateReady();
            readyLabel.text = self.Ready ? Fmt.T("chess.ready.cancel", "取消就绪") : Fmt.T("chess.ready", "就 绪");
            ((Image)readyButton.targetGraphic).transform.GetChild(0).GetComponent<Image>().color = self.Ready ? Theme.InkSoft : Theme.Red;
            if (!prep) SetOpen(false);
            UpdateReady();

            refreshLabel.text = self.FreeRefreshes > 0 ? $"刷新 · 免费 ×{self.FreeRefreshes}" : $"刷新 · {(self.RefreshCost > 0 ? self.RefreshCost : cfg?.RefreshCost ?? 2)} 金";
            xpLabel.text = self.XpToNext == 0 ? "已满级" : $"经验 · {cfg?.XpCost ?? 4} 金";
            lockLabel.text = self.ShopLocked ? "已锁定" : "锁定";

            UIKit.Clear(cardsRow);
            float x = 0;
            for (int i = 0; i < self.Shop.Count; i++)
            {
                var slot = self.Shop[i];
                var index = i;
                RectTransform rt;
                if (string.IsNullOrEmpty(slot.LineKey))
                {
                    rt = CardView.Empty(cardsRow, CardW, CardH);
                }
                else
                {
                    var line = ChessSession.Instance.Line(slot.LineKey);
                    var preview = new ChessCard { Name = line.StarNames[0], LineKey = slot.LineKey, Star = 1, Points = line.Points, SellPrice = slot.Cost, Color = slot.Color };
                    rt = CardView.Build(cardsRow, preview, CardW, CardH);
                    // 卡牌下方：金币 + 价格（左），已拥有 / 可合成（右）
                    var coin = UIKit.Circle(cardsRow, "Coin", Theme.GoldPaper, Theme.Gold, 2f);
                    UIKit.Place(coin.rectTransform, x + 6, CardH + 5, 24, 24);
                    UIKit.Text(coin.transform, "G", "金", 12, Theme.GoldText, TextAlignmentOptions.Center, true);
                    var price = UIKit.Text(cardsRow, "Price", slot.Cost.ToString(), 20, Theme.GoldText, TextAlignmentOptions.Left, true);
                    UIKit.Place(price.rectTransform, x + 36, CardH + 3, 50, 28);
                    // 已有 2 张同系 1★：买下会合成 → 金色高亮「可合成」；否则手里有这张卡（任意星级）→「已拥有」
                    if (slot.Owned >= 2)
                    {
                        var tag = UIKit.Panel(cardsRow, "Mergeable", Theme.GoldPaper, Theme.Gold, 2f);
                        UIKit.Place(tag.rectTransform, x + CardW - 100, CardH + 4, 100, 26);
                        var mt = UIKit.Text(tag.transform, "Text", Fmt.T("chess.shop.mergeable", "可合成"), 16, Theme.GoldText, TextAlignmentOptions.Center, true);
                        mt.gameObject.AddComponent<ShimmerText>();
                    }
                    else if (self.Board.Any(c => c.LineKey == slot.LineKey) || self.Bench.Any(c => c.LineKey == slot.LineKey))
                    {
                        var own = UIKit.Text(cardsRow, "Owned", Fmt.T("chess.shop.owned", "已拥有"), 15, Theme.InkSoft, TextAlignmentOptions.Right);
                        UIKit.Place(own.rectTransform, x + CardW - 110, CardH + 4, 104, 26);
                    }
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = rt.GetComponent<Image>();
                    btn.onClick.AddListener(() => ChessActions.BuyCard(index));
                    var px = DrawerX + 28 + x;
                    UIKit.Hover(rt.gameObject, () =>
                    {
                        var text = CardView.Describe(preview, out var head, out var sub);
                        tooltip.Show(preview, px, DrawerH + 12, 280, head, sub, text); // 抽屉下方
                    }, () => tooltip.Hide(preview));
                }
                UIKit.Place(rt, x, 0, CardW, CardH);
                x += CardStep;
            }
        }
    }
}
