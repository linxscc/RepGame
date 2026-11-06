using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>拖拽的内容：一张卡（uid）或一个法宝栏位。</summary>
    public sealed class DragPayload
    {
        public string CardUid;     // 拖的是卡
        public bool FromBoard;
        public int RelicSlot = -1; // 拖的是法宝
    }

    /// <summary>
    /// 统一处理拖拽：拖动时在最上层显示一个跟随鼠标的“幽灵”，松手时交给落点区域处理。
    /// 原物体留在原位（半透明），即使拖动途中被服务器状态刷新销毁也不影响。
    /// </summary>
    public sealed class DragController
    {
        private sealed class Zone
        {
            public RectTransform Rect;
            public Func<DragPayload, bool> Accepts;
            public Action<DragPayload, Vector2> Drop; // 第二个参数：落点在该区域内的本地坐标
            public Func<bool> Active;
        }

        private readonly RectTransform layer;
        private readonly List<Zone> zones = new List<Zone>();
        private RectTransform ghost;

        public bool Dragging => ghost != null;
        public event Action<bool> DraggingChanged;

        public DragController(RectTransform dragLayer)
        {
            layer = dragLayer;
        }

        public void AddZone(RectTransform rect, Func<DragPayload, bool> accepts, Action<DragPayload, Vector2> drop, Func<bool> active = null)
        {
            zones.Add(new Zone { Rect = rect, Accepts = accepts, Drop = drop, Active = active });
        }

        public void RemoveZonesUnder(Transform parent)
        {
            zones.RemoveAll(z => z.Rect == null || z.Rect.IsChildOf(parent));
        }

        public void Begin(RectTransform source, PointerEventData e)
        {
            Cancel();
            ghost = UIKit.Rect(layer, "DragGhost");
            ghost.sizeDelta = source.rect.size;
            ghost.pivot = new Vector2(0.5f, 0.5f);
            ghost.anchorMin = ghost.anchorMax = new Vector2(0, 1);
            var copy = UnityEngine.Object.Instantiate(source.gameObject, ghost, false);
            var crt = (RectTransform)copy.transform;
            UIKit.Fill(crt);
            foreach (var g in copy.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>())
            {
                if (!(mb is Graphic) && !(mb is Mask) && !(mb is RectMask2D)) mb.enabled = false;
            }
            var cg = UIKit.GetOrAdd<CanvasGroup>(copy);
            cg.alpha = 0.9f;
            cg.blocksRaycasts = false;
            ghost.localScale = Vector3.one * 1.06f;
            Move(e);
            DraggingChanged?.Invoke(true);
        }

        public void Move(PointerEventData e)
        {
            if (ghost == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, e.position, e.pressEventCamera, out var local))
            {
                ghost.anchoredPosition = new Vector2(local.x + layer.rect.width * layer.pivot.x, local.y - layer.rect.height * (1 - layer.pivot.y));
            }
        }

        public void End(DragPayload payload, PointerEventData e)
        {
            Cancel();
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];
                if (z.Rect == null || !z.Rect.gameObject.activeInHierarchy) continue;
                if (z.Active != null && !z.Active()) continue;
                if (!z.Accepts(payload)) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(z.Rect, e.position, e.pressEventCamera))
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(z.Rect, e.position, e.pressEventCamera, out var local);
                    z.Drop(payload, local);
                    return;
                }
            }
        }

        public void Cancel()
        {
            if (ghost != null)
            {
                UnityEngine.Object.Destroy(ghost.gameObject);
                ghost = null;
                DraggingChanged?.Invoke(false);
            }
        }
    }

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
