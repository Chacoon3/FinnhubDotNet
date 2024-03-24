using Newtonsoft.Json;

namespace FinnhubDotNet.Data
{
    public class PressRelease
    {
        [JsonProperty]
        internal long datetime { get; set; } // unix miliseconds
        [JsonIgnore]
        public DateTime timeUtc => DateTimeOffset.FromUnixTimeMilliseconds(datetime).UtcDateTime;
        public string fullText { get; set; }
        public string headline { get; set; }
        [JsonProperty("symbol")]
        public string _symbols;
        [JsonIgnore]
        public string[] symbols { get; set; }
        public string url { get; set; }

        public override string ToString() {
            return $"PressRelease: {headline} {timeUtc}";
        }
    }
}
