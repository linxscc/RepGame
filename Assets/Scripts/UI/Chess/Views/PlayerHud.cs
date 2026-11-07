using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>血量条 + 一行文字（左上对手 / 左下我方共用）。</summary>
    public sealed class PlayerHud
    {
        private readonly Image fill;
        private readonly TextMeshProUGUI hpText;
        private readonly TextMeshProUGUI infoText;
        private readonly Image xpFill;

        public RectTransform Avatar { get; }

        private readonly CanvasGroup avatarGroup;

        public PlayerHud(RectTransform parent, float x, float y, bool self, TooltipLayer tooltip = null)
        {
            var skin = ChessUISkin.Instance;
            var avatar = UIKit.Circle(parent, self ? "SelfAvatar" : "EnemyAvatar", self ? Theme.Paper : Theme.Ink, Theme.Ink);
            UIKit.Place(avatar.rectTransform, x, y, 64, 64);
            var pic = self ? skin.avatarMe : skin.avatarEnemy;
            if (pic != null)
            {
                var img = UIKit.Image(avatar.transform, "Pic", Color.white, pic);
                UIKit.Fill(img.rectTransform, 4);
            }
            else
            {
                UIKit.Text(avatar.transform, "Char", self ? "我" : "敌", 24, self ? Theme.Ink : Theme.Paper, bold: true);
            }
            avatar.raycastTarget = true;
            Avatar = avatar.rectTransform;
            avatarGroup = avatar.gameObject.AddComponent<CanvasGroup>();

            var block = UIKit.Rect(parent, self ? "SelfHp" : "EnemyHp");
            UIKit.Place(block, x + 78, y + 4, 240, 56);
            hpText = UIKit.Live(UIKit.Text(block, "Hp", "", 16, Theme.Ink, TextAlignmentOptions.TopLeft, true), 18); // 「100 / 100」
            infoText = UIKit.Live(UIKit.Text(block, "Info", "", 15, Theme.InkSoft, TextAlignmentOptions.TopRight), 18); // 「10 级」
            var bar = UIKit.Panel(block, "Bar", Theme.Paper, Theme.Ink, 1.5f);
            UIKit.Place(bar.rectTransform, 0, 24, 240, 14);
            fill = UIKit.Image(bar.transform.GetChild(0), "Fill", Theme.Red);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1, 1);
            fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            if (self)
            {
                var xpBar = UIKit.Image(block, "Xp", Theme.WithAlpha(Theme.Ink, 0.12f));
                UIKit.Place(xpBar.rectTransform, 0, 44, 240, 6);
                xpFill = UIKit.Image(xpBar.transform, "Fill", Theme.CyanDeep);
                xpFill.rectTransform.anchorMin = Vector2.zero;
                xpFill.rectTransform.anchorMax = new Vector2(0, 1);
                xpFill.rectTransform.pivot = new Vector2(0, 0.5f);
                xpFill.rectTransform.offsetMin = xpFill.rectTransform.offsetMax = Vector2.zero;
                // 经验数值下沉为悬停提示（经验条只有 6px，悬停区域放大到 16px）
                var hit = UIKit.Image(block, "XpHit", Theme.Clear, raycast: true);
                UIKit.Place(hit.rectTransform, 0, 39, 240, 16);
                if (tooltip != null)
                {
                    UIKit.Hover(hit.gameObject, () => tooltip.Show(this, x + 78, y - 96, 220, Fmt.T("chess.hud.xp", "经验"), null, xpTip), () => tooltip.Hide(this));
                }
            }
        }

        private string xpTip = "";
        private float shownHp = -1;
        private int targetHp;
        private int max = 100;
        private bool frozen;

        public void Set(int hp, int maxHp, string info, float xp01 = -1, string xpDetail = null, bool online = true)
        {
            infoText.text = info;
            if (xpDetail != null) xpTip = xpDetail;
            avatarGroup.alpha = online ? 1f : 0.4f;
            max = maxHp;
            targetHp = hp;
            if (xpFill != null) xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(xp01 < 0 ? 1 : xp01), 1);
            if (shownHp < 0)
            {
                DOTween.Kill(this);
                ShowHp(hp);
                return;
            }
            if (!frozen) AnimateHp();
        }

        /// <summary>过场动画期间保持旧血量，等攻击命中后再下降。</summary>
        public void Freeze() => frozen = true;

        public void Release()
        {
            frozen = false;
            AnimateHp();
        }

        /// <summary>受击：头像震动。</summary>
        public void Hit()
        {
            DOTween.Complete(Avatar);
            Avatar.DOShakeAnchorPos(0.45f, 14f, 30, 90, false, true).SetTarget(Avatar);
        }

        private void AnimateHp()
        {
            // 还没收到过血量（新建或重连后的第一帧）时不做动画，否则会从 -1 走向默认的 0 并停在那里
            if (shownHp < 0 || Mathf.Approximately(shownHp, targetHp)) return;
            DOTween.Kill(this);
            DOTween.To(() => shownHp, ShowHp, targetHp, ChessUISkin.Instance.hpDrainSeconds).SetEase(Ease.OutCubic).SetTarget(this);
        }

        private void ShowHp(float hp)
        {
            shownHp = hp;
            hpText.text = $"{Mathf.RoundToInt(hp)} / {max}";
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(max > 0 ? hp / max : 0), 1);
        }
    }
}
