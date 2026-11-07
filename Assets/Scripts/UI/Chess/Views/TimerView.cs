using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
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
            caption = UIKit.Live(UIKit.Text(cap, "Text", "", 15, Theme.InkSoft), 16); // 最长「第 12 回合 · 阶段伤害 10」，260 宽
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
                MatchPhase.Omen => Fmt.T("chess.phase.omen", "第 {0} 回合 · 天象", st.Round),
                MatchPhase.Settle => Fmt.T("chess.phase.settle", "第 {0} 回合 · 结算", st.Round),
                _ => Fmt.T("chess.phase.prep", "第 {0} 回合 · 阶段伤害 {1}", st.Round, st.StageDamage),
            };
        }
    }
}
