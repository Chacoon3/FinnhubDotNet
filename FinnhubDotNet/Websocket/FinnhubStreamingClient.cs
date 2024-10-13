using FinnhubDotNet.Data;
using FinnhubDotNet.Websocket.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

namespace FinnhubDotNet.Websocket;
public class FinnhubStreamingClient : IDisposable {
    JsonLoadSettings jsonLoadSettings = new JsonLoadSettings {
        CommentHandling = CommentHandling.Ignore,
        LineInfoHandling = LineInfoHandling.Ignore
    };
    private readonly ClientWebSocket websocket;
    private readonly Uri uri;
    private readonly Pipe inbound;
    private const int sizehint = 1024 * 2;
    private BlockingCollection<byte[]> messageQueue = new BlockingCollection<byte[]>();
    private CancellationTokenSource cts = new CancellationTokenSource();

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
                return WebSocketState.None;
            }
            return websocket.State;
        }
    }

    public FinnhubStreamingClient(string key, int receiveBuffer = 16 * 1024) {
        uri = new Uri($"wss://ws.finnhub.io?token={key}");
        var pipeOptions = new PipeOptions(minimumSegmentSize: sizehint, useSynchronizationContext: true);
        inbound = new Pipe(pipeOptions);
        websocket = new ClientWebSocket();
        websocket.Options.SetBuffer(receiveBuffer, 1024);
    }

    private void DeserializeAndNotify(byte[] data) {
        var texts = Encoding.UTF8.GetString(data);

        var token = JToken.Parse(texts, jsonLoadSettings);
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

    private async void ProducerLoop() {
        while (websocket.State == WebSocketState.Open) {
            try {
                var memory = inbound.Writer.GetMemory(sizehint);
                var data = await websocket.ReceiveAsync(memory, CancellationToken.None).ConfigureAwait(false);
                inbound.Writer.Advance(data.Count);
                if (data.EndOfMessage) {
                    var res = await inbound.Writer.FlushAsync().ConfigureAwait(false);
                    var rawData = await inbound.Reader.ReadAsync().ConfigureAwait(false);
                    var copy = rawData.Buffer.ToArray();
                    messageQueue.Add(copy);
                    inbound.Reader.AdvanceTo(rawData.Buffer.End);
                }
            }
            catch (Exception e) {
                onError(e);
            }
        }
    }

    private void ConsumerLoop() {
        try {
            foreach (var data in messageQueue.GetConsumingEnumerable(cts.Token)) {
                try {
                    DeserializeAndNotify(data);
                }
                catch (Exception e) {
                    onError(e);
                }
            }
        }
        catch (OperationCanceledException) {
            //this suggests we are gracefully stopping so no need to raise
        }
    }

    #region subscription
    private async ValueTask SubscribeAsync(Subscription msg) {
        try {
            if (websocket.State != WebSocketState.Connecting && websocket.State != WebSocketState.Open) {
                throw new StreamingException("Websocket client is not connected");
            }
            else {
                await websocket.SendAsync(msg.ToArraySegment(), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        catch (Exception e) {
            onError(e);
        }
    }

    public async ValueTask SubscribeTradeAsync(string symbol) {
        var msg = Subscription.GetTradeSubscription(symbol);
        await SubscribeAsync(msg);
    }

    public async ValueTask SubscribeNewsAsync(string symbol) {
        var msg = Subscription.GetNewsSubscription(symbol);
        await SubscribeAsync(msg);
    }

    public async ValueTask SubscribePressReleaseAsync(string symbol) {
        var msg = Subscription.GetPressReleaseSubscription(symbol);
        await SubscribeAsync(msg);
    }
    #endregion

    public async Task ConnectAsync() {
        await websocket.ConnectAsync(uri, CancellationToken.None);

        var receiver = new Thread(ProducerLoop);
        receiver.IsBackground = true;
        receiver.Priority = ThreadPriority.Normal;
        receiver.Start();

        var eventDispatcher = new Thread(ConsumerLoop);
        eventDispatcher.IsBackground = true;
        eventDispatcher.Priority = ThreadPriority.Normal;
        eventDispatcher.Start();

        onConnected(this);
    }

    public async Task DisconnectAsync() {
        inbound.Reader.Complete();
        inbound.Writer.Complete();
        inbound.Reset();
        await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        cts.Cancel();
        onDisconnected(this);
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public void Dispose() {
        if (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting) {
            DisconnectAsync().Wait();
        }
        cts.Dispose();
        websocket.Dispose();
    }
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
}
