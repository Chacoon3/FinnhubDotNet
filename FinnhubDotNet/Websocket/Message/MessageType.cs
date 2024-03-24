namespace FinnhubDotNet.Websocket.Message
{
    internal static class MessageType
    {
        internal const string ping = "ping";
        internal const string error = "error";

        internal const string tradeSubscription = "subscribe";
        internal const string newsSubscription = "subscribe-news";
        internal const string pressReleaseSubscription = "subscribe-pr";

        internal const string tradeUpdate = "trade";
        internal const string newsUpdate = "news";
        internal const string pressReleaseUpdate = "pr";
    }
}