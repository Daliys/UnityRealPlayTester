using System;
using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// A single log entry in the unified diagnostic stream.
    /// </summary>
    public class LogEntry
    {
        public int Frame { get; set; }
        public float Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Severity { get; set; }
        public int RepeatCount { get; set; } = 1;

        public LogEntry(string type, string message, string stackTrace = "", LogType severity = LogType.Log)
        {
            try { Frame = UnityEngine.Time.frameCount; Timestamp = UnityEngine.Time.time; } catch { }
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            Severity = severity;
        }
    }
}
