using System;
using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// A single high-level event in the test timeline.
    /// </summary>
    public class Breadcrumb
    {
        public int Frame { get; set; }
        public float Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public Breadcrumb(string type, string message)
        {
            try { Frame = UnityEngine.Time.frameCount; Timestamp = UnityEngine.Time.time; } catch { }
            Type = type;
            Message = message;
        }
    }
}
