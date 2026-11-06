using RepGame.Protocol;
using RepGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RepGame.Chess.UI
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
        private BoardBenchView board;
        private RelicBarView relics;
        private ScoreDockView scoreDock;
        private ShopView shop;
        private PickPopup picker;
        private BattleCutscene cutscene;
        private GameOverPopup gameOver;
        private OpponentPopover opponentBoard;
        private Toast toast;
        private RectTransform enemyRelics;
        private RectTransform surrenderButton;
        private ConfirmPopup confirm;

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
        }

        private void OnDestroy()
        {
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
            board = new BoardBenchView(baseLayer, drag, tooltip);
            relics = new RelicBarView(baseLayer, drag, tooltip);
            scoreDock = new ScoreDockView(baseLayer);

            picker = new PickPopup(popupLayer, tooltip);
            shop = new ShopView(baseLayer, popupLayer, drag, tooltip, picker);
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
            if (UIPanelController.Instance != null) UIPanelController.Instance.HideAllPanels();
            canvas.gameObject.SetActive(true);
        }

        private void OnState(MatchState previous, MatchState st)
        {
            if (!canvas.gameObject.activeSelf) OnConfig(ChessSession.Instance.Config);
            Render(st);
        }

        private void Render(MatchState st)
        {
            var cfg = ChessSession.Instance.Config;
            var maxHp = cfg != null && cfg.StartHp > 0 ? cfg.StartHp : 100;
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

            surrenderButton.gameObject.SetActive(st.Phase != MatchPhase.Over);
            timer.Tick(st);
            bonds.Render(st);
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
                case MatchPhase.Prep:
                    picker.Hide();
                    break;
                case MatchPhase.Settle:
                    picker.Hide();
                    break;
            }
        }

        private void OnRound(RoundResult rr) => cutscene.Play(rr);

        private void OnOver(MatchOver over)
        {
            confirm.Hide();
            surrenderButton.gameObject.SetActive(false);
            picker.Hide();
            shop.SetOpen(false);
            gameOver.Show(over);
        }

        private void OnFailed(string message)
        {
            if (canvas.gameObject.activeSelf) toast.Show(message);
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
            canvas.gameObject.SetActive(false);
            ChessSession.Instance.Clear();
            if (UIPanelController.Instance != null) UIPanelController.Instance.ShowPanel(LobbyPanel, true);
        }
    }
}
