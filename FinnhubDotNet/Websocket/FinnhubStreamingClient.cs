using FinnhubDotNet.Data;
using FinnhubDotNet.Websocket.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

namespace FinnhubDotNet.Websocket; 
public class FinnhubStreamingClient : IDisposable {

    ClientWebSocket websocket;
    private Uri uri;
    private Pipe inbound;
    private bool isSending = false;
    private readonly object lockObj = new object();

    public event Action<Trade[]> tradeUpdate = delegate { };
    public event Action<News[]> newsUpdate = delegate { };
    public event Action<PressRelease[]> pressReleaseUpdate = delegate { };

    public FinnhubStreamingClient(string key) {
        websocket = new ClientWebSocket();
        uri = new Uri($"wss://ws.finnhub.io?token={key}"); 
        inbound = new Pipe();
    }

    private static void ExceptionHandler(Exception ex) {
        throw ex;
    }

    public async Task ConnectAsync() {
        await websocket.ConnectAsync(uri, CancellationToken.None);
        var receiverThread = new Thread(ReceiveLoop);
        receiverThread.IsBackground = true;
        receiverThread.Start();
    }

    private void ReceiveString(string texts) {
        try {
            var token = JToken.Parse(texts);
            var messageType = token["type"].ToString();
            switch (messageType) {
                case MessageType.ping:
                    break;
                case MessageType.error:
                    var msg = token["msg"].ToString();
                    var error = new StreamingException(msg);
                    ExceptionHandler(error);
                    break;
                case MessageType.tradeUpdate:
                    var payload = token["data"].ToString();
                    var trade = JsonConvert.DeserializeObject<Trade[]>(payload);
                    tradeUpdate(trade);
                    break;
                case MessageType.newsUpdate:
                    payload = token["data"].ToString();
                    var news = JsonConvert.DeserializeObject<News[]>(payload);
                    newsUpdate(news);
                    break;
                case MessageType.pressReleaseUpdate:
                    payload = token["data"].ToString();
                    var pressRelease = JsonConvert.DeserializeObject<PressRelease[]>(payload);
                    pressReleaseUpdate(pressRelease);
                    break;
            }
        }
        catch (Exception e) {
            ExceptionHandler(e);
        }
    }

    private async void ReceiveLoop() {
        try {
            while (websocket.State == WebSocketState.Open) {
                var memory = inbound.Writer.GetMemory(1024 * 5);
                var data = await websocket.ReceiveAsync(memory, CancellationToken.None);
                inbound.Writer.Advance(data.Count);
                await inbound.Writer.FlushAsync();
                if (data.EndOfMessage) {
                    var rawData = await inbound.Reader.ReadAsync();
                    var texts = Encoding.UTF8.GetString(rawData.Buffer.ToArray());
                    inbound.Reader.AdvanceTo(rawData.Buffer.End);
                    ReceiveString(texts);
                }
            }
        }
        finally {
            inbound.Reader.Complete();
            inbound.Writer.Complete();
            inbound.Reset();
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    #region subscription
    private async Task SubscribeAsync(Subscription msg) {
        await websocket.SendAsync(msg.ToArraySegment(), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SubscribeTradeAsync(string symbol) {
        var msg = Subscription.GetTradeSubscription(symbol);
        await SubscribeAsync(msg);
    }

    public async Task SubscribeNewsAsync(string symbol) {
        var msg = Subscription.GetNewsSubscription(symbol);
        await SubscribeAsync(msg);
    }

    public async Task SubscribePressReleaseAsync(string symbol) {
        var msg = Subscription.GetPressReleaseSubscription(symbol);
        await SubscribeAsync(msg);
    }
    #endregion

    public void Dispose() {
        websocket.Dispose();
        inbound.Reader.Complete();
        inbound.Writer.Complete();
        inbound.Reset();
        GC.SuppressFinalize(this);
    }
}
