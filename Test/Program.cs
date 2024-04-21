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
                Console.WriteLine(trades.Count());
                foreach (var trade in trades) {
                    Console.WriteLine(trade);
                }
            };
            ws.onError += Console.WriteLine;

            while (true) {
                var k = Console.ReadKey();
                if (k.Key == ConsoleKey.T) {
                    await ws.ConnectAsync();
                    await ws.SubscribeTradeAsync("AAPL");
                    await ws.SubscribeTradeAsync("NVDA");
                    ws.tradeUpdate += (trades) => {
                        Console.WriteLine(trades.Count());
                        foreach (var trade in trades) {
                            Console.WriteLine(trade);
                        }
                    };
                }
                else if (k.Key == ConsoleKey.S) {
                    await ws.DisconnectAsync();
                }
                else if (k.Key == ConsoleKey.Y) {
                    ws.Dispose();
                }

                await Task.Delay(100000);
            }
        }
    }
}
