using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace BluetoothMicMonitor
{
    public class ProcessWatcher : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly object _lock = new object();
        private CancellationTokenSource _cts;
        private Task _loopTask;
        private string[] _targets = new string[0];
        private bool _disposed;

        public ProcessWatcher(EventBus eventBus) { _eventBus = eventBus; }

        public void Start(string[] targets)
        {
            _targets = (string[])targets.Clone();
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(new Func<Task>(() => PollLoopAsync(_cts.Token)));
            Logger.Info("ProcessWatcher started (polling mode).");
        }

        public void Stop()
        {
            if (_cts != null) _cts.Cancel();
            try { if (_loopTask != null) _loopTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            Logger.Info("ProcessWatcher stopped.");
        }

        private async Task PollLoopAsync(CancellationToken ct)
        {
            var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (!ct.IsCancellationRequested)
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        var nameOnly = Path.GetFileNameWithoutExtension(target);
                        var procs = Process.GetProcessesByName(nameOnly);
                        var isRunning = procs.Length > 0;
                        if (isRunning && !running.Contains(target))
                        {
                            running.Add(target);
                            _eventBus.Publish(new AppEvent { Type = "START", ProcessName = target });
                        }
                        else if (!isRunning && running.Contains(target))
                        {
                            running.Remove(target);
                            _eventBus.Publish(new AppEvent { Type = "STOP", ProcessName = target });
                        }
                    }
                    catch { }
                }
                try { await Task.Delay(500, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
        }
    }
}
