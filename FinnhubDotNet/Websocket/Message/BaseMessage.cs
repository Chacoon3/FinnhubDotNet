using Newtonsoft.Json;

namespace FinnhubDotNet.Websocket.Message
{

    internal abstract class BaseMessage
    {
        [JsonProperty("type")]
        internal string msgType { get; set; }
    }
}