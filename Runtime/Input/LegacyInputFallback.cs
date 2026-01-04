using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    internal static class LegacyInputFallback
    {
        private static bool warnedOnce;

        public static void WarnLimited()
        {
            if (!warnedOnce)
            {
                warnedOnce = true;
                RealPlayLog.Warn("Legacy input manager cannot simulate true keyboard state; UI events only. Install/use the new Input System for full fidelity.");
            }
        }

        public static Task SimulateKeyPress(KeyCode key, float duration, bool downOnly, bool upOnly, CancellationToken token)
        {
            return RealPlayTesterHost.Instance.RunCoroutineTask(SimulateRoutine(key, duration, downOnly, upOnly), token);
        }

        private static IEnumerator SimulateRoutine(KeyCode key, float duration, bool downOnly, bool upOnly)
        {
            var es = RealInputUtility.EnsureEventSystem();
            var target = es.currentSelectedGameObject ?? es.firstSelectedGameObject;
            if (target == null)
            {
                RealPlayLog.Warn("Legacy key simulation: no selected GameObject to receive events.");
                yield break;
            }

            var baseEvent = new BaseEventData(es);
            ExecuteEvents.Execute<IUpdateSelectedHandler>(target, baseEvent, ExecuteEvents.updateSelectedHandler);

            if (!upOnly)
            {
                if (IsSubmitKey(key)) ExecuteEvents.Execute<ISubmitHandler>(target, baseEvent, ExecuteEvents.submitHandler);
                else if (TryBuildMoveEvent(es, key, out var moveEvent)) ExecuteEvents.Execute<IMoveHandler>(target, moveEvent, ExecuteEvents.moveHandler);
            }

            float elapsed = 0f;
            while (!upOnly && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                ExecuteEvents.Execute<IUpdateSelectedHandler>(target, baseEvent, ExecuteEvents.updateSelectedHandler);
                yield return null;
            }
        }

        private static bool IsSubmitKey(KeyCode key) => key == KeyCode.Return || key == KeyCode.Space || key == KeyCode.KeypadEnter;

        private static bool TryBuildMoveEvent(EventSystem es, KeyCode key, out AxisEventData moveEvent)
        {
            moveEvent = null;
            Vector2 move = key switch { KeyCode.UpArrow => Vector2.up, KeyCode.DownArrow => Vector2.down, KeyCode.LeftArrow => Vector2.left, KeyCode.RightArrow => Vector2.right, _ => Vector2.zero };
            if (move == Vector2.zero) return false;

            moveEvent = new AxisEventData(es) { moveVector = move, moveDir = GetMoveDirection(move) };
            return true;
        }

        private static MoveDirection GetMoveDirection(Vector2 move)
        {
            if (move == Vector2.up) return MoveDirection.Up;
            if (move == Vector2.down) return MoveDirection.Down;
            if (move == Vector2.left) return MoveDirection.Left;
            if (move == Vector2.right) return MoveDirection.Right;
            return MoveDirection.None;
        }
    }
}
