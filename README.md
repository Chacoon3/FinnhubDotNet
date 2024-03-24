# FinnhubDotNet

- This is a C# wrapper of the Finnhub Websocket client.
- Sample usage:
```
﻿namespace Demo {
    internal class Program {
        static async Task Main(string[] args) {
            string yourKey = "";
            var ws = new FinnhubDotNet.Websocket.FinnhubWsClient(yourKey);
            await ws.ConnectAsync();
            await ws.SubscribeTradeAsync("BINANCE:BTCUSDT");
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
```
