using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>点击对手头像：上回合阵容与得分。</summary>
    public sealed class OpponentPopover
    {
        private readonly RectTransform root;

        private readonly RectTransform catcher;

        public OpponentPopover(RectTransform layer)
        {
            // 点击空白处关闭：全屏透明按钮，位于面板之下
            var c = UIKit.Image(layer, "OpponentCatcher", Theme.Clear, raycast: true);
            UIKit.FillScreen(c.rectTransform);
            var cb = c.gameObject.AddComponent<Button>();
            cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(Close);
            catcher = c.rectTransform;
            catcher.gameObject.SetActive(false);
            root = UIKit.Rect(layer, "OpponentBoard");
            root.gameObject.SetActive(false);
        }

        public void Hide() => Close();

        private void Close()
        {
            catcher.gameObject.SetActive(false);
            UIFx.Out(root.gameObject);
        }

        private void Ensure()
        {
            catcher.gameObject.SetActive(true);
            catcher.SetAsLastSibling();
            root.SetAsLastSibling();
        }

        public void Toggle(MatchState st)
        {
            if (UIFx.IsShown(root.gameObject) || st == null)
            {
                Close();
                return;
            }
            UIKit.Clear(root);
            var o = st.Opponent;
            Ensure();
            int n = Mathf.Max(1, o.LastBoard.Count);
            float w = 40 + n * 84;
            UIKit.Place(root, 32, 100, Mathf.Max(w, 360), 190);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2f);
            UIKit.Fill(panel.rectTransform);
            panel.raycastTarget = true;
            panel.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var title = UIKit.Text(root, "Title", $"{o.Username} · 上回合得分 {Fmt.Score(o.LastScore)} · {Fmt.T("chess.opponent.hand", "手牌 {0}", o.CardCount)}", 16, Theme.Ink, TextAlignmentOptions.TopLeft, true);
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
            UIFx.In(root.gameObject);
        }
    }
}
