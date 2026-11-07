using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>对局结束弹窗。</summary>
    public sealed class GameOverPopup
    {
        private readonly RectTransform root;
        private readonly Action onRematch;
        private readonly Action onLobby;
        private TextMeshProUGUI status;

        public GameOverPopup(RectTransform layer, Action rematch, Action lobby)
        {
            root = UIKit.Rect(layer, "GameOver");
            UIKit.Fill(root);
            onRematch = rematch;
            onLobby = lobby;
            root.gameObject.SetActive(false);
        }

        public void Hide() => UIFx.Out(root.gameObject, UIFx.Style.Fade);

        public void Show(MatchOver over)
        {
            UIKit.Clear(root);
            UIFx.In(root.gameObject, UIFx.Style.Fade, 0.35f);
            var dim = UIKit.Image(root, "Dim", Theme.Dim, raycast: true);
            UIKit.FillScreen(dim.rectTransform);
            var panel = UIKit.Panel(root, "Panel", Theme.Paper, Theme.Ink, 2.5f);
            UIKit.PlaceCenter(panel.rectTransform, 960, 520, 640, 420);
            panel.rectTransform.DOScale(1f, 0.45f).From(0.7f).SetEase(Ease.OutBack).SetDelay(0.1f);
            var word = over.Result > 0 ? Fmt.T("game.win", "你胜利了！") : over.Result < 0 ? Fmt.T("game.lose", "你失败了！") : Fmt.T("chess.draw", "平局");
            var t = UIKit.Text(panel.transform, "Result", word, 56, over.Result > 0 ? Theme.Red : Theme.Ink, TextAlignmentOptions.Top, true);
            UIKit.Place(t.rectTransform, 0, 50, 640, 80);
            var why = over.Surrendered ? (over.Result < 0 ? "你已认输 · " : "对手认输 · ") : "";
            var info = UIKit.Text(panel.transform, "Info", $"{why}共 {over.Rounds} 回合 · 我方血量 {over.Hp} · 对手血量 {over.OpponentHp}", 20, Theme.InkSoft, TextAlignmentOptions.Top);
            UIKit.Place(info.rectTransform, 0, 150, 640, 30);
            status = UIKit.Text(panel.transform, "Status", "", 16, Theme.CyanDeep, TextAlignmentOptions.Top);
            UIKit.Place(status.rectTransform, 0, 200, 640, 30);
            var again = UIKit.Button(panel.transform, "Rematch", Theme.Red, Theme.Ink, Fmt.T("chess.rematch", "再来一局"), 22, Theme.Paper, () =>
            {
                onRematch();
            });
            UIKit.Place((RectTransform)again.transform, 120, 290, 180, 60);
            var lobby = UIKit.Button(panel.transform, "Lobby", Theme.Paper, Theme.Ink, Fmt.T("chess.lobby", "返回大厅"), 22, Theme.Ink, onLobby);
            UIKit.Place((RectTransform)lobby.transform, 340, 290, 180, 60);
        }
    }
}
