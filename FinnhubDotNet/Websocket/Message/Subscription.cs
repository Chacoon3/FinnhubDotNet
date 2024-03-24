using Newtonsoft.Json;

namespace FinnhubDotNet.Websocket.Message
{
    internal class Subscription : BaseMessage
    {
        [JsonProperty]
        internal string symbol { get; set; }

        [JsonConstructor]
        private Subscription(string symbol, string msgType)
        {
            this.symbol = symbol;
            this.msgType = msgType;
        }

        internal static Subscription GetTradeSubscription(string symbol)
        {
            return new Subscription(symbol, MessageType.tradeSubscription);
        }

        internal static Subscription GetNewsSubscription(string symbol)
        {
            return new Subscription(symbol, MessageType.newsSubscription);
        }

        internal static Subscription GetPressReleaseSubscription(string symbol)
        {
            return new Subscription(symbol, MessageType.pressReleaseSubscription);
        }
    }
}
