using System;
using System.Collections.Generic;
using RepGame.Core;
using RepGame.Protocol;
using UnityEngine;

namespace RepGame.Net
{
    /// <summary>
    /// 把服务器消息转换为领域事件发布出去。每种消息一个处理方法，
    /// 新增消息类型只需在构造函数里注册，不改动分发逻辑。
    /// 必须在主线程调用。
    /// </summary>
    public sealed class ServerMessageRouter
    {
        private readonly Dictionary<ServerMessage.BodyOneofCase, Action<ServerMessage, RequestKind>> handlers;

        /// <summary>登录/恢复会话成功后的会话信息回调。</summary>
        public event Action<AuthResult> Authenticated;

        /// <summary>会话失效（令牌过期、被顶号）。</summary>
        public event Action SessionInvalidated;

        public ServerMessageRouter()
        {
            handlers = new Dictionary<ServerMessage.BodyOneofCase, Action<ServerMessage, RequestKind>>
            {
                { ServerMessage.BodyOneofCase.Welcome, (m, _) => OnWelcome(m) },
                { ServerMessage.BodyOneofCase.Pong, (m, _) => { } },
                { ServerMessage.BodyOneofCase.Error, OnError },
                { ServerMessage.BodyOneofCase.AuthResult, (m, _) => OnAuthResult(m) },
                { ServerMessage.BodyOneofCase.RegisterResult, (m, _) => EventManager.TriggerEvent(NetEvents.RegisterSucceeded, m.RegisterResult.Username) },
                { ServerMessage.BodyOneofCase.BondList, (m, _) => EventManager.TriggerEvent(NetEvents.BondsReceived, ProtocolMapper.ToBonds(m.BondList)) },
                { ServerMessage.BodyOneofCase.GameState, (m, _) => OnGameState(m) },
                { ServerMessage.BodyOneofCase.PlayerPresence, (m, _) => OnPlayerPresence(m) },
            };
        }

        /// <param name="request">该消息回复的是哪类请求；服务器主动推送时为 Unknown。</param>
        public void Route(ServerMessage message, RequestKind request)
        {
            if (handlers.TryGetValue(message.BodyCase, out var handler))
            {
                handler(message, request);
            }
            else
            {
                Debug.LogWarning($"未处理的服务器消息: {message.BodyCase}");
            }
        }

        private static void OnWelcome(ServerMessage m)
        {
            Debug.Log($"服务器协议版本 {m.Welcome.ProtocolVersion}");
        }

        private void OnAuthResult(ServerMessage m)
        {
            Authenticated?.Invoke(m.AuthResult);
            EventManager.TriggerEvent(NetEvents.LoginSucceeded, m.AuthResult.Username);
        }

        private void OnError(ServerMessage m, RequestKind request)
        {
            if (m.Error.Code == ErrorCode.SessionExpired)
            {
                SessionInvalidated?.Invoke();
            }

            var failure = new RequestFailure
            {
                Request = request,
                Message = ProtocolMapper.ToUserMessage(m.Error.Code),
            };
            Debug.LogWarning($"请求失败 [{failure.Request}] {m.Error.Code}: {m.Error.Message}");
            EventManager.TriggerEvent(NetEvents.RequestFailed, failure);
        }

        private static void OnGameState(ServerMessage m)
        {
            var info = ProtocolMapper.ToPlayerGameInfo(m.GameState);
            switch (m.GameState.Reason)
            {
                case GameStateReason.GameStarted:
                case GameStateReason.Reconnected:
                    EventManager.TriggerEvent(NetEvents.GameStarted, info);
                    break;
                case GameStateReason.CardsComposed:
                    EventManager.TriggerEvent(NetEvents.CardsComposed, info);
                    break;
                case GameStateReason.ForcedPlay:
                    EventManager.TriggerEvent(NetEvents.ForcePlay, info);
                    break;
                case GameStateReason.GameOver:
                    EventManager.TriggerEvent(NetEvents.GameOver, info);
                    break;
                default:
                    EventManager.TriggerEvent(NetEvents.CardsPlayed, info);
                    break;
            }
        }

        private static void OnPlayerPresence(ServerMessage m)
        {
            EventManager.TriggerEvent(NetEvents.PlayerPresenceChanged, new PlayerPresenceInfo
            {
                Username = m.PlayerPresence.Username,
                RoomId = m.PlayerPresence.RoomId,
                Online = m.PlayerPresence.Status == PresenceStatus.Reconnected,
            });
        }
    }
}
