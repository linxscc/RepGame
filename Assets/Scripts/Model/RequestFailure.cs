using System.Collections.Generic;

namespace RepGame.Model
{
    public sealed class RequestFailure
    {
        public RequestKind Request;
        /// <summary>给玩家看的提示（已本地化）。</summary>
        public string Message;
    }
}
