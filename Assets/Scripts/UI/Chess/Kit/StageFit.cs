using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 任意宽高比 + 安全区：CanvasScaler 用 Expand（参考 1920×1080），保证整张 1920×1080 设计稿总能放下；
    /// 所有内容挂在居中的 1920×1080「Stage」下（UIKit.Place 的左上角坐标在 Stage 内照常有效）。
    /// Stage 放在 Screen.safeArea 对应的区域里居中，安全区放不下 1920×1080 时整体等比缩小，刘海、灵动岛、
    /// 打孔、Home 指示条都不会挡住内容。分辨率、方向或安全区变化时自动重新计算。
    /// 需要盖满整块屏幕的东西（背景、遮罩、点空白关闭的接收层）用 UIKit.FillScreen（见 ScreenFill）。
    /// </summary>
    public sealed class StageFit : MonoBehaviour
    {
        public const float DesignW = 1920, DesignH = 1080;

        /// <summary>每次重新布局加一，ScreenFill 据此刷新。</summary>
        public static int Version { get; private set; }

        private RectTransform canvasRect;
        private RectTransform safe;
        private RectTransform stage;
        private Vector2 lastCanvas;
        private Rect lastSafe;
        private Vector2Int lastScreen;

        public RectTransform Stage => stage;

        /// <summary>设置画布缩放方式（Expand，参考 1920×1080）。</summary>
        public static void SetupScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignW, DesignH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        /// <summary>在画布根节点下建「SafeArea → Stage」，返回 Stage。</summary>
        public static RectTransform Create(Transform canvasRoot)
        {
            var safeRt = UIKit.Rect(canvasRoot, "SafeArea");
            UIKit.Fill(safeRt);
            var fit = safeRt.gameObject.AddComponent<StageFit>();
            fit.canvasRect = (RectTransform)canvasRoot;
            fit.safe = safeRt;
            var st = UIKit.Rect(safeRt, "Stage");
            st.anchorMin = st.anchorMax = new Vector2(0.5f, 0.5f);
            st.pivot = new Vector2(0.5f, 0.5f);
            st.sizeDelta = new Vector2(DesignW, DesignH);
            fit.stage = st;
            fit.Apply();
            return st;
        }

        private void LateUpdate()
        {
            if (canvasRect == null) return;
            var size = canvasRect.rect.size;
            if (size != lastCanvas || Screen.safeArea != lastSafe || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            lastCanvas = canvasRect.rect.size;
            lastSafe = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            var area = lastSafe;
            if (area.width <= 0 || area.height <= 0) area = new Rect(0, 0, sw, sh);
            var min = new Vector2(Mathf.Clamp01(area.xMin / sw), Mathf.Clamp01(area.yMin / sh));
            var max = new Vector2(Mathf.Clamp01(area.xMax / sw), Mathf.Clamp01(area.yMax / sh));
            safe.anchorMin = min;
            safe.anchorMax = max;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            var avail = new Vector2(lastCanvas.x * (max.x - min.x), lastCanvas.y * (max.y - min.y));
            float scale = Mathf.Min(1f, avail.x / DesignW, avail.y / DesignH);
            if (scale <= 0 || float.IsNaN(scale)) scale = 1;
            stage.localScale = new Vector3(scale, scale, 1);
            stage.anchoredPosition = Vector2.zero;
            Version++;
        }
    }
}
