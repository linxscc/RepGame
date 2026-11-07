using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>代码构建 UGUI 的小工具，所有坐标以 1920×1080 设计稿为准。</summary>
    public static class UIKit
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>按设计稿左上角坐标放置：x, y 为距父物体左上角的像素。</summary>
        public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>以中心点放置（便于缩放动画）。</summary>
        public static RectTransform PlaceCenter(RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Fill(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color fill, Color border, float borderWidth = 2f)
        {
            var skin = ChessUISkin.Instance;
            var outer = Image(parent, name, border, skin.Rounded);
            if (borderWidth > 0)
            {
                var inner = Image(outer.transform, "Fill", fill, skin.Rounded);
                Fill(inner.rectTransform, borderWidth);
                // 内衬是背景，不能被父物体上的自动排版（如提示框的 VerticalLayoutGroup）当成一行内容
                inner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            else
            {
                outer.color = fill;
            }
            return outer;
        }

        /// <summary>圆形：外圈 ring 色，内部 fill 色。</summary>
        public static Image Circle(Transform parent, string name, Color fill, Color ring, float ringWidth = 2.5f)
        {
            var skin = ChessUISkin.Instance;
            var outer = Image(parent, name, ring, skin.Circle);
            var inner = Image(outer.transform, "Fill", fill, skin.Circle);
            Fill(inner.rectTransform, ringWidth);
            return outer;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = ChessUISkin.Instance.UIFont;
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            Fill(rt);
            return t;
        }

        public static Button Button(Transform parent, string name, Color fill, Color border, string label, float fontSize,
            Color labelColor, Action onClick)
        {
            var bg = Panel(parent, name, fill, border);
            bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var colors = b.colors;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            if (!string.IsNullOrEmpty(label)) Text(bg.transform, "Label", label, fontSize, labelColor, bold: true);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>
        /// 取组件，没有就添加。不能写成 GetComponent&lt;T&gt;() ?? AddComponent&lt;T&gt;()：
        /// 编辑器里缺失的组件是“假 null”对象，?? 不会把它当成 null。
        /// </summary>
        public static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        /// <summary>给任意 Graphic 挂悬停回调（用于提示框）。</summary>
        public static HoverRelay Hover(GameObject go, Action enter, Action exit)
        {
            var g = go.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            var h = GetOrAdd<HoverRelay>(go);
            h.Enter = enter;
            h.Exit = exit;
            return h;
        }
    }
}
