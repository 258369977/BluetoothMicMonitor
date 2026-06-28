using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

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
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static string ConfigDirectory { get { return ConfigDir; } }

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
                RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                if (key == null) return false;
                object val = key.GetValue(AppName);
                key.Close();
                return val != null;
            }
            catch { return false; }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null) { Logger.Error("Cannot open registry Run key."); return; }
                if (enable)
                {
                    string exePath = System.Reflection.Assembly.GetEntryAssembly() != null
                        ? System.Reflection.Assembly.GetEntryAssembly().Location
                        : AppName + ".exe";
                    key.SetValue(AppName, "\"" + exePath + "\" --minimized");
                    Logger.Info("Auto-start enabled (Registry).");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    Logger.Info("Auto-start disabled (Registry).");
                }
                key.Close();
            }
            catch (Exception ex) { Logger.Error("Auto-start registry failed: " + ex.Message); }
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