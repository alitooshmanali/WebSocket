namespace SportScore.Live.Exceptions
{
    public class WebsocketException : Exception
    {
        public WebsocketException()
        {
        }

        public WebsocketException(string message)
            :base(message)
        {
        }

        public WebsocketException(string message, Exception innerException)
            :base(message, innerException)
        {
        }
    }
}
