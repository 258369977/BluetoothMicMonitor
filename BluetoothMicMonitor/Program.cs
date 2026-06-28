using System;
using System.IO;
using System.Security.Principal;
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

        [STAThread]
        public static void Main(string[] args)
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    var exePath = System.Reflection.Assembly.GetEntryAssembly() != null ? System.Reflection.Assembly.GetEntryAssembly().Location : "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "RunAs"
                        };
                        try { System.Diagnostics.Process.Start(psi); } catch { }
                    }
                    return;
                }
            }

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            Logger.Initialize(logDir);
            Logger.Info("=== BluetoothMicMonitor starting ===");

            _worker = new DeviceWorker(_eventBus);
            _watcher = new ProcessWatcher(_eventBus);
            _config = ConfigManager.Load();

            _tray = new TrayService();
            _tray.OpenRequested += OnOpenPanel;
            _tray.LogsRequested += OnViewLogs;
            _tray.AutoStartToggled += OnAutoStartToggled;
            _tray.ExitRequested += OnExit;
            _tray.AutoStartChecked = ConfigManager.IsAutoStartEnabled();
            _tray.Show();

            var startMinimized = args.Length > 0 && args[0].Equals("--minimized", StringComparison.OrdinalIgnoreCase);

            if (_config.AutoStart || startMinimized)
            {
                StartDaemon(_config);
            }

            _tray.Running = _monitorRunning;

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            _mainWindow.Closing += (sender, e) => { e.Cancel = true; _mainWindow.Hide(); };

            if (!startMinimized)
                _mainWindow.Show();

            app.Run();
        }

        private static void OnOpenPanel()
        {
            _mainWindow.Dispatcher.Invoke(new Action(() => { _mainWindow.Show(); _mainWindow.Activate(); }));
        }

        private static void OnViewLogs()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            if (Directory.Exists(logDir))
            {
                try { System.Diagnostics.Process.Start("explorer.exe", logDir); }
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
