using System;
using System.IO;
using System.Threading;

namespace LeafClient.Services
{
    public static class LeafLog
    {
        private static Action<string, string>? _windowSink;
        private static readonly object _windowSinkLock = new();
        private static readonly object _fileLock = new();
        private static readonly string _logFilePath = ComputeLogPath();
        private static bool _rolledOver;

        private static string ComputeLogPath()
        {
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(root, "LeafClient", "Logs");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "launcher.log");
            }
            catch { return ""; }
        }

        public static void RegisterWindowSink(Action<string, string> sink)
        {
            lock (_windowSinkLock) { _windowSink = sink; }
        }

        public static void UnregisterWindowSink()
        {
            lock (_windowSinkLock) { _windowSink = null; }
        }

        public static void Info(string tag, string message) => Write("INFO", tag, message);
        public static void Warn(string tag, string message) => Write("WARN", tag, message);
        public static void Error(string tag, string message) => Write("ERROR", tag, message);
        public static void Debug(string tag, string message) => Write("DEBUG", tag, message);

        public static void Info(string tag, string message, Exception ex) => Write("INFO", tag, FormatWithException(message, ex));
        public static void Warn(string tag, string message, Exception ex) => Write("WARN", tag, FormatWithException(message, ex));
        public static void Error(string tag, string message, Exception ex) => Write("ERROR", tag, FormatWithException(message, ex));

        private static string FormatWithException(string message, Exception ex)
        {
            if (ex == null) return message;
            return $"{message} | {ex.GetType().Name}: {ex.Message}";
        }

        private static void Write(string level, string tag, string message)
        {
            string formatted = string.IsNullOrEmpty(tag) ? message : $"[{tag}] {message}";
            try { Console.WriteLine($"[{level}] {formatted}"); } catch { }
            Action<string, string>? sink;
            lock (_windowSinkLock) { sink = _windowSink; }
            if (sink != null)
            {
                try { sink(formatted, level); } catch { }
            }
            WriteToFile(level, formatted);
        }

        private static void WriteToFile(string level, string formatted)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            try
            {
                lock (_fileLock)
                {
                    if (!_rolledOver)
                    {
                        _rolledOver = true;
                        try
                        {
                            if (File.Exists(_logFilePath))
                            {
                                var fi = new FileInfo(_logFilePath);
                                if (fi.Length > 2_000_000)
                                {
                                    string old = _logFilePath + ".old";
                                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                                    try { File.Move(_logFilePath, old); } catch { }
                                }
                            }
                        }
                        catch { }
                    }
                    string line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z [{level}] {formatted}{Environment.NewLine}";
                    File.AppendAllText(_logFilePath, line);
                }
            }
            catch { }
        }

        public static string LogFilePath => _logFilePath;
    }
}
