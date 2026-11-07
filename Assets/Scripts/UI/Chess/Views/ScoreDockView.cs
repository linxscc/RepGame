using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>出阵区上方的预计得分：固定位置，出现/消失时淡入淡出，分数变化时数字滚动到新值。</summary>
    public sealed class ScoreDockView
    {
        private readonly RectTransform root;
        private readonly TextMeshProUGUI score;
        private readonly TextMeshProUGUI formula;
        private readonly CanvasGroup group;
        private static readonly Vector2 Dock = new Vector2(960, 352);
        private double shown = -1;
        private float rolling; // 正在显示的数字（滚动动画用）

        public ScoreDockView(RectTransform parent)
        {
            root = UIKit.Rect(parent, "ScoreDock");
            UIKit.PlaceCenter(root, Dock.x, Dock.y, 520, 100);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            score = UIKit.Text(root, "Score", "", 56, Theme.Red, TextAlignmentOptions.Top, true);
            score.outlineWidth = 0.18f;
            score.outlineColor = Theme.Paper;
            var f = UIKit.Rect(root, "FormulaBox");
            f.anchorMin = new Vector2(0.5f, 0);
            f.anchorMax = new Vector2(0.5f, 0);
            f.pivot = new Vector2(0.5f, 0);
            f.sizeDelta = new Vector2(320, 26);
            var bg = UIKit.Panel(f, "Bg", Theme.WithAlpha(Theme.Paper, 0.92f), Theme.Ink, 1.5f);
            UIKit.Fill(bg.rectTransform);
            formula = UIKit.Text(f, "Formula", "", 15, Theme.Ink);
        }

        public void Render(ScoreBreakdown preview, bool visible)
        {
            if (!(visible && preview != null && preview.Score > 0))
            {
                    UIFx.Out(root.gameObject, UIFx.Style.Fade);
                shown = -1;
                return;
            }
            formula.text = $"战力 {Fmt.Num(preview.Points)} × 气势 {Fmt.Num(preview.Mult)}";
            if (!UIFx.IsShown(root.gameObject))
            {
                // 刚出现：直接显示最终值并淡入
                DOTween.Kill(score);
                shown = preview.Score;
                rolling = (float)preview.Score;
                score.text = Fmt.Score(preview.Score);
                UIFx.In(root.gameObject, UIFx.Style.Fade);
                return;
            }
            if (preview.Score == shown) return;
            shown = preview.Score;
            DOTween.Kill(score);
            DOTween.To(() => rolling, v => { rolling = v; score.text = Fmt.Score(v); }, (float)preview.Score, 0.35f)
                .SetEase(Ease.OutCubic).SetTarget(score);
        }
    }
}
