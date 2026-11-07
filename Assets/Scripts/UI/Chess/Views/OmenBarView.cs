using System.Text;
using TMPro;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>顶部计时器右侧的天象栏：每条生效天象一个圆角块，悬停看说明。天象骰子动画（Task 19）会飞入这里的槽位。</summary>
    public sealed class OmenBarView
    {
        private const float X = 1010, Y = 28, W = 380, H = 52, BlockW = 170, BlockH = 48, Gap = 10;
        private readonly RectTransform root;
        private readonly TooltipLayer tooltip;
        private string signature = "";

        public OmenBarView(RectTransform parent, TooltipLayer tooltip)
        {
            this.tooltip = tooltip;
            root = UIKit.Rect(parent, "Omens");
            UIKit.Place(root, X, Y, W, H);
        }

        /// <summary>第 index 个天象槽位中心（设计稿坐标，左上原点），供骰子落点动画使用。</summary>
        public Vector2 SlotCenter(int index) =>
            new Vector2(X + index * (BlockW + Gap) + BlockW / 2, Y + BlockH / 2);

        public void Render(MatchState st)
        {
            var s = ChessSession.Instance;
            var sb = new StringBuilder();
            foreach (var o in st.Omens) sb.Append(o.Key).Append(':').Append(o.RoundsLeft).Append('|');
            var sig = sb.ToString();
            if (sig == signature) return;
            signature = sig;

            tooltip.Hide(null);
            UIKit.Clear(root);
            for (int i = 0; i < st.Omens.Count; i++)
            {
                var active = st.Omens[i];
                var info = s.Omen(active.Key);
                var name = info != null ? info.Name : active.Key;
                var panel = UIKit.Panel(root, "Omen_" + active.Key, Theme.Paper, Theme.Ink, 2f);
                UIKit.Place(panel.rectTransform, i * (BlockW + Gap), 0, BlockW, BlockH);
                var title = UIKit.Text(panel.transform, "Name", name, 20, Theme.Ink, bold: true);
                UIKit.Place(title.rectTransform, 0, 2, BlockW, 26);
                var left = UIKit.Text(panel.transform, "Left", Fmt.T("chess.omen.left", "剩 {0} 回合", active.RoundsLeft), 14, Theme.InkSoft);
                UIKit.Place(left.rectTransform, 0, 28, BlockW, 18);
                if (active.RoundsLeft == 1)
                {
                    title.alpha = left.alpha = 0.6f;
                    panel.color = Theme.WithAlpha(panel.color, 0.6f);
                }
                var captured = active;
                var bx = X + i * (BlockW + Gap);
                UIKit.Hover(panel.gameObject, () =>
                {
                    var aspect = info != null ? info.Aspect : "";
                    tooltip.Show(captured, bx - 20, Y + H + 8, 320, name,
                        $"{aspect} · {Fmt.T("chess.omen.left", "剩 {0} 回合", captured.RoundsLeft)}",
                        info != null ? info.Desc : "");
                }, () => tooltip.Hide(captured));
                panel.gameObject.SetActive(false); // 先隐藏再 In，才会播放弹出动画
                UIFx.In(panel.gameObject, UIFx.Style.Pop);
            }
        }
    }
}
