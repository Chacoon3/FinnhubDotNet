using Newtonsoft.Json;

namespace FinnhubDotNet.Data
{
    public class PressRelease
    {
        [JsonProperty]
        private long datetime { get; set; } // unix miliseconds
        [JsonIgnore]
        public DateTime timeUtc => DateTimeOffset.FromUnixTimeMilliseconds(datetime).UtcDateTime;
        public string fullText { get; set; }
        public string headline { get; set; }
        [JsonProperty]
        private string symbol;
        [JsonIgnore]
        /*
         *  symbol returned from the client is a comma delimited string
         *  we use an array to cache the split symbols
         */
        private string[] _symbols; 
        [JsonIgnore]
        public string[] symbols => _symbols ??= symbol.Split(',');
        public string url { get; set; }

        public override string ToString() {
            return $"PressRelease: {headline} {timeUtc}";
        }
    }
}
