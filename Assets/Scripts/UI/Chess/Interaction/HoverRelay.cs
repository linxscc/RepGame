using System;
using UnityEngine.EventSystems;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 说明（L3 提示）的统一入口，鼠标和触屏规则见 <see cref="PointerInput"/>：
    /// 鼠标 Enter/Exit 显示/收起；触屏长按显示（并取消松手时同一物体上的点击，包括 Button 和 Draggable），
    /// 下一次触摸收起。物体上没有别的点击功能时，触屏轻点也直接显示说明。
    /// </summary>
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public Action Enter;
        public Action Exit;
        public Action Click;

        private PointerEventData touch; // 正在按住的手指
        private float touchDownAt;
        private bool longPressed;
        private int clickHandlers = -1; // 本物体上的点击处理组件数（缓存，避免每次轻点分配数组）

        public void OnPointerEnter(PointerEventData e)
        {
            if (!PointerInput.IsTouch(e)) Enter?.Invoke();
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!PointerInput.IsTouch(e)) Exit?.Invoke();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!PointerInput.IsTouch(e)) return;
            touch = e;
            touchDownAt = Time.unscaledTime;
            longPressed = false;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e == touch) touch = null;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!PointerInput.IsTouch(e))
            {
                Click?.Invoke();
                return;
            }
            if (longPressed) return; // 长按已经显示说明（输入模块也不会再发点击，这里双保险）
            if (Click != null) Click();
            else if (Enter != null && ClickHandlers() == 1) Enter(); // 只有说明可看：轻点即显示
            else return;
            if (Exit != null) PointerInput.HideOnNextTouch(Exit);
        }

        private int ClickHandlers()
        {
            if (clickHandlers < 0) clickHandlers = GetComponents<IPointerClickHandler>().Length;
            return clickHandlers;
        }

        private void Update()
        {
            if (touch == null || longPressed) return;
            if (touch.dragging || touch.pointerPress != gameObject)
            {
                touch = null;
                return;
            }
            var es = EventSystem.current;
            float threshold = es != null ? es.pixelDragThreshold : 10;
            if ((touch.position - touch.pressPosition).sqrMagnitude > threshold * threshold)
            {
                touch = null; // 已经开始移动：交给拖拽，不再算长按
                return;
            }
            if (Time.unscaledTime - touchDownAt < PointerInput.LongPressSeconds) return;
            longPressed = true;
            touch.eligibleForClick = false; // 松手时不再触发点击（买卡、上阵等）
            Enter?.Invoke();
            if (Exit != null) PointerInput.HideOnNextTouch(Exit);
        }

        private void OnDisable()
        {
            touch = null;
            clickHandlers = -1; // 组件可能在禁用期间增减
            PointerInput.ClearPendingHide(Exit);
            Exit?.Invoke();
        }
    }
}
