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

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (_isProcessing) return;
            var context = TestRunContextTracker.Current;
            if (context == null) return;

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

                context.Logs.Add(new LogEntry(category, message, stackTrace, type));
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
            context.Logs.Add(new LogEntry(type, message));
        }
    }
}
