using DG.Tweening;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 天象阶段的弹窗：掷骰 / 不掷的选择面板，以及双方点数揭晓动画（骰子翻滚 → 点数之和 → 天象名 → 飞入天象栏）。
    /// 只显示服务器数据；对手的选择在揭晓前不可见，只显示“已决定”。
    /// </summary>
    public sealed class OmenDicePopup
    {
        private const float DiceSize = 120f, DiceY = 470f, SelfX = 760f, OppX = 1160f;
        private const float TumbleSeconds = 0.9f, FaceSeconds = 0.06f, FlySeconds = 0.45f;

        private readonly RectTransform choiceRoot, revealRoot;
        private readonly OmenBarView bar;
        private string choiceKey;       // 面板内容签名，避免同一状态重复重建
        private int pickedRound = -1;   // 本地已点击的回合（服务器确认前就显示等待态）
        private bool pickedRoll;
        private int revealedRound = -1; // 每个天象回合只揭晓一次（重连后状态里带着已揭晓的结果，不重放）
        private Sequence seq;

        public OmenDicePopup(RectTransform layer, OmenBarView bar)
        {
            this.bar = bar;
            choiceRoot = UIKit.Rect(layer, "OmenChoice");
            UIKit.Fill(choiceRoot);
            choiceRoot.gameObject.SetActive(false);
            revealRoot = UIKit.Rect(layer, "OmenReveal");
            UIKit.Fill(revealRoot);
            revealRoot.gameObject.SetActive(false);
        }

        public bool Revealing { get; private set; }

        // ---------- 选择 ----------

        public void ShowChoice(MatchState st)
        {
            if (Revealing || st == null) return;
            var roll = st.OmenRoll;
            int round = st.Round;
            // OMEN 阶段服务器只告诉我们“已决定”，不告诉选了什么
            bool selfDone = (roll != null && roll.SelfChosen) || pickedRound == round;
            bool oppDone = roll != null && roll.OpponentChosen;
            var key = $"{round}|{selfDone}|{oppDone}|{pickedRound == round}|{pickedRoll}";
            if (key == choiceKey && UIFx.IsShown(choiceRoot.gameObject)) return;
            choiceKey = key;

            UIKit.Clear(choiceRoot);
            UIFx.In(choiceRoot.gameObject, UIFx.Style.Fade);
            var dim = UIKit.Image(choiceRoot, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);

            var panel = UIKit.Panel(choiceRoot, "Panel", Theme.Paper, Theme.Ink, 3f);
            panel.raycastTarget = true;
            UIKit.PlaceCenter(panel.rectTransform, 960, 500, 780, 520);
            var pt = panel.transform;
            var title = UIKit.Text(pt, "Title", Fmt.T("chess.omen.title", "天象将至 · 第 {0} 回合", round), 40, Theme.Ink, bold: true);
            UIKit.Place(title.rectTransform, 0, 24, 780, 56);
            var desc = UIKit.Text(pt, "Desc", Fmt.T("chess.omen.rule", "双方点数相加决定天象：不掷记 0，双方都不掷则由天定"), 20, Theme.InkSoft);
            UIKit.Place(desc.rectTransform, 40, 86, 700, 30);

            BuildOmenColumns(pt);

            if (selfDone)
            {
                string msg;
                if (pickedRound == round)
                {
                    var what = pickedRoll ? Fmt.T("chess.omen.rollShort", "掷骰") : Fmt.T("chess.omen.skipShort", "不掷");
                    msg = Fmt.T("chess.omen.waiting", "已选择「{0}」，等待对手…", what);
                }
                else
                {
                    msg = Fmt.T("chess.omen.waitingAny", "已做出选择，等待对手…");
                }
                var wait = UIKit.Text(pt, "Wait", msg, 26, Theme.Ink, bold: true);
                UIKit.Place(wait.rectTransform, 40, 420, 700, 40);
                if (oppDone) OpponentDoneLine(pt, 464);
            }
            else
            {
                var rollBtn = UIKit.Button(pt, "Roll", Theme.Red, Theme.Ink, Fmt.T("chess.omen.roll", "掷 骰"), 30, Theme.Paper, () => Pick(round, true));
                UIKit.Place((RectTransform)rollBtn.transform, 120, 400, 240, 70);
                var skipBtn = UIKit.Button(pt, "Skip", Theme.Paper, Theme.Ink, Fmt.T("chess.omen.skip", "不 掷"), 30, Theme.Ink, () => Pick(round, false));
                UIKit.Place((RectTransform)skipBtn.transform, 420, 400, 240, 70);
                if (oppDone) OpponentDoneLine(pt, 484);
            }
        }

        private static void OpponentDoneLine(Transform parent, float y)
        {
            var o = UIKit.Text(parent, "OppDone", Fmt.T("chess.omen.opponentDone", "对手已决定"), 20, Theme.CyanDeep);
            UIKit.Place(o.rectTransform, 40, y, 700, 28);
        }

        private void Pick(int round, bool roll)
        {
            if (pickedRound == round) return;
            pickedRound = round;
            pickedRoll = roll;
            ChessActions.RollOmen(roll);
            var st = ChessSession.Instance.State;
            if (st != null) ShowChoice(st);
        }

        /// <summary>两列小字：点数 1–6、7–12 对应的天象名。</summary>
        private static void BuildOmenColumns(Transform parent)
        {
            var cfg = ChessSession.Instance.Config;
            if (cfg == null) return;
            var low = new StringBuilder();
            var high = new StringBuilder();
            foreach (var o in cfg.Omens.OrderBy(x => x.Point))
            {
                var sb = o.Point <= 6 ? low : high;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(o.Point).Append("  ").Append(o.Name);
            }
            var a = UIKit.Text(parent, "Low", low.ToString(), 18, Theme.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Place(a.rectTransform, 150, 136, 220, 250);
            var b = UIKit.Text(parent, "High", high.ToString(), 18, Theme.Ink, TextAlignmentOptions.TopLeft);
            UIKit.Place(b.rectTransform, 430, 136, 220, 250);
        }

        public void Hide()
        {
            if (Revealing) return;
            UIFx.Out(choiceRoot.gameObject, UIFx.Style.Fade);
            choiceKey = null;
        }

        // ---------- 揭晓 ----------

        public void Reveal(OmenRollView roll)
        {
            if (roll == null || roll.Round == revealedRound) return;
            revealedRound = roll.Round;
            FinishNow();
            UIFx.HideNow(choiceRoot.gameObject);
            choiceKey = null;
            Revealing = true;

            UIKit.Clear(revealRoot);
            var mask = UIKit.Image(revealRoot, "Mask", new Color(0.07f, 0.07f, 0.07f, 0.6f), raycast: true);
            UIKit.Fill(mask.rectTransform);
            var click = mask.gameObject.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(() => { if (seq != null && seq.IsActive()) seq.Complete(true); });
            var body = UIKit.Rect(revealRoot, "Body");
            UIKit.Fill(body);
            var bodyGroup = UIKit.GetOrAdd<CanvasGroup>(body.gameObject);
            bodyGroup.blocksRaycasts = false;

            var s = ChessSession.Instance;
            var info = s.Omen(roll.OmenKey);
            var st = s.State;
            int index = 0;
            if (st != null)
            {
                for (int i = 0; i < st.Omens.Count; i++) if (st.Omens[i].Key == roll.OmenKey) index = i;
            }

            var q = DOTween.Sequence();
            const float DiceAt = 0.1f, TumbleAt = 0.3f;

            BuildDie(body, q, SelfX, roll.SelfDie, Fmt.T("chess.omen.self", "我方"), DiceAt, TumbleAt);
            BuildDie(body, q, OppX, roll.OpponentDie, Fmt.T("chess.omen.opponent", "对手"), DiceAt, TumbleAt);

            if (roll.Forced)
                q.Insert(DiceAt, Label(body, "Forced", Fmt.T("chess.omen.forced", "双方都不掷 · 天定"), 32, Theme.Gold, 960, 330, 800, 44).DOFade(1, 0.25f));

            float sumAt = TumbleAt + TumbleSeconds + 0.3f;
            var sum = Label(body, "Sum", $"{roll.SelfDie} + {roll.OpponentDie} = {roll.Total}", 56, Theme.Paper, 960, 620, 800, 72);
            q.Insert(sumAt, sum.DOFade(1, 0.25f));
            q.Insert(sumAt, sum.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f));

            float nameAt = sumAt + 0.2f;
            if (roll.Shifted)
            {
                var sh = Label(body, "Shifted", Fmt.T("chess.omen.shifted", "与生效中的天象相同，顺延为 {0}", roll.Point), 24, Theme.GoldPaper, 960, 676, 800, 34);
                q.Insert(sumAt + 0.25f, sh.DOFade(1, 0.2f));
                nameAt += 0.2f;
            }

            var name = UIKit.Text(revealRoot, "OmenName", info != null ? info.Name : roll.OmenKey, 72, Theme.Gold, bold: true);
            name.outlineWidth = 0.3f;
            name.outlineColor = Theme.Ink;
            name.gameObject.AddComponent<ShimmerText>();
            var nameRt = name.rectTransform;
            UIKit.PlaceCenter(nameRt, 960, 760, 900, 100);
            name.alpha = 0;
            q.Insert(nameAt, name.DOFade(1, 0.3f));

            if (info != null)
            {
                var d = Label(body, "OmenDesc", $"{info.Aspect} · {info.Desc}", 26, Theme.GoldPaper, 960, 838, 1100, 40);
                q.Insert(nameAt + 0.15f, d.DOFade(1, 0.3f));
            }

            // 停留后：名字缩小飞入天象栏，其余淡出（总长不超过 4.5 秒）
            float flyAt = nameAt + 0.3f + 1.6f;
            var slot = bar.SlotCenter(index);
            q.Insert(flyAt, nameRt.DOAnchorPos(new Vector2(slot.x, -slot.y), FlySeconds).SetEase(Ease.InCubic));
            q.Insert(flyAt, nameRt.DOScale(0.3f, FlySeconds).SetEase(Ease.InCubic));
            q.Insert(flyAt, bodyGroup.DOFade(0, FlySeconds * 0.7f));
            q.Insert(flyAt, mask.DOFade(0, FlySeconds));
            q.Insert(flyAt + FlySeconds * 0.8f, name.DOFade(0, FlySeconds * 0.2f));

            seq = q;
            q.OnComplete(() =>
            {
                if (seq == q) seq = null;
                UIFx.Out(revealRoot.gameObject, UIFx.Style.Fade, () => Revealing = false, 0.2f);
            });
            UIFx.In(revealRoot.gameObject, UIFx.Style.Fade, 0.2f);
        }

        /// <summary>被新的揭晓打断时，立即收尾上一次动画。</summary>
        private void FinishNow()
        {
            if (seq != null && seq.IsActive()) { var q = seq; seq = null; q.Kill(); }
            Revealing = false;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, float cx, float cy, float w, float h)
        {
            var t = UIKit.Text(parent, name, text, size, color, bold: true);
            t.outlineWidth = 0.2f;
            t.outlineColor = Theme.Ink;
            UIKit.PlaceCenter(t.rectTransform, cx, cy, w, h);
            t.alpha = 0;
            return t;
        }

        /// <summary>一颗骰子：die 为 0 表示没掷（半透明虚框 + “不掷”），否则翻滚后落定。</summary>
        private static void BuildDie(Transform parent, Sequence q, float cx, int die, string caption, float appearAt, float tumbleAt)
        {
            bool rolled = die > 0;
            var fill = rolled ? Theme.Paper : Theme.WithAlpha(Theme.Paper, 0.12f);
            var border = rolled ? Theme.Ink : Theme.WithAlpha(Theme.Paper, 0.55f);
            var panel = UIKit.Panel(parent, rolled ? "Die" : "DieSkipped", fill, border, rolled ? 4f : 3f);
            UIKit.PlaceCenter(panel.rectTransform, cx, DiceY, DiceSize, DiceSize);
            var group = UIKit.GetOrAdd<CanvasGroup>(panel.gameObject);
            group.alpha = 0;
            panel.rectTransform.localScale = Vector3.one * 0.6f;
            q.Insert(appearAt, group.DOFade(1, 0.2f));
            q.Insert(appearAt, panel.rectTransform.DOScale(1, 0.2f).SetEase(Ease.OutBack));

            var face = UIKit.Text(panel.transform, "Face", rolled ? Random.Range(1, 7).ToString() : Fmt.T("chess.omen.noRoll", "不掷"),
                rolled ? 72 : 30, rolled ? Theme.Ink : Theme.WithAlpha(Theme.Paper, 0.8f), bold: true);

            var cap = UIKit.Text(parent, "Caption", caption, 24, Theme.Paper, bold: true);
            UIKit.PlaceCenter(cap.rectTransform, cx, DiceY + DiceSize / 2 + 28, 200, 34);
            var capGroup = UIKit.GetOrAdd<CanvasGroup>(cap.gameObject);
            capGroup.alpha = 0;
            q.Insert(appearAt, capGroup.DOFade(1, 0.2f));

            if (!rolled) return;
            int last = -1;
            q.Insert(tumbleAt, DOTween.To(() => 0f, t =>
            {
                int step = Mathf.FloorToInt(t * TumbleSeconds / FaceSeconds);
                if (t >= 1f) face.text = die.ToString();
                else if (step != last) { last = step; face.text = Random.Range(1, 7).ToString(); }
            }, 1f, TumbleSeconds).SetEase(Ease.Linear));
            q.Insert(tumbleAt, panel.rectTransform.DOShakeRotation(TumbleSeconds, new Vector3(0, 0, 28), 18, 90, false));
        }
    }
}
