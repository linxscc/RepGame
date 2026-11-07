using System.Collections.Generic;
using RepGame.Protocol;

namespace RepGame.Model
{
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
}
