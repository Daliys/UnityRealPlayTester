using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Internal utilities for pointer event handling.
    /// </summary>
    internal static class RealInputUtility
    {
        private static readonly Type InputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        private static readonly Type PanelRaycasterType = Type.GetType("UnityEngine.UIElements.PanelRaycaster, UnityEngine.UIElementsModule");
        private static readonly System.Reflection.MethodInfo PanelRaycastMethod = PanelRaycasterType != null
            ? PanelRaycasterType.GetMethod("Raycast", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            : null;

        private static readonly List<RaycastResult> RaycastCache = new List<RaycastResult>(16);
        private static PointerEventData _pooledPointerData;

        public static EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return EventSystem.current;
            }

            // aggressive search including inactive
            var existing = UnityEngine.Object.FindObjectOfType<EventSystem>(true);
            if (existing != null)
            {
                if (!existing.gameObject.activeInHierarchy)
                {
                    RealPlayLog.Warn("Found inactive EventSystem; activating it for tests.");
                    existing.gameObject.SetActive(true);
                }
                if (!existing.enabled)
                {
                    existing.enabled = true;
                }
                EventSystem.current = existing;
                return existing;
            }

            // Create new if absolutely none exist
            var go = new GameObject("RealPlayTester_EventSystem");
            var es = go.AddComponent<EventSystem>();
            if (InputSystemUIModuleType != null)
            {
                go.AddComponent(InputSystemUIModuleType);
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
            UnityEngine.Object.DontDestroyOnLoad(go);
            return es;
        }

        public static PointerEventData GetPooledPointerData(EventSystem eventSystem)
        {
            if (_pooledPointerData == null || EventSystem.current != eventSystem)
            {
                _pooledPointerData = new PointerEventData(eventSystem);
            }

            _pooledPointerData.Reset();
            _pooledPointerData.pointerId = -1;
            return _pooledPointerData;
        }

        public static List<RaycastResult> Raycasts(PointerEventData data)
        {
            RaycastCache.Clear();
            var es = EventSystem.current ?? EnsureEventSystem();
            es.RaycastAll(data, RaycastCache);
            PanelRaycast(data, RaycastCache);
            return RaycastCache;
        }

        public static bool TryRaycastWorld(Camera camera, Vector2 screenPosition, out RaycastHit hit)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, camera.farClipPlane);
        }

        public static Vector2 ClampToScreen(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, 1f, Screen.width - 2f),
                Mathf.Clamp(position.y, 1f, Screen.height - 2f)
            );
        }

        private static void PanelRaycast(PointerEventData data, List<RaycastResult> results)
        {
            if (PanelRaycasterType == null || PanelRaycastMethod == null)
            {
                return;
            }

            var panelRaycasters = UnityEngine.Object.FindObjectsOfType(PanelRaycasterType);
            foreach (var pr in panelRaycasters)
            {
                var prResults = new List<RaycastResult>();
                PanelRaycastMethod.Invoke(pr, new object[] { data, prResults });
                results.AddRange(prResults);
            }
        }
    }

    /// <summary>
    /// Limited fallback for legacy Input Manager (no true hardware injection; uses EventSystem events).
    /// </summary>
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

        public static Task SimulateKeyPress(KeyCode key, float durationSeconds, bool downOnly, bool upOnly, CancellationToken token)
        {
            var host = RealPlayTesterHost.Instance;
            return host.RunCoroutineTask(SimulateRoutine(key, durationSeconds, downOnly, upOnly), token);
        }

        private static IEnumerator SimulateRoutine(KeyCode key, float durationSeconds, bool downOnly, bool upOnly)
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
                if (IsSubmitKey(key))
                {
                    ExecuteEvents.Execute<ISubmitHandler>(target, baseEvent, ExecuteEvents.submitHandler);
                }
                else if (TryBuildMoveEvent(es, key, out var moveEvent))
                {
                    ExecuteEvents.Execute<IMoveHandler>(target, moveEvent, ExecuteEvents.moveHandler);
                }
            }

            float elapsed = 0f;
            while (!upOnly && elapsed < durationSeconds)
            {
                elapsed += Time.deltaTime;
                ExecuteEvents.Execute<IUpdateSelectedHandler>(target, baseEvent, ExecuteEvents.updateSelectedHandler);
                yield return null;
            }
        }

        private static bool IsSubmitKey(KeyCode key)
        {
            return key == KeyCode.Return || key == KeyCode.Space || key == KeyCode.KeypadEnter;
        }

        private static bool TryBuildMoveEvent(EventSystem es, KeyCode key, out AxisEventData moveEvent)
        {
            moveEvent = null;
            Vector2 move = Vector2.zero;
            switch (key)
            {
                case KeyCode.UpArrow: move = Vector2.up; break;
                case KeyCode.DownArrow: move = Vector2.down; break;
                case KeyCode.LeftArrow: move = Vector2.left; break;
                case KeyCode.RightArrow: move = Vector2.right; break;
            }

            if (move == Vector2.zero)
            {
                return false;
            }

            moveEvent = new AxisEventData(es)
            {
                moveVector = move,
                moveDir = GetMoveDirection(move)
            };
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
