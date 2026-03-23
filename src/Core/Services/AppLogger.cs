using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using DhCodetaskExtension.Services;

namespace DhCodetaskExtension.Core.Services
{
    /// <summary>
    /// Thread-safe logger: ghi vào file và VS Output Window.
    /// Tự động tạo file log trong %AppData%\DhCodetaskExtension\logs\.
    /// </summary>
    public sealed class AppLogger
    {
        private static AppLogger _instance;
        private static readonly object _initLock = new object();

        private readonly string _logFilePath;
        private readonly object _fileLock = new object();
        private OutputWindowService _outputWindow;

        private AppLogger()
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DhCodetaskExtension", "logs");
                Directory.CreateDirectory(dir);
                _logFilePath = Path.Combine(dir,
                    string.Format("devtask_{0:yyyyMMdd}.log", DateTime.Today));
            }
            catch
            {
                // non-critical
            }
        }

        public static AppLogger Instance
        {
            get
            {
                if (_instance == null)
                    lock (_initLock)
                        if (_instance == null)
                            _instance = new AppLogger();
                return _instance;
            }
        }

        public void Init(OutputWindowService outputWindow)
        {
            _outputWindow = outputWindow;
        }

        public void Info(string message)  => Write("INFO ", message);
        public void Warn(string message)  => Write("WARN ", message);
        public void Error(string message) => Write("ERROR", message);

        public void Error(string context, Exception ex)
        {
            if (ex == null)
            {
                Error(context);
                return;
            }

            Write("ERROR", string.Format("[{0}] {1}{2}  {3}",
                context,
                ex.Message,
                Environment.NewLine,
                ex.StackTrace?.Replace(Environment.NewLine, Environment.NewLine + "  ") ?? string.Empty));
        }

        public void TryCatch(string context, Action action)
        {
            try { action(); }
            catch (Exception ex) { Error(context, ex); }
        }

        public async System.Threading.Tasks.Task<bool> TryCatchAsync(string context,
            Func<System.Threading.Tasks.Task> action)
        {
            try { await action(); return true; }
            catch (Exception ex) { Error(context, ex); return false; }
        }

        private void Write(string level, string message)
        {
            var normalizedMessage = (message ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            var line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] [T{2}] {3}",
                DateTime.Now,
                level,
                Thread.CurrentThread.ManagedThreadId,
                normalizedMessage);

            ThreadPool.QueueUserWorkItem(_ => AppendToFile(line));

            try
            {
                var outputLine = string.Format("[{0}] [T{1}] {2}",
                    level.Trim(),
                    Thread.CurrentThread.ManagedThreadId,
                    normalizedMessage.Replace("\n", Environment.NewLine + "    "));
                _outputWindow?.LogSafe(outputLine);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[DhCodetaskExtension][Logger] Output window logging failed: " + ex.Message);
            }
        }

        private void AppendToFile(string line)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("[DhCodetaskExtension][Logger] File logging failed: " + ex.Message);
                }
            }
        }
    }
}
