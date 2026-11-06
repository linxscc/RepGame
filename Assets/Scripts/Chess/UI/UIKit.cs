using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>配色（见 UIConcept/STYLE.md）</summary>
    public static class Theme
    {
        public static readonly Color Ink = Hex("#111111");
        public static readonly Color InkSoft = Hex("#3A4A48");
        public static readonly Color Paper = Hex("#F4F1EA");
        public static readonly Color Water = Hex("#DCEBE8");
        public static readonly Color Cyan = Hex("#BFE3DD");
        public static readonly Color CyanDeep = Hex("#2F7A72");
        public static readonly Color Red = Hex("#C8322A");
        public static readonly Color Gold = Hex("#C9A24A");
        public static readonly Color GoldText = Hex("#8A6A1E");
        public static readonly Color GoldPaper = Hex("#FFF7E0");
        public static readonly Color Dim = new Color(0.07f, 0.07f, 0.07f, 0.32f);
        public static readonly Color Clear = new Color(0, 0, 0, 0);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>稀有度颜色：0 普通（墨）、1 稀有（青）、2 传说（金）</summary>
        public static Color Rarity(int r) => r >= 2 ? Gold : r == 1 ? CyanDeep : Ink;

        /// <summary>卡面底色：1★ 宣纸、2★ 浅青、3★ 金纸</summary>
        public static Color StarPaper(int star) => star >= 3 ? GoldPaper : star == 2 ? Cyan : Paper;
    }

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

        /// <summary>给任意 Graphic 挂悬停回调（用于提示框）。</summary>
        public static HoverRelay Hover(GameObject go, Action enter, Action exit)
        {
            var g = go.GetComponent<Graphic>();
            if (g != null) g.raycastTarget = true;
            var h = go.GetComponent<HoverRelay>() ?? go.AddComponent<HoverRelay>();
            h.Enter = enter;
            h.Exit = exit;
            return h;
        }
    }

    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Enter;
        public Action Exit;
        public Action Click;

        public void OnPointerEnter(PointerEventData e) => Enter?.Invoke();
        public void OnPointerExit(PointerEventData e) => Exit?.Invoke();
        public void OnPointerClick(PointerEventData e) => Click?.Invoke();
        private void OnDisable() => Exit?.Invoke();
    }
}
