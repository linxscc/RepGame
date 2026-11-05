using System.Collections.Generic;
using System.Linq;
using RepGame.Protocol;
using RepGameModels;
using PbCard = RepGame.Protocol.Card;
using DomainCard = RepGameModels.Card;
using PbDamageInfo = RepGame.Protocol.DamageInfo;
using DomainDamageInfo = RepGameModels.DamageInfo;

namespace RepGame.Net
{
    /// <summary>
    /// 协议消息 -> 领域模型 的转换，协议类型只在网络层内部使用。
    /// </summary>
    public static class ProtocolMapper
    {
        public static ResPlayerGameInfo ToPlayerGameInfo(GameState s)
        {
            return new ResPlayerGameInfo
            {
                Room_Id = s.RoomId,
                Username = s.Username,
                Round = s.Round,
                Health = (float)s.Health,
                SelfCards = s.SelfCards.Select(ToCard).ToList(),
                OtherPlayers = s.OtherPlayers.Select(o => new OtherPlayerGameInfo
                {
                    Username = o.Username,
                    Round = o.Round,
                    Health = (float)o.Health,
                    CardsCount = o.CardsCount,
                }).ToList(),
                DamageInfo = s.DamageInfo.Select(ToDamageInfo).ToList(),
            };
        }

        public static DomainCard ToCard(PbCard c)
        {
            return new DomainCard
            {
                UID = c.Uid,
                ID = c.Id,
                Name = c.Name,
                Damage = (float)c.Damage,
                TargetName = c.HasTargetName ? c.TargetName : null,
                Level = c.Level,
            };
        }

        public static BondModel ToBond(Bond b)
        {
            return new BondModel
            {
                ID = b.Id,
                Name = b.Name,
                Level = b.Level,
                CardNames = b.CardNames.ToList(),
                Damage = (float)b.Damage,
                Description = b.Description,
                Skill = b.Skill,
            };
        }

        public static List<BondModel> ToBonds(BondList list)
        {
            return list.Bonds.Select(ToBond).ToList();
        }

        private static DomainDamageInfo ToDamageInfo(PbDamageInfo d)
        {
            return new DomainDamageInfo
            {
                DamageSource = d.Source,
                DamageTarget = d.Target,
                DamageType = d.Type,
                DamageValue = (float)d.Value,
                TriggeredBonds = d.TriggeredBonds.Select(ToBond).ToList(),
            };
        }

        public static RequestKind ToRequestKind(ClientMessage.BodyOneofCase body)
        {
            switch (body)
            {
                case ClientMessage.BodyOneofCase.Login: return RequestKind.Login;
                case ClientMessage.BodyOneofCase.Register: return RequestKind.Register;
                case ClientMessage.BodyOneofCase.ResumeSession: return RequestKind.ResumeSession;
                case ClientMessage.BodyOneofCase.Ready: return RequestKind.Ready;
                case ClientMessage.BodyOneofCase.Restart: return RequestKind.Restart;
                case ClientMessage.BodyOneofCase.PlayCards: return RequestKind.PlayCards;
                case ClientMessage.BodyOneofCase.ComposeCards: return RequestKind.ComposeCards;
                default: return RequestKind.Unknown;
            }
        }

        /// <summary>错误码 -> 给玩家看的提示。</summary>
        public static string ToUserMessage(ErrorCode code)
        {
            switch (code)
            {
                case ErrorCode.InvalidCredentials: return "用户名或密码错误";
                case ErrorCode.UserExists: return "用户名已被注册";
                case ErrorCode.NotAuthenticated: return "请先登录";
                case ErrorCode.SessionExpired: return "登录已失效，请重新登录";
                case ErrorCode.InvalidGameState: return "当前无法进行该操作";
                case ErrorCode.InvalidCards: return "所选卡牌无效";
                case ErrorCode.InvalidRequest: return "请求格式错误";
                default: return "服务器繁忙，请稍后再试";
            }
        }
    }
}
