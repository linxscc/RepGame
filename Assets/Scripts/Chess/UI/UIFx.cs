using System;
using DG.Tweening;
using UnityEngine;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 统一的界面过渡：显示时淡入并轻微放大到位，隐藏时淡出缩小后再 SetActive(false)。
    /// 所有弹窗、面板、按钮的出现/消失都走这里，避免界面“啪”地切换。
    /// 时长在 ChessUISkin 里可调。
    /// </summary>
    public static class UIFx
    {
        public enum Style
        {
            Pop,     // 淡入 + 从略小放大（弹窗、面板、按钮）
            Fade,    // 只淡入淡出（整屏、遮罩、提示框）
            DropIn,  // 从上方滑落 + 淡入（顶部浮条）
        }

        /// <summary>对象是否处于（或正在进入）显示状态。正在淡出的对象视为已隐藏。</summary>
        public static bool IsShown(GameObject go) =>
            go != null && go.activeSelf && (!go.TryGetComponent<UIFxState>(out var st) || st.Shown);

        public static void Toggle(GameObject go, bool show, Style style = Style.Pop)
        {
            if (show) In(go, style);
            else Out(go, style);
        }

        public static void In(GameObject go, Style style = Style.Pop, float duration = -1)
        {
            if (go == null) return;
            var st = UIKit.GetOrAdd<UIFxState>(go);
            if (go.activeSelf && st.Shown) return; // 已经显示，避免重复播放闪烁
            var skin = ChessUISkin.Instance;
            if (duration < 0) duration = skin.uiInSeconds;
            DOTween.Kill(st);
            var wasActive = go.activeSelf;
            st.Shown = true;
            go.SetActive(true);
            var group = UIKit.GetOrAdd<CanvasGroup>(go);
            group.blocksRaycasts = true;
            if (!wasActive) group.alpha = 0;
            group.DOFade(1, duration).SetEase(Ease.OutCubic).SetTarget(st);

            if (go.transform is RectTransform rt)
            {
                if (style == Style.Pop)
                {
                    if (!wasActive) rt.localScale = Vector3.one * skin.uiPopFrom;
                    rt.DOScale(1, duration).SetEase(Ease.OutBack).SetTarget(st);
                }
                else if (style == Style.DropIn)
                {
                    if (!st.HasRestPos) { st.RestPos = rt.anchoredPosition; st.HasRestPos = true; }
                    if (!wasActive) rt.anchoredPosition = st.RestPos + new Vector2(0, skin.uiSlideDistance);
                    rt.DOAnchorPos(st.RestPos, duration).SetEase(Ease.OutCubic).SetTarget(st);
                }
            }
        }

        public static void Out(GameObject go, Style style = Style.Pop, Action done = null, float duration = -1)
        {
            if (go == null) return;
            var st = UIKit.GetOrAdd<UIFxState>(go);
            if (!go.activeSelf || !st.Shown)
            {
                if (!go.activeSelf) done?.Invoke();
                return;
            }
            var skin = ChessUISkin.Instance;
            if (duration < 0) duration = skin.uiOutSeconds;
            DOTween.Kill(st);
            st.Shown = false;
            var group = UIKit.GetOrAdd<CanvasGroup>(go);
            group.blocksRaycasts = false; // 淡出期间不再挡点击
            var seq = DOTween.Sequence().SetTarget(st);
            seq.Join(group.DOFade(0, duration).SetEase(Ease.InCubic));
            var rt = go.transform as RectTransform;
            if (rt != null && style == Style.Pop)
                seq.Join(rt.DOScale(skin.uiPopFrom, duration).SetEase(Ease.InCubic));
            else if (rt != null && style == Style.DropIn && st.HasRestPos)
                seq.Join(rt.DOAnchorPos(st.RestPos + new Vector2(0, skin.uiSlideDistance), duration).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                go.SetActive(false);
                group.alpha = 1;
                group.blocksRaycasts = true;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    if (st.HasRestPos) rt.anchoredPosition = st.RestPos;
                }
                done?.Invoke();
            });
        }

        /// <summary>立刻显示，不播放动画（由调用方自己做入场动画时使用），并打断进行中的淡出。</summary>
        public static void ShowNow(GameObject go)
        {
            if (go == null) return;
            var st = UIKit.GetOrAdd<UIFxState>(go);
            DOTween.Kill(st);
            st.Shown = true;
            go.SetActive(true);
            if (go.TryGetComponent<CanvasGroup>(out var g)) g.blocksRaycasts = true;
        }

        /// <summary>立刻隐藏，不播放动画（初始化时使用）。</summary>
        public static void HideNow(GameObject go)
        {
            if (go == null) return;
            var st = UIKit.GetOrAdd<UIFxState>(go);
            DOTween.Kill(st);
            st.Shown = false;
            go.SetActive(false);
        }
    }

    /// <summary>记录对象的“目标状态”，隐藏动画进行中也能正确判断。</summary>
    public sealed class UIFxState : MonoBehaviour
    {
        public bool Shown = true;
        public Vector2 RestPos;
        public bool HasRestPos;
    }
}
