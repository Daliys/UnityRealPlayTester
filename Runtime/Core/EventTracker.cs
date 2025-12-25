using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Tracks game events for assertion during tests.
    /// Supports 'Event Chain Verification' requirements.
    /// </summary>
    public static class EventTracker
    {
        private static readonly Dictionary<string, int> _events = new Dictionary<string, int>();
        private static readonly List<string> _history = new List<string>();

        public static void Record(string eventName)
        {
            if (!_events.ContainsKey(eventName)) _events[eventName] = 0;
            _events[eventName]++;
            _history.Add(eventName);
            RealPlayLog.Info($"[EventTracker] Recorded: {eventName}");
        }

        public static void Clear()
        {
            _events.Clear();
            _history.Clear();
        }

        public static bool WasFired(string eventName, int minCount = 1)
        {
            return _events.TryGetValue(eventName, out int count) && count >= minCount;
        }

        public static int GetCount(string eventName)
        {
            return _events.TryGetValue(eventName, out int count) ? count : 0;
        }

        public static IReadOnlyList<string> GetHistory() => _history;
    }
}
