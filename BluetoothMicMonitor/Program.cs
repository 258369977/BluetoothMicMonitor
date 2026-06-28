using System;
using System.Diagnostics;
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
            var startMinimized = args.Length > 0 && args[0].Equals("--minimized", StringComparison.OrdinalIgnoreCase);

            // Admin check: elevate if needed (only prompt UAC when NOT in minimized mode)
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    if (!startMinimized)
                    {
                        // Manual launch: auto-elevate via UAC (prompts once)
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
                        // Auto-start via scheduled task (which runs with highest privileges).
                        // If we somehow are not admin here, silently exit to avoid UAC popup.
                        Logger.Initialize(Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                            "BluetoothMicMonitor", "logs"));
                        Logger.Warn("Not running as admin in minimized mode — exiting.");
                    }
                    return;
                }
            }

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            Logger.Initialize(logDir);
            Logger.Info("=== BluetoothMicMonitor starting " + (startMinimized ? "(minimized)" : "(normal)") + " ===");

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

            // In minimized mode (auto-start): start daemon immediately, no GUI window
            if (_config.AutoStart || startMinimized)
                StartDaemon(_config);

            _tray.Running = _monitorRunning;

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            _mainWindow.Closing += (sender, e) => { e.Cancel = true; _mainWindow.Hide(); };

            // Only show window when NOT in minimized mode
            if (!startMinimized)
                _mainWindow.Show();

            Logger.Info("Application running (window=" + (!startMinimized) + ").");
            app.Run();
        }

        private static void OnOpenPanel()
        {
            if (_mainWindow != null)
                _mainWindow.Dispatcher.Invoke(new Action(() => { _mainWindow.Show(); _mainWindow.Activate(); }));
        }

        private static void OnViewLogs()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BluetoothMicMonitor", "logs");
            if (Directory.Exists(logDir))
            {
                try { Process.Start("explorer.exe", logDir); }
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