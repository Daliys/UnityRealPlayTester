using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Await;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Simulates complex multi-finger touch patterns (Gestures).
    /// </summary>
    public static class GestureSimulator
    {
        /// <summary>
        /// Simulates a two-finger pinch or spread gesture.
        /// </summary>
        /// <param name="center">The center point of the gesture in screen coordinates.</param>
        /// <param name="startDistance">Initial distance between fingers.</param>
        /// <param name="endDistance">Final distance between fingers.</param>
        /// <param name="angle">Rotation angle of the finger axis (in degrees).</param>
        /// <param name="duration">How long the gesture takes.</param>
        public static async Task Pinch(Vector2 center, float startDistance, float endDistance, float angle = 0f, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            
            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gesture: Pinch at {center}, dist {startDistance}->{endDistance}, angle {angle}");
            await RealPlayTesterHost.Instance.RunCoroutineTask(PinchRoutine(center, startDistance, endDistance, angle, duration), RealPlayExecutionContext.Token);
        }

        /// <summary>
        /// Simulates a two-finger rotation (twist) gesture.
        /// </summary>
        /// <param name="center">Center of rotation.</param>
        /// <param name="distance">Distance between the two fingers.</param>
        /// <param name="startAngle">Initial angle in degrees.</param>
        /// <param name="endAngle">Final angle in degrees.</param>
        /// <param name="duration">How long the rotation takes.</param>
        public static async Task Twist(Vector2 center, float distance, float startAngle, float endAngle, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gesture: Twist at {center}, angle {startAngle}->{endAngle}");
            await RealPlayTesterHost.Instance.RunCoroutineTask(TwistRoutine(center, distance, startAngle, endAngle, duration), RealPlayExecutionContext.Token);
        }

        /// <summary>
        /// Simulates rapid repeated taps at the same location.
        /// </summary>
        public static async Task MultiTap(Vector2 screenPos, int count, float interval = 0.1f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gesture: MultiTap x{count} at {screenPos}");
            for (int i = 0; i < count; i++)
            {
                await Touch.Tap(screenPos, 0.05f);
                if (i < count - 1) await Wait.Seconds(interval);
            }
        }

        private static IEnumerator PinchRoutine(Vector2 center, float startDist, float endDist, float angle, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var f1 = new PointerEventData(es) { pointerId = 10, button = PointerEventData.InputButton.Left };
            var f2 = new PointerEventData(es) { pointerId = 11, button = PointerEventData.InputButton.Left };
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            
            float elapsed = 0f;
            GameObject t1 = null, t2 = null;

            while (elapsed < duration)
            {
                float t = duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
                float currentDist = Mathf.Lerp(startDist, endDist, t);
                f1.position = center + dir * (currentDist * 0.5f);
                f2.position = center - dir * (currentDist * 0.5f);

                if (elapsed == 0f) (t1, t2) = StartGesture(f1, f2);
                UpdateGesture(t1, t2, f1, f2);

                elapsed += Time.deltaTime;
                yield return null;
            }
            EndGesture(t1, t2, f1, f2);
        }

        private static IEnumerator TwistRoutine(Vector2 center, float distance, float startAngle, float endAngle, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var f1 = new PointerEventData(es) { pointerId = 20, button = PointerEventData.InputButton.Left };
            var f2 = new PointerEventData(es) { pointerId = 21, button = PointerEventData.InputButton.Left };

            float elapsed = 0f;
            GameObject t1 = null, t2 = null;

            while (elapsed < duration)
            {
                float t = duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                Vector2 dir = Quaternion.Euler(0, 0, currentAngle) * Vector2.up;
                f1.position = center + dir * (distance * 0.5f);
                f2.position = center - dir * (distance * 0.5f);

                if (elapsed == 0f) (t1, t2) = StartGesture(f1, f2);
                UpdateGesture(t1, t2, f1, f2);

                elapsed += Time.deltaTime;
                yield return null;
            }
            EndGesture(t1, t2, f1, f2);
        }

        private static (GameObject, GameObject) StartGesture(PointerEventData f1, PointerEventData f2)
        {
            var r1 = RealInputUtility.Raycasts(f1);
            var r2 = RealInputUtility.Raycasts(f2);
            GameObject t1 = r1.Count > 0 ? r1[0].gameObject : null;
            GameObject t2 = r2.Count > 0 ? r2[0].gameObject : null;
            
            if (t1 != null) { ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(t1, f1, ExecuteEvents.beginDragHandler); }
            if (t2 != null) { ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerDownHandler); ExecuteEvents.Execute(t2, f2, ExecuteEvents.beginDragHandler); }
            return (t1, t2);
        }

        private static void UpdateGesture(GameObject t1, GameObject t2, PointerEventData f1, PointerEventData f2)
        {
            if (t1 != null) ExecuteEvents.Execute(t1, f1, ExecuteEvents.dragHandler);
            if (t2 != null) ExecuteEvents.Execute(t2, f2, ExecuteEvents.dragHandler);
        }

        private static void EndGesture(GameObject t1, GameObject t2, PointerEventData f1, PointerEventData f2)
        {
            if (t1 != null) ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerUpHandler);
            if (t2 != null) ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerUpHandler);
        }
    }
}
