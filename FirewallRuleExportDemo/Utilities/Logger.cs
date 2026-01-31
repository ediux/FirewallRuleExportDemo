using System;
using System.IO;

namespace FirewallRuleExportDemo.Utilities
{
    public static class Logger
    {
        private static string logFilePath;
        private static string logLevel = "Info";
        private static bool enableConsoleLog = true;
        private static bool enableFileLog = true;

        public static void Initialize(string filePath, string level, bool consoleLog, bool fileLog)
        {
            logFilePath = filePath;
            logLevel = level;
            enableConsoleLog = consoleLog;
            enableFileLog = fileLog;

            if (enableFileLog && !string.IsNullOrEmpty(logFilePath))
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}\n例外訊息: {ex.Message}\n堆疊追蹤: {ex.StackTrace}" : message;
            Log("ERROR", fullMessage);
        }

        public static void LogDebug(string message)
        {
            if (logLevel == "Debug" || logLevel == "Trace")
            {
                Log("DEBUG", message);
            }
        }

        public static void LogTrace(string message)
        {
            if (logLevel == "Trace")
            {
                Log("TRACE", message);
            }
        }

        private static void Log(string level, string message)
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            if (enableConsoleLog)
            {
                Console.WriteLine(logMessage);
            }

            if (enableFileLog && !string.IsNullOrEmpty(logFilePath))
            {
                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"無法寫入日誌檔案: {ex.Message}");
                }
            }
        }
    }
}
