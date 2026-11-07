using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 触屏设备上把小控件的点击区域放大到约 88×88 设计像素（只改 raycastPadding，外观不变）。
    /// 每一边的放大量不超过 MaxPad，并且不超过到相邻可点击兄弟物体间距的一半，避免抢邻居的点击。
    /// 尺寸或位置变化后下一帧重新计算（邻居常常在本物体之后才摆好位置）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TouchHitArea : MonoBehaviour
    {
        public const float Target = 88f;
        public float MaxPad = 24f;

        private Graphic graphic;
        private bool dirty = true;
        private Vector2 lastPos, lastSize;

        /// <summary>只在可触屏设备上添加；返回 null 表示不需要。</summary>
        public static TouchHitArea Add(GameObject go, float maxPad = 24f)
        {
            if (!PointerInput.TouchCapable || go == null) return null;
            var t = UIKit.GetOrAdd<TouchHitArea>(go);
            t.MaxPad = maxPad;
            t.dirty = true;
            return t;
        }

        private void OnEnable() => dirty = true;

        private void OnRectTransformDimensionsChange() => dirty = true;

        private void LateUpdate()
        {
            var rt = (RectTransform)transform;
            if (rt.anchoredPosition != lastPos || rt.rect.size != lastSize) dirty = true;
            if (!dirty) return;
            dirty = false;
            lastPos = rt.anchoredPosition;
            lastSize = rt.rect.size;
            Apply(rt);
        }

        private void Apply(RectTransform rt)
        {
            if (graphic == null) graphic = GetComponent<Graphic>();
            if (graphic == null) return;
            var size = rt.rect.size;
            float padX = Mathf.Clamp((Target - size.x) / 2, 0, MaxPad);
            float padY = Mathf.Clamp((Target - size.y) / 2, 0, MaxPad);
            float left = padX, right = padX, bottom = padY, top = padY;

            var parent = rt.parent as RectTransform;
            if (parent != null && (padX > 0 || padY > 0))
            {
                var me = RectIn(parent, rt);
                for (int i = 0; i < parent.childCount; i++)
                {
                    var sib = parent.GetChild(i) as RectTransform;
                    if (sib == null || sib == rt || !sib.gameObject.activeSelf) continue;
                    if (!sib.TryGetComponent<Graphic>(out var g) || !g.raycastTarget) continue;
                    var o = RectIn(parent, sib);
                    if (o.width >= parent.rect.width * 0.9f && o.height >= parent.rect.height * 0.9f) continue; // 背景、遮罩之类
                    bool rowOverlap = o.yMin < me.yMax + bottom && o.yMax > me.yMin - top;
                    bool colOverlap = o.xMin < me.xMax + right && o.xMax > me.xMin - left;
                    if (rowOverlap)
                    {
                        if (o.xMin >= me.xMax) right = Mathf.Min(right, (o.xMin - me.xMax) / 2);
                        else if (o.xMax <= me.xMin) left = Mathf.Min(left, (me.xMin - o.xMax) / 2);
                    }
                    if (colOverlap)
                    {
                        if (o.yMin >= me.yMax) top = Mathf.Min(top, (o.yMin - me.yMax) / 2);
                        else if (o.yMax <= me.yMin) bottom = Mathf.Min(bottom, (me.yMin - o.yMax) / 2);
                    }
                }
            }
            // raycastPadding：正值缩小、负值放大（left, bottom, right, top）
            graphic.raycastPadding = new Vector4(-left, -bottom, -right, -top);
        }

        private static readonly Vector3[] corners = new Vector3[4];

        private static Rect RectIn(RectTransform parent, RectTransform child)
        {
            child.GetWorldCorners(corners);
            var a = parent.InverseTransformPoint(corners[0]);
            var b = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
