using RepGame.Core;
using RepGame.Protocol;

namespace RepGame.Net
{
    /// <summary>
    /// 协议相关的小工具：请求类型识别、错误码转提示文本。
    /// </summary>
    public static class ProtocolMapper
    {
        public static RequestKind ToRequestKind(ClientMessage.BodyOneofCase body)
        {
            switch (body)
            {
                case ClientMessage.BodyOneofCase.Login: return RequestKind.Login;
                case ClientMessage.BodyOneofCase.Register: return RequestKind.Register;
                case ClientMessage.BodyOneofCase.ResumeSession: return RequestKind.ResumeSession;
                case ClientMessage.BodyOneofCase.Ready: return RequestKind.Ready;
                case ClientMessage.BodyOneofCase.GameAction: return RequestKind.GameAction;
                case ClientMessage.BodyOneofCase.CancelMatch: return RequestKind.CancelMatch;
                default: return RequestKind.Unknown;
            }
        }

        /// <summary>错误码 -> 给玩家看的提示（文本见 StreamingAssets/Config/error_messages.json）</summary>
        public static string ToUserMessage(ErrorCode code)
        {
            return ConfigTables.ErrorText(code.ToString());
        }
    }
}
