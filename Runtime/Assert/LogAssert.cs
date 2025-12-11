using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    /// <summary>
    /// Monitor Unity log messages during tests.
    /// </summary>
    public static class LogAssert
    {
        private static bool _listening;
        private static List<string> _unexpectedErrors = new List<string>();
        private static List<Regex> _expectedPatterns = new List<Regex>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            // hooks are managed by Start/Stop calls from TestRunner or user
        }

        internal static void StartListening()
        {
            if (_listening) return;
            _listening = true;
            _unexpectedErrors.Clear();
            _expectedPatterns.Clear(); // Ensure clean slate
            Application.logMessageReceived += OnLogMessage;
        }

        internal static void StopListening()
        {
            if (!_listening) return;
            _listening = false;
            Application.logMessageReceived -= OnLogMessage;
        }

        public static void Expect(string regexPattern)
        {
            if (RealPlayEnvironment.IsEnabled)
            {
                try
                {
                    _expectedPatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
                }
                catch (Exception ex)
                {
                    RealPlayLog.Warn($"Invalid regex in LogAssert.Expect: {ex.Message}");
                }
            }
        }

        public static void NoUnexpectedErrors()
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            if (_unexpectedErrors.Count > 0)
            {
                string msg = $"Found {_unexpectedErrors.Count} unexpected error(s):\n" + string.Join("\n", _unexpectedErrors);
                
                // Clear errors so we don't double-fail if called again
                _unexpectedErrors.Clear();
                
                RealPlayTester.Assert.Assert.Fail(msg);
            }
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            // Check if expected
            foreach (var pattern in _expectedPatterns)
            {
                if (pattern.IsMatch(condition))
                {
                    return; // It was expected, ignore it
                }
            }

            _unexpectedErrors.Add($"[{type}] {condition}");
        }
    }
}
