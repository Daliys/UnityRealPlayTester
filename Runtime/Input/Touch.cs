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
        public static bool IsAvailable => InputSystemShim.IsAvailable || Application.isMobilePlatform;

        /// <summary>Simulate a tap at the given screen position.</summary>
        public static async Task Tap(Vector2 screenPos, float duration = 0.1f)
        {
            RealInputUtility.SimulateMouseMove(screenPos);
            await Task.Yield(); 

            var target = ResolveTarget(screenPos);
            if (target == null) return;

            RealInputUtility.SimulateMouseClick(true);
            await SimulateTouch(target, screenPos, TouchPhase.Began);
            
            await Wait.Seconds(duration);

            RealInputUtility.SimulateMouseClick(false);
            await SimulateTouch(target, screenPos, TouchPhase.Ended);
        }

        /// <summary>Simulate a swipe gesture.</summary>
        public static async Task Swipe(Vector2 from, Vector2 to, float duration = 0.3f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            RealInputUtility.SimulateMouseMove(from);
            await Task.Yield();
            
            var target = ResolveTarget(from);
            if (target == null) return;
            
            RealInputUtility.SimulateMouseClick(true);
            await SimulateTouch(target, from, TouchPhase.Began);
            
            await SimulateSwipeMove(target, from, to, duration);
            
            RealInputUtility.SimulateMouseClick(false);
            await SimulateTouch(target, to, TouchPhase.Ended);
        }

        /// <summary>Simulate a pinch (two-finger) gesture.</summary>
        public static async Task Pinch(Vector2 center, float startDistance, float endDistance, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            await RealPlayTesterHost.Instance.RunCoroutineTask(PinchRoutine(center, startDistance, endDistance, duration), RealPlayExecutionContext.Token);
        }

        /// <summary>Simulate a long press at the given position.</summary>
        public static async Task LongPress(Vector2 screenPos, float duration = 1.0f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var target = ResolveTarget(screenPos);
            if (target == null) return;

            await SimulateTouch(target, screenPos, TouchPhase.Began);
            // STABILITY FIX: Use unscaled wait for technical press
            await Wait.Seconds(duration, unscaled: true);
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

        private static async Task SimulateTouch(GameObject target, Vector2 screenPos, TouchPhase phase, int pointerId = 0)
        {
            if (target == null) return;

            var es = RealInputUtility.EnsureEventSystem();
            // MULTI-TOUCH FIX: Use dedicated data for touch to avoid pooled data corruption
            var data = new PointerEventData(es); 
            data.pointerId = pointerId;
            data.position = screenPos;
            data.button = PointerEventData.InputButton.Left;
            
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
                data.pointerPress = target; 
                data.rawPointerPress = target;
                
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.endDragHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerClickHandler);
            }

            await Task.Yield();
        }

        private static async Task SimulateSwipeMove(GameObject target, Vector2 from, Vector2 to, float duration)
        {
            if (target == null) return;

            var es = RealInputUtility.EnsureEventSystem();
            var data = new PointerEventData(es);
            data.pointerId = 0;
            data.button = PointerEventData.InputButton.Left;
            data.pointerPress = target; 
            data.pointerDrag = target;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                data.position = Vector2.Lerp(from, to, t);
                RealInputUtility.SimulateMouseMove(data.position);
                
                ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.dragHandler);
                await Task.Yield();
            }
            RealInputUtility.SimulateMouseMove(to);
        }

        private struct PinchPoints { public Vector2 S1, S2, E1, E2; }
        private struct PinchTargets { public GameObject T1, T2; }

        private static IEnumerator PinchRoutine(Vector2 center, float startDistance, float endDistance, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var f1 = new PointerEventData(es) { pointerId = 0, button = PointerEventData.InputButton.Left };
            var f2 = new PointerEventData(es) { pointerId = 1, button = PointerEventData.InputButton.Left };

            var pts = new PinchPoints
            {
                S1 = center + Vector2.up * (startDistance * 0.5f),
                S2 = center - Vector2.up * (startDistance * 0.5f),
                E1 = center + Vector2.up * (endDistance * 0.5f),
                E2 = center - Vector2.up * (endDistance * 0.5f)
            };

            var targets = PinchStart(f1, f2, pts);
            yield return PinchLoop(new PinchContext { center = center, f1 = f1, f2 = f2, targets = targets, pts = pts, duration = duration });
            PinchEnd(f1, f2, targets);
        }

        private static PinchTargets PinchStart(PointerEventData f1, PointerEventData f2, PinchPoints pts)
        {
            f1.position = pts.S1;
            f2.position = pts.S2;
            var r1 = RealInputUtility.Raycasts(f1);
            var r2 = RealInputUtility.Raycasts(f2);
            var targets = new PinchTargets { T1 = r1.Count > 0 ? r1[0].gameObject : null, T2 = r2.Count > 0 ? r2[0].gameObject : null };

            if (targets.T1 != null) { ExecuteEvents.Execute(targets.T1, f1, ExecuteEvents.pointerEnterHandler); ExecuteEvents.Execute(targets.T1, f1, ExecuteEvents.pointerDownHandler); }
            if (targets.T2 != null) { ExecuteEvents.Execute(targets.T2, f2, ExecuteEvents.pointerEnterHandler); ExecuteEvents.Execute(targets.T2, f2, ExecuteEvents.pointerDownHandler); }

            RealInputUtility.SimulateMouseClick(true);
            RealInputUtility.SimulateDragging(true);
            return targets;
        }

        private struct PinchContext { public Vector2 center; public PointerEventData f1; public PointerEventData f2; public PinchTargets targets; public PinchPoints pts; public float duration; }

        private static IEnumerator PinchLoop(PinchContext ctx)
        {
            float elapsed = 0f;
            while (elapsed < ctx.duration)
            {
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = ctx.duration > 0f ? Mathf.Clamp01(elapsed / ctx.duration) : 1f;
                ctx.f1.position = Vector2.Lerp(ctx.pts.S1, ctx.pts.E1, t);
                ctx.f2.position = Vector2.Lerp(ctx.pts.S2, ctx.pts.E2, t);

                if (ctx.targets.T1 != null) ExecuteEvents.Execute(ctx.targets.T1, ctx.f1, ExecuteEvents.dragHandler);
                if (ctx.targets.T2 != null) ExecuteEvents.Execute(ctx.targets.T2, ctx.f2, ExecuteEvents.dragHandler);

                RealInputUtility.SimulateMouseMove(ctx.center);
                yield return null;
            }
        }

        private static void PinchEnd(PointerEventData f1, PointerEventData f2, PinchTargets targets)
        {
            RealInputUtility.SimulateMouseClick(false);
            RealInputUtility.SimulateDragging(false);

            if (targets.T1 != null) { ExecuteEvents.ExecuteHierarchy(targets.T1, f1, ExecuteEvents.pointerUpHandler); ExecuteEvents.ExecuteHierarchy(targets.T1, f1, ExecuteEvents.pointerClickHandler); }
            if (targets.T2 != null) { ExecuteEvents.ExecuteHierarchy(targets.T2, f2, ExecuteEvents.pointerUpHandler); ExecuteEvents.ExecuteHierarchy(targets.T2, f2, ExecuteEvents.pointerClickHandler); }
        }
    }
}