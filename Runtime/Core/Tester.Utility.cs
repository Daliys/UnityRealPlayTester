using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Utilities;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static class Utility
        {
            /// <summary>Calculate screen center for UI or World objects.</summary>
            public static Vector2 GetScreenCenter(GameObject go, Camera cam = null) => RealPlayEnvironment.IsEnabled ? RealInputUtility.GetScreenCenter(go, cam) : Vector2.zero;

            /// <summary>Perform custom raycast from camera.</summary>
            public static RaycastResult Raycast(Vector2 screenPos, Camera cam = null)
            {
                if (!RealPlayEnvironment.IsEnabled) return default;
                var es = RealInputUtility.EnsureEventSystem();
                var data = RealInputUtility.GetPooledPointerData(es);
                data.position = screenPos;
                var results = RealInputUtility.Raycasts(data);
                return results.Count > 0 ? results[0] : default;
            }

            /// <summary>Center mouse/pointer to middle of screen.</summary>
            public static Task ResetCursor() => RealPlayEnvironment.IsEnabled ? Interaction.Mouse.MoveTo(new Vector2(Screen.width / 2f, Screen.height / 2f)) : Task.CompletedTask;

            /// <summary>Visual debugging helpers.</summary>
            public static class Debug
            {
                public static Task Breakpoint(KeyCode resume = KeyCode.Space) => RealPlayEnvironment.IsEnabled ? DevTools.Breakpoint(resume) : Task.CompletedTask;
                public static void ShowMarker(Vector2 pos, float duration = 0.5f) { if (RealPlayEnvironment.IsEnabled) DevTools.ShowClickMarker(pos, duration); }
                public static void SetSlowMotion(float scale = 0.25f) { if (RealPlayEnvironment.IsEnabled) DevTools.SetSlowMotion(scale); }
                public static void Inspect<T>(string name, T value) { if (RealPlayEnvironment.IsEnabled) DevTools.Inspect(name, value); }
            }

            /// <summary>Perform action and verify condition immediately.</summary>
            public static async Task PerformAndVerify(Func<Task> action, Func<bool> check, string failMsg = null, float timeout = 5f)
            {
                if (!RealPlayEnvironment.IsEnabled) return;
                await action();
                await Wait.Until(check, timeout);
                if (!check()) Assert.Fail(failMsg ?? "Verification failed after action.");
            }

            /// <summary>Records a high-level event breadcrumb for AI timeline analysis.</summary>
            public static void RecordBreadcrumb(string type, string message)
            {
                if (RealPlayEnvironment.IsEnabled) RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb(type, message);
            }

            /// <summary>Marks a logical test step in the AI timeline.</summary>
            public static void TestStep(string message)
            {
                if (RealPlayEnvironment.IsEnabled) RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Step", message);
            }
        }
    }
}
