using System.Collections.Generic;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 大厅（场景里的 Canvas 下各个 Panel_*）的安全区适配，只在需要时启用：
    /// 只有移动设备且 Screen.safeArea 小于整块屏幕（刘海、灵动岛、挖孔、Home 指示条）时，
    /// 才把每个面板的内容收进一个按安全区缩进的「SafeContent」节点里；面板自身（背景）仍铺满全屏。
    /// 16:9 无刘海手机、平板、PC 上安全区等于全屏，什么都不改，和原来完全一样。
    /// 安全区变化（旋转到另一侧横屏）时自动重算；变回全屏时把内容放回原处。
    /// </summary>
    public sealed class LobbySafeArea : MonoBehaviour
    {
        private const string ContentName = "SafeContent";
        private readonly Dictionary<RectTransform, RectTransform> wrapped = new Dictionary<RectTransform, RectTransform>();
        private Rect lastSafe;
        private Vector2Int lastScreen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isMobilePlatform) return; // PC 不需要
            var canvas = GameObject.Find("Canvas");
            if (canvas == null || canvas.GetComponent<LobbySafeArea>() != null) return;
            canvas.AddComponent<LobbySafeArea>();
        }

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastScreen.x || Screen.height != lastScreen.y) Apply();
        }

        private static bool NeedsInset(Rect safe, float sw, float sh) =>
            safe.width > 0 && safe.height > 0 &&
            (safe.xMin > 0.5f || safe.yMin > 0.5f || safe.xMax < sw - 0.5f || safe.yMax < sh - 0.5f);

        private void Apply()
        {
            lastSafe = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            float sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            bool inset = NeedsInset(lastSafe, sw, sh);
            var min = new Vector2(Mathf.Clamp01(lastSafe.xMin / sw), Mathf.Clamp01(lastSafe.yMin / sh));
            var max = new Vector2(Mathf.Clamp01(lastSafe.xMax / sw), Mathf.Clamp01(lastSafe.yMax / sh));

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith("Panel_") || !(child is RectTransform panel)) continue;
                wrapped.TryGetValue(panel, out var content);
                if (!inset)
                {
                    if (content != null) Unwrap(panel, content);
                    continue;
                }
                if (content == null) content = Wrap(panel);
                content.anchorMin = min;
                content.anchorMax = max;
                content.offsetMin = content.offsetMax = Vector2.zero;
            }
        }

        private RectTransform Wrap(RectTransform panel)
        {
            var go = new GameObject(ContentName, typeof(RectTransform));
            go.layer = panel.gameObject.layer;
            var content = (RectTransform)go.transform;
            content.SetParent(panel, false);
            var kids = new List<Transform>();
            foreach (Transform k in panel) if (k != content) kids.Add(k);
            foreach (var k in kids) k.SetParent(content, false); // 保留各自锚点，改为相对安全区
            wrapped[panel] = content;
            return content;
        }

        private void Unwrap(RectTransform panel, RectTransform content)
        {
            var kids = new List<Transform>();
            foreach (Transform k in content) kids.Add(k);
            foreach (var k in kids) k.SetParent(panel, false);
            wrapped.Remove(panel);
            Destroy(content.gameObject);
        }
    }
}
