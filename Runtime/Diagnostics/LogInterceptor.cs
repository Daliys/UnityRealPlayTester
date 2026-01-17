using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Intercepts UnityEngine.Debug logs and pipes them into the active TestRunContext.
    /// </summary>
    public static class LogInterceptor
    {
        private static bool _isSubscribed;
        private static readonly object _lock = new object();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            if (_isSubscribed)
            {
                Application.logMessageReceivedThreaded -= HandleLog;
            }
            _isSubscribed = false;
            _isProcessing = false;
            _lastMessage = null;
            _lastIndex = -1;
        }

        public static void Initialize()
        {
            if (_isSubscribed) return;
            
            // RESET DEDUPLICATION STATE
            _lastMessage = null;
            _lastType = LogType.Log;
            _lastIndex = -1;

            // logMessageReceivedThreaded catches logs from ALL threads, including main.
            Application.logMessageReceivedThreaded += HandleLog;
            _isSubscribed = true;
        }

        public static void Shutdown()
        {
            if (!_isSubscribed) return;
            Application.logMessageReceivedThreaded -= HandleLog;
            _isSubscribed = false;
        }

        [System.ThreadStatic]
        private static bool _isProcessing;

        private static string _lastMessage;
        private static LogType _lastType;
        private static int _lastIndex = -1;

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (_isProcessing) return;
            
            var context = TestRunContextTracker.Current;
            if (context == null) return;

            lock (_lock)
            {
                if (IsDuplicate(context, message, type)) return;

                // PERFORMANCE FIX: Cap log count to prevent memory leaks in massive test runs
                if (context.GetLogCount() > 1000) return;

                _isProcessing = true;
                try
                {
                    string category = GetLogCategory(message);
                    var newEntry = new LogEntry(category, message, stackTrace, type);
                    context.AddLog(newEntry);
                    UpdateLastLog(message, type, context.GetLogCount() - 1);
                }
                finally
                {
                    _isProcessing = false;
                }
            }
        }

        private static bool IsDuplicate(TestRunContext context, string message, LogType type)
        {
            int count = context.GetLogCount();
            if (_lastIndex >= 0 && _lastIndex < count && message == _lastMessage && type == _lastType)
            {
                var entry = context.GetLogAt(_lastIndex);
                if (entry != null)
                {
                    entry.RepeatCount++;
                    try { entry.Frame = UnityEngine.Time.frameCount; entry.Timestamp = UnityEngine.Time.time; } catch { }
                    return true;
                }
            }
            return false;
        }

        private static string GetLogCategory(string message)
        {
            if (message.Contains("Test Step:")) return "Step";
            if (message.Contains("[RealPlayTest]")) return "Test";
            if (message.Contains("[INPUT]") || message.Contains("[BLOCKER]")) return "Input";
            if (message.Contains("[HEARTBEAT]")) return "Heartbeat";
            if (message.StartsWith("[RealPlayTester]")) return "Library";
            return "Game";
        }

        private static void UpdateLastLog(string message, LogType type, int index)
        {
            _lastMessage = message;
            _lastType = type;
            _lastIndex = index;
        }

        /// <summary>
        /// Manually inject a test step or library log into the unified stream.
        /// </summary>
        public static void Inject(string type, string message)
        {
            var context = TestRunContextTracker.Current;
            if (context == null) return;

            lock (_lock)
            {
                // M011: Enforce same log cap as regular logs
                if (context.GetLogCount() > 1000) return;

                int count = context.GetLogCount();
                if (_lastIndex >= 0 && _lastIndex < count && message == _lastMessage)
                {
                    var log = context.GetLogAt(_lastIndex);
                    if (log != null)
                    {
                        log.RepeatCount++;
                        return;
                    }
                }

                var entry = new LogEntry(type, message);
                context.AddLog(entry);
                UpdateLastLog(message, LogType.Log, context.GetLogCount() - 1);
            }
        }
    }
}
