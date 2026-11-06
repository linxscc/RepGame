using System.Collections.Generic;
using RepGame.Protocol;

namespace RepGame.Chess
{
    /// <summary>
    /// 玩家操作：只把“意图”发给服务器。结果以服务器推送的 MatchState 为准，客户端不做判定。
    /// </summary>
    public static class ChessActions
    {
        private static void Send(GameAction action)
        {
            if (GameTcpClient.Instance != null)
            {
                GameTcpClient.Instance.SendGameAction(action);
            }
        }

        public static void PickFamily(int offerIndex) =>
            Send(new GameAction { PickFamily = new PickFamily { OfferIndex = offerIndex } });

        public static void BuyCard(int shopSlot) =>
            Send(new GameAction { BuyCard = new BuyCard { ShopSlot = shopSlot } });

        public static void SellCard(string uid) =>
            Send(new GameAction { SellCard = new SellCard { CardUid = uid } });

        public static void Refresh() => Send(new GameAction { RefreshShop = new RefreshShop() });

        public static void BuyXp() => Send(new GameAction { BuyXp = new BuyXp() });

        public static void BuyRelic(int tradeChoice) =>
            Send(new GameAction { BuyRelic = new BuyRelic { TradeChoice = tradeChoice } });

        public static void SellRelic(int slot) =>
            Send(new GameAction { SellRelic = new SellRelic { RelicSlot = slot } });

        public static void Arrange(IEnumerable<string> boardUids, IEnumerable<int> relicOrder = null)
        {
            var a = new ArrangeBoard();
            a.BoardUids.AddRange(boardUids);
            if (relicOrder != null) a.RelicOrder.AddRange(relicOrder);
            Send(new GameAction { Arrange = a });
        }

        public static void Lock(bool locked) => Send(new GameAction { LockShop = new LockShop { Locked = locked } });

        public static void Ready(bool ready) => Send(new GameAction { SetReady = new SetReady { Ready = ready } });

        public static void PickVault(int offerIndex, int sellRelicSlot, int tradeChoice) =>
            Send(new GameAction
            {
                PickVault = new PickVault { OfferIndex = offerIndex, SellRelicSlot = sellRelicSlot, TradeChoice = tradeChoice },
            });

        public static void SkipVault() => Send(new GameAction { SkipVault = new SkipVault() });
    }
}
