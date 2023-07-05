using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportScore.Live.Threading
{
    public sealed class TimerAsync : IDisposable
    {
        private readonly Func<CancellationToken, Task> _callback;

        private readonly object _changeLock = new object();

        private TimeSpan _dueTime;

        private TimeSpan _period;

        private CancellationTokenSource _cancellationSource;

        private Task _timerTask;

        public TimerAsync(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period)
            : this(callback, dueTime, period, CancellationToken.None)
        {
        }

        public TimerAsync(Func<CancellationToken, Task> callback, TimeSpan dueTime, TimeSpan period, CancellationToken token)
        {
            _callback = callback;
            _dueTime = dueTime;
            _period = period;
            _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            Start();
        }

        public Task ChangeAsync(TimeSpan dueTime, TimeSpan period)
        {
            return ChangeAsync(dueTime, period, CancellationToken.None);
        }
        public async Task ChangeAsync(TimeSpan dueTime, TimeSpan period, CancellationToken token)
        {
            await StopAsync()
                .ConfigureAwait(false);

            lock (_changeLock)
            {
                _dueTime = dueTime;
                _period = period;
                _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                Start();
            }
        }

        public async Task StopAsync()
        {
            _cancellationSource.Cancel();

            try
            {
                await _timerTask
                        .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelled requested, just ignore it...
            }
        }

        public void Dispose()
        {
            StopAsync()
                .Wait();
        }

        private void Start()
        {
            _timerTask = IsRecurring
                ? StartRecurringAsync(_cancellationSource.Token)
                : StartOneTimeAsync(_cancellationSource.Token);
        }

        private Task StartRecurringAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(_dueTime, cancellationToken)
                    .ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    await RunCallback(cancellationToken)
                        .ConfigureAwait(false);
                    await Task.Delay(_period, cancellationToken)
                        .ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private Task StartOneTimeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(_dueTime, cancellationToken)
                        .ConfigureAwait(false);
                await RunCallback(cancellationToken)
                    .ConfigureAwait(false);
            }, cancellationToken);
        }

        private async Task RunCallback(CancellationToken cancellationToken)
        {
            try
            {
                await _callback(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelled requested, just ignore it...
                throw;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError(e.ToString());
            }
        }

        private bool IsRecurring => _period.TotalMilliseconds > 0;
    }
}
