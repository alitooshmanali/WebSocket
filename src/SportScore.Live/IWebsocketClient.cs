using SportScore.Live.Models;
using System.Net.WebSockets;
using System.Text;

namespace SportScore.Live
{
    public interface IWebsocketClient: IDisposable
    {
        IObservable<ResponseMessage> MessageReceived { get; }

        IObservable<ReconnectionInfo> ReconnectionHappened { get; }

        IObservable<DisconnectionInfo> DisconnectionHappened { get; }

        TimeSpan? ReconnectTimeout { get; set; }

        TimeSpan? ErrorReconnectTimeout { get; set; }

        string Name { get; set; }

        bool IsStarted { get; }

        bool IsRunning { get; }

        bool IsReconnectionEnabled { get; set; }

        bool IsTextMessageConversionEnabled { get;set; }

        ClientWebSocket NativeClient { get; }

        Encoding MessageEncoding { get; set; }

        Task Start();

        Task StartOrFail();

        Task<bool> Stop(WebSocketCloseStatus status, string statusDescription);

        Task<bool> StopOrFail(WebSocketCloseStatus status, string statusDescription);

        void Send(string message);

        void Send(byte[] message);

        void Send(ArraySegment<byte> message);

        Task SendInstant(string message);

        Task SendInstant(byte[] message);

        Task Reconnect();

        Task ReconnectOrFail();

        void StreamFakeMessage(ResponseMessage message);
    }
}
