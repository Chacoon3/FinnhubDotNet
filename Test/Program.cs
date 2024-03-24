namespace Test {
    internal class Program {
        static async Task Main(string[] args) {
            var creds = File.ReadLines("credential");
            string yourKey = creds.First();
            var ws = new FinnhubDotNet.Websocket.FinnhubWsClient(yourKey);
            await ws.ConnectAsync();
            await ws.SubscribeTradeAsync("BINANCE:BTCUSDT");
            await ws.SubscribeTradeAsync("BINANCE:ETHUSDT");
            ws.tradeUpdate += (trades) => {
                var trade = trades[0];
                Console.WriteLine(trade.symbol);
                Console.WriteLine(trade.price);
                Console.WriteLine(trade.volume);
                Console.WriteLine(trade.timeUtc);
            };

            while (true) {
                await Task.Delay(1000);
            }
        }
    }
}
