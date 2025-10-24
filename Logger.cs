using System;
using System.IO;

namespace MKV_Converter
{
    public static class Logger
    {
        private static readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        private static readonly object _lock = new object();

        public static void Clear()
        {
            try
            {
                File.WriteAllText(_logFilePath, $"Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear log file: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
