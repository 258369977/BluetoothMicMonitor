using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace BluetoothMicMonitor
{
    public static class Program
    {
        private static EventBus _eventBus = new EventBus();
        private static DeviceWorker _worker;
        private static ProcessWatcher _watcher;
        private static TrayService _tray;
        private static MainWindow _mainWindow;
        private static AppConfig _config = new AppConfig();
        private static bool _monitorRunning;
        private static Mutex _mutex;

        private const string MutexName = "BluetoothMicMonitor_SingleInstance";
        private const string ProcessName = "BluetoothMicMonitor";

        [STAThread]
        public static void Main(string[] args)
        {
            var startMinimized = args.Length > 0 && args[0].Equals("--minimized", StringComparison.OrdinalIgnoreCase);

            // --- Single-instance check (before admin check) ---
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Another instance is running.
                if (startMinimized)
                {
                    // Silent daemon: another instance already running, just exit silently
                    return;
                }
                else
                {
                    // Manual launch: kill existing silent daemon, then restart with full UI
                    KillExistingInstance();
                    // Retry acquiring mutex
                    _mutex.Close();
                    _mutex = new Mutex(true, MutexName, out createdNew);
                }
            }

            // --- Admin check ---
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    if (!startMinimized)
                    {
                        var exePath = System.Reflection.Assembly.GetEntryAssembly() != null
                            ? System.Reflection.Assembly.GetEntryAssembly().Location : "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            try { Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true, Verb = "RunAs" }); }
                            catch { }
                        }
                    }
                    else
                    {
                        var silentLogDir = Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                            "BluetoothMicMonitor", "logs");
                        Logger.Initialize(silentLogDir);
                        Logger.Warn("Not admin in minimized mode - exiting.");
                    }
                    _mutex.Close();
                    return;
                }
            }

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            Logger.Initialize(logDir);
            Logger.Info("=== BluetoothMicMonitor starting " + (startMinimized ? "(silent)" : "(normal)") + " ===");

            _worker = new DeviceWorker(_eventBus);
            _watcher = new ProcessWatcher(_eventBus);
            _config = ConfigManager.Load();

            if (startMinimized)
            {
                // === SILENT MODE: no tray, no window, daemon only ===
                Logger.Info("Silent mode - no tray, no window.");
                if (_config.AutoStart || startMinimized)
                    StartDaemon(_config);

                // Block forever until process is killed externally
                // (the daemon runs on background threads)
                var waitHandle = new ManualResetEvent(false);
                waitHandle.WaitOne();
                return;
            }

            // === NORMAL MODE: tray + window ===
            _tray = new TrayService();
            _tray.OpenRequested += OnOpenPanel;
            _tray.LogsRequested += OnViewLogs;
            _tray.AutoStartToggled += OnAutoStartToggled;
            _tray.ExitRequested += OnExit;
            _tray.AutoStartChecked = ConfigManager.IsAutoStartEnabled();
            _tray.Show();

            if (_config.AutoStart)
                StartDaemon(_config);

            _tray.Running = _monitorRunning;

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            _mainWindow.Closing += (sender, e) => { e.Cancel = true; _mainWindow.Hide(); };

            _mainWindow.Show();
            Logger.Info("Application running (normal mode).");
            app.Run();
        }

        private static void KillExistingInstance()
        {
            try
            {
                var procs = Process.GetProcessesByName(ProcessName);
                foreach (var p in procs)
                {
                    // Don't kill ourselves
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        Logger.Initialize(Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                            "BluetoothMicMonitor", "logs"));
                        Logger.Info("Killing existing silent instance (PID " + p.Id + ")...");
                        p.Kill();
                        p.WaitForExit(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                // Best effort - if we can't kill, continue anyway
                Logger.Warn("Could not kill existing instance: " + ex.Message);
            }
        }

        private static void OnOpenPanel()
        {
            if (_mainWindow != null)
                _mainWindow.Dispatcher.Invoke(new Action(() => { _mainWindow.Show(); _mainWindow.Activate(); }));
        }

        private static void OnViewLogs()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            if (Directory.Exists(dir))
            {
                try { Process.Start("explorer.exe", dir); }
                catch (Exception ex) { Logger.Error("Cannot open logs: " + ex.Message); }
            }
        }

        private static void OnAutoStartToggled(bool enable)
        {
            ConfigManager.SetAutoStart(enable);
        }

        private static void OnExit()
        {
            StopDaemon();
            if (_tray != null) _tray.Hide();
            if (_mutex != null) { _mutex.Close(); _mutex = null; }
            Application.Current.Shutdown();
        }

        public static AppConfig GetConfig() { return _config.Clone(); }
        public static void SetConfig(AppConfig config) { _config = config.Clone(); ConfigManager.Save(_config); }
        public static bool IsMonitorRunning() { return _monitorRunning; }

        public static void StartDaemon(AppConfig config)
        {
            if (_monitorRunning) return;
            var targets = config.TargetProcesses.ToArray();
            if (targets.Length == 0) { Logger.Warn("No target processes configured."); return; }
            _config = config.Clone();
            ConfigManager.Save(_config);
            _watcher.Start(targets);
            _worker.Start(targets, config.DeviceName);
            _monitorRunning = true;
            if (_tray != null) _tray.Running = true;
            Logger.Info("Daemon started.");
        }

        public static void StopDaemon()
        {
            if (!_monitorRunning) return;
            _watcher.Stop();
            _worker.Stop();
            _monitorRunning = false;
            if (_tray != null) _tray.Running = false;
            Logger.Info("Daemon stopped.");
        }
    }
}