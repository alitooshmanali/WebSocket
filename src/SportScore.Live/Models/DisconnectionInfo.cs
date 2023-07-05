using SportScore.Live.Enums;
using System.Net.WebSockets;

namespace SportScore.Live.Models
{
    public class DisconnectionInfo
    {
        public DisconnectionInfo(
            DisconnectionType type, 
            WebSocketCloseStatus? closeStatus,
            string closeStatusDescription,
            string subProtocol,
            Exception exception)
        {
            Type = type;
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
            SubProtocol = subProtocol;
            Exception = exception;
        }

        public DisconnectionType Type { get; }

        public WebSocketCloseStatus? CloseStatus { get; }

        public string CloseStatusDescription { get; }

        public string SubProtocol { get; }

        public Exception Exception { get; }

        public bool CancelReconnection { get; set; }

        public bool CancelClosing { get; set; }

        public static DisconnectionInfo Create(DisconnectionType type, WebSocket client, Exception exception)
        {
            return new DisconnectionInfo(type, client?.CloseStatus, client?.CloseStatusDescription, client?.SubProtocol, exception);
        }
    }
}
