using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Logic;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 天象阶段的弹窗：只问「是否投掷」（是 / 否），以及揭晓过场（双方骰子翻滚落定 → 点数和 → 天象名与效果
    /// → 停留 1.5 秒 → 金色名字缩小飞入右上角天象栏）。点数与天象的对应是服务器每局打乱且保密的，这里不显示任何对应表。
    /// </summary>
    public sealed class OmenDicePopup
    {
        private const float DiceSize = 120f, DiceY = 440f, SelfX = 760f, OppX = 1160f;
        private const float TumbleSeconds = 0.8f, FaceSeconds = 0.06f, FlySeconds = 0.45f, HoldSeconds = 1.5f;

        private readonly RectTransform choiceRoot, revealRoot;
        private readonly OmenBarView bar;
        private string choiceKey;       // 面板内容签名，避免同一状态重复重建
        private int pickedRound = -1;   // 本地已点击的回合（服务器确认前就显示等待态）
        private float pickedAt;         // 发送时刻；发出后 RecoverAfter 秒仍未被服务器记录才视为丢失
        private const float RecoverAfter = 1f;
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

        /// <summary>强制停止：打断揭晓动画并立即隐藏全部弹窗（认输、对局结束、重连时使用）。</summary>
        public void Stop()
        {
            if (seq != null) { var q = seq; seq = null; if (q.IsActive()) q.Kill(); }
            Revealing = false;
            bar.Release();
            UIFx.HideNow(choiceRoot.gameObject);
            UIFx.HideNow(revealRoot.gameObject);
            choiceKey = null;
        }

        /// <summary>新对局 / 对局结束：清掉按局记录的状态。</summary>
        public void Reset()
        {
            Stop();
            pickedRound = revealedRound = -1;
        }

        // ---------- 选择 ----------

        public void ShowChoice(MatchState st)
        {
            if (Revealing || st == null) return;
            var roll = st.OmenRoll;
            int round = st.Round;
            // 乐观状态只是短暂的：发送一段时间后收到的状态仍显示未选择，说明服务器没记录（发送丢失或被拒），恢复按钮
            if (pickedRound == round && !(roll != null && roll.SelfChosen) && Time.unscaledTime - pickedAt >= RecoverAfter)
                pickedRound = -1;
            bool selfDone = (roll != null && roll.SelfChosen) || pickedRound == round;
            var key = $"{round}|{selfDone}";
            if (key == choiceKey && UIFx.IsShown(choiceRoot.gameObject)) return;
            choiceKey = key;

            UIKit.Clear(choiceRoot);
            UIFx.In(choiceRoot.gameObject, UIFx.Style.Fade);
            var dim = UIKit.Image(choiceRoot, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);

            var panel = UIKit.Panel(choiceRoot, "Panel", Theme.Paper, Theme.Ink, 3f);
            panel.raycastTarget = true;
            UIKit.PlaceCenter(panel.rectTransform, 960, 500, 620, 300);
            var pt = panel.transform;
            var prompt = UIKit.Text(pt, "Prompt", Fmt.T("chess.omen.prompt", "是否投掷"), 52, Theme.Ink, bold: true);
            UIKit.Place(prompt.rectTransform, 0, 40, 620, 80);

            if (selfDone)
            {
                var wait = UIKit.Text(pt, "Wait", Fmt.T("chess.omen.waiting", "等待对手…"), 28, Theme.InkSoft, bold: true);
                UIKit.Place(wait.rectTransform, 0, 170, 620, 50);
            }
            else
            {
                var yes = UIKit.Button(pt, "Yes", Theme.Red, Theme.Ink, Fmt.T("chess.omen.yes", "是"), 34, Theme.Paper, () => Pick(round, true));
                UIKit.Place((RectTransform)yes.transform, 100, 160, 190, 74);
                var no = UIKit.Button(pt, "No", Theme.Paper, Theme.Ink, Fmt.T("chess.omen.no", "否"), 34, Theme.Ink, () => Pick(round, false));
                UIKit.Place((RectTransform)no.transform, 330, 160, 190, 74);
            }
        }

        private void Pick(int round, bool roll)
        {
            if (pickedRound == round) return;
            pickedRound = round;
            pickedAt = Time.unscaledTime;
            ChessActions.RollOmen(roll);
            var st = ChessSession.Instance.State;
            if (st != null) ShowChoice(st);
        }

        /// <summary>动作被服务器拒绝：撤销乐观选择并重新显示按钮。</summary>
        public void OnActionFailed()
        {
            if (pickedRound < 0 || Revealing) return;
            var st = ChessSession.Instance.State;
            if (st == null || st.Phase != MatchPhase.Omen || st.Round != pickedRound) return;
            if (st.OmenRoll != null && st.OmenRoll.SelfChosen) return;
            pickedRound = -1;
            ShowChoice(st);
        }

        public void Hide()
        {
            if (Revealing) return;
            UIFx.Out(choiceRoot.gameObject, UIFx.Style.Fade);
            choiceKey = null;
        }

        // ---------- 揭晓 ----------
        // 时间线（秒）：0 骰子出现 → 0.2~1.0 翻滚 → 1.1 点数和 → 1.5 天象名+效果（0.3 淡入）→ 停留 1.5 → 3.3 飞入右上角（0.45）→ 约 3.8 结束

        public void Reveal(OmenRollView roll)
        {
            if (roll == null || roll.Round == revealedRound) return;
            revealedRound = roll.Round;
            FinishNow();
            UIFx.HideNow(choiceRoot.gameObject);
            choiceKey = null;
            Revealing = true;

            // 先激活根节点再建子物体：TMP 在未激活的父节点下还没初始化材质，此时设置描边会空引用
            UIKit.Clear(revealRoot);
            UIFx.In(revealRoot.gameObject, UIFx.Style.Fade, 0.15f);
            var mask = UIKit.Image(revealRoot, "Mask", new Color(0.07f, 0.07f, 0.07f, 0.6f), raycast: true);
            UIKit.Fill(mask.rectTransform);
            var body = UIKit.Rect(revealRoot, "Body");
            UIKit.Fill(body);
            var bodyGroup = UIKit.GetOrAdd<CanvasGroup>(body.gameObject);
            bodyGroup.blocksRaycasts = false;

            var s = ChessSession.Instance;
            var info = s.Omen(roll.OmenKey);
            var st = s.State;
            int index = -1;
            if (st != null)
            {
                for (int i = 0; i < st.Omens.Count; i++) if (st.Omens[i].Key == roll.OmenKey) index = i;
            }
            if (index >= 0) bar.Hold(roll.OmenKey);

            var q = DOTween.Sequence();
            const float DiceAt = 0f, TumbleAt = 0.2f;

            string self = Fmt.T("chess.omen.self", "我方"), opp = Fmt.T("chess.omen.opponent", "对手");
            if (roll.Forced)
            {
                var fate = Fmt.T("chess.omen.forced", "天定");
                self += " · " + fate;
                opp += " · " + fate;
            }
            BuildDie(body, q, SelfX, roll.SelfDie, self, DiceAt, TumbleAt);
            BuildDie(body, q, OppX, roll.OpponentDie, opp, DiceAt, TumbleAt);

            float sumAt = TumbleAt + TumbleSeconds + 0.1f;
            var sum = Label(body, "Sum", $"{roll.SelfDie} + {roll.OpponentDie} = {roll.Total}", 56, Theme.Paper, 960, 590, 800, 72);
            q.Insert(sumAt, sum.DOFade(1, 0.2f));
            q.Insert(sumAt, sum.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f));

            float nameAt = sumAt + 0.4f;
            if (roll.Shifted)
            {
                var sh = Label(body, "Shifted", Fmt.T("chess.omen.shifted", "与生效中的天象相同，顺延"), 24, Theme.GoldPaper, 960, 645, 800, 34);
                q.Insert(sumAt + 0.2f, sh.DOFade(1, 0.2f));
            }

            var name = UIKit.Text(revealRoot, "OmenName", info != null ? info.Name : roll.OmenKey, 72, Theme.Gold, bold: true);
            name.outlineWidth = 0.3f;
            name.outlineColor = Theme.Ink;
            name.gameObject.AddComponent<ShimmerText>();
            var nameRt = name.rectTransform;
            UIKit.PlaceCenter(nameRt, 960, 730, 900, 100);
            name.alpha = 0;
            q.Insert(nameAt, name.DOFade(1, 0.3f));

            if (info != null)
            {
                var d = Label(body, "OmenDesc", info.Desc, 28, Theme.GoldPaper, 960, 815, 1100, 44);
                q.Insert(nameAt + 0.1f, d.DOFade(1, 0.3f));
            }

            // 结果完全出现后停留 1.5 秒，名字缩小飞入天象栏，其余淡出
            float flyAt = nameAt + 0.3f + HoldSeconds;
            if (index >= 0)
            {
                var slot = bar.SlotCenter(index);
                q.Insert(flyAt, nameRt.DOAnchorPos(new Vector2(slot.x, -slot.y), FlySeconds).SetEase(Ease.InCubic));
                q.Insert(flyAt, nameRt.DOScale(22f / 72f, FlySeconds).SetEase(Ease.InCubic));
            }
            q.Insert(flyAt, bodyGroup.DOFade(0, FlySeconds * 0.7f));
            q.Insert(flyAt, mask.DOFade(0, FlySeconds));
            q.Insert(flyAt + FlySeconds, name.DOFade(0, 0.05f));

            seq = q;
            q.SetLink(revealRoot.gameObject);
            q.OnComplete(() =>
            {
                if (seq == q) seq = null;
                Revealing = false;
                bar.Release();
                UIFx.Out(revealRoot.gameObject, UIFx.Style.Fade, null, 0.1f);
            });
        }

        /// <summary>被新的揭晓打断时，立即收尾上一次动画。</summary>
        private void FinishNow()
        {
            if (seq != null && seq.IsActive()) { var q = seq; seq = null; q.Kill(); }
            Revealing = false;
            bar.Release();
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

        /// <summary>一颗骰子：die 为 0 表示没掷（半透明虚框 + 不掷），否则旋转抖动、点数飞速变换后落定。</summary>
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
            q.Insert(appearAt, group.DOFade(1, 0.15f));
            q.Insert(appearAt, panel.rectTransform.DOScale(1, 0.2f).SetEase(Ease.OutBack));

            var face = UIKit.Text(panel.transform, "Face", rolled ? Random.Range(1, 7).ToString() : Fmt.T("chess.omen.noRoll", "不掷"),
                rolled ? 72 : 30, rolled ? Theme.Ink : Theme.WithAlpha(Theme.Paper, 0.8f), bold: true);

            var cap = UIKit.Text(parent, "Caption", caption, 24, Theme.Paper, bold: true);
            UIKit.PlaceCenter(cap.rectTransform, cx, DiceY + DiceSize / 2 + 28, 260, 34);
            var capGroup = UIKit.GetOrAdd<CanvasGroup>(cap.gameObject);
            capGroup.alpha = 0;
            q.Insert(appearAt, capGroup.DOFade(1, 0.15f));

            if (!rolled) return;
            int last = -1;
            q.Insert(tumbleAt, DOTween.To(() => 0f, t =>
            {
                int step = Mathf.FloorToInt(t * TumbleSeconds / FaceSeconds);
                if (t >= 1f) face.text = die.ToString();
                else if (step != last) { last = step; face.text = Random.Range(1, 7).ToString(); }
            }, 1f, TumbleSeconds).SetEase(Ease.Linear));
            var rt = panel.rectTransform;
            q.Insert(tumbleAt, rt.DOLocalRotate(new Vector3(0, 0, 720), TumbleSeconds, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            q.Insert(tumbleAt, rt.DOShakeAnchorPos(TumbleSeconds, new Vector2(18, 24), 22, 90, false, false));
            q.Insert(tumbleAt + TumbleSeconds, rt.DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.6f));
        }
    }
}
