using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    public static class MouseMove
    {
        public static Task To(Vector2 pos, float duration)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            var token = RealPlayExecutionContext.Token;
            return RealPlayTesterHost.Instance.RunCoroutineTask(MoveRoutine(RealInputUtility.ClampToScreen(pos), duration), token);
        }

        private static IEnumerator MoveRoutine(Vector2 target, float duration)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(es);
            Vector2 start = RealInputUtility.LastSimulatedPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = duration > 0f ? Mathf.SmoothStep(0f, 1f, elapsed / duration) : 1f;
                Vector2 current = Vector2.Lerp(start, target, t);
                data.position = current;
                RealInputUtility.SimulateMouseMove(current);
                TriggerHoverEvents(data);
                if (Tester.Settings.EnableInputHeartbeat) InputSystemShim.UpdateInput();
                yield return null;
            }
            data.position = target;
            RealInputUtility.SimulateMouseMove(target);
        }

        private static void TriggerHoverEvents(PointerEventData data)
        {
            var results = RealInputUtility.Raycasts(data);
            if (results.Count > 0 && results[0].gameObject != null)
            {
                data.pointerCurrentRaycast = results[0];
                ExecuteEvents.Execute(results[0].gameObject, data, ExecuteEvents.pointerMoveHandler);
            }
        }
    }
}
