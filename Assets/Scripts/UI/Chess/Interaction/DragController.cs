using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
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
}
