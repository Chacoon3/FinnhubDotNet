# FinnhubDotNet - clean and efficient C# wrapper for Finnhub

#### Disclaimer
- This is <b>not</b> an official C# SDK of Finnhub. This is a personal project aimed to contribute to the open-source community.

#### Installation
- .NET CLI: ```dotnet add package FinnhubDotNet --version 1.0.14```
- Or search <b>FinnhubDotNet</b> in Visual Studio using NugetPackageManager.
- Or download the <b>./FinnhubDotNet</b> folder and build the class library locally.

#### Sample Usage
```
namespace Test {
    internal class Program {
        static async Task Main(string[] args) {
            string yourKey = "";
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
                await Task.Delay(5000);
            }
        }
    }
}
```

#### Advantages
- Efficient data handling: The data receiving logic is implemented with a pipe, minimizing memory overheads and ensuring low latency.
- Minimalistic interfaces.

#### Upcoming Updates
- REST endpoints.

#### Known Issues
- The author currently does not have Finnhub premium. As a result, the news subsciprtion and the press release subscription have not yet been tested. If you find any problems with those two subscriptions, you are very welcome to let me know.

#### Contact/Contribute
- My email is zizh3ng@gmail.com. Please let me know if you have any suggestions or questions on this project.
