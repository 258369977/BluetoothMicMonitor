using System;
using System.IO;

namespace BluetoothMicMonitor
{
    public static class Logger
    {
        private static string _logDir;

        public static void Initialize(string logDir)
        {
            _logDir = logDir;
            Directory.CreateDirectory(_logDir);
        }

        public static void Info(string message) { Write("INFO", message); }
        public static void Warn(string message) { Write("WARN", message); }
        public static void Error(string message) { Write("ERROR", message); }

        private static void Write(string level, string message)
        {
            if (_logDir == null) return;
            var line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}", DateTime.Now, level, message);
            try
            {
                var file = Path.Combine(_logDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                File.AppendAllText(file, line + Environment.NewLine);
            }
            catch { }
            ConsoleColor fg;
            switch (level)
            {
                case "ERROR": fg = ConsoleColor.Red; break;
                case "WARN": fg = ConsoleColor.Yellow; break;
                default: fg = ConsoleColor.Cyan; break;
            }
            Console.ForegroundColor = fg;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }
}
