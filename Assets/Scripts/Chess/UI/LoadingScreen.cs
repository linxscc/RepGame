using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 进入对局 / 断线重连时的加载界面：盖住整个屏幕，显示进度条和当前步骤，
    /// 等对局配置、对局数据、卡牌资源和界面都准备好后再淡出，避免看到半成品界面。
    /// 只负责显示；进度由 ChessBattleScreen 推进。
    /// </summary>
    public sealed class LoadingScreen
    {
        private readonly GameObject root;
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI title, step, percent;
        private readonly RectTransform fill;
        private float progress;

        public bool Active { get; private set; }

        public LoadingScreen(Transform owner)
        {
            root = new GameObject("LoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(owner, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // 在对局界面（50）和匹配浮条（60）之上
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            group = root.AddComponent<CanvasGroup>();

            var skin = ChessUISkin.Instance;
            var bg = UIKit.Image(root.transform, "Background", Theme.Water, raycast: true); // 挡住下面的点击
            UIKit.Fill(bg.rectTransform);
            if (skin.voidMist != null)
            {
                var mist = UIKit.Image(root.transform, "Mist", Theme.WithAlpha(Color.white, 0.35f), skin.voidMist);
                UIKit.Fill(mist.rectTransform, -200);
            }

            var stage = UIKit.Rect(root.transform, "Stage");
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(1920, 1080);

            title = UIKit.Text(stage, "Title", "", 40, Theme.Ink, TextAlignmentOptions.Center, true);
            UIKit.PlaceCenter(title.rectTransform, 960, 470, 900, 56);

            var bar = UIKit.Panel(stage, "Bar", Theme.Paper, Theme.Ink, 2f);
            UIKit.PlaceCenter(bar.rectTransform, 960, 540, 640, 18);
            var f = UIKit.Image(bar.transform.GetChild(0), "Fill", Theme.Red);
            fill = f.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.pivot = new Vector2(0, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            step = UIKit.Text(stage, "Step", "", 20, Theme.InkSoft, TextAlignmentOptions.Left);
            UIKit.Place(step.rectTransform, 640, 562, 520, 30);
            percent = UIKit.Text(stage, "Percent", "", 20, Theme.InkSoft, TextAlignmentOptions.Right);
            UIKit.Place(percent.rectTransform, 1160, 562, 120, 30);

            root.SetActive(false);
        }

        /// <summary>打开（已打开时只更新标题）。</summary>
        public void Begin(string heading)
        {
            title.text = heading;
            if (Active) return;
            Active = true;
            DOTween.Kill(group);
            root.SetActive(true);
            group.alpha = 1;
            SetProgress(0, "");
        }

        /// <summary>推进到 value（0~1，只增不减）。</summary>
        public void SetProgress(float value, string text)
        {
            if (!Active) return;
            if (!string.IsNullOrEmpty(text)) step.text = text;
            value = Mathf.Clamp01(value);
            if (value < progress && value > 0) return;
            progress = value;
            DOTween.Kill(fill);
            fill.DOAnchorMax(new Vector2(value, 1), 0.25f).SetEase(Ease.OutCubic).SetTarget(fill)
                .OnUpdate(() => percent.text = Mathf.RoundToInt(fill.anchorMax.x * 100) + "%");
            if (value == 0)
            {
                fill.anchorMax = new Vector2(0, 1);
                percent.text = "0%";
            }
        }

        /// <summary>全部就绪：进度走满，稍停后淡出。</summary>
        public void Finish(Action hidden = null)
        {
            if (!Active) return;
            SetProgress(1, "准备就绪");
            Active = false;
            DOTween.Kill(group);
            DOTween.Sequence().SetTarget(group)
                .AppendInterval(0.35f)
                .Append(group.DOFade(0, 0.35f).SetEase(Ease.InCubic))
                .OnComplete(() =>
                {
                    root.SetActive(false);
                    progress = 0;
                    hidden?.Invoke();
                });
        }

        /// <summary>中止（例如对局已不存在），直接淡出。</summary>
        public void Cancel()
        {
            if (!Active) return;
            Active = false;
            DOTween.Kill(group);
            group.DOFade(0, 0.25f).SetTarget(group).OnComplete(() => { root.SetActive(false); progress = 0; });
        }
    }
}
