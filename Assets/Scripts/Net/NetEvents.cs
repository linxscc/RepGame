using System.Collections.Generic;

namespace RepGame.Net
{
    /// <summary>
    /// 网络层通过 EventManager 发布的事件名称及其参数类型。
    /// UI 只依赖这些名称和领域模型，不直接依赖 protobuf 生成的类型。
    /// </summary>
    public static class NetEvents
    {
        // ---- 连接状态，参数：string 提示信息 ----
        public const string ConnectedToServer = "ConnectedToServer";
        public const string ConnectionFailed = "ConnectionFailed";
        public const string Disconnected = "Disconnected";

        // ---- 账号，参数：string 用户名 ----
        public const string LoginSucceeded = "Net.LoginSucceeded";
        public const string RegisterSucceeded = "Net.RegisterSucceeded";

        /// <summary>登录态失效（会话过期、账号在其他设备登录、未登录就发起请求），参数：string 原因</summary>
        public const string LoggedOut = "Net.LoggedOut";

        /// <summary>请求被服务器拒绝，参数：<see cref="RequestFailure"/></summary>
        public const string RequestFailed = "Net.RequestFailed";

        /// <summary>对手掉线/重连，参数：<see cref="PlayerPresenceInfo"/></summary>
        public const string PlayerPresenceChanged = "Net.PlayerPresenceChanged";
    }

    public enum RequestKind
    {
        Unknown,
        Login,
        Register,
        ResumeSession,
        Ready,
        GameAction,
        CancelMatch,
    }

    public sealed class RequestFailure
    {
        public RequestKind Request;
        /// <summary>给玩家看的提示（已本地化）。</summary>
        public string Message;
    }

    public sealed class PlayerPresenceInfo
    {
        public string Username;
        public string RoomId;
        public bool Online;
    }
}
