using System.Net.WebSockets;
using System.Text;

namespace SportScore.Live
{
    public class ResponseMessage
    {
        private ResponseMessage(byte[] binary, string text, WebSocketMessageType messageType)
        {
            Binary = binary;
            Text = text;
            MessageType = messageType;
        }

        public byte[] Binary { get; }

        public string Text { get; }

        public WebSocketMessageType MessageType { get; }

        public override string ToString()
        {
            if(MessageType == WebSocketMessageType.Text)
                return Text;

            return Encoding.UTF8.GetString(Binary);
        }

        public static ResponseMessage TextMessage(string data)
        {
            return new ResponseMessage(null, data, WebSocketMessageType.Text);
        }

        public static ResponseMessage BinaryMessage(byte[] data)
        {
            return new ResponseMessage(data, null, WebSocketMessageType.Binary);
        }
    }
}
