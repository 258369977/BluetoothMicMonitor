using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BluetoothMicMonitor
{
    public class DeviceWorker : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly HashSet<string> _runningTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string[] _targets = new string[0];
        private string _deviceName = "";
        private CancellationTokenSource _cts;
        private Task _loopTask;

        public bool IsRunning { get { return _loopTask != null && !_loopTask.IsCompleted; } }

        public DeviceWorker(EventBus eventBus) { _eventBus = eventBus; }

        public void Start(string[] targets, string deviceName)
        {
            _targets = (string[])targets.Clone();
            _deviceName = deviceName;
            _cts = new CancellationTokenSource();
            SyncInitialState();
            _loopTask = Task.Run(new Func<Task>(() => EventLoopAsync(_cts.Token)));
            Logger.Info("DeviceWorker started.");
        }

        public void Stop()
        {
            if (_cts != null) _cts.Cancel();
            try { if (_loopTask != null) _loopTask.Wait(TimeSpan.FromSeconds(3)); } catch { }
            Logger.Info("DeviceWorker stopped.");
        }

        private async Task EventLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                AppEvent evt = null;
                try { evt = await _eventBus.ConsumeAsync(ct); }
                catch (OperationCanceledException) { break; }
                if (evt == null) continue;
                if (!_targets.Contains(evt.ProcessName, StringComparer.OrdinalIgnoreCase)) continue;
                Logger.Info("Event: " + evt.Type + " => " + evt.ProcessName);
                try
                {
                    if (evt.Type == "START")
                    {
                        _runningTargets.Add(evt.ProcessName);
                        if (_runningTargets.Count == 1) DisableDevice();
                    }
                    else if (evt.Type == "STOP")
                    {
                        _runningTargets.Remove(evt.ProcessName);
                        if (_runningTargets.Count == 0) EnableDevice();
                    }
                }
                catch (Exception ex) { Logger.Error("DeviceWorker: " + ex.Message); }
            }
        }

        private void SyncInitialState()
        {
            _runningTargets.Clear();
            foreach (var t in _targets)
            {
                var nameOnly = System.IO.Path.GetFileNameWithoutExtension(t);
                try { if (Process.GetProcessesByName(nameOnly).Length > 0) _runningTargets.Add(t); }
                catch { }
            }
            Logger.Info("Init sync: " + _runningTargets.Count + " target(s) running: [" + string.Join(", ", _runningTargets) + "]");
            if (_runningTargets.Count > 0)
            {
                try { DisableDevice(); }
                catch (Exception ex) { Logger.Error("Init disable failed: " + ex.Message); }
            }
        }

        private void DisableDevice()
        {
            if (string.IsNullOrWhiteSpace(_deviceName)) return;
            string err;
            if (SetupApi.SetDeviceState(_deviceName, false, out err))
            {
                Logger.Info("Disabled: " + _deviceName);
                RestartBthServ();
            }
            else Logger.Warn("Disable failed: " + err);
        }

        private void EnableDevice()
        {
            if (string.IsNullOrWhiteSpace(_deviceName)) return;
            string err;
            if (SetupApi.SetDeviceState(_deviceName, true, out err))
            {
                Logger.Info("Enabled: " + _deviceName);
                RestartBthServ();
            }
            else Logger.Warn("Enable failed: " + err);
        }

        private static void RestartBthServ()
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "stop bthserv")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var p1 = Process.Start(psi)) { if (p1 != null) p1.WaitForExit(5000); }
                psi.Arguments = "start bthserv";
                using (var p2 = Process.Start(psi)) { if (p2 != null) p2.WaitForExit(5000); }
                Logger.Info("bthserv restarted.");
            }
            catch (Exception ex) { Logger.Warn("bthserv restart failed: " + ex.Message); }
        }

        public void Dispose()
        {
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
            _runningTargets.Clear();
        }
    }
}
