﻿using SportScore.Live.Enums;
using SportScore.Live.Logging;
using SportScore.Live.Models;

namespace SportScore.Live
{
    public partial class WebSocketClient
    {
        public Task Reconnect()
        {
            return ReconnectInternal(false);
        }

        public Task ReconnectOrFail()
        {
            return ReconnectInternal(true);
        }

        private async Task ReconnectInternal(bool failFast)
        {
            if (!IsStarted)
            {
                logger.Debug(L("Client not started, ignoring reconnection.."));
                return;
            }
            
            try
            {
                await ReconnectSynchronized(ReconnectionType.ByUser, failFast, null).ConfigureAwait(false);
            }
            finally
            {
                _reconnecting = false;
            }
        }

        private async Task ReconnectSynchronized(ReconnectionType type, bool failFast, Exception causedException)
        {
            using (await _locker.LockAsync())
            {
                await Reconnect(type, failFast, causedException);
            }
        }

        private async Task Reconnect(ReconnectionType type, bool failFast, Exception causedException)
        {
            IsRunning = false;
            if (_disposing || !IsStarted)
                return;

            _reconnecting = true;

            var disType = TranslateTypeToDisconnection(type);
            var disInfo = DisconnectionInfo.Create(disType, _client, causedException);
            if (type != ReconnectionType.Error)
            {
                _disconnectedSubject.OnNext(disInfo);
                if (disInfo.CancelReconnection)
                    logger.Info(L($"Reconnecting canceled by user, exiting."));
                
            }

            _cancellation.Cancel();

            try
            {
                _client?.Abort();
            }
            catch (Exception e)
            {
                logger.Error(e, L($"Exception while aborting client. " + $"Error: '{e.Message}'"));
            }
            _client?.Dispose();

            if (!IsReconnectionEnabled || disInfo.CancelReconnection)
            {
                IsStarted = false;
                _reconnecting = false;
                return;
            }

            logger.Debug(L("Reconnecting..."));
            _cancellation = new CancellationTokenSource();
            await StartClient(_url, _cancellation.Token, type, failFast).ConfigureAwait(false);
            _reconnecting = false;
        }

        private void ActivateLastChance()
        {
            var timerMs = 1000 * 1;
            _lastChanceTimer = new Timer(LastChance, null, timerMs, timerMs);
        }

        private void DeactivateLastChance()
        {
            _lastChanceTimer?.Dispose();
            _lastChanceTimer = null;
        }

        private void LastChance(object state)
        {
            if (!IsReconnectionEnabled || ReconnectTimeout == null)
            {
                DeactivateLastChance();
                return;
            }

            var timeoutMs = Math.Abs(ReconnectTimeout.Value.TotalMilliseconds);
            var diffMs = Math.Abs(DateTime.UtcNow.Subtract(_lastReceivedMsg).TotalMilliseconds);
            if (diffMs > timeoutMs)
            {
                logger.Debug(L($"Last message received more than {timeoutMs:F} ms ago. Hard restart.."));

                DeactivateLastChance();
                _ = ReconnectSynchronized(ReconnectionType.NoMessageReceived, false, null);
            }
        }
    }
}
