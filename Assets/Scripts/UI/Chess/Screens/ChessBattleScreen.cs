using System.Collections;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine;
using RepGame.Core;
using RepGame.Data;
using RepGame.Logic;
using RepGame.Network;
using RepGame.Protocol;

namespace RepGame.UI.Chess
{
    /// <summary>
    /// 新版对局界面（全部由代码构建，不依赖场景里的旧 Panel_Main）。
    /// 分层：常驻层（血量、出阵、手牌、法宝、羁绊、金币、就绪）→ 拖拽层 → 弹出层
    /// （商店、结算、选择、结束）→ 提示层。只显示服务器数据，只发送操作意图。
    /// </summary>
    public sealed class ChessBattleScreen : MonoBehaviour
    {
        private const string LobbyPanel = "Panel_Start";

        private Canvas canvas;
        private RectTransform baseLayer, dragLayer, popupLayer, fxLayer, tipLayer;

        private TooltipLayer tooltip;
        private DragController drag;
        private PlayerHud enemyHud, selfHud;
        private TimerView timer;
        private BondBarView bonds;
        private OmenBarView omenBar;
        private BoardBenchView board;
        private RelicBarView relics;
        private ScoreDockView scoreDock;
        private ShopView shop;
        private PickPopup picker;
        private OmenDicePopup omenPopup;
        private BattleCutscene cutscene;
        private GameOverPopup gameOver;
        private OpponentPopover opponentBoard;
        private Toast toast;
        private RectTransform enemyRelics;
        private RectTransform surrenderButton;
        private ConfirmPopup confirm;
        private LoadingScreen loader;
        private Coroutine loading;
        private bool stateReady;      // 本次加载后已收到并画好对局数据
        private bool resuming;        // 断线后等待恢复对局
        private bool configSinceReconnect;
        private const float ResumeTimeoutSeconds = 10f;

        public static ChessBattleScreen Create()
        {
            var go = new GameObject("ChessBattleScreen");
            DontDestroyOnLoad(go);
            return go.AddComponent<ChessBattleScreen>();
        }

        private void Awake()
        {
            EnsureEventSystem();
            Build();
            canvas.gameObject.SetActive(false);
            var s = ChessSession.Instance;
            s.ConfigReceived += OnConfig;
            s.StateChanged += OnState;
            s.RoundSettled += OnRound;
            s.MatchEnded += OnOver;
            s.ActionFailed += OnFailed;
            loader = new LoadingScreen(transform);
            EventManager.Subscribe<string>(NetEvents.Disconnected, OnDisconnected);
            EventManager.Subscribe<string>(NetEvents.ConnectedToServer, OnReconnected);
        }

