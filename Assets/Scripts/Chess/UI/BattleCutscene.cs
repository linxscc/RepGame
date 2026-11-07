using System;
using System.Collections.Generic;
using DG.Tweening;
using RepGame.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RepGame.Chess.UI
{
    /// <summary>
    /// 回合结算的过场动画（取代结算弹窗）：
    /// 虚空中双方上阵卡牌背面朝上，按服务器下发的计分步骤逐张翻开，双方积分从 0 逐项累加；
    /// 判定胜负后淡出，由胜方头像发出火焰击中败方头像，败方受击震动、扣血。
    /// 所有数值都来自 RoundResult，这里只负责播放。
    /// </summary>
    public sealed class BattleCutscene
    {
        private const float CardW = 120, CardH = 180, CardGap = 16;
        private const float EnemyRowY = 220, SelfRowY = 860;
        private const float SelfScoreX = 600, EnemyScoreX = 1320, ScoreY = 520;

        private readonly RectTransform layer;     // 弹出层：过场画面
        private readonly RectTransform fxLayer;   // 火焰与伤害数字（在 HUD 之上）
        private readonly PlayerHud selfHud, enemyHud;
        private RectTransform root;
        private Sequence master;
        private RoundResult current;
        private bool firing;
        private bool fading;            // 已进入淡出
        private CanvasGroup group;
        private Side enemySide, selfSide;

        public BattleCutscene(RectTransform layer, RectTransform fxLayer, PlayerHud self, PlayerHud enemy)
        {
            this.layer = layer;
            this.fxLayer = fxLayer;
            selfHud = self;
            enemyHud = enemy;
        }

        public bool Playing => root != null;

        public void Play(RoundResult rr)
        {
            Stop();
            current = rr;
            firing = false;
            fading = false;
            selfHud.Freeze();
            enemyHud.Freeze();

            root = UIKit.Rect(layer, "BattleCutscene");
            UIKit.Fill(root);
            root.SetAsLastSibling();
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;

            // 云雾遮罩：半透明，对局界面仍隐约可见；整层随 CanvasGroup 淡入淡出
            var skin = ChessUISkin.Instance;
            var catcher = UIKit.Image(root, "Catcher", Color.clear, raycast: true);   // 点击任意处跳过
            UIKit.Fill(catcher.rectTransform);
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Skip);
            var veil = UIKit.Image(root, "Veil", skin.voidBackground != null ? Theme.WithAlpha(Color.white, skin.veilAlpha) : Theme.WithAlpha(Theme.Ink, skin.veilAlpha),
                skin.voidBackground);
            UIKit.Fill(veil.rectTransform);
            // 云团从两侧飘入，然后缓慢漂移
            var mistSprite = skin.voidMist != null ? skin.voidMist : skin.Circle;
            var mistTint = skin.voidMist != null ? Color.white : Theme.Cyan;
            foreach (var (x, y, s, from) in new[] { (260f, 260f, 1000f, -500f), (1660f, 300f, 900f, 500f), (300f, 860f, 1000f, -500f), (1640f, 820f, 1100f, 500f) })
            {
                var mist = UIKit.Image(root, "Mist", Theme.WithAlpha(mistTint, skin.voidMist != null ? skin.mistAlpha : skin.mistAlpha * 0.1f), mistSprite);
                UIKit.PlaceCenter(mist.rectTransform, x, y, s, s * 0.6f);
                var rt = mist.rectTransform;
                var targetX = rt.anchoredPosition.x;
                rt.anchoredPosition += new Vector2(from, 0);
                rt.DOAnchorPosX(targetX, skin.cutsceneIntro + 0.6f).SetEase(Ease.OutCubic).SetTarget(root)
                    .OnComplete(() => rt.DOAnchorPosX(targetX + (from < 0 ? 50 : -50), 6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(root));
                mist.DOFade(0, skin.cutsceneIntro + 0.6f).From().SetTarget(root);
            }
            var title = UIKit.Text(root, "Title", $"第 {rr.Round} 回合 · 交锋", 30, Theme.Paper, TextAlignmentOptions.Center, true);
            UIKit.PlaceCenter(title.rectTransform, 960, 46, 600, 44);
            var hint = UIKit.Text(root, "Hint", "点击跳过", 14, Theme.WithAlpha(Theme.Paper, 0.5f), TextAlignmentOptions.Right);
            UIKit.Place(hint.rectTransform, 1600, 30, 280, 24);

            var enemy = enemySide = new Side(root, rr.Opponent, EnemyRowY, EnemyScoreX, true);
            var self = selfSide = new Side(root, rr.Self, SelfRowY, SelfScoreX, false);

            var verdict = UIKit.Text(root, "Verdict", "", 96, Theme.Red, TextAlignmentOptions.Center, true);
            UIKit.PlaceCenter(verdict.rectTransform, 960, 530, 600, 120);
            verdict.outlineWidth = 0.15f;
            verdict.outlineColor = Theme.Paper;
            verdict.alpha = 0;
            var stampSprite = rr.Winner > 0 ? skin.verdictWin : rr.Winner < 0 ? skin.verdictLose : skin.verdictDraw;
            Image stamp = null;
            if (stampSprite != null) // 有“胜/负/平”贴图时用贴图，否则用文字
            {
                stamp = UIKit.Image(root, "Stamp", Theme.WithAlpha(Color.white, 0), stampSprite);
                stamp.preserveAspect = true;
                UIKit.PlaceCenter(stamp.rectTransform, 960, 530, 320, 320);
            }

            // 时间预算：按步骤数分配，每步再额外放慢 stepExtraSeconds；服务器的结算上限足够长，
            // 双方播完后客户端会发 SettleDone 提前结束
            var total = Mathf.Clamp((rr.NextPhaseAtMs - ChessSession.Instance.ServerNowMs) / 1000f, 4f, 12f);
            float intro = skin.cutsceneIntro, verdictT = skin.verdictHold, outro = Mathf.Max(0.1f, skin.cutsceneOutro);
            var fire = skin.fireFlightSeconds + 1.2f;
            var reveal = Mathf.Max(2f, total - intro - verdictT - outro - fire);
            var steps = Mathf.Max(1, Mathf.Max(enemy.Events.Count, self.Events.Count));
            var per = Mathf.Clamp(reveal / steps, skin.stepMinSeconds, skin.stepMaxSeconds) + skin.stepExtraSeconds;

            master = DOTween.Sequence().SetTarget(root);
            master.Append(group.DOFade(1, intro));
            var revealSeq = DOTween.Sequence();
            revealSeq.Insert(0, enemy.Build(per));
            revealSeq.Insert(0, self.Build(per));
            master.Append(revealSeq);
            master.AppendCallback(() =>
            {
                enemy.Finish();
                self.Finish();
                verdict.text = rr.Winner > 0 ? "胜" : rr.Winner < 0 ? "负" : "平";
                verdict.color = rr.Winner > 0 ? Theme.Red : rr.Winner < 0 ? Theme.InkSoft : Theme.Paper;
                (rr.Winner > 0 ? self : rr.Winner < 0 ? enemy : null)?.Highlight();
            });
            if (stamp != null)
            {
                master.Append(stamp.DOFade(1, 0.2f));
                master.Join(stamp.rectTransform.DOScale(1f, 0.35f).From(2.2f).SetEase(Ease.OutBack));
            }
            else
            {
                master.Append(verdict.DOFade(1, 0.2f));
                master.Join(verdict.rectTransform.DOScale(1f, 0.35f).From(2.2f).SetEase(Ease.OutBack));
            }
            master.AppendInterval(verdictT - 0.35f);
            master.AppendCallback(FadeOut);
        }

        /// <summary>点击跳过：直接显示最终分数并开始淡出（淡出中再点无效）。</summary>
        private void Skip()
        {
            if (root == null || fading) return;
            master?.Kill();
            enemySide.Finish();
            selfSide.Finish();
            FadeOut();
        }

        /// <summary>结束过渡：云雾和画面一起淡出并轻微放大，完成后再发出火焰。</summary>
        private void FadeOut()
        {
            if (root == null || fading) return;
            fading = true;
            var outro = Mathf.Max(0.1f, ChessUISkin.Instance.cutsceneOutro);
            master = DOTween.Sequence().SetTarget(root);
            master.Append(group.DOFade(0, outro).SetEase(Ease.InCubic));
            master.Join(root.DOScale(1.04f, outro).SetEase(Ease.InCubic));
            master.OnComplete(Fire);
        }

        /// <summary>立刻结束（例如对局结束、离开对局）。</summary>
        public void Stop()
        {
            master?.Kill();
            master = null;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            root = null;
            if (!firing)
            {
                selfHud.Release();
                enemyHud.Release();
            }
        }

        // ---------- 火焰攻击 ----------

        private static readonly Vector2 EnemyAvatar = new Vector2(64, 56);
        private static readonly Vector2 SelfAvatar = new Vector2(64, 1080 - 32 - 32);

        private void Fire()
        {
            var rr = current;
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            root = null;
            master = null;
            firing = true;
            if (rr.Winner > 0) Shoot(SelfAvatar, EnemyAvatar, enemyHud, rr.Opponent.DamageTaken);
            else if (rr.Winner < 0) Shoot(EnemyAvatar, SelfAvatar, selfHud, rr.Self.DamageTaken);
            else
            {
                var center = new Vector2(960, 540);
                Shoot(center, EnemyAvatar, enemyHud, rr.Opponent.DamageTaken);
                Shoot(center, SelfAvatar, selfHud, rr.Self.DamageTaken);
            }
            DOVirtual.DelayedCall(ChessUISkin.Instance.fireFlightSeconds + 1f, () =>
            {
                firing = false;
                selfHud.Release();
                enemyHud.Release();
                ChessActions.SettleDone(); // 伤害演完：本回合结束，服务器进入下一阶段
            });
        }

        private void Shoot(Vector2 from, Vector2 to, PlayerHud target, int damage)
        {
            var skin = ChessUISkin.Instance;
            var orb = UIKit.Rect(fxLayer, "Fireball");
            var dir = (to - from).normalized;
            if (skin.fireball != null)
            {
                UIKit.PlaceCenter(orb, from.x, from.y, 140, 140);
                var img = UIKit.Image(orb, "Fireball", Color.white, skin.fireball);
                img.preserveAspect = true;
                UIKit.Fill(img.rectTransform);
            }
            else
            {
                UIKit.PlaceCenter(orb, from.x, from.y, 90, 90);
                var glow = UIKit.Image(orb, "Glow", Theme.WithAlpha(Theme.Hex("#FF8A3D"), 0.35f), skin.Circle);
                UIKit.Fill(glow.rectTransform);
                var ring = UIKit.Image(orb, "Flame", Theme.WithAlpha(Theme.Red, 0.85f), skin.Circle);
                UIKit.Fill(ring.rectTransform, 14);
                var core = UIKit.Image(orb, "Core", Theme.Hex("#FFD27A"), skin.Circle);
                UIKit.Fill(core.rectTransform, 24);
            }

            // 弧线轨迹：二次贝塞尔，控制点偏离连线
            var mid = (from + to) / 2;
            var ctrl = mid + new Vector2(-dir.y, dir.x) * skin.fireArcHeight;
            float lastTrail = 0;
            float t = 0;
            DOTween.To(() => t, v =>
            {
                t = v;
                var p = (1 - v) * (1 - v) * from + 2 * (1 - v) * v * ctrl + v * v * to;
                orb.anchoredPosition = new Vector2(p.x, -p.y);
                orb.localScale = Vector3.one * (0.8f + 0.4f * Mathf.Sin(v * Mathf.PI));
                // 贴图火球朝向飞行方向（设计稿坐标 y 向下，所以角度取反）
                var tangent = 2 * (1 - v) * (ctrl - from) + 2 * v * (to - ctrl);
                orb.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
                if (v - lastTrail > skin.trailInterval)
                {
                    lastTrail = v;
                    Trail(p);
                }
            }, 1f, skin.fireFlightSeconds).SetEase(Ease.InQuad).OnComplete(() =>
            {
                UnityEngine.Object.Destroy(orb.gameObject);
                Impact(to, target, damage);
            });
        }

        private void Trail(Vector2 p)
        {
            var skin = ChessUISkin.Instance;
            var spark = UIKit.Image(fxLayer, "Spark", skin.fireTrail != null ? Color.white : Theme.WithAlpha(Theme.Hex("#FF8A3D"), 0.7f),
                skin.fireTrail != null ? skin.fireTrail : skin.Circle);
            UIKit.PlaceCenter(spark.rectTransform, p.x, p.y, 46, 46);
            spark.transform.SetAsFirstSibling();
            spark.DOFade(0, 0.35f);
            spark.rectTransform.DOScale(0.2f, 0.35f).OnComplete(() => UnityEngine.Object.Destroy(spark.gameObject));
        }

        private void Impact(Vector2 at, PlayerHud target, int damage)
        {
            var skin = ChessUISkin.Instance;
            var burst = UIKit.Image(fxLayer, "Burst", skin.impactBurst != null ? Color.white : Theme.WithAlpha(Theme.Hex("#FFB347"), 0.9f),
                skin.impactBurst != null ? skin.impactBurst : skin.Circle);
            burst.preserveAspect = true;
            UIKit.PlaceCenter(burst.rectTransform, at.x, at.y, 80, 80);
            burst.rectTransform.DOScale(skin.impactScale, 0.35f).SetEase(Ease.OutCubic);
            burst.DOFade(0, 0.35f).OnComplete(() => UnityEngine.Object.Destroy(burst.gameObject));
            target.Hit();
            target.Release();
            if (damage > 0)
            {
                var num = UIKit.Text(fxLayer, "Damage", "-" + damage, 44, Theme.Red, TextAlignmentOptions.Center, true);
                num.outlineWidth = 0.2f;
                num.outlineColor = Theme.Paper;
                var rt = num.rectTransform;
                UIKit.PlaceCenter(rt, at.x + 120, at.y, 160, 60);
                rt.DOAnchorPosY(rt.anchoredPosition.y + 70, 1.1f).SetEase(Ease.OutCubic);
                num.DOFade(0, 0.5f).SetDelay(0.6f).OnComplete(() => UnityEngine.Object.Destroy(num.gameObject));
            }
        }

        // ---------- 一方：卡牌行 + 积分 ----------

        private sealed class Side
        {
            public readonly List<Event> Events = new List<Event>();
            private readonly Dictionary<string, Flip> cards = new Dictionary<string, Flip>();
            private readonly TextMeshProUGUI scoreText, formula, ticker;
            private readonly RectTransform bonusBox;          // 积分外侧：正在结算的羁绊 / 法宝
            private readonly CanvasGroup bonusGroup;
            private readonly TextMeshProUGUI bonusKind, bonusName, bonusValue;
            private readonly float bonusX, bonusFrom;
            private bool bonusShown;
            private readonly SideResult side;
            private float shown;

            public struct Event
            {
                public string CardUid, Label, Source, Change;
                public int Phase;
                public double Points, Mult;
            }

            private sealed class Flip
            {
                public RectTransform Root, Back, Face;
                public bool Open;
            }

            public Side(RectTransform root, SideResult side, float rowY, float scoreX, bool enemy)
            {
                this.side = side;
                var n = side.Board.Count;
                float w = n * CardW + Mathf.Max(0, n - 1) * CardGap;
                for (int i = 0; i < n; i++)
                {
                    var c = side.Board[i];
                    var holder = UIKit.Rect(root, "Card");
                    UIKit.PlaceCenter(holder, 960 - w / 2 + CardW / 2 + i * (CardW + CardGap), rowY, CardW, CardH);
                    var face = CardView.Build(holder, c, CardW, CardH);
                    UIKit.Fill(face);
                    face.gameObject.SetActive(false);
                    var back = Back(holder);
                    cards[c.Uid] = new Flip { Root = holder, Back = back, Face = face };
                }
                // 积分区：双方并排在画面中央（我方左、对手右），名字在上，算式和当前步骤在下
                var skin = ChessUISkin.Instance;
                var who = UIKit.Text(root, "Who", enemy ? $"对手 · {side.Username}" : "我方", 26, Theme.Paper, TextAlignmentOptions.Center, true);
                UIKit.PlaceCenter(who.rectTransform, scoreX, ScoreY - skin.scoreFontSize * 0.62f, 560, 34);
                scoreText = UIKit.Text(root, "Score", "0", skin.scoreFontSize, enemy ? Theme.Paper : Theme.Hex("#FFD27A"), TextAlignmentOptions.Center, true);
                scoreText.outlineWidth = 0.12f;
                scoreText.outlineColor = Theme.Ink;
                UIKit.PlaceCenter(scoreText.rectTransform, scoreX, ScoreY, 640, skin.scoreFontSize * 1.1f);
                formula = UIKit.Text(root, "Formula", "战力 0 × 气势 1", 24, Theme.Paper, TextAlignmentOptions.Center);
                UIKit.PlaceCenter(formula.rectTransform, scoreX, ScoreY + skin.scoreFontSize * 0.62f, 560, 32);
                ticker = UIKit.Text(root, "Ticker", "", 28, Theme.Hex("#FFD27A"), TextAlignmentOptions.Center, true);
                UIKit.PlaceCenter(ticker.rectTransform, scoreX, ScoreY + skin.scoreFontSize * 0.62f + 38, 560, 36);

                // 羁绊 / 法宝结算提示：我方在积分左侧，对手在积分右侧；金色流光文字
                bonusX = enemy ? scoreX + 450 : scoreX - 450;
                bonusFrom = enemy ? 60 : -60;
                bonusBox = UIKit.Rect(root, "Bonus");
                UIKit.PlaceCenter(bonusBox, bonusX, ScoreY, 300, 170);
                bonusGroup = bonusBox.gameObject.AddComponent<CanvasGroup>();
                bonusGroup.alpha = 0;
                bonusGroup.blocksRaycasts = false;
                bonusKind = UIKit.Text(bonusBox, "Kind", "", 22, Theme.WithAlpha(Theme.Paper, 0.85f), TextAlignmentOptions.Center);
                UIKit.PlaceCenter(bonusKind.rectTransform, 150, 22, 300, 30);
                bonusName = UIKit.Text(bonusBox, "Name", "", 52, Theme.Hex("#FFD27A"), TextAlignmentOptions.Center, true);
                bonusName.outlineWidth = 0.15f;
                bonusName.outlineColor = Theme.Ink;
                bonusName.enableWordWrapping = false;
                bonusName.enableAutoSizing = true;
                bonusName.fontSizeMin = 28;
                bonusName.fontSizeMax = 52;
                UIKit.PlaceCenter(bonusName.rectTransform, 150, 82, 300, 70);
                bonusName.gameObject.AddComponent<ShimmerText>();
                bonusValue = UIKit.Text(bonusBox, "Value", "", 30, Theme.Hex("#FFD27A"), TextAlignmentOptions.Center, true);
                UIKit.PlaceCenter(bonusValue.rectTransform, 150, 145, 300, 40);
                bonusValue.gameObject.AddComponent<ShimmerText>();

                // 合并同一来源的连续步骤，避免一张卡挂很多小步骤时动画过长
                var steps = side.Score.Steps;
                for (int i = 0; i < steps.Count;)
                {
                    var s = steps[i];
                    int j = i;
                    double add = 0, times = 1;
                    while (j < steps.Count && steps[j].Source == s.Source && steps[j].Kind == s.Kind && steps[j].CardUid == s.CardUid
                        && steps[j].Trigger == s.Trigger)
                    {
                        if (steps[j].Kind == ScoreStepKind.TimesMult) times *= steps[j].Value; else add += steps[j].Value;
                        j++;
                    }
                    var last = steps[j - 1];
                    var change = s.Kind == ScoreStepKind.Points ? $"战力 +{Fmt.Num(add)}"
                        : s.Kind == ScoreStepKind.AddMult ? $"气势 +{Fmt.Num(add)}" : $"气势 ×{Fmt.Num(times)}";
                    // 重复触发时标出是第几次，让玩家看懂为什么同一来源又结算了一遍
                    if (s.Triggers > 1) change += $"（触发 {s.Trigger}/{s.Triggers}）";
                    Events.Add(new Event
                    {
                        CardUid = s.CardUid, Label = $"{s.Source} {change}", Source = s.Source, Change = change, Phase = s.Phase,
                        Points = last.Points, Mult = last.Mult,
                    });
                    i = j;
                }
            }

            private static RectTransform Back(RectTransform holder)
            {
                var skin = ChessUISkin.Instance;
                var frame = UIKit.Panel(holder, "Back", Theme.Hex("#1E2A28"), Theme.Gold, 2.5f);
                UIKit.Fill(frame.rectTransform);
                if (skin.cardBack != null)
                {
                    var img = UIKit.Image(frame.transform, "Art", Color.white, skin.cardBack);
                    UIKit.Fill(img.rectTransform, 6);
                }
                else
                {
                    UIKit.Text(frame.transform, "Mark", "符", 48, Theme.Gold, TextAlignmentOptions.Center, true);
                }
                return frame.rectTransform;
            }

            public Sequence Build(float per)
            {
                var seq = DOTween.Sequence();
                foreach (var e in Events)
                {
                    var ev = e;
                    if (!string.IsNullOrEmpty(ev.CardUid) && cards.TryGetValue(ev.CardUid, out var f))
                    {
                        if (!f.Open)
                        {
                            f.Open = true;
                            var flipT = Mathf.Min(0.14f, per * 0.35f);
                            seq.Append(f.Root.DOScaleX(0, flipT));
                            seq.AppendCallback(() => { f.Back.gameObject.SetActive(false); f.Face.gameObject.SetActive(true); });
                            seq.Append(f.Root.DOScaleX(1, flipT));
                        }
                        else
                        {
                            seq.Append(f.Root.DOPunchScale(Vector3.one * 0.12f, Mathf.Min(0.2f, per * 0.5f), 6, 0.5f));
                        }
                    }
                    seq.AppendCallback(() =>
                    {
                        if (ev.Phase == PhaseBond || ev.Phase == PhaseRelic)
                        {
                            ShowBonus(ev);
                            ticker.text = "";
                        }
                        else
                        {
                            HideBonus();
                            ticker.text = ev.Label;
                            ticker.alpha = 1;
                        }
                        formula.text = $"战力 {Fmt.Num(ev.Points)} × 气势 {Fmt.Num(ev.Mult)}";
                        var target = (float)Math.Floor(ev.Points * ev.Mult);
                        var dur = Mathf.Max(0.1f, per * 0.8f);
                        if (target > shown) Shake(dur);
                        DOTween.To(() => shown, v => { shown = v; scoreText.text = Fmt.Score(v); }, target, dur);
                    });
                    seq.AppendInterval(Mathf.Max(0.05f, per - 0.1f));
                }
                return seq;
            }

            private const int PhaseBond = 3, PhaseRelic = 5; // ScoreStep.phase：3 羁绊 5 法宝

            /// <summary>在积分外侧显示正在结算的羁绊 / 法宝：首次滑入，之后每步轻弹一下。</summary>
            private void ShowBonus(Event ev)
            {
                bonusKind.text = ev.Phase == PhaseBond ? "羁 绊" : "法 宝";
                bonusName.text = ev.Source;
                bonusValue.text = ev.Change;
                DOTween.Kill(bonusBox);
                if (!bonusShown)
                {
                    bonusShown = true;
                    bonusBox.anchoredPosition = new Vector2(bonusX + bonusFrom, -ScoreY);
                    bonusBox.localScale = Vector3.one;
                    bonusBox.DOAnchorPos(new Vector2(bonusX, -ScoreY), 0.3f).SetEase(Ease.OutCubic).SetTarget(bonusBox);
                    bonusGroup.DOFade(1, 0.25f).SetTarget(bonusBox);
                }
                else
                {
                    bonusBox.anchoredPosition = new Vector2(bonusX, -ScoreY);
                    bonusGroup.alpha = 1;
                    bonusBox.localScale = Vector3.one;
                    bonusBox.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetTarget(bonusBox);
                }
            }

            private void HideBonus()
            {
                if (!bonusShown) return;
                bonusShown = false;
                DOTween.Kill(bonusBox);
                bonusGroup.DOFade(0, 0.25f).SetTarget(bonusBox);
            }

            /// <summary>积分增长时数字晃动：先结束上一次晃动（回到原位），再左右摆动 + 轻微弹缩。</summary>
            private void Shake(float duration)
            {
                var skin = ChessUISkin.Instance;
                var rt = scoreText.rectTransform;
                rt.DOKill(true);
                rt.DOShakeRotation(duration, new Vector3(0, 0, skin.scoreShakeAngle), 14, 90, true).SetTarget(rt);
                rt.DOShakeAnchorPos(duration, skin.scoreShakeOffset, 18, 90, false, true).SetTarget(rt);
                rt.DOPunchScale(Vector3.one * 0.12f, duration, 6, 0.6f).SetTarget(rt);
            }

            /// <summary>演出结束（或被跳过）时，确保所有卡翻开、显示最终分数。</summary>
            public void Finish()
            {
                foreach (var f in cards.Values)
                {
                    f.Root.localScale = Vector3.one;
                    f.Back.gameObject.SetActive(false);
                    f.Face.gameObject.SetActive(true);
                }
                scoreText.rectTransform.DOKill(true);
                shown = (float)side.Score.Score;
                scoreText.text = Fmt.Score(side.Score.Score);
                formula.text = $"战力 {Fmt.Num(side.Score.Points)} × 气势 {Fmt.Num(side.Score.Mult)}";
                ticker.text = "";
                HideBonus();
            }

            public void Highlight()
            {
                scoreText.color = Theme.Hex("#FFD27A");
                scoreText.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6, 0.5f);
            }
        }
    }
}
