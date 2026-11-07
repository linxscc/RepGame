using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 让一个元素盖满整个画布（真实屏幕），即使它挂在 1920×1080 的 Stage 里：
    /// 根据画布四角在父物体里的位置设置 offset。Stage 重新布局时自动刷新。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFill : MonoBehaviour
    {
        private int seen = -1;
        private static readonly Vector3[] corners = new Vector3[4];

        private void OnEnable() => Apply();

        private void LateUpdate()
        {
            if (seen == StageFit.Version) return;
            Apply();
        }

        public void Apply()
        {
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            var canvas = GetComponentInParent<Canvas>(true);
            if (parent == null || canvas == null) return;
            seen = StageFit.Version;
            var root = (RectTransform)canvas.rootCanvas.transform;
            root.GetWorldCorners(corners);
            var a = parent.InverseTransformPoint(corners[0]);
            var b = parent.InverseTransformPoint(corners[2]);
            var pr = parent.rect;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 多盖 2 像素，避免缩放取整露出细缝
            rt.offsetMin = new Vector2(Mathf.Min(a.x, b.x) - pr.xMin - 2, Mathf.Min(a.y, b.y) - pr.yMin - 2);
            rt.offsetMax = new Vector2(Mathf.Max(a.x, b.x) - pr.xMax + 2, Mathf.Max(a.y, b.y) - pr.yMax + 2);
        }
    }
}
