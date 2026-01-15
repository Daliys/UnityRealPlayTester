using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Intercepts UnityEngine.Debug logs and pipes them into the active TestRunContext.
    /// </summary>
    public static class LogInterceptor
    {
        private static bool _isSubscribed;

        public static void Initialize()
        {
            if (_isSubscribed) return;
            Application.logMessageReceived += HandleLog;
            _isSubscribed = true;
        }

        public static void Shutdown()
        {
            if (!_isSubscribed) return;
            Application.logMessageReceived -= HandleLog;
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

            // Deduplication
            if (_lastIndex >= 0 && _lastIndex < context.Logs.Count && message == _lastMessage && type == _lastType)
            {
                var entry = context.Logs[_lastIndex];
                entry.RepeatCount++;
                entry.Frame = UnityEngine.Time.frameCount;
                entry.Timestamp = UnityEngine.Time.time;
                return;
            }

            _isProcessing = true;
            try
            {
                string category = "Game";
                if (message.Contains("Test Step:")) category = "Step";
                else if (message.Contains("[RealPlayTest]")) category = "Test";
                else if (message.Contains("[INPUT]")) category = "Input";
                else if (message.Contains("[BLOCKER]")) category = "Input";
                else if (message.Contains("[HEARTBEAT]")) category = "Heartbeat";
                else if (message.StartsWith("[RealPlayTester]")) category = "Library";

                var newEntry = new LogEntry(category, message, stackTrace, type);
                context.Logs.Add(newEntry);
                _lastMessage = message;
                _lastType = type;
                _lastIndex = context.Logs.Count - 1;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Manually inject a test step or library log into the unified stream.
        /// </summary>
        public static void Inject(string type, string message)
        {
            var context = TestRunContextTracker.Current;
            if (context == null) return;

            if (_lastIndex >= 0 && _lastIndex < context.Logs.Count && message == _lastMessage)
            {
                context.Logs[_lastIndex].RepeatCount++;
                return;
            }

            var entry = new LogEntry(type, message);
            context.Logs.Add(entry);
            _lastMessage = message;
            _lastIndex = context.Logs.Count - 1;
        }
    }
}
