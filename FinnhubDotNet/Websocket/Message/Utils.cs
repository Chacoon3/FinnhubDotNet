using Newtonsoft.Json;

namespace FinnhubDotNet.Websocket.Message {
    internal static class Utils {
        internal static string ToJson<T>(this T wsMessage) where T : BaseMessage {
            return JsonConvert.SerializeObject(wsMessage);
        }

        internal static ArraySegment<byte> ToArraySegment<T>(this T wsMessage) where T : BaseMessage {
            string json = JsonConvert.SerializeObject(wsMessage);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return new ArraySegment<byte>(bytes);
        }
    }
}
