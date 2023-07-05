using SportScore.Live.Logging;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace SportScore.Live
{
    public partial class WebSocketClient
    {
        private readonly Channel<string> _messagesTextToSendQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false
        });
        private readonly Channel<ArraySegment<byte>> _messagesBinaryToSendQueue = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = false
        });

        public void Send(string message)
        {
            Validation.ValidateInput(message, nameof(message));

            _messagesTextToSendQueue.Writer.TryWrite(message);
        }

        public void Send(byte[] message)
        {
            Validation.ValidateInput(message, nameof(message));

            _messagesBinaryToSendQueue.Writer.TryWrite(new ArraySegment<byte>(message));
        }

        public void Send(ArraySegment<byte> message)
        {
            Validation.ValidateInput(message, nameof(message));

            _messagesBinaryToSendQueue.Writer.TryWrite(message);
        }

        public Task SendInstant(string message)
        {
            Validation.ValidateInput(message, nameof(message));

            return SendInternalSynchronized(message);
        }

        public Task SendInstant(byte[] message)
        {
            return SendInternalSynchronized(new ArraySegment<byte>(message));
        }

        public void StreamFakeMessage(ResponseMessage message)
        {
            Validation.ValidateInput(message, nameof(message));

            _messageReceivedSubject.OnNext(message);
        }

        private async Task SendTextFromQueue()
        {
            try
            {
                while (await _messagesTextToSendQueue.Reader.WaitToReadAsync())
                {
                    while (_messagesTextToSendQueue.Reader.TryRead(out var message))
                    {
                        try
                        {
                            await SendInternalSynchronized(message).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, L($"Failed to send text message: '{message}'. Error: {e.Message}"));
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // task was canceled, ignore
            }
            catch (OperationCanceledException)
            {
                // operation was canceled, ignore
            }
            catch (Exception e)
            {
                if (_cancellationTotal.IsCancellationRequested || _disposing)
                {
                    return;
                }

                logger.Trace(L($"Sending text thread failed, error: {e.Message}. Creating a new sending thread."));
                StartBackgroundThreadForSendingText();
            }
        }

        private async Task SendBinaryFromQueue()
        {
            try
            {
                while (await _messagesBinaryToSendQueue.Reader.WaitToReadAsync())
                {
                    while (_messagesBinaryToSendQueue.Reader.TryRead(out var message))
                    {
                        try
                        {
                            await SendInternalSynchronized(message).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, L($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // task was canceled, ignore
            }
            catch (OperationCanceledException)
            {
                // operation was canceled, ignore
            }
            catch (Exception e)
            {
                if (_cancellationTotal.IsCancellationRequested || _disposing)
                {
                    return;
                }

                logger.Trace(L($"Sending binary thread failed, error: {e.Message}. Creating a new sending thread."));
                StartBackgroundThreadForSendingBinary();
            }
        }

        private void StartBackgroundThreadForSendingText()
        {
            _ = Task.Factory.StartNew(_ => SendTextFromQueue(), TaskCreationOptions.LongRunning, _cancellationTotal.Token);
        }

        private void StartBackgroundThreadForSendingBinary()
        {
            _ = Task.Factory.StartNew(_ => SendBinaryFromQueue(), TaskCreationOptions.LongRunning, _cancellationTotal.Token);
        }

        private async Task SendInternalSynchronized(string message)
        {
            using (await _locker.LockAsync())
            {
                await SendInternal(message);
            }
        }

        private async Task SendInternal(string message)
        {
            if (!IsClientConnected())
            {
                logger.Debug(L($"Client is not connected to server, cannot send:  {message}"));
                return;
            }

            logger.Trace(L($"Sending:  {message}"));
            var buffer = GetEncoding().GetBytes(message);
            var messageSegment = new ArraySegment<byte>(buffer);
            await _client
                .SendAsync(messageSegment, WebSocketMessageType.Text, true, _cancellation.Token)
                .ConfigureAwait(false);
        }

        private async Task SendInternalSynchronized(ArraySegment<byte> message)
        {
            using (await _locker.LockAsync())
            {
                await SendInternal(message);
            }
        }

        private async Task SendInternal(ArraySegment<byte> message)
        {
            if (!IsClientConnected())
            {
                logger.Debug(L($"Client is not connected to server, cannot send binary, length: {message.Count}"));
                return;
            }

            logger.Trace(L($"Sending binary, length: {message.Count}"));

            await _client
                .SendAsync(message, WebSocketMessageType.Binary, true, _cancellation.Token)
                .ConfigureAwait(false);
        }
    }
}
