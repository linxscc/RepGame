using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 触屏与鼠标统一的输入规则（手机、电脑同时上线，所有操作两种方式都能完成）：
    /// - 鼠标：悬停看说明，点击执行。
    /// - 触屏：轻点 = 点击；长按 ≥ LongPressSeconds 不移动 = 显示同样的说明并取消松手时的点击，
    ///   说明一直保留到下一次触摸屏幕；按住后移动超过拖拽阈值 = 拖拽。触屏不响应进入/离开。
    /// 拖拽阈值按屏幕 DPI 设置（约 0.08 英寸，至少 10 像素），避免高 DPI 手机上轻点被当成拖拽。
    /// </summary>
    public static class PointerInput
    {
        public const float LongPressSeconds = 0.4f;
        private const float DragThresholdInches = 0.08f;
        private const int MinDragThreshold = 10;

        private static Action pendingHide;
        private static Runner runner;

        /// <summary>这个指针事件来自手指（而不是鼠标或笔）。</summary>
        public static bool IsTouch(PointerEventData e) =>
            e is ExtendedPointerEventData x && x.pointerType == UIPointerType.Touch;

        /// <summary>创建常驻的监听器：配置拖拽阈值，并在下一次触摸时收起长按/轻点打开的说明。</summary>
        public static void Ensure()
        {
            if (runner != null) return;
            var go = new GameObject("PointerInput");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        /// <summary>触屏打开的说明：下一次手指按下屏幕（任何位置）时调用 hide。</summary>
        public static void HideOnNextTouch(Action hide)
        {
            Ensure();
            pendingHide = hide;
        }

        /// <summary>该 relay 被禁用/销毁时，撤掉它挂着的「下次触摸收起」。</summary>
        public static void ClearPendingHide(Action hide)
        {
            if (hide != null && pendingHide == hide) pendingHide = null;
        }

        public static int DragThresholdFor(float dpi) =>
            dpi > 0 ? Mathf.Max(MinDragThreshold, Mathf.RoundToInt(dpi * DragThresholdInches)) : MinDragThreshold;

        private sealed class Runner : MonoBehaviour
        {
            private EventSystem configured;

            private void Update()
            {
                var es = EventSystem.current;
                if (es != null && es != configured)
                {
                    configured = es;
                    if (Application.isMobilePlatform) es.pixelDragThreshold = DragThresholdFor(Screen.dpi); // 电脑（含触摸屏电脑）保持场景默认值
                }

                if (pendingHide == null) return;
                var ts = Touchscreen.current;
                if (ts == null) return;
                var touches = ts.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].press.wasPressedThisFrame) continue;
                    var hide = pendingHide;
                    pendingHide = null;
                    hide();
                    return;
                }
            }
        }
    }
}
