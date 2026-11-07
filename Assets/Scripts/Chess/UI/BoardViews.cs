using System.Collections.Generic;
using System.Linq;
using RepGame.Protocol;
using UnityEngine;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 出阵区 + 手牌（备战席）。拖动或点击只生成“新的出阵顺序”发给服务器，
    /// 合法性由服务器判定，界面以服务器回来的状态为准。
    /// </summary>
    public sealed class BoardBenchView
    {
        public const float BoardY = 420, BoardW = 116, BoardH = 174, BoardGap = 12;
        public const float BenchY = 862, BenchW = 104, BenchH = 150, BenchGap = 12;

        private readonly RectTransform boardRoot;
        private readonly RectTransform benchRoot;
        private readonly DragController drag;
        private readonly TooltipLayer tooltip;
        private MatchState state;
        private int boardSlots;

        public BoardBenchView(RectTransform parent, DragController drag, TooltipLayer tooltip)
        {
            this.drag = drag;
            this.tooltip = tooltip;
            boardRoot = UIKit.Rect(parent, "Board");
            benchRoot = UIKit.Rect(parent, "Bench");
            var boardHit = UIKit.Image(boardRoot, "Hit", Theme.Clear, raycast: false);
            UIKit.Fill(boardHit.rectTransform, -10);
            drag.AddZone(boardRoot, p => p.CardUid != null, DropOnBoard, CanEdit);
            drag.AddZone(benchRoot, p => p.CardUid != null && p.FromBoard, (p, _) => Send(BoardUids().Where(u => u != p.CardUid)), CanEdit);
        }

        private bool CanEdit() => state != null && state.Phase == MatchPhase.Prep;

        private List<string> BoardUids() => state?.Self.Board.Select(c => c.Uid).ToList() ?? new List<string>();

        private static void Send(IEnumerable<string> board) => ChessActions.Arrange(board);

        public void Render(MatchState st)
        {
            state = st;
            tooltip.Hide(null);
            UIKit.Clear(boardRoot);
            UIKit.Clear(benchRoot);
            var self = st.Self;

            boardSlots = Mathf.Max(self.Slots, self.Board.Count);
            float bw = boardSlots * BoardW + (boardSlots - 1) * BoardGap;
            UIKit.Place(boardRoot, 960 - bw / 2, BoardY, bw, BoardH);
            for (int i = 0; i < boardSlots; i++)
            {
                RectTransform rt;
                if (i < self.Board.Count)
                {
                    var c = self.Board[i];
                    rt = CardView.Build(boardRoot, c, BoardW, BoardH);
                    Interact(rt, c, true);
                }
                else
                {
                    rt = CardView.Empty(boardRoot, BoardW, BoardH);
                }
                UIKit.Place(rt, i * (BoardW + BoardGap), 0, BoardW, BoardH);
            }

            var benchSlots = ChessSession.Instance.Config?.BenchSize ?? 8;
            benchSlots = Mathf.Max(benchSlots, self.Bench.Count);
            float nw = benchSlots * BenchW + (benchSlots - 1) * BenchGap;
            UIKit.Place(benchRoot, 960 - nw / 2, BenchY, nw, BenchH);
            for (int i = 0; i < benchSlots; i++)
            {
                RectTransform rt;
                if (i < self.Bench.Count)
                {
                    var c = self.Bench[i];
                    rt = CardView.Build(benchRoot, c, BenchW, BenchH);
                    Interact(rt, c, false);
                }
                else
                {
                    rt = CardView.Empty(benchRoot, BenchW, BenchH);
                }
                UIKit.Place(rt, i * (BenchW + BenchGap), 0, BenchW, BenchH);
            }
        }

        private void Interact(RectTransform rt, ChessCard c, bool onBoard)
        {
            var d = rt.gameObject.AddComponent<Draggable>();
            d.Controller = drag;
            d.CanDrag = CanEdit;
            d.Payload = () => new DragPayload { CardUid = c.Uid, FromBoard = onBoard };
            d.Clicked = () =>
            {
                if (!CanEdit()) return;
                var board = BoardUids();
                if (onBoard) Send(board.Where(u => u != c.Uid));
                else if (board.Count < state.Self.Slots) Send(board.Append(c.Uid));
            };
            UIKit.Hover(rt.gameObject, () =>
            {
                if (drag.Dragging) return;
                var text = CardView.Describe(c, out var head, out var sub);
                var p = rt.position; // world == canvas space for overlay; use design coords instead
                var x = onBoard ? rt.anchoredPosition.x + boardRoot.anchoredPosition.x : rt.anchoredPosition.x + benchRoot.anchoredPosition.x;
                var y = onBoard ? BoardY + BoardH + 10 : BenchY - 170;
                tooltip.Show(c, x - 60, y, 280, head, sub, text);
            }, () => tooltip.Hide(c));
        }

        private void DropOnBoard(DragPayload p, Vector2 local)
        {
            // local 是相对 boardRoot 轴心（左上角）的坐标
            int index = Mathf.Clamp(Mathf.FloorToInt(local.x / (BoardW + BoardGap)), 0, boardSlots - 1);
            var board = BoardUids();
            var from = board.IndexOf(p.CardUid);
            if (from >= 0)
            {
                board.RemoveAt(from);
                board.Insert(Mathf.Min(index, board.Count), p.CardUid);
            }
            else if (board.Count < state.Self.Slots)
            {
                board.Insert(Mathf.Min(index, board.Count), p.CardUid);
            }
            else
            {
                board[Mathf.Min(index, board.Count - 1)] = p.CardUid; // 满了：与该位置的卡互换
            }
            Send(board);
        }
    }

    /// <summary>我的法宝栏（只显示图标，悬停看效果，拖动调整顺序）。</summary>
    public sealed class RelicBarView
    {
        public const float Y = 620, Size = 60, Gap = 16;
        private readonly RectTransform root;
        private readonly DragController drag;
        private readonly TooltipLayer tooltip;
        private MatchState state;
        private int slots;

        public RelicBarView(RectTransform parent, DragController drag, TooltipLayer tooltip)
        {
            this.drag = drag;
            this.tooltip = tooltip;
            root = UIKit.Rect(parent, "Relics");
            drag.AddZone(root, p => p.RelicSlot >= 0, Reorder, () => state != null && state.Phase == MatchPhase.Prep);
        }

        public void Render(MatchState st)
        {
            state = st;
            UIKit.Clear(root);
            var relics = st.Self.Relics;
            slots = Mathf.Max(st.Self.RelicSlots, relics.Count);
            float w = slots * Size + (slots - 1) * Gap;
            UIKit.Place(root, 960 - w / 2, Y, w, Size);
            for (int i = 0; i < slots; i++)
            {
                RectTransform rt;
                if (i < relics.Count)
                {
                    var slot = i;
                    var r = relics[i];
                    rt = RelicIcon.Build(root, r.RelicKey, Size);
                    var d = rt.gameObject.AddComponent<Draggable>();
                    d.Controller = drag;
                    d.CanDrag = () => state.Phase == MatchPhase.Prep;
                    d.Payload = () => new DragPayload { RelicSlot = slot };
                    var x = 960 - w / 2 + i * (Size + Gap);
                    UIKit.Hover(rt.gameObject, () => { if (!drag.Dragging) RelicIcon.ShowTip(tooltip, r, x - 110, Y + Size + 10); }, () => tooltip.Hide(r));
                }
                else
                {
                    rt = UIKit.Circle(root, "EmptyRelic", Theme.Clear, Theme.WithAlpha(Theme.Ink, 0.3f), 2f).rectTransform;
                }
                UIKit.Place(rt, i * (Size + Gap), 0, Size, Size);
            }
        }

        private void Reorder(DragPayload p, Vector2 local)
        {
            int n = state.Self.Relics.Count;
            int to = Mathf.Clamp(Mathf.FloorToInt(local.x / (Size + Gap)), 0, n - 1);
            var order = Enumerable.Range(0, n).ToList();
            order.Remove(p.RelicSlot);
            order.Insert(to, p.RelicSlot);
            ChessActions.Arrange(state.Self.Board.Select(c => c.Uid), order);
        }
    }

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
