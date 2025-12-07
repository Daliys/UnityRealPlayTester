using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Await;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Touch helpers for mobile-like interactions (tap, swipe, pinch, long press).
    /// </summary>
    public static class Touch
    {
        /// <summary>Simulate a tap at the given screen position.</summary>
        public static async Task Tap(Vector2 screenPos, float duration = 0.1f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            await SimulateTouch(screenPos, TouchPhase.Began);
            await Wait.Seconds(duration);
            await SimulateTouch(screenPos, TouchPhase.Ended);
        }

        /// <summary>Simulate a swipe gesture.</summary>
        public static async Task Swipe(Vector2 from, Vector2 to, float duration = 0.3f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            await SimulateTouch(from, TouchPhase.Began);
            await SimulateSwipeMove(from, to, duration);
            await SimulateTouch(to, TouchPhase.Ended);
        }

        /// <summary>Simulate a pinch (two-finger) gesture.</summary>
        public static async Task Pinch(Vector2 center, float startDistance, float endDistance, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            await RealPlayTesterHost.Instance.RunCoroutineTask(PinchRoutine(center, startDistance, endDistance, duration), RealPlayExecutionContext.Token);
        }

        /// <summary>Simulate a long press at the given position.</summary>
        public static async Task LongPress(Vector2 screenPos, float duration = 1.0f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            await SimulateTouch(screenPos, TouchPhase.Began);
            await Wait.Seconds(duration);
            await SimulateTouch(screenPos, TouchPhase.Ended);
        }

        private static Task SimulateTouch(Vector2 screenPos, TouchPhase phase)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.pointerId = 0;
            data.position = screenPos;
            data.button = PointerEventData.InputButton.Left;

            var results = RealInputUtility.Raycasts(data);
            var target = results.Count > 0 ? results[0].gameObject : null;
            if (target != null)
            {
                if (phase == TouchPhase.Began)
                {
                    ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
                }
                else if (phase == TouchPhase.Ended)
                {
                    ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task SimulateSwipeMove(Vector2 from, Vector2 to, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.pointerId = 0;
            data.button = PointerEventData.InputButton.Left;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                data.position = Vector2.Lerp(from, to, t);
                var results = RealInputUtility.Raycasts(data);
                var target = results.Count > 0 ? results[0].gameObject : null;
                if (target != null)
                {
                    ExecuteEvents.Execute(target, data, ExecuteEvents.pointerMoveHandler);
                }
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

        private static IEnumerator PinchRoutine(Vector2 center, float startDistance, float endDistance, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();

            var f1 = new PointerEventData(es) { pointerId = 0, button = PointerEventData.InputButton.Left };
            var f2 = new PointerEventData(es) { pointerId = 1, button = PointerEventData.InputButton.Left };

            Vector2 start1 = center + Vector2.up * (startDistance * 0.5f);
            Vector2 start2 = center - Vector2.up * (startDistance * 0.5f);
            Vector2 end1 = center + Vector2.up * (endDistance * 0.5f);
            Vector2 end2 = center - Vector2.up * (endDistance * 0.5f);

            f1.position = start1;
            f2.position = start2;

            var results1 = RealInputUtility.Raycasts(f1);
            var results2 = RealInputUtility.Raycasts(f2);
            var t1 = results1.Count > 0 ? results1[0].gameObject : null;
            var t2 = results2.Count > 0 ? results2[0].gameObject : null;

            if (t1 != null)
            {
                ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerDownHandler);
            }
            if (t2 != null)
            {
                ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerDownHandler);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                f1.position = Vector2.Lerp(start1, end1, t);
                f2.position = Vector2.Lerp(start2, end2, t);

                if (t1 != null)
                {
                    ExecuteEvents.Execute(t1, f1, ExecuteEvents.dragHandler);
                }

                if (t2 != null)
                {
                    ExecuteEvents.Execute(t2, f2, ExecuteEvents.dragHandler);
                }

                yield return null;
            }

            if (t1 != null)
            {
                ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(t1, f1, ExecuteEvents.pointerClickHandler);
            }

            if (t2 != null)
            {
                ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(t2, f2, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