        private void OnDestroy()
        {
            EventManager.Unsubscribe<string>(NetEvents.Disconnected, OnDisconnected);
            EventManager.Unsubscribe<string>(NetEvents.ConnectedToServer, OnReconnected);
            var s = ChessSession.Instance;
            s.ConfigReceived -= OnConfig;
            s.StateChanged -= OnState;
            s.RoundSettled -= OnRound;
            s.MatchEnded -= OnOver;
            s.ActionFailed -= OnFailed;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        // ---------- 构建 ----------

        private void Build()
        {
            var cgo = new GameObject("ChessCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 设计稿坐标的根节点：固定 1920×1080，居中
            var stage = UIKit.Rect(cgo.transform, "Stage");
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(1920, 1080);

            var bg = UIKit.Image(stage, "Background", Theme.Water);
            UIKit.Fill(bg.rectTransform, -400);
            baseLayer = Layer(stage, "Base");
            dragLayer = Layer(stage, "Drag");
            popupLayer = Layer(stage, "Popup");
            fxLayer = Layer(stage, "Fx");          // 火焰、伤害数字：在 HUD 和弹出层之上
            tipLayer = Layer(stage, "Tooltip");

            tooltip = new TooltipLayer(tipLayer);
            drag = new DragController(dragLayer);
            drag.DraggingChanged += dragging => { if (dragging) tooltip.Hide(null); };

            enemyHud = new PlayerHud(baseLayer, 32, 24, false);
            enemyRelics = UIKit.Rect(baseLayer, "EnemyRelics");
            UIKit.Place(enemyRelics, 370, 36, 400, 40);
            selfHud = new PlayerHud(baseLayer, 32, 1080 - 32 - 64, true);
            timer = new TimerView(baseLayer);
            bonds = new BondBarView(baseLayer, tooltip);
            omenBar = new OmenBarView(baseLayer, tooltip);
            board = new BoardBenchView(baseLayer, drag, tooltip);
            relics = new RelicBarView(baseLayer, drag, tooltip);
            scoreDock = new ScoreDockView(baseLayer);

            picker = new PickPopup(popupLayer, tooltip);
            omenPopup = new OmenDicePopup(popupLayer, omenBar);
            shop = new ShopView(baseLayer, popupLayer, drag, tooltip);
            cutscene = new BattleCutscene(popupLayer, fxLayer, selfHud, enemyHud);
            opponentBoard = new OpponentPopover(popupLayer);
            gameOver = new GameOverPopup(popupLayer, Rematch, BackToLobby);
            toast = new Toast(tipLayer);
            confirm = new ConfirmPopup(popupLayer);

            var giveUp = UIKit.Button(baseLayer, "Surrender", Theme.WithAlpha(Theme.Paper, 0.9f), Theme.InkSoft,
                Fmt.T("chess.surrender", "认输"), 16, Theme.InkSoft, () => confirm.Show(
                    Fmt.T("chess.surrender.confirm", "确定认输吗？本局将直接判负。"),
                    Fmt.T("chess.surrender", "认输"), ChessActions.Surrender));
            surrenderButton = (RectTransform)giveUp.transform;
            UIKit.Place(surrenderButton, 1920 - 32 - 88, 28, 88, 38);

            enemyHud.Avatar.gameObject.AddComponent<Button>().onClick.AddListener(() => opponentBoard.Toggle(ChessSession.Instance.State));
        }

        private static RectTransform Layer(RectTransform parent, string name)
        {
            var rt = UIKit.Rect(parent, name);
            UIKit.Fill(rt);
            return rt;
        }

        // ---------- 会话事件 ----------

        private void OnConfig(MatchConfig config)
        {
            gameOver.Hide();
            cutscene.Stop();
            picker.Hide();
            omenPopup.Reset();
            if (UIPanelController.Instance != null) UIPanelController.Instance.HideAllPanels();
            UIFx.In(canvas.gameObject, UIFx.Style.Fade, 0.4f);

            // 加载界面盖住对局界面，直到资源和对局数据都就绪
            configSinceReconnect = true;
            loader.Begin(resuming ? "正在恢复对局" : "进入对局");
            loader.SetProgress(0.3f, "对局配置已加载");
            stateReady = false;
            if (loading != null) StopCoroutine(loading);
            loading = StartCoroutine(Load(config));
        }

        private void OnState(MatchState previous, MatchState st)
        {
            if (!UIFx.IsShown(canvas.gameObject)) OnConfig(ChessSession.Instance.Config);
            Render(st);
            // 天象阶段 → 准备阶段且本回合天象已揭晓：播一次揭晓动画（断线重连直接进入准备阶段时不播）
            if (previous != null && previous.Phase == MatchPhase.Omen && st.Phase == MatchPhase.Prep
                && st.OmenRoll != null && st.OmenRoll.Round == st.Round)
                omenPopup.Reveal(st.OmenRoll);
            stateReady = true;
        }

        // ---------- 加载 / 断线重连 ----------

        /// <summary>预载卡图、法宝图标和要用到的文字，再等第一份对局数据画好，然后撤掉加载界面。</summary>
        private IEnumerator Load(MatchConfig config)
        {
            var skin = ChessUISkin.Instance;
            var text = new StringBuilder("0123456789+-×/·%★ ");
            int total = Mathf.Max(1, config.Lines.Count + config.Relics.Count), done = 0;
            foreach (var l in config.Lines)
            {
                foreach (var n in l.StarNames)
                {
                    skin.Card(n);
                    text.Append(n);
                }
                text.Append(l.Name).Append(l.LegendDesc);
                if (++done % 4 == 0)
                {
                    loader.SetProgress(0.3f + 0.4f * done / total, "加载卡牌资源");
                    yield return null;
                }
            }
            foreach (var r in config.Relics)
            {
                skin.RelicIcon(r.Key);
                text.Append(r.Name).Append(r.Desc);
                if (++done % 4 == 0)
                {
                    loader.SetProgress(0.3f + 0.4f * done / total, "加载法宝");
                    yield return null;
                }
            }
            foreach (var b in config.Trades) text.Append(b.Name).Append(b.Desc);
            foreach (var b in config.Origins) text.Append(b.Name).Append(b.Desc);
            foreach (var f in config.Families) text.Append(f.Name).Append(f.Desc);
            foreach (var o in config.Omens) text.Append(o.Name).Append(o.Aspect).Append(o.Desc);
            text.Append("剩回合失和门槛天象");
            loader.SetProgress(0.75f, "准备文字");
            yield return null;
            var font = skin.UIFont;
            if (font != null) font.TryAddCharacters(text.ToString());

            float waited = 0;
            while (!stateReady && waited < ResumeTimeoutSeconds)
            {
                loader.SetProgress(0.85f, "同步对局数据");
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return null; // 让布局完成一帧
            yield return null;
            resuming = false;
            loading = null;
            loader.Finish();
        }

        private void OnDisconnected(string _)
        {
            if (!UIFx.IsShown(canvas.gameObject) || ChessSession.Instance.Over != null) return;
            resuming = true;
            configSinceReconnect = false;
            omenPopup.Stop();
            if (loading != null) StopCoroutine(loading);
            loading = null;
            loader.Begin("连接断开，正在重连");
            loader.SetProgress(0.05f, "正在连接服务器");
        }

        private void OnReconnected(string _)
        {
            if (!resuming) return;
            loader.SetProgress(0.15f, "正在恢复对局");
            StartCoroutine(ResumeTimeout());
        }

        /// <summary>重连后一段时间仍没收到对局配置：对局已不存在（例如服务器重启），回到大厅。</summary>
        private IEnumerator ResumeTimeout()
        {
            yield return new WaitForSecondsRealtime(ResumeTimeoutSeconds);
            if (!resuming || configSinceReconnect) yield break;
            resuming = false;
            loader.Cancel();
            BackToLobby();
            Debug.LogWarning("重连后没有收到对局数据，对局可能已结束");
        }

        private void Render(MatchState st)
        {
            var cfg = ChessSession.Instance.Config;
            // 收到对局配置前用 client_settings.json 的兜底血量上限
            var maxHp = cfg != null && cfg.StartHp > 0 ? cfg.StartHp : (int)ConfigTables.Settings.gameplay.fallbackMaxHealth;
            var o = st.Opponent;
            enemyHud.Set(o.Hp, maxHp, $"{o.Level} 级 · 手牌 {o.CardCount}{(o.Connected ? "" : " · 离线")}");
            var self = st.Self;
            var xp01 = self.XpToNext > 0 ? (float)self.Xp / self.XpToNext : 1f;
            selfHud.Set(self.Hp, maxHp, self.XpToNext > 0 ? $"{self.Level} 级 · 经验 {self.Xp}/{self.XpToNext}" : $"{self.Level} 级 · 满级", xp01);

            UIKit.Clear(enemyRelics);
            for (int i = 0; i < o.Relics.Count; i++)
            {
                var r = o.Relics[i];
                var icon = RelicIcon.Build(enemyRelics, r.RelicKey, 38);
                UIKit.Place(icon, i * 44, 0, 38, 38);
                var x = 370 + i * 44;
                UIKit.Hover(icon.gameObject, () => RelicIcon.ShowTip(tooltip, new RelicSlot { RelicKey = r.RelicKey, Value = r.Value, TradeChoice = r.TradeChoice }, x - 100, 84), () => tooltip.Hide(null));
            }

            UIFx.Toggle(surrenderButton.gameObject, st.Phase != MatchPhase.Over);
            timer.Tick(st);
            bonds.Render(st);
            omenBar.Render(st);
            board.Render(st);
            relics.Render(st);
            shop.Render(st);
            scoreDock.Render(self.Preview, st.Phase == MatchPhase.Prep);

            switch (st.Phase)
            {
                case MatchPhase.Family:
                    picker.ShowFamilies(st);
                    break;
                case MatchPhase.Vault:
                    picker.ShowVault(st);
                    break;
                case MatchPhase.Omen:
                    picker.Hide();
                    omenPopup.ShowChoice(st);
                    break;
                default:
                    picker.Hide();
                    omenPopup.Hide();
                    break;
            }
        }

        private void OnRound(RoundResult rr) => cutscene.Play(rr);

        private void OnOver(MatchOver over)
        {
            confirm.Hide();
            UIFx.Out(surrenderButton.gameObject);
            picker.Hide();
            omenPopup.Reset();
            shop.SetOpen(false);
            gameOver.Show(over);
        }

        private void OnFailed(string message)
        {
            if (canvas.gameObject.activeSelf) toast.Show(message);
            omenPopup.OnActionFailed();
        }

        private void Update()
        {
            if (canvas.gameObject.activeSelf) timer.Tick(ChessSession.Instance.State);
        }

        // ---------- 结束后 ----------

        /// <summary>再来一局：回到大厅并直接开始匹配（上方出现匹配计时框）。</summary>
        private void Rematch()
        {
            BackToLobby();
            MatchmakingBar.Instance?.Begin();
        }

        private void BackToLobby()
        {
            gameOver.Hide();
            omenPopup.Reset();
            UIFx.Out(canvas.gameObject, UIFx.Style.Fade, null, 0.3f);
            ChessSession.Instance.Clear();
            if (UIPanelController.Instance != null) UIPanelController.Instance.ShowPanel(LobbyPanel, true);
        }
    }
}
