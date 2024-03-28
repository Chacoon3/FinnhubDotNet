using Newtonsoft.Json;

namespace FinnhubDotNet.Data
{
    public class Trade
    {
        [JsonProperty("s")]
        public string symbol { get; set; }
        [JsonProperty("p")]
        public decimal price { get; set; }
        [JsonProperty("v")]
        public decimal volume { get; set; }
        [JsonProperty("t")]
        private long timestamp { get; set; } // unix miliseconds
        [JsonIgnore]
        public DateTime timeUtc => DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;

        public override string ToString()
        {
            return $"TradeUpdate: {symbol} {price} {volume} {timeUtc}";
        }
    }
}