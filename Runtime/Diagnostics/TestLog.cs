using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Centralized logging API for RealPlay tests.
    /// Routes to GameLogger + EventAggregator when available, otherwise uses Debug.Log.
    /// </summary>
    public static class TestLog
    {
        private static bool s_hasGameLogger;
        private static bool s_hasEventAggregator;
        private static System.Type s_gameLoggerType;
        private static System.Type s_eventAggregatorType;
        private static object s_gameLoggerInstance;
        private static object s_eventAggregatorInstance;

        static TestLog()
        {
            // Try to find GameLogger and EventAggregator via reflection
            s_gameLoggerType = System.Type.GetType("GameLogger, Assembly-CSharp");
            s_eventAggregatorType = System.Type.GetType("EventAggregator, Assembly-CSharp");

            if (s_gameLoggerType != null)
            {
                var instanceProp = s_gameLoggerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp != null)
                {
                    s_gameLoggerInstance = instanceProp.GetValue(null);
                    s_hasGameLogger = s_gameLoggerInstance != null;
                }
            }

            if (s_eventAggregatorType != null)
            {
                var instanceProp = s_eventAggregatorType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProp != null)
                {
                    s_eventAggregatorInstance = instanceProp.GetValue(null);
                    s_hasEventAggregator = s_eventAggregatorInstance != null;
                }
            }
        }

        /// <summary>
        /// Log an informational message.
        /// </summary>
        public static void Info(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            Log("WARN", message);
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        private static void Log(string level, string message)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            string formattedMessage = $"[RealPlayTest] [{level}] {message}";

            // Try GameLogger
            if (s_hasGameLogger && s_gameLoggerInstance != null)
            {
                var logMethod = s_gameLoggerType.GetMethod("Log", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (logMethod != null)
                {
                    logMethod.Invoke(s_gameLoggerInstance, new object[] { formattedMessage });
                }
            }

            // Try EventAggregator
            if (s_hasEventAggregator && s_eventAggregatorInstance != null)
            {
                var publishMethod = s_eventAggregatorType.GetMethod("Publish", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (publishMethod != null)
                {
                    // Assume EventAggregator.Publish takes (string eventType, object data)
                    publishMethod.Invoke(s_eventAggregatorInstance, new object[] { "TestLog", formattedMessage });
                }
            }

            // Fallback to Debug.Log
            switch (level)
            {
                case "ERROR":
                    Debug.LogError(formattedMessage);
                    break;
                case "WARN":
                    Debug.LogWarning(formattedMessage);
                    break;
                default:
                    Debug.Log(formattedMessage);
                    break;
            }
        }
    }
}
