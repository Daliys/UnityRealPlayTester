using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>Touch gesture simulation helpers.</summary>
            public static class Touch
            {
                public static Task Tap(Vector2 pos, float duration = 0.1f) 
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Tap at {pos}");
                    return RealPlayTester.Input.Touch.Tap(pos, duration);
                }

                public static Task Swipe(Vector2 from, Vector2 to, float duration = 0.3f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Swipe(from, to, duration) : Task.CompletedTask;
                public struct PinchParams { public Vector2 center; public float start; public float end; public float duration; }
                public static Task Pinch(PinchParams p) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Pinch(p.center, p.start, p.end, p.duration) : Task.CompletedTask;
                public static Task LongPress(Vector2 pos, float duration = 1.0f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.LongPress(pos, duration) : Task.CompletedTask;
            }
        }
    }
}
