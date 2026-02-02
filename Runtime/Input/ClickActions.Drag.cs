using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Await;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    public static class Drag
    {
        public static Task FromTo(Vector2 start, Vector2 end, float duration)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            var token = RealPlayExecutionContext.Token;
            return RealPlayTesterHost.Instance.RunCoroutineTask(DragRoutine(RealInputUtility.ClampToScreen(start), RealInputUtility.ClampToScreen(end), duration), token);
        }

        private static IEnumerator DragRoutine(Vector2 start, Vector2 end, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            data.position = start;

#if UNITY_EDITOR
            bool wasLocked = false;
            if (RealPlaySettings.Input.LockAssemblies)
            {
                UnityEditor.EditorApplication.LockReloadAssemblies();
                wasLocked = true;
            }
            try {
#endif
            RealInputUtility.SimulateMouseMove(start);
            data.button = PointerEventData.InputButton.Left;
            Canvas.ForceUpdateCanvases();

            var results = RealInputUtility.Raycasts(data);
            var (down, drag) = FindDragTargets(start, results, es);

            if (down == null && drag == null)
            {
                RealPlayLog.Warn($"Drag: no target at {start}");
                yield break;
            }

            InitiateDrag(down, drag, data, start);
            yield return StartDragMotion(start, end, duration, drag, data);
            EndDrag(down, drag, data, end);
#if UNITY_EDITOR
            } finally {
                if (wasLocked) UnityEditor.EditorApplication.UnlockReloadAssemblies();
            }
#endif
        }

        private static (GameObject down, GameObject drag) FindDragTargets(Vector2 pos, System.Collections.Generic.List<RaycastResult> results, EventSystem es)
        {
            GameObject target = null;
            foreach (var res in results)
            {
                if (res.gameObject != null && (ExecuteEvents.GetEventHandler<IPointerDownHandler>(res.gameObject) != null || ExecuteEvents.GetEventHandler<IDragHandler>(res.gameObject) != null))
                {
                    target = res.gameObject;
                    break;
                }
            }

            if (target == null && (Application.isBatchMode || Tester.Settings.ForceBatchmodeVisibility))
                target = RealInputUtility.FindFallbackInteractionTarget(pos);

            if (target == null && results.Count > 0) target = results[0].gameObject;
            if (target == null) return (null, null);

            return (ExecuteEvents.GetEventHandler<IPointerDownHandler>(target), ExecuteEvents.GetEventHandler<IDragHandler>(target));
        }

        private static void InitiateDrag(GameObject down, GameObject drag, PointerEventData data, Vector2 start)
        {
            data.pointerPress = down;
            data.pointerDrag = drag;
            data.pressPosition = start;
            ExecuteEvents.Execute(down ?? drag, data, ExecuteEvents.pointerEnterHandler);
            RealInputUtility.SimulateMouseClick(true);
            RealInputUtility.SimulateDragging(true);
            if (down != null) ExecuteEvents.Execute(down, data, ExecuteEvents.pointerDownHandler);
            if (drag != null) ExecuteEvents.Execute(drag, data, ExecuteEvents.beginDragHandler);
        }

        private static IEnumerator StartDragMotion(Vector2 start, Vector2 end, float duration, GameObject drag, PointerEventData data)
        {
            float elapsed = 0f;
            float lastRealTime = Time.realtimeSinceStartup;

            while (elapsed < duration)
            {
                yield return null;
                float currentRealTime = Time.realtimeSinceStartup;
                float realDelta = currentRealTime - lastRealTime;
                lastRealTime = currentRealTime;

                float timeScale = Time.timeScale;
                float effectiveDelta = timeScale > 0.01f ? (realDelta * timeScale) : realDelta;
                elapsed += effectiveDelta;

                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                data.position = Vector2.Lerp(start, end, t);
                RealInputUtility.SimulateMouseMove(data.position);
                data.delta = (end - start) * (effectiveDelta / duration);
                if (drag != null) ExecuteEvents.Execute(drag, data, ExecuteEvents.dragHandler);
                if (Tester.Settings.EnableInputHeartbeat) InputSystemShim.UpdateInput();
            }
        }

        private static void EndDrag(GameObject down, GameObject drag, PointerEventData data, Vector2 end)
        {
            data.position = end;
            RealInputUtility.SimulateMouseMove(end);
            RealInputUtility.SimulateMouseClick(false);
            RealInputUtility.SimulateDragging(false);
            if (down != null) ExecuteEvents.Execute(down, data, ExecuteEvents.pointerUpHandler);
            if (drag != null)
            {
                ExecuteEvents.Execute(drag, data, ExecuteEvents.endDragHandler);
                ExecuteEvents.Execute(drag, data, ExecuteEvents.dropHandler);
            }
        }
    }
}
