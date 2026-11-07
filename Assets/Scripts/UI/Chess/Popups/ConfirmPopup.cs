using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace RepGame.UI.Chess
{
    /// <summary>二次确认框（认输等不可撤销的操作）。</summary>
    public sealed class ConfirmPopup
    {
        private readonly RectTransform root;

        public ConfirmPopup(RectTransform layer)
        {
            root = UIKit.Rect(layer, "Confirm");
            UIKit.Fill(root);
            root.gameObject.SetActive(false);
        }

        public void Hide() => UIFx.Out(root.gameObject, UIFx.Style.Fade);

        public void Show(string message, string okLabel, Action onOk)
        {
            UIKit.Clear(root);
            UIFx.In(root.gameObject, UIFx.Style.Fade);
            root.SetAsLastSibling();
            var dim = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
            UIKit.Fill(dim.rectTransform);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2.5f);
            panel.raycastTarget = true;
            UIKit.PlaceCenter(panel.rectTransform, 960, 520, 520, 240);
            panel.rectTransform.DOScale(1f, 0.3f).From(0.85f).SetEase(Ease.OutBack);
            var text = UIKit.Text(panel.transform, "Message", message, 22, Theme.Ink, TextAlignmentOptions.Center, true);
            UIKit.Place(text.rectTransform, 30, 30, 460, 100);
            var ok = UIKit.Button(panel.transform, "Ok", Theme.Red, Theme.Ink, okLabel, 20, Theme.Paper, () => { Hide(); onOk(); });
            UIKit.Place((RectTransform)ok.transform, 70, 160, 170, 54);
            var cancel = UIKit.Button(panel.transform, "Cancel", Theme.Paper, Theme.Ink, Fmt.T("chess.cancel", "取消"), 20, Theme.Ink, Hide);
            UIKit.Place((RectTransform)cancel.transform, 280, 160, 170, 54);
        }
    }
}
