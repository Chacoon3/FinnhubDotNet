namespace Test {
    internal class Program {
        static async Task Main(string[] args) {
            var creds = File.ReadLines("credential");
            string yourKey = creds.First();
            var ws = new FinnhubDotNet.Websocket.FinnhubStreamingClient(yourKey);
            await ws.ConnectAsync();
            await ws.SubscribeTradeAsync("BINANCE:BTCUSDT");
            await ws.SubscribeTradeAsync("BINANCE:ETHUSDT");
            ws.tradeUpdate += (trades) => {
                foreach (var trade in trades) {
                    Console.WriteLine(trade);
                }
            };

            while (true) {
                await Task.Delay(100000);
            }
        }
    }
}
