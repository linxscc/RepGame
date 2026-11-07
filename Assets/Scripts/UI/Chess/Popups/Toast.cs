using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>屏幕上方的短提示（操作失败等）。</summary>
    public sealed class Toast
    {
        private readonly RectTransform root;
        private readonly TextMeshProUGUI text;
        private readonly CanvasGroup group;

        public Toast(RectTransform layer)
        {
            var bg = UIKit.Panel(layer, "Toast", Theme.Ink, Theme.Ink, 0);
            root = bg.rectTransform;
            UIKit.PlaceCenter(root, 960, 160, 520, 48);
            text = UIKit.Text(root, "Text", "", 18, Theme.Paper, bold: true);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0;
        }

        public void Show(string message)
        {
            text.text = message;
            DOTween.Kill(group);
            group.alpha = 1;
            group.DOFade(0, 0.4f).SetDelay(1.6f).SetTarget(group);
            root.SetAsLastSibling();
        }
    }
}
