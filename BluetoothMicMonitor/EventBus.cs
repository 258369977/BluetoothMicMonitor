using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothMicMonitor
{
    public class EventBus
    {
        private readonly ConcurrentQueue<AppEvent> _queue = new ConcurrentQueue<AppEvent>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void Publish(AppEvent evt)
        {
            _queue.Enqueue(evt);
            try { _signal.Release(); } catch (SemaphoreFullException) { }
        }

        public async Task<AppEvent> ConsumeAsync(CancellationToken ct)
        {
            await _signal.WaitAsync(ct);
            AppEvent evt;
            _queue.TryDequeue(out evt);
            return evt;
        }
    }
}
