using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using RepGame.Protocol;

namespace RepGame.Logic
{
    /// <summary>
    /// 对局数据的唯一来源：只保存服务器下发的最新消息，不做任何规则计算。
    /// UI 订阅这里的事件来刷新显示。
    /// </summary>
    public sealed class ChessSession
    {
        public static ChessSession Instance { get; } = new ChessSession();

        public MatchConfig Config { get; private set; }
        public MatchState State { get; private set; }
        public RoundResult LastRound { get; private set; }
        public MatchOver Over { get; private set; }

        /// <summary>服务器时间 - 本地时间（毫秒），由 Welcome 校准。</summary>
        public long ServerTimeOffsetMs { get; private set; }

        public event Action<MatchConfig> ConfigReceived;
        public event Action<MatchState, MatchState> StateChanged;   // (previous, current)
        public event Action<RoundResult> RoundSettled;
        public event Action<MatchOver> MatchEnded;
        public event Action<string> ActionFailed;
        public event Action<bool> MatchmakingChanged; // 服务器确认的匹配状态

        private readonly Dictionary<string, LineInfo> lines = new Dictionary<string, LineInfo>();
        private readonly Dictionary<string, RelicInfo> relics = new Dictionary<string, RelicInfo>();
        private readonly Dictionary<string, BondInfo> bonds = new Dictionary<string, BondInfo>();

        public bool InMatch => Config != null && State != null && Over == null;

        public static long LocalNowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public long ServerNowMs => LocalNowMs + ServerTimeOffsetMs;

        // ---------- 由 ServerMessageRouter 调用 ----------

        public void OnWelcome(long serverTimeMs)
        {
            ServerTimeOffsetMs = serverTimeMs - LocalNowMs;
        }

        public void OnMatchConfig(MatchConfig config)
        {
            Config = config;
            Over = null;
            LastRound = null;
            lines.Clear();
            relics.Clear();
            bonds.Clear();
            foreach (var l in config.Lines) lines[l.Key] = l;
            foreach (var r in config.Relics) relics[r.Key] = r;
            foreach (var b in config.Trades) bonds[b.Key] = b;
            foreach (var b in config.Origins) bonds[b.Key] = b;
            ConfigReceived?.Invoke(config);
        }

        public void OnMatchState(MatchState state)
        {
            if (Config == null)
            {
                Debug.LogWarning("收到 MatchState 但还没有 MatchConfig，忽略");
                return;
            }
            var previous = State;
            State = state;
            StateChanged?.Invoke(previous, state);
        }

        public void OnRoundResult(RoundResult result)
        {
            LastRound = result;
            RoundSettled?.Invoke(result);
        }

        public void OnMatchOver(MatchOver over)
        {
            Over = over;
            MatchEnded?.Invoke(over);
        }

        public void OnActionFailed(string message) => ActionFailed?.Invoke(message);

        public void OnMatchmaking(bool searching) => MatchmakingChanged?.Invoke(searching);

        /// <summary>离开对局（返回大厅）后清空。</summary>
        public void Clear()
        {
            Config = null;
            State = null;
            LastRound = null;
            Over = null;
        }

        // ---------- 查询（只读静态数据） ----------

        public LineInfo Line(string key) => key != null && lines.TryGetValue(key, out var l) ? l : null;
        public RelicInfo Relic(string key) => key != null && relics.TryGetValue(key, out var r) ? r : null;
        public BondInfo Bond(string key) => key != null && bonds.TryGetValue(key, out var b) ? b : null;

        public RarityInfo Rarity(int index) =>
            Config != null && index >= 0 && index < Config.Rarities.Count ? Config.Rarities[index] : null;

        public FamilyInfo Family(int index) =>
            Config != null && index >= 0 && index < Config.Families.Count ? Config.Families[index] : null;

        public int RelicPrice(string key)
        {
            var r = Relic(key);
            return r == null ? 0 : Rarity(r.Rarity)?.Price ?? 0;
        }

        /// <summary>剩余秒数（向上取整），没有倒计时时返回 -1。</summary>
        public int SecondsLeft()
        {
            if (State == null || State.PhaseDeadlineMs <= 0) return -1;
            var ms = State.PhaseDeadlineMs - ServerNowMs;
            return ms <= 0 ? 0 : (int)Math.Ceiling(ms / 1000.0);
        }

        public IEnumerable<string> TradeNames() => Config?.Trades.Select(t => t.Name) ?? Enumerable.Empty<string>();
    }
}
