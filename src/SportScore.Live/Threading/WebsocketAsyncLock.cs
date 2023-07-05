namespace SportScore.Live.Threading
{
    public class WebsocketAsyncLock
    {
        private readonly Task<IDisposable> _releaserTask;

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly IDisposable _releaser;

        public WebsocketAsyncLock()
        {
            _releaser = new Releaser(semaphoreSlim);
            _releaserTask = Task.FromResult(_releaser);
        }

        public IDisposable Lock()
        {
            semaphoreSlim.Wait();

            return _releaser;
        }


        public Task<IDisposable> LockAsync()
        {
            var waitTask = semaphoreSlim.WaitAsync();
            return waitTask.IsCompleted
                ? _releaserTask
                : waitTask.ContinueWith(
                    (_, releaser) => (IDisposable)releaser,
                    _releaser,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public Releaser(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
}
