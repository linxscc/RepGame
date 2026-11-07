using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>我的法宝栏（只显示图标，悬停看效果，拖动调整顺序）。</summary>
    public sealed class RelicBarView
    {
        public const float Y = 662, Size = 60, Gap = 16; // 出阵卡放大后下移，避免重叠
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
}
