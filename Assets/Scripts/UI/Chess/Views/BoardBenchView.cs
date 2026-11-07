using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 出阵区 + 手牌（备战席）。拖动或点击只生成“新的出阵顺序”发给服务器，
    /// 合法性由服务器判定，界面以服务器回来的状态为准。
    /// </summary>
    public sealed class BoardBenchView
    {
        // 卡牌尺寸整体放大约 1/3（原 116×174 / 104×150）
        public const float BoardY = 410, BoardW = 155, BoardH = 232, BoardGap = 14;
        public const float BenchY = 860, BenchW = 139, BenchH = 200, BenchGap = 14;

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

            var benchSlots = self.BenchSize > 0 ? self.BenchSize : ChessSession.Instance.Config?.BenchSize ?? 8;
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
}
