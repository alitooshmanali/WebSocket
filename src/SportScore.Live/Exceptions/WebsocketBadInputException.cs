using System.Net.WebSockets;

namespace SportScore.Live.Exceptions
{
    public class WebsocketBadInputException : WebsocketException
    {
        public WebsocketBadInputException()
        {
        }

        public WebsocketBadInputException(string message)
            :base(message)
        {
        }

        public WebsocketBadInputException(string message, Exception innerException)
            :base(message, innerException)
        {
        }
    }
}
