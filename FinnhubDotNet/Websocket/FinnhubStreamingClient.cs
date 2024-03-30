using FinnhubDotNet.Data;
using FinnhubDotNet.Websocket.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

namespace FinnhubDotNet.Websocket;
public class FinnhubStreamingClient : IDisposable {

    private readonly ClientWebSocket websocket;
    private readonly Uri uri;
    private readonly Pipe inbound;
    private readonly object counterLock = new object();
    private int countSubscriptions = 0;
    private int sizehint = 1024 * 4;

    #region event
    public event Action<Trade[]> tradeUpdate = delegate { };
    public event Action<News[]> newsUpdate = delegate { };
    public event Action<PressRelease[]> pressReleaseUpdate = delegate { };
    public event Action<FinnhubStreamingClient> onConnected = delegate { };
    public event Action<FinnhubStreamingClient> onDisconnected = delegate { };
    public event Action<Exception> onError = delegate { };
    #endregion

    public WebSocketCloseStatus? closeStatus => websocket.CloseStatus;
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
        var pipeOptions = new PipeOptions(minimumSegmentSize: 4096, useSynchronizationContext: false);
        inbound = new Pipe(pipeOptions);
        websocket = new ClientWebSocket();
    }

    private void UpdateSizeHint() {
        /*
          * one trade is about 134bytes, the rest is about 60b.
          * assume 10 trades per symbol per message. the overall size of a message is 60 + 134 * 10 * S where S is the number of symbols.
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
        var token = JToken.Parse(texts, null);
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
                var data = await websocket.ReceiveAsync(memory, CancellationToken.None).ConfigureAwait(false);
                inbound.Writer.Advance(data.Count);
                if (data.EndOfMessage) {
                    await inbound.Writer.FlushAsync().ConfigureAwait(false);
                    var rawData = await inbound.Reader.ReadAsync().ConfigureAwait(false);
                    var texts = Encoding.UTF8.GetString(rawData.Buffer);
                    inbound.Reader.AdvanceTo(rawData.Buffer.End);
                    DeserializeAndNotify(texts);
                }
            }
        }
        catch (Exception e) {
            onError(e);
        }
        finally {
            await DisconnectAsync();
        }
    }

    #region subscription
    private async Task SubscribeAsync(Subscription msg) {
        try {
            if (websocket == null) {
                throw new StreamingException("Websocket client is not connected");
            }
            await websocket.SendAsync(msg.ToArraySegment(), WebSocketMessageType.Text, true, CancellationToken.None);
            lock (counterLock) {
                countSubscriptions++;
            }
            UpdateSizeHint();
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

    public async Task ConnectAsync() {
        countSubscriptions = 0;
        await websocket.ConnectAsync(uri, CancellationToken.None);
        await Task.Factory.StartNew(ReceiveLoop, TaskCreationOptions.LongRunning);
        onConnected(this);
    }

    public async Task DisconnectAsync() {
        inbound.Reader.Complete();
        inbound.Writer.Complete();
        inbound.Reset();
        await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        onDisconnected(this);
    }

    public void Dispose() {
        DisconnectAsync().Wait();
        websocket.Dispose();
        GC.SuppressFinalize(this);
    }
}
