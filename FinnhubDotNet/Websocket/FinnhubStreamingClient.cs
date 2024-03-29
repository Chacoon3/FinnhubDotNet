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
    private readonly Uri uri;
    private Pipe inbound;
#pragma warning disable CS0414 // The field 'FinnhubStreamingClient.isSending' is assigned but its value is never used
    private bool isSending = false;
#pragma warning restore CS0414 // The field 'FinnhubStreamingClient.isSending' is assigned but its value is never used
    private readonly object counterLock = new object();
    private int countSubscriptions = 0;
    private int sizehint = 1024 * 4;    

    public event Action<Trade[]> tradeUpdate = delegate { };
    public event Action<News[]> newsUpdate = delegate { };
    public event Action<PressRelease[]> pressReleaseUpdate = delegate { };
    public event Action<FinnhubStreamingClient> onConnected = delegate { };
    public event Action<FinnhubStreamingClient> onDisconnected = delegate { };
    public event Action<Exception> onError = delegate { };
    public WebSocketCloseStatus? closeStatus => websocket?.CloseStatus;
    public WebSocketState state {
        get {
            if (websocket == null) {
                return WebSocketState.Closed;
            }
            return websocket.State;
        }
    }


    public FinnhubStreamingClient(string key) {
        uri = new Uri($"wss://ws.finnhub.io?token={key}");
        var pipeOptions = new PipeOptions(minimumSegmentSize:4096);
        inbound =  new Pipe(pipeOptions);
    }

    private void UpdateSizeHint() {
        /*
          * one trade is about 134bytes, the rest is about 60b.
          * assume 10 trades per symbol per message. the overall size of a message is 60 + 134 * 5 * S where S is the number of symbols.
          */
        int min = 4096; // at least 4KB
        int max = 1024 * 1024; // at most 1 mb
        int estimate = 60 + 134 * 10 * countSubscriptions;
        if (estimate < min) { 
            estimate = min;
        }
        if (estimate % min != 0) {
            estimate = ((estimate / min) + 1) * min;
        }
        if (estimate > max) {
            estimate = max;
        }
        sizehint = estimate;
    }

    private void DeserializeAndNotify(string texts) {
        var token = JToken.Parse(texts);
        var messageType = token["type"].ToString();
        switch (messageType) {
            case MessageType.error:
                var msg = token["msg"].ToString();
                onError(new StreamingException(msg));
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

    private async void ReceiveLoop() {
        try {
            while (websocket.State == WebSocketState.Open) {
                var memory = inbound.Writer.GetMemory(sizehint);
                var data = await websocket.ReceiveAsync(memory, CancellationToken.None);
                inbound.Writer.Advance(data.Count);
                await inbound.Writer.FlushAsync();
                if (data.EndOfMessage) {
                    var rawData = await inbound.Reader.ReadAsync();
                    var texts = Encoding.UTF8.GetString(rawData.Buffer.ToArray());
                    inbound.Reader.AdvanceTo(rawData.Buffer.End);
                    DeserializeAndNotify(texts);
                }
            }
        }
        catch (Exception e) {
            onError(e);
        }
    }

    #region subscription
    private async Task SubscribeAsync(Subscription msg) {
        try {
            await websocket.SendAsync(msg.ToArraySegment(), WebSocketMessageType.Text, true, CancellationToken.None);
            lock (counterLock) {
                countSubscriptions++;
                UpdateSizeHint();
            }
        }
        catch (Exception e) {
            onError(e);
        }
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

    /// <summary>
    /// Create a new client websocket instance and connect it to Finnhub.
    /// </summary>
    /// <remarks>Connect event will be raised only if there is no active websocket and a new instance is created</remarks>
    public async Task ConnectAsync() {
        bool isInactive;
        if (websocket != null) {
            switch (websocket.State) {
                case WebSocketState.Connecting:
                case WebSocketState.Open:
                    isInactive = false;
                    break;
                default:
                    isInactive = true;
                    break;
            }
        }
        else {
            isInactive = true;
        }

        if (isInactive) {
            websocket = new ClientWebSocket();
            await websocket.ConnectAsync(uri, CancellationToken.None);
            await Task.Factory.StartNew(ReceiveLoop, TaskCreationOptions.LongRunning);
            onConnected(this);
        }
    }

    /// <summary>
    /// Disconnect and dispose current websocket instance if any.
    /// </summary>
    /// <remarks>Disconnect event will be raised only if there is an active websocket being closed</remarks>
    public async Task DisconnectAsync() {
        if (websocket == null) {
            return;
        }
        if (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting) {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            onDisconnected(this);
        }
        websocket.Dispose();
        websocket = null;
        inbound.Reader.Complete();
        inbound.Writer.Complete();
        inbound.Reset();
    }

    public void Dispose() {
        DisconnectAsync().Wait();
        GC.SuppressFinalize(this);
    }
}
