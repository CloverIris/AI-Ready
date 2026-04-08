#nullable enable

using System;
using System.IO;
using System.Threading;

namespace AIReady.Shared.Services
{
    /// <summary>
    /// 简单的文件日志记录器
    /// </summary>
    public static class FileLogger
    {
        private static readonly string LogPath;
        private static readonly ReaderWriterLockSlim Lock = new();

        static FileLogger()
        {
            LogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIReady",
                "logs",
                $"app_{DateTime.Now:yyyyMMdd}.log");
            
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch { }
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        public static void Log(string message, string level = "INFO")
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                
                Lock.EnterWriteLock();
                try
                {
                    File.AppendAllText(LogPath, logLine);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
                
                // 同时输出到控制台
                System.Diagnostics.Debug.WriteLine(logLine);
            }
            catch
            {
                // 忽略日志写入失败
            }
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        public static void LogException(Exception ex, string context = "")
        {
            var message = string.IsNullOrEmpty(context) 
                ? $"Exception: {ex.GetType().Name}: {ex.Message}"
                : $"{context} - Exception: {ex.GetType().Name}: {ex.Message}";
            
            Log(message, "ERROR");
            
            if (ex.InnerException != null)
            {
                Log($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}", "ERROR");
            }
            
            Log($"  Stack: {ex.StackTrace}", "ERROR");
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public static string GetLogPath() => LogPath;
    }
}
