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
            // Human Simulation: Move Mouse first
            RealInputUtility.SimulateMouseMove(screenPos);
            // Small delay for headers/hovers to process
            await Task.Yield(); 

            var target = ResolveTarget(screenPos);
            if (target == null)
            {
                return;
            }

            // Press
            RealInputUtility.SimulateMouseClick(true);
            await SimulateTouch(target, screenPos, TouchPhase.Began);
            
            await Wait.Seconds(duration);

            // Release
            RealInputUtility.SimulateMouseClick(false);
            await SimulateTouch(target, screenPos, TouchPhase.Ended);
        }

        /// <summary>Simulate a swipe gesture.</summary>
        public static async Task Swipe(Vector2 from, Vector2 to, float duration = 0.3f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            // Human Input: Move to start
            RealInputUtility.SimulateMouseMove(from);
            await Task.Yield();
            
            var target = ResolveTarget(from);
            
            // Press
            RealInputUtility.SimulateMouseClick(true);
            await SimulateTouch(target, from, TouchPhase.Began);
            
            // Drag
            await SimulateSwipeMove(target, from, to, duration);
            
            // Release
            RealInputUtility.SimulateMouseClick(false);
            await SimulateTouch(target, to, TouchPhase.Ended);
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

            var target = ResolveTarget(screenPos);
            await SimulateTouch(target, screenPos, TouchPhase.Began);
            await Wait.Seconds(duration);
            await SimulateTouch(target, screenPos, TouchPhase.Ended);
        }

        private static GameObject ResolveTarget(Vector2 screenPos)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.position = screenPos;
            var results = RealInputUtility.Raycasts(data);
            return results.Count > 0 ? results[0].gameObject : null;
        }

        private static Task SimulateTouch(GameObject target, Vector2 screenPos, TouchPhase phase)
        {
            if (target == null) return Task.CompletedTask;

            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.pointerId = 0;
            data.position = screenPos;
            data.button = PointerEventData.InputButton.Left;
            // Re-populate raycast info so event system knows what we hit (even if we force target)
            var results = RealInputUtility.Raycasts(data);
            if (results.Count > 0)
            {
                data.pointerPressRaycast = results[0];
                data.pointerCurrentRaycast = results[0];
            }

            if (phase == TouchPhase.Began)
            {
                data.pointerPress = target;
                data.rawPointerPress = target;

                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.beginDragHandler);
            }
            else if (phase == TouchPhase.Ended)
            {
                data.pointerPress = target; // Ensure Up happens on the same press target
                data.rawPointerPress = target;
                
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.endDragHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
            }

            return Task.CompletedTask;
        }

        private static async Task SimulateSwipeMove(GameObject target, Vector2 from, Vector2 to, float duration)
        {
            if (target == null) return;

            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.pointerId = 0;
            data.button = PointerEventData.InputButton.Left;
            data.pointerPress = target; // Drag needs this
            data.pointerDrag = target;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                data.position = Vector2.Lerp(from, to, t);
                RealInputUtility.SimulateMouseMove(data.position);
                
                // Execute Drag on the specific target we started with
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.dragHandler);
                
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
            RealInputUtility.SimulateMouseMove(to);
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

            RealInputUtility.SimulateMouseMove(center);
            RealInputUtility.SimulateMouseClick(true);
            RealInputUtility.SimulateDragging(true);

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

                RealInputUtility.SimulateMouseMove(center);
                yield return null;
            }

            RealInputUtility.SimulateMouseClick(false);
            RealInputUtility.SimulateDragging(false);

            if (t1 != null)
            {
                ExecuteEvents.ExecuteHierarchy(t1, f1, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(t1, f1, ExecuteEvents.pointerClickHandler);
            }

            if (t2 != null)
            {
                ExecuteEvents.ExecuteHierarchy(t2, f2, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(t2, f2, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
