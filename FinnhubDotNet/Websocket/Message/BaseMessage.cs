using Newtonsoft.Json;

namespace FinnhubDotNet.Websocket.Message
{

    public abstract class BaseMessage
    {
        [JsonProperty("type")]
        internal string msgType { get; set; }
    }
}