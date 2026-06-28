using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;

namespace BluetoothMicMonitor
{
    public class AppConfig
    {
        public List<string> TargetProcesses { get; set; }
        public string DeviceName { get; set; }
        public bool AutoStart { get; set; }

        public AppConfig()
        {
            TargetProcesses = new List<string>();
            DeviceName = "ROSE EarFeel i7 Hands-Free";
            AutoStart = false;
        }

        public AppConfig Clone()
        {
            return new AppConfig
            {
                TargetProcesses = new List<string>(TargetProcesses),
                DeviceName = DeviceName,
                AutoStart = AutoStart
            };
        }
    }

    public static class ConfigManager
    {
        private const string AppName = "BluetoothMicMonitor";
        private const string TaskName = "BluetoothMicMonitor";
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static string ConfigDirectory { get { return ConfigDir; } }

        private static string GetExePath()
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly();
            if (asm != null) return asm.Location;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppName + ".exe");
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(CfgData));
                        var s = (CfgData)ser.ReadObject(ms);
                        if (s != null)
                            return new AppConfig
                            {
                                TargetProcesses = new List<string>(s.TargetProcesses ?? new string[0]),
                                DeviceName = s.DeviceName ?? "ROSE EarFeel i7 Hands-Free",
                                AutoStart = s.AutoStart
                            };
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("Config load failed: " + ex.Message); }
            return new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var s = new CfgData
                {
                    TargetProcesses = config.TargetProcesses.ToArray(),
                    DeviceName = config.DeviceName,
                    AutoStart = config.AutoStart
                };
                var ser = new DataContractJsonSerializer(typeof(CfgData));
                using (var ms = new MemoryStream())
                {
                    ser.WriteObject(ms, s);
                    var json = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                    File.WriteAllText(ConfigPath, json);
                }
                Logger.Info("Configuration saved.");
            }
            catch (Exception ex) { Logger.Error("Config save failed: " + ex.Message); }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                var psi = new ProcessStartInfo("schtasks.exe", "/query /tn \"" + TaskName + "\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return false;
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    return p.ExitCode == 0 && !output.Contains("ERROR:");
                }
            }
            catch { return false; }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                if (enable)
                {
                    string exePath = GetExePath();
                    string args = "/create /tn \"" + TaskName + "\" /tr \"cmd /c start /min \\\"\\\" \\\"" + exePath + "\\\" --minimized\" /sc onlogon /rl highest /f";
                    var psi = new ProcessStartInfo("schtasks.exe", args)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using (var p = Process.Start(psi))
                    {
                        if (p == null) return;
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(10000);
                        if (p.ExitCode == 0)
                            Logger.Info("Scheduled task created (auto-start enabled).");
                        else
                            Logger.Error("schtasks create failed: " + output);
                    }
                }
                else
                {
                    var psi = new ProcessStartInfo("schtasks.exe", "/delete /tn \"" + TaskName + "\" /f")
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using (var p = Process.Start(psi))
                    {
                        if (p == null) return;
                        p.WaitForExit(5000);
                        Logger.Info("Scheduled task removed (auto-start disabled).");
                    }
                }
            }
            catch (Exception ex) { Logger.Error("Auto-start operation failed: " + ex.Message); }
        }

        [System.Runtime.Serialization.DataContract]
        private class CfgData
        {
            [System.Runtime.Serialization.DataMember(Name = "targetProcesses")]
            public string[] TargetProcesses { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "deviceName")]
            public string DeviceName { get; set; }

            [System.Runtime.Serialization.DataMember(Name = "autoStart")]
            public bool AutoStart { get; set; }

            public CfgData()
            {
                TargetProcesses = new string[0];
                DeviceName = "";
            }
        }
    }
}