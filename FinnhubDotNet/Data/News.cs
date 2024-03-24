using Newtonsoft.Json;

namespace FinnhubDotNet.Data
{
    public class News
    {
        public string category { get; set; }
        [JsonProperty]
        internal long datetime { get; set; } // unix miliseconds
        [JsonIgnore]
        public DateTime timeUtc => DateTimeOffset.FromUnixTimeMilliseconds(datetime).UtcDateTime;
        public string headline { get; set; }
        public long id { get; set; }
        public string image { get; set; }
        public string related { get; set; }
        public string source { get; set; }
        public string summary { get; set; }
        public string url { get; set; }

        public override string ToString() {
            return $"{category} news: {headline} {timeUtc}";
        }
    }
}
