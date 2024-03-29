namespace Test {
    internal class Program {
        static async Task Main(string[] args) {
            var creds = File.ReadLines("credential");
            string yourKey = creds.First();
            var ws = new FinnhubDotNet.Websocket.FinnhubStreamingClient(yourKey);
            await ws.ConnectAsync();
            await ws.SubscribeTradeAsync("BINANCE:BTCUSDT");
            await ws.SubscribeTradeAsync("BINANCE:ETHUSDT");
            await ws.SubscribeNewsAsync("AAPL");
            ws.tradeUpdate += (trades) => {
                foreach (var trade in trades) {
                    Console.WriteLine(trade);
                }
            };

            while (true) {
                //var k = Console.ReadKey();
                //if (k.Key == ConsoleKey.T) {
                //    if (ws.state == System.Net.WebSockets.WebSocketState.Closed) {
                //        await ws.ConnectAsync();
                //        await ws.SubscribeTradeAsync("BINANCE:BTCUSDT");
                //        await ws.SubscribeTradeAsync("BINANCE:ETHUSDT");
                //        await ws.SubscribeNewsAsync("AAPL");
                //        ws.tradeUpdate += (trades) => {
                //            foreach (var trade in trades) {
                //                Console.WriteLine(trade);
                //            }
                //        };
                //    }
                //    else {
                //        await ws.DisconnectAsync();
                //    }
                //}

                await Task.Delay(1000);
            }
        }
    }
}
