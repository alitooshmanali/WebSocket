using SportScore.Live.Enums;
using SportScore.Live.Exceptions;
using SportScore.Live.Logging;
using SportScore.Live.Models;
using SportScore.Live.Threading;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace SportScore.Live
{
    public partial class WebSocketClient : IWebsocketClient
    {
        private static readonly ILog logger = GetLogger();

        private readonly WebsocketAsyncLock _locker = new WebsocketAsyncLock();

        private readonly Func<CancellationToken, Task<WebSocket>> _connectionFactory;

        private Timer _lastChanceTimer;

        private DateTime _lastReceivedMsg = DateTime.UtcNow;

        private bool _disposing;

        private bool _reconnecting;

        private bool _stopping;

        private bool _isReconnectionEnabled = true;

        private WebSocket _client;

        private CancellationTokenSource _cancellation;

        private CancellationTokenSource _cancellationTotal;

        private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();

        private readonly Subject<ReconnectionInfo> _reconnectionSubject = new Subject<ReconnectionInfo>();

        private readonly Subject<DisconnectionInfo> _disconnectedSubject = new Subject<DisconnectionInfo>();

        public Encoding MessageEncoding { get; set; }

        public WebSocketClient(Func<ClientWebSocket> clientFactory = null)
            : this(GetClientFactory(clientFactory))
        {
        }

        public WebSocketClient(Func<CancellationToken, Task<WebSocket>> connectionFactory)
        {
            Validation.ValidateInput(BaseOption.BaseUrl, nameof(BaseOption.BaseUrl));

            _connectionFactory = connectionFactory ?? (async (token) =>
            {
                var client = new ClientWebSocket();
                await client.ConnectAsync(new Uri(BaseOption.BaseUrl), token).ConfigureAwait(false);
                return client;
            });
        }

        public Uri Url { get => new Uri(BaseOption.BaseUrl); }

        private static Func<CancellationToken, Task<WebSocket>> GetClientFactory(Func<ClientWebSocket> clientFactory)
        {
            if (clientFactory == null)
                return null;

            return (async (token) =>
            {
                var client = clientFactory();
                await client.ConnectAsync(new Uri(BaseOption.BaseUrl), token).ConfigureAwait(false);
                return client;
            });
        }

        public IObservable<ResponseMessage> MessageReceived => _messageReceivedSubject.AsObservable();

        public IObservable<ReconnectionInfo> ReconnectionHappened => _reconnectionSubject.AsObservable();

        public IObservable<DisconnectionInfo> DisconnectionHappened => _disconnectedSubject.AsObservable();

        public TimeSpan? ReconnectTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan? ErrorReconnectTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public string Name { get; set; }

        public bool IsStarted { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsReconnectionEnabled { get => _isReconnectionEnabled; set
            {
                _isReconnectionEnabled = value;

                if (IsStarted)
                {
                    if (_isReconnectionEnabled)
                    {
                        ActivateLastChance();
                    }
                    else
                    {
                        DeactivateLastChance();
                    }
                }
            } }

        public bool IsTextMessageConversionEnabled { get; set; } = true;

        public ClientWebSocket NativeClient => GetSpecificOrThrow(_client);

        public Encoding MessageEncode { get; set; }

        public void Dispose()
        {
            _disposing = true;
            logger.Debug(L("Disposing.."));
            try
            {
                _messagesTextToSendQueue?.Writer.Complete();
                _messagesBinaryToSendQueue?.Writer.Complete();
                _lastChanceTimer?.Dispose();
                _cancellation?.Cancel();
                _cancellationTotal?.Cancel();
                _client?.Abort();
                _client?.Dispose();
                _cancellation?.Dispose();
                _cancellationTotal?.Dispose();
                _messageReceivedSubject.OnCompleted();
                _reconnectionSubject.OnCompleted();
            }
            catch (Exception e)
            {
                logger.Error(e, L($"Failed to dispose client, error: {e.Message}"));
                throw;
            }

            if (IsRunning)
            {
                _disconnectedSubject.OnNext(DisconnectionInfo.Create(DisconnectionType.Exit, _client, null));
            }

            IsRunning = false;
            IsStarted = false;
            _disconnectedSubject.OnCompleted();
        }

        public Task Start()
        {
            return StartInternal(false);
        }

        public Task StartOrFail()
        {
            return StartInternal(true);
        }

        public async Task<bool> Stop(WebSocketCloseStatus status, string statusDescription)
        {
            var result = await StopInternal(
                _client,
                status,
                statusDescription,
                null,
                false,
                false
                )
                .ConfigureAwait(false);

            _disconnectedSubject.OnNext(DisconnectionInfo.Create(Enums.DisconnectionType.ByUser, _client, null));

            return result;

        }

        public async Task<bool> StopOrFail(WebSocketCloseStatus status, string statusDescription)
        {
            var result = await StopInternal(
               _client,
               status,
               statusDescription,
               null,
               true,
               false).ConfigureAwait(false);
            _disconnectedSubject.OnNext(DisconnectionInfo.Create(DisconnectionType.ByUser, _client, null));

            return result;
        }

        private async Task StartInternal(bool failFast)
        {
            if (_disposing)
            {
                throw new WebsocketException(L("Client is already disposed, starting not possible"));
            }

            if (IsStarted)
            {
                logger.Debug(L("Client already started, ignoring.."));
                return;
            }

            IsStarted = true;

            logger.Debug(L("Starting.."));
            _cancellation = new CancellationTokenSource();
            _cancellationTotal = new CancellationTokenSource();

            await StartClient(Url, _cancellation.Token, ReconnectionType.Initial, failFast).ConfigureAwait(false);

            StartBackgroundThreadForSendingText();
            StartBackgroundThreadForSendingBinary();
        }

        private async Task<bool> StopInternal(WebSocket client, WebSocketCloseStatus status, string statusDescription,
            CancellationToken? cancellation, bool failFast, bool byServer)
        {
            if (_disposing)
            {
                throw new WebsocketException(L("Client is already disposed, stopping not possible"));
            }

            DeactivateLastChance();

            if (client == null)
            {
                IsStarted = false;
                IsRunning = false;
                return false;
            }

            if (!IsRunning)
            {
                logger.Info(L("Client is already stopped"));
                IsStarted = false;
                return false;
            }

            var result = false;
            try
            {
                var cancellationToken = cancellation ?? CancellationToken.None;
                _stopping = true;
                if (byServer)
                    await client.CloseOutputAsync(status, statusDescription, cancellationToken);
                else
                    await client.CloseAsync(status, statusDescription, cancellationToken);
                result = true;
            }
            catch (Exception e)
            {
                logger.Error(e, L($"Error while stopping client, message: '{e.Message}'"));
                if (failFast)
                    throw new WebsocketException($"Failed to stop Websocket client, error: '{e.Message}'", e);
            }
            finally
            {
                IsRunning = false;
                _stopping = false;

                if (!byServer || !IsReconnectionEnabled)
                    IsStarted = false;
                

            }

            return result;
        }

        private async Task StartClient(Uri uri, CancellationToken token, ReconnectionType type, bool failFast)
        {
            DeactivateLastChance();

            try
            {
                _client = await _connectionFactory(token).ConfigureAwait(false);
                _ = Listen(_client, token);
                IsRunning = true;
                IsStarted = true;
                _reconnectionSubject.OnNext(ReconnectionInfo.Create(type));
                _lastReceivedMsg = DateTime.UtcNow;
                ActivateLastChance();

                
            }
            catch (Exception e)
            {
                var info = DisconnectionInfo.Create(DisconnectionType.Error, _client, e);
                _disconnectedSubject.OnNext(info);

                if (info.CancelReconnection)
                {
                    logger.Error(e, L($"Exception while connecting. " +
                                      $"Reconnecting canceled by user, exiting. Error: '{e.Message}'"));
                    return;
                }
                

                if (failFast)
                    throw new WebsocketException($"Failed to start Websocket client, error: '{e.Message}'", e);


                if (ErrorReconnectTimeout == null)
                {
                    logger.Error(e, L($"Exception while connecting. " +
                                     $"Reconnecting disabled, exiting. Error: '{e.Message}'"));
                    return;
                }
                

                var timeout = ErrorReconnectTimeout.Value;
                logger.Error(e, L($"Exception while connecting. " +
                                  $"Waiting {timeout.TotalSeconds} sec before next reconnection try. Error: '{e.Message}'"));
                await Task.Delay(timeout, token).ConfigureAwait(false);
                await Reconnect(ReconnectionType.Error, false, e).ConfigureAwait(false);
            }
        }

        private bool IsClientConnected()
        {
            return _client.State == WebSocketState.Open;
        }

        private async Task Listen(WebSocket client, CancellationToken token)
        {
            Exception causedException = null;
            try
            {
                // define buffer here and reuse, to avoid more allocation
                const int chunkSize = 1024 * 4;
                var buffer = new ArraySegment<byte>(new byte[chunkSize]);

                do
                {
                    WebSocketReceiveResult result;
                    byte[] resultArrayWithTrailing = null;
                    var resultArraySize = 0;
                    var isResultArrayCloned = false;
                    MemoryStream memoryStream = null;

                    while (true)
                    {
                        result = await client.ReceiveAsync(buffer, token);
                        var currentChunk = buffer.Array;
                        var currentChunkSize = result.Count;

                        var isFirstChunk = resultArrayWithTrailing == null;
                        if (isFirstChunk)
                        {
                            resultArraySize += currentChunkSize;
                            resultArrayWithTrailing = currentChunk;
                            isResultArrayCloned = false;
                        }
                        else
                        {
                            if (memoryStream == null)
                            {
                                memoryStream = new MemoryStream();
                                memoryStream.Write(resultArrayWithTrailing, 0, resultArraySize);
                            }

                            memoryStream.Write(currentChunk, buffer.Offset, currentChunkSize);
                        }

                        if (result.EndOfMessage)
                            break;
                        

                        if (isResultArrayCloned)
                            continue;

                        resultArrayWithTrailing = resultArrayWithTrailing?.ToArray();
                        isResultArrayCloned = true;
                    }

                    memoryStream?.Seek(0, SeekOrigin.Begin);

                    ResponseMessage message;
                    if (result.MessageType == WebSocketMessageType.Text && IsTextMessageConversionEnabled)
                    {
                        var data = memoryStream != null ?
                            GetEncoding().GetString(memoryStream.ToArray()) :
                            resultArrayWithTrailing != null ?
                                GetEncoding().GetString(resultArrayWithTrailing, 0, resultArraySize) :
                                null;

                        message = ResponseMessage.TextMessage(data);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.Trace(L($"Received close message"));

                        if (!IsStarted || _stopping)
                            return;
                        

                        var info = DisconnectionInfo.Create(DisconnectionType.ByServer, client, null);
                        _disconnectedSubject.OnNext(info);

                        if (info.CancelClosing)
                        {
                            if (IsReconnectionEnabled)
                                throw new OperationCanceledException("Websocket connection was closed by server");
                            
                            continue;
                        }

                        await StopInternal(client, WebSocketCloseStatus.NormalClosure, "Closing",
                            token, false, true);

                        if (IsReconnectionEnabled && !ShouldIgnoreReconnection(client))
                        {
                            _ = ReconnectSynchronized(ReconnectionType.Lost, false, null);
                        }

                        return;
                    }
                    else
                    {
                        if (memoryStream != null)
                        {
                            message = ResponseMessage.BinaryMessage(memoryStream.ToArray());
                        }
                        else
                        {
                            Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                            message = ResponseMessage.BinaryMessage(resultArrayWithTrailing);
                        }
                    }

                    memoryStream?.Dispose();

                    logger.Trace(L($"Received:  {message}"));
                    _lastReceivedMsg = DateTime.UtcNow;
                    _messageReceivedSubject.OnNext(message);

                } while (client.State == WebSocketState.Open && !token.IsCancellationRequested);
            }
            catch (TaskCanceledException e)
            {
                causedException = e;
            }
            catch (OperationCanceledException e)
            {
                causedException = e;
            }
            catch (ObjectDisposedException e)
            {
                causedException = e;
            }
            catch (Exception e)
            {
                logger.Error(e, L($"Error while listening to websocket stream, error: '{e.Message}'"));
                causedException = e;
            }


            if (ShouldIgnoreReconnection(client) || !IsStarted)
                return;
            
            _ = ReconnectSynchronized(ReconnectionType.Lost, false, causedException);
        }

        private bool ShouldIgnoreReconnection(WebSocket client)
        {
            var inProgress = _disposing || _reconnecting || _stopping;

            var differentClient = client != _client;

            return inProgress || differentClient;
        }

        private Encoding GetEncoding()
        {
            if (MessageEncoding == null)
                MessageEncoding = Encoding.UTF8;

            return MessageEncoding;
        }

        private ClientWebSocket GetSpecificOrThrow(WebSocket client)
        {
            if (client == null)
                return null;
            var specific = client as ClientWebSocket;
            if (specific == null)
                throw new WebsocketException("Cannot cast 'WebSocket' client to 'ClientWebSocket', " +
                                             "provide correct type via factory or don't use this property at all.");
            return specific;
        }

        private string L(string msg)
        {
            var name = Name ?? "CLIENT";
            return $"[WEBSOCKET {name}] {msg}";
        }

        private static ILog GetLogger()
        {
            try
            {
                return LogProvider.GetCurrentClassLogger();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"[WEBSOCKET] Failed to initialize logger, disabling.. " +
                                $"Error: {e}");
                return LogProvider.NoOpLogger.Instance;
            }
        }

        private DisconnectionType TranslateTypeToDisconnection(ReconnectionType type)
        {
            return (DisconnectionType)type;
        }
    }
}
