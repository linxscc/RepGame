using TMPro;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Core;
using RepGame.Logic;
using RepGame.Model;
using RepGame.Network;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 匹配计时框：点击“开始游戏”后浮在屏幕上方，显示已等待时间，右侧 × 取消匹配。
    /// 收到对局数据、服务器确认已不在匹配中、或匹配请求失败时自动收起。
    /// </summary>
    public sealed class MatchmakingBar : MonoBehaviour
    {
        public static MatchmakingBar Instance { get; private set; }

        private RectTransform box;
        private TextMeshProUGUI label;
        private float startedAt;
        private bool searching;

        public bool Searching => searching;

        public static MatchmakingBar Create()
        {
            var go = new GameObject("MatchmakingBar");
            DontDestroyOnLoad(go);
            return go.AddComponent<MatchmakingBar>();
        }

        private void Awake()
        {
            Instance = this;
            var cgo = new GameObject("MatchmakingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = UIKit.Panel(cgo.transform, "Box", Theme.WithAlpha(Theme.Paper, 0.96f), Theme.Ink, 2f);
            panel.raycastTarget = true;
            box = panel.rectTransform;
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 1);
            box.pivot = new Vector2(0.5f, 1);
            box.sizeDelta = new Vector2(380, 56);
            box.anchoredPosition = new Vector2(0, -24);

            label = UIKit.Text(box, "Label", "", 20, Theme.Ink, TextAlignmentOptions.Left, true);
            label.rectTransform.offsetMin = new Vector2(24, 0);
            label.rectTransform.offsetMax = new Vector2(-64, 0);

            var close = UIKit.Button(box, "Cancel", Theme.Paper, Theme.InkSoft, "×", 22, Theme.InkSoft, Cancel);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 0.5f);
            crt.pivot = new Vector2(1, 0.5f);
            crt.sizeDelta = new Vector2(40, 40);
            crt.anchoredPosition = new Vector2(-10, 0);

            box.gameObject.SetActive(false);

            var s = ChessSession.Instance;
            s.ConfigReceived += _ => Hide();
            s.MatchmakingChanged += OnServerState;
            EventManager.Subscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
        }

        private void OnDestroy()
        {
            EventManager.Unsubscribe<RequestFailure>(NetEvents.RequestFailed, OnRequestFailed);
            if (Instance == this) Instance = null;
        }

        /// <summary>开始匹配（已在匹配中时不重复发送）。</summary>
        public void Begin()
        {
            if (searching || GameTcpClient.Instance == null) return;
            searching = true;
            startedAt = Time.unscaledTime;
            UIFx.In(box.gameObject, UIFx.Style.DropIn);
            Tick();
            GameTcpClient.Instance.Ready();
        }

        public void Cancel()
        {
            if (!searching) return;
            Hide();
            GameTcpClient.Instance?.CancelMatch();
        }

        private void Hide()
        {
            searching = false;
            UIFx.Out(box.gameObject, UIFx.Style.DropIn);
        }

        private void OnServerState(bool serverSearching)
        {
            if (!serverSearching) Hide();
        }

        private void OnRequestFailed(RequestFailure failure)
        {
            if (failure.Request == RequestKind.Ready) Hide();
        }

        private void Update()
        {
            if (searching) Tick();
        }

        private void Tick()
        {
            int sec = Mathf.FloorToInt(Time.unscaledTime - startedAt);
            label.text = Fmt.T("chess.matching", "正在匹配对手  {0:00}:{1:00}", sec / 60, sec % 60);
        }
    }
}
