using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 最上层的提示框：悬停羁绊、法宝、卡牌时显示说明。永远画在卡牌之上，
    /// 并自动避开屏幕边缘。
    /// </summary>
    public sealed class TooltipLayer
    {
        private readonly RectTransform root;
        private readonly RectTransform box;
        private readonly TextMeshProUGUI title;
        private readonly TextMeshProUGUI subtitle;
        private readonly TextMeshProUGUI body;
        private object owner;

        public TooltipLayer(RectTransform layer)
        {
            root = layer;
            var panel = UIKit.Panel(layer, "Tooltip", Theme.Paper, Theme.Ink);
            box = panel.rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0, 1);
            box.pivot = new Vector2(0, 1);
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 12, 14);
            v.spacing = 6;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            title = Line("Title", 20, Theme.Ink, true);
            subtitle = Line("Subtitle", 13, Theme.InkSoft, false);
            body = Line("Body", 15, Theme.Ink, false);
            body.richText = true;
            panel.gameObject.SetActive(false);
        }

        private TextMeshProUGUI Line(string name, float size, Color c, bool bold)
        {
            var t = UIKit.Text(box, name, "", size, c, TextAlignmentOptions.TopLeft, bold);
            t.gameObject.AddComponent<LayoutElement>();
            return t;
        }

        /// <summary>在设计稿坐标 (x, y)（左上角）显示提示框，宽度 width。</summary>
        public void Show(object who, float x, float y, float width, string heading, string sub, string text)
        {
            owner = who;
            title.text = heading ?? "";
            subtitle.text = sub ?? "";
            subtitle.gameObject.SetActive(!string.IsNullOrEmpty(sub));
            body.text = text ?? "";
            box.sizeDelta = new Vector2(width, box.sizeDelta.y);
            x = Mathf.Clamp(x, 8, 1920 - width - 8);
            y = Mathf.Clamp(y, 8, 1080 - 160);
            box.anchoredPosition = new Vector2(x, -y);
            UIFx.In(box.gameObject, UIFx.Style.Fade, 0.12f);
            box.SetAsLastSibling();
        }

        public void Hide(object who)
        {
            if (who == null || who == owner)
            {
                UIFx.Out(box.gameObject, UIFx.Style.Fade, null, 0.1f);
                owner = null;
            }
        }
    }
}
