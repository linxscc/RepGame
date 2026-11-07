using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 右上角天象栏（认输按钮下方，右对齐到 x=1888）：每条生效天象一个金色流光名字块，
    /// 点击展开 / 收起下方的说明面板（说明、属性、剩余回合）。揭晓动画会飞入对应块的位置。
    /// </summary>
    public sealed class OmenBarView
    {
        private const float Right = 1888, Y = 80, ChipW = 150, ChipH = 40, Gap = 8, PanelW = 340, PanelH = 170;
        private const float RootX = 1300, RootW = Right - RootX, RootH = 52 + PanelH;
        private readonly RectTransform root;
        private string signature = "";
        private string expanded;      // 当前展开的天象 key
        private string held;          // 揭晓动画飞入前先隐藏的天象 key
        private MatchState last;
        private readonly Dictionary<string, GameObject> chips = new Dictionary<string, GameObject>();
        private readonly HashSet<string> shown = new HashSet<string>();

        public OmenBarView(RectTransform parent, TooltipLayer tooltip)
        {
            root = UIKit.Rect(parent, "Omens");
            UIKit.Place(root, RootX, Y, RootW, RootH);
        }

        /// <summary>第 index 个天象块中心（设计稿坐标，左上原点），供揭晓动画落点使用。</summary>
        public Vector2 SlotCenter(int index) =>
            new Vector2(Right - ChipW / 2 - index * (ChipW + Gap), Y + ChipH / 2);

        /// <summary>揭晓动画开始：先把该天象块藏起来，等名字飞到后再 Release。</summary>
        public void Hold(string key)
        {
            held = key;
            if (chips.TryGetValue(key, out var go) && go != null) UIKit.GetOrAdd<CanvasGroup>(go).alpha = 0;
        }

        public void Release()
        {
            if (held == null) return;
            var key = held;
            held = null;
            if (!chips.TryGetValue(key, out var go) || go == null) return;
            DOTween.Kill(go);
            var g = UIKit.GetOrAdd<CanvasGroup>(go);
            g.alpha = 1;
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.6f).SetLink(go);
        }

        public void Collapse()
        {
            if (expanded == null) return;
            expanded = null;
            if (last != null) Render(last);
        }

        public void Render(MatchState st)
        {
            last = st;
            var s = ChessSession.Instance;
            var sb = new StringBuilder();
            foreach (var o in st.Omens) sb.Append(o.Key).Append(':').Append(o.RoundsLeft).Append('|');
            sb.Append('#').Append(expanded);
            var sig = sb.ToString();
            if (sig == signature) return;
            signature = sig;

            if (expanded != null)
            {
                bool alive = false;
                foreach (var o in st.Omens) if (o.Key == expanded) alive = true;
                if (!alive) expanded = null;
            }

            UIKit.Clear(root);
            chips.Clear();
            var now = new HashSet<string>();

            // 点击空白处收起：展开时铺一层全屏透明按钮（在块和面板之下）
            if (expanded != null)
            {
                var catcher = UIKit.Image(root, "Catcher", Theme.Clear, raycast: true);
                UIKit.Place(catcher.rectTransform, -RootX, -Y, 1920, 1080);
                var cb = catcher.gameObject.AddComponent<Button>();
                cb.transition = Selectable.Transition.None;
                cb.onClick.AddListener(Collapse);
            }

            for (int i = 0; i < st.Omens.Count; i++)
            {
                var active = st.Omens[i];
                var info = s.Omen(active.Key);
                var name = info != null ? info.Name : active.Key;
                var key = active.Key;
                now.Add(key);
                float chipX = RootW - ChipW - i * (ChipW + Gap);

                var chip = UIKit.Panel(root, "Omen_" + key, Theme.WithAlpha(Theme.Ink, 0.88f), Theme.Gold, 2f);
                chip.raycastTarget = true;
                UIKit.Place(chip.rectTransform, chipX, 0, ChipW, ChipH);
                var title = UIKit.Text(chip.transform, "Name", name, 22, Theme.Gold, bold: true);
                UIKit.Place(title.rectTransform, 0, 0, ChipW - 30, ChipH);
                title.gameObject.AddComponent<ShimmerText>();
                var left = UIKit.Text(chip.transform, "Left", active.RoundsLeft.ToString(), 15, Theme.GoldPaper);
                UIKit.Place(left.rectTransform, ChipW - 32, 0, 28, ChipH);
                var btn = chip.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    expanded = expanded == key ? null : key;
                    if (last != null) Render(last);
                });
                chips[key] = chip.gameObject;

                if (key == held) UIKit.GetOrAdd<CanvasGroup>(chip.gameObject).alpha = 0;
                else if (!shown.Contains(key))
                {
                    chip.gameObject.SetActive(false); // 先隐藏再 In，才会播放弹出动画
                    UIFx.In(chip.gameObject, UIFx.Style.Pop);
                }

                if (expanded == key)
                {
                    var panel = UIKit.Panel(root, "OmenInfo", Theme.Paper, Theme.Gold, 3f);
                    panel.raycastTarget = true;
                    float px = Mathf.Max(0, chipX + ChipW - PanelW);
                    UIKit.Place(panel.rectTransform, px, ChipH + 8, PanelW, PanelH);
                    var pn = UIKit.Text(panel.transform, "Title", name, 26, Theme.GoldText, TextAlignmentOptions.TopLeft, bold: true);
                    UIKit.Place(pn.rectTransform, 16, 10, PanelW - 32, 34);
                    var aspect = info != null ? info.Aspect : "";
                    var meta = UIKit.Text(panel.transform, "Meta", $"{aspect} · {Fmt.T("chess.omen.left", "剩 {0} 回合", active.RoundsLeft)}", 18, Theme.InkSoft, TextAlignmentOptions.TopLeft);
                    UIKit.Place(meta.rectTransform, 16, 48, PanelW - 32, 26);
                    var desc = UIKit.Text(panel.transform, "Desc", info != null ? info.Desc : "", 20, Theme.Ink, TextAlignmentOptions.TopLeft);
                    UIKit.Place(desc.rectTransform, 16, 80, PanelW - 32, PanelH - 90);
                    panel.gameObject.SetActive(false);
                    UIFx.In(panel.gameObject, UIFx.Style.Fade, 0.15f);
                }
            }
            shown.Clear();
            foreach (var k in now) shown.Add(k);
        }
    }
}
