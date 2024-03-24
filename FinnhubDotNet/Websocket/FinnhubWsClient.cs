using FinnhubDotNet.Data;
using FinnhubDotNet.Websocket.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

namespace FinnhubDotNet.Websocket
{
    public class FinnhubWsClient : IDisposable
    {

        ClientWebSocket _ws;
        private string key;
        private Pipe outbound;
        private Pipe inbound;
        private MessageQueue msgQueue;
        private bool isSending= false;
        private readonly object lockObj = new object();

        public event Action<Trade[]> tradeUpdate = delegate { };
        public event Action<News[]> newsUpdate = delegate { };
        public event Action<PressRelease[]> pressReleaseUpdate = delegate { };

        public FinnhubWsClient(string key)
        {
            _ws = new ClientWebSocket();
            this.key = key;
            inbound = new Pipe();
            outbound = new Pipe();
            msgQueue = new MessageQueue();
        }

        private void ExceptionHandler(Exception ex) {
            Console.WriteLine(ex.Message);
            throw ex;
        }

        public async Task ConnectAsync()
        {
            await _ws.ConnectAsync(new Uri($"wss://ws.finnhub.io?token={key}"), CancellationToken.None);
            var receiverThread = new Thread(() => ReceiveLoop());
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }

        #region deserialization and event dispatching
        private void ReceiveString(string texts)
        {
            try {
                var token = JToken.Parse(texts);
                var messageType = token["type"].ToString();
                if (messageType == MessageType.ping) {
                    return;
                }
                var payload = token["data"].ToString();
                switch (messageType) {
                    case MessageType.tradeUpdate:
                        var trade = JsonConvert.DeserializeObject<Trade[]>(payload);
                        tradeUpdate(trade);
                        break;
                    case MessageType.newsUpdate:
                        var news = JsonConvert.DeserializeObject<News[]>(payload);
                        newsUpdate(news);
                        break;
                    case MessageType.pressReleaseUpdate:
                        var pressRelease = JsonConvert.DeserializeObject<PressRelease[]>(payload);
                        pressReleaseUpdate(pressRelease);
                        break;
                }
            }
            catch (Exception e) {
                ExceptionHandler(e);
            }
        }
        #endregion

        #region event loop
        private async Task ReceiveLoop()
        {
            while (_ws.State == WebSocketState.Open) {
                var memory = inbound.Writer.GetMemory();
                var data = await _ws.ReceiveAsync(memory, CancellationToken.None);
                inbound.Writer.Advance(data.Count);
                await inbound.Writer.FlushAsync();
                if (data.EndOfMessage)
                {
                    var rawData = await inbound.Reader.ReadAsync();
                    var texts = Encoding.UTF8.GetString(rawData.Buffer.ToArray());
                    inbound.Reader.AdvanceTo(rawData.Buffer.End);
                    ReceiveString(texts);
                }
            }
        }
        #endregion

        #region subscription
        private async Task SubscribeAsync<T>(T msg) where T : Subscription
        {
            await _ws.SendAsync(msg.ToArraySegment(), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeTradeAsync(string symbol)
        {
            var msg = Subscription.GetTradeSubscription(symbol);
            await SubscribeAsync(msg);
        }

        public async Task SubscribeNewsAsync(string symbol)
        {
            var msg = Subscription.GetNewsSubscription(symbol);
            await SubscribeAsync(msg);
        }

        public async Task SubscribePressReleaseAsync(string symbol)
        {
            var msg = Subscription.GetPressReleaseSubscription(symbol);
            await SubscribeAsync(msg);
        }
        #endregion

        public void Dispose()
        {
            _ws.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
