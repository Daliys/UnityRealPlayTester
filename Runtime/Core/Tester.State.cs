using System;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static class State
        {
            /// <summary>Capture a serializable snapshot of an object's state.</summary>
            public static string CaptureObject(object target) => RealPlayEnvironment.IsEnabled ? StateTracker.CaptureObjectState(target) : string.Empty;

            /// <summary>Capture full scene state for diffing.</summary>
            public static string Capture()
            {
                if (!RealPlayEnvironment.IsEnabled) return string.Empty;
                var snap = StateTracker.Capture();
                return JsonUtility.ToJson(snap, true); 
            }

            /// <summary>Get human-readable diff between two states.</summary>
            public static string GetDiff(string before, string after)
            {
                if (!RealPlayEnvironment.IsEnabled) return string.Empty;
                try
                {
                    var b = JsonUtility.FromJson<StateTracker.StateSnapshot>(before);
                    var a = JsonUtility.FromJson<StateTracker.StateSnapshot>(after);
                    return StateTracker.GetDiff(b, a);
                }
                catch { return StateTracker.GetJsonDiff(before, after); }
            }
        }
    }
}
