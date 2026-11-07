using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>挂在可拖拽的卡或法宝上。短按（未拖动）触发 Clicked。</summary>
    public sealed class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public DragController Controller;
        public Func<DragPayload> Payload;
        public Action Clicked;
        public Func<bool> CanDrag;

        private CanvasGroup group;
        private bool dragging;

        public void OnBeginDrag(PointerEventData e)
        {
            if (Controller == null || (CanDrag != null && !CanDrag())) return;
            dragging = true;
            group = UIKit.GetOrAdd<CanvasGroup>(gameObject);
            group.alpha = 0.35f;
            Controller.Begin((RectTransform)transform, e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (dragging) Controller.Move(e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            if (group != null) group.alpha = 1f;
            Controller.End(Payload?.Invoke(), e);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!e.dragging) Clicked?.Invoke();
        }

        private void OnDestroy()
        {
            if (dragging) Controller?.Cancel();
        }
    }
}
