# FinnhubDotNet

#### Disclaimer
- This is <b>not</b> an official C# SDK of Finnhub. This is a personal project aimed to contribute to the open-source community.

#### Introduction
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

#### Features
- C#-wrapped Finnhub Websocket client.

#### Advantages
- Efficient data handling: The data receiving logic is implemented with a duplex pipe, minimizing memory overheads and ensuring low latency.
- Minimalistic interfaces.

#### Upcoming Updates
- REST endpoints.
- Stability improvement.

#### Contact
- My email is zizh3ng@gmail.com. Please let me know if you have any suggestions or questions on this project.
