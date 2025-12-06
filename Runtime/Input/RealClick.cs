using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Click helpers for simulating pointer clicks via real EventSystem events.
    /// </summary>
    public static class Click
    {
        /// <summary>
        /// Click at a screen position specified as a percentage (0-1 range).
        /// </summary>
        public static Task ScreenPercent(float x, float y)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var pos = new Vector2(Mathf.Clamp01(x) * Screen.width, Mathf.Clamp01(y) * Screen.height);
            return PerformClick(pos);
        }

        /// <summary>
        /// Click at an exact screen pixel position.
        /// </summary>
        public static Task ScreenPixels(float x, float y)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var pos = new Vector2(Mathf.Clamp(x, 0f, Screen.width), Mathf.Clamp(y, 0f, Screen.height));
            return PerformClick(pos);
        }

        /// <summary>
        /// Click at a world position, converted to screen coordinates using the provided camera.
        /// </summary>
        public static Task WorldPosition(Vector3 worldPosition, Camera camera = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            Camera cam = SelectCamera(camera);
            if (cam == null)
            {
                RealPlayLog.Warn("No camera available for WorldPosition click.");
                return Task.CompletedTask;
            }

            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            return PerformClick(screen, cam);
        }

        /// <summary>
        /// Click on a specific GameObject in world space.
        /// </summary>
        public static Task WorldObject(GameObject target, Camera camera = null)
        {
            if (!RealPlayEnvironment.IsEnabled || target == null)
            {
                return Task.CompletedTask;
            }

            Camera cam = SelectCamera(camera);
            Vector3 pos = cam != null ? cam.WorldToScreenPoint(target.transform.position) : Vector3.zero;
            return PerformClick(pos, cam, target);
        }

        /// <summary>
        /// Raycast from camera at the given screen position and return the hit result.
        /// </summary>
        public static RaycastResult RaycastFromCamera(Camera camera, Vector2 screenPosition)
        {
            Camera cam = SelectCamera(camera);
            if (cam == null)
            {
                return default;
            }

            EnsurePhysicsRaycaster(cam);
            var eventSystem = RealInputUtility.EnsureEventSystem();
            var data = RealInputUtility.GetPooledPointerData(eventSystem);
            data.position = screenPosition;
            var results = RealInputUtility.Raycasts(data);
            return results.Count > 0 ? results[0] : default;
        }

        private static Task PerformClick(Vector2 screenPosition, Camera camera = null, GameObject preferredTarget = null)
        {
            var token = RealPlayExecutionContext.Token;
            var host = RealPlayTesterHost.Instance;
            return host.RunCoroutineTask(ClickRoutine(screenPosition, camera, preferredTarget, token), token);
        }

        private static IEnumerator ClickRoutine(Vector2 screenPosition, Camera camera, GameObject preferredTarget, CancellationToken token)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            Camera cam = SelectCamera(camera);
            if (cam != null)
            {
                EnsurePhysicsRaycaster(cam);
            }

            var pointerData = RealInputUtility.GetPooledPointerData(eventSystem);
            pointerData.position = screenPosition;
            pointerData.button = PointerEventData.InputButton.Left;

            var raycastResults = RealInputUtility.Raycasts(pointerData);
            GameObject target = preferredTarget != null ? preferredTarget : (raycastResults.Count > 0 ? raycastResults[0].gameObject : null);

            // Fallback: raycast into 3D world for physics colliders
            if (target == null && cam != null)
            {
                if (RealInputUtility.TryRaycastWorld(cam, screenPosition, out var worldHit))
                {
                    target = worldHit.collider.gameObject;
                }
            }

            if (target == null)
            {
                RealPlayLog.Warn("Click: no target hit at " + screenPosition);
                yield break;
            }

            pointerData.pointerPressRaycast = raycastResults.Count > 0 ? raycastResults[0] : new RaycastResult { gameObject = target };
            pointerData.pointerCurrentRaycast = pointerData.pointerPressRaycast;
            pointerData.pressPosition = screenPosition;
            pointerData.pointerPress = target;
            pointerData.rawPointerPress = target;

            // Execute full pointer event sequence
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
            yield return null;
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            
            // Also try to invoke onClick for UI Buttons directly if click handler had no receiver
            var button = target.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private static Camera SelectCamera(Camera camera)
        {
            if (camera != null)
            {
                return camera;
            }

            if (Camera.main != null)
            {
                return Camera.main;
            }

            if (Camera.allCamerasCount > 0)
            {
                return Camera.allCameras[0];
            }

            return null;
        }

        private static void EnsurePhysicsRaycaster(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            if (cam.GetComponent<PhysicsRaycaster>() == null)
            {
                cam.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }
    }

    /// <summary>
    /// Drag helpers for simulating drag operations via real EventSystem events.
    /// </summary>
    public static class Drag
    {
        /// <summary>
        /// Drag from start to end screen position over the specified duration.
        /// </summary>
        public static Task FromTo(Vector2 startScreenPos, Vector2 endScreenPos, float durationSeconds)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var token = RealPlayExecutionContext.Token;
            return RealPlayTesterHost.Instance.RunCoroutineTask(DragRoutine(startScreenPos, endScreenPos, durationSeconds), token);
        }

        private static IEnumerator DragRoutine(Vector2 start, Vector2 end, float duration)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            var pointer = RealInputUtility.GetPooledPointerData(eventSystem);
            pointer.position = start;
            pointer.button = PointerEventData.InputButton.Left;

            var raycastResults = RealInputUtility.Raycasts(pointer);
            var target = raycastResults.Count > 0 ? raycastResults[0].gameObject : null;

            if (target == null)
            {
                RealPlayLog.Warn("Drag: no target at start position " + start);
                yield break;
            }

            pointer.pointerPressRaycast = raycastResults[0];
            pointer.pointerCurrentRaycast = raycastResults[0];
            pointer.pressPosition = start;
            pointer.pointerDrag = target;

            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.beginDragHandler);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Use scaled time, but don't hang if timeScale is 0
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                pointer.position = Vector2.Lerp(start, end, t);
                pointer.delta = (end - start) * (delta / duration);
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.dragHandler);
                yield return null;
            }

            pointer.position = end;
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.dropHandler);
        }
    }

    /// <summary>
    /// Keyboard press helpers. New Input System is fully supported; legacy Input has limited simulation.
    /// </summary>
    public static class Press
    {
        /// <summary>
        /// Press and hold a key for the specified duration, then release.
        /// </summary>
        public static Task Key(KeyCode key, float durationSeconds)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, durationSeconds);
        }

        /// <summary>
        /// Press a key down (without releasing).
        /// </summary>
        public static Task KeyDown(KeyCode key)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, 0f, true, false);
        }

        /// <summary>
        /// Release a previously pressed key.
        /// </summary>
        public static Task KeyUp(KeyCode key)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, 0f, false, true);
        }

        private static async Task PressInternal(KeyCode key, float durationSeconds, bool downOnly = false, bool upOnly = false)
        {
            if (!upOnly)
            {
                if (InputSystemShim.IsAvailable)
                {
                    InputSystemShim.KeyDown(key);
                }
                else
                {
                    // Legacy fallback: send key event via IMGUI event queue (limited)
                    LegacyInputFallback.SimulateKeyDown(key);
                }
            }

            if (!upOnly && durationSeconds > 0f)
            {
                await Wait.Seconds(durationSeconds);
            }

            if (!downOnly)
            {
                if (InputSystemShim.IsAvailable)
                {
                    InputSystemShim.KeyUp(key);
                }
                else
                {
                    LegacyInputFallback.SimulateKeyUp(key);
                }
            }
        }
    }

    /// <summary>
    /// Internal utilities for pointer event handling.
    /// </summary>
    internal static class RealInputUtility
    {
        private static readonly Type InputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        private static readonly List<RaycastResult> RaycastCache = new List<RaycastResult>(16);
        private static PointerEventData _pooledPointerData;

        public static EventSystem EnsureEventSystem()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                var go = new GameObject("RealPlayTester_EventSystem");
                es = go.AddComponent<EventSystem>();
                if (InputSystemUIModuleType != null)
                {
                    go.AddComponent(InputSystemUIModuleType);
                }
                else
                {
                    go.AddComponent<StandaloneInputModule>();
                }
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            return es;
        }

        /// <summary>
        /// Get a pooled PointerEventData to reduce allocations.
        /// </summary>
        public static PointerEventData GetPooledPointerData(EventSystem eventSystem)
        {
            // Create new if null or if EventSystem changed (can't access eventSystem property directly)
            if (_pooledPointerData == null || EventSystem.current != eventSystem)
            {
                _pooledPointerData = new PointerEventData(eventSystem);
            }
            
            // Reset state
            _pooledPointerData.Reset();
            _pooledPointerData.pointerId = -1;
            return _pooledPointerData;
        }

        public static List<RaycastResult> Raycasts(PointerEventData data)
        {
            RaycastCache.Clear();
            var es = EventSystem.current ?? EnsureEventSystem();
            es.RaycastAll(data, RaycastCache);
            return RaycastCache;
        }

        public static bool TryRaycastWorld(Camera camera, Vector2 screenPosition, out RaycastHit hit)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, camera.farClipPlane);
        }
    }

    /// <summary>
    /// Limited fallback for legacy Input Manager (no true injection possible).
    /// </summary>
    internal static class LegacyInputFallback
    {
        private static readonly HashSet<KeyCode> SimulatedDownKeys = new HashSet<KeyCode>();

        public static void SimulateKeyDown(KeyCode key)
        {
            SimulatedDownKeys.Add(key);
            RealPlayLog.Warn($"Legacy input simulation: KeyDown({key}) - limited fidelity. Consider using new Input System.");
        }

        public static void SimulateKeyUp(KeyCode key)
        {
            SimulatedDownKeys.Remove(key);
        }

        public static bool IsKeySimulated(KeyCode key)
        {
            return SimulatedDownKeys.Contains(key);
        }
    }

    /// <summary>
    /// Reflection-based shim for Unity's new Input System.
    /// Uses proper KeyCode to Input System Key mapping.
    /// </summary>
    internal static class InputSystemShim
    {
        // Cached types and methods
        private static readonly Type KeyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        private static readonly Type KeyboardStateType = Type.GetType("UnityEngine.InputSystem.LowLevel.KeyboardState, Unity.InputSystem");
        private static readonly Type KeyEnumType = Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
        private static readonly Type InputSystemType = Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
        private static readonly Type KeyControlType = Type.GetType("UnityEngine.InputSystem.Controls.KeyControl, Unity.InputSystem");

        private static readonly MethodInfo QueueStateEventGeneric = GetQueueStateEventGeneric();
        private static readonly MethodInfo KeyboardStateSetMethod = KeyboardStateType != null && KeyEnumType != null
            ? KeyboardStateType.GetMethod("Set", new[] { KeyEnumType, typeof(bool) })
            : null;

        private static readonly PropertyInfo KeyboardCurrentProperty = KeyboardType != null
            ? KeyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static)
            : null;

        private static readonly PropertyInfo KeyboardItemProperty = KeyboardType != null && KeyEnumType != null
            ? KeyboardType.GetProperty("Item", new[] { KeyEnumType })
            : null;

        private static readonly PropertyInfo WasPressedThisFrameProperty = KeyControlType != null
            ? KeyControlType.GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance)
            : null;

        // KeyCode to Input System Key mapping - these DO NOT map 1:1!
        private static readonly Dictionary<KeyCode, int> KeyCodeToInputSystemKey = new Dictionary<KeyCode, int>
        {
            // Letters (Key.A = 15, sequential through Z = 40)
            { KeyCode.A, 15 }, { KeyCode.B, 16 }, { KeyCode.C, 17 }, { KeyCode.D, 18 },
            { KeyCode.E, 19 }, { KeyCode.F, 20 }, { KeyCode.G, 21 }, { KeyCode.H, 22 },
            { KeyCode.I, 23 }, { KeyCode.J, 24 }, { KeyCode.K, 25 }, { KeyCode.L, 26 },
            { KeyCode.M, 27 }, { KeyCode.N, 28 }, { KeyCode.O, 29 }, { KeyCode.P, 30 },
            { KeyCode.Q, 31 }, { KeyCode.R, 32 }, { KeyCode.S, 33 }, { KeyCode.T, 34 },
            { KeyCode.U, 35 }, { KeyCode.V, 36 }, { KeyCode.W, 37 }, { KeyCode.X, 38 },
            { KeyCode.Y, 39 }, { KeyCode.Z, 40 },
            
            // Numbers (Key.Digit1 = 41, sequential)
            { KeyCode.Alpha0, 50 }, { KeyCode.Alpha1, 41 }, { KeyCode.Alpha2, 42 },
            { KeyCode.Alpha3, 43 }, { KeyCode.Alpha4, 44 }, { KeyCode.Alpha5, 45 },
            { KeyCode.Alpha6, 46 }, { KeyCode.Alpha7, 47 }, { KeyCode.Alpha8, 48 },
            { KeyCode.Alpha9, 49 },
            
            // Function keys (Key.F1 = 92)
            { KeyCode.F1, 92 }, { KeyCode.F2, 93 }, { KeyCode.F3, 94 }, { KeyCode.F4, 95 },
            { KeyCode.F5, 96 }, { KeyCode.F6, 97 }, { KeyCode.F7, 98 }, { KeyCode.F8, 99 },
            { KeyCode.F9, 100 }, { KeyCode.F10, 101 }, { KeyCode.F11, 102 }, { KeyCode.F12, 103 },
            
            // Special keys
            { KeyCode.Space, 1 },
            { KeyCode.Return, 2 }, { KeyCode.KeypadEnter, 2 },
            { KeyCode.Tab, 3 },
            { KeyCode.Backspace, 5 },
            { KeyCode.Escape, 6 },
            { KeyCode.LeftShift, 7 }, { KeyCode.RightShift, 8 },
            { KeyCode.LeftAlt, 9 }, { KeyCode.RightAlt, 10 },
            { KeyCode.LeftControl, 11 }, { KeyCode.RightControl, 12 },
            { KeyCode.LeftCommand, 13 }, { KeyCode.RightCommand, 14 },
            
            // Arrow keys
            { KeyCode.UpArrow, 63 }, { KeyCode.DownArrow, 64 },
            { KeyCode.LeftArrow, 65 }, { KeyCode.RightArrow, 66 },
            
            // Other common keys
            { KeyCode.Delete, 76 },
            { KeyCode.Home, 78 }, { KeyCode.End, 79 },
            { KeyCode.PageUp, 80 }, { KeyCode.PageDown, 81 },
            { KeyCode.Insert, 77 },
            { KeyCode.CapsLock, 4 },
        };

        public static bool IsAvailable
        {
            get { return KeyboardType != null && KeyboardStateType != null && KeyEnumType != null && InputSystemType != null && KeyboardCurrentProperty != null && QueueStateEventGeneric != null; }
        }

        public static void KeyDown(KeyCode key)
        {
            QueueKeyState(key, true);
        }

        public static void KeyUp(KeyCode key)
        {
            QueueKeyState(key, false);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            if (!IsAvailable)
            {
                return false;
            }

            object keyboard = KeyboardCurrentProperty.GetValue(null);
            if (keyboard == null || KeyboardItemProperty == null || WasPressedThisFrameProperty == null)
            {
                return false;
            }

            if (!TryMapKeyCode(key, out int inputSystemKeyValue))
            {
                return false;
            }

            object keyEnum = Enum.ToObject(KeyEnumType, inputSystemKeyValue);
            object keyControl = KeyboardItemProperty.GetValue(keyboard, new[] { keyEnum });
            if (keyControl == null)
            {
                return false;
            }

            object wasPressed = WasPressedThisFrameProperty.GetValue(keyControl);
            return wasPressed is bool pressed && pressed;
        }

        private static bool TryMapKeyCode(KeyCode key, out int inputSystemKeyValue)
        {
            return KeyCodeToInputSystemKey.TryGetValue(key, out inputSystemKeyValue);
        }

        private static void QueueKeyState(KeyCode key, bool pressed)
        {
            if (!IsAvailable)
            {
                return;
            }

            if (!TryMapKeyCode(key, out int inputSystemKeyValue))
            {
                RealPlayLog.Warn($"KeyCode.{key} has no mapping to Input System Key enum.");
                return;
            }

            object keyboard = KeyboardCurrentProperty.GetValue(null);
            if (keyboard == null)
            {
                return;
            }

            object state = Activator.CreateInstance(KeyboardStateType);
            object keyValue = Enum.ToObject(KeyEnumType, inputSystemKeyValue);
            KeyboardStateSetMethod.Invoke(state, new[] { keyValue, pressed });
            QueueStateEventGeneric.Invoke(null, new[] { keyboard, state, -1d });
        }

        private static MethodInfo GetQueueStateEventGeneric()
        {
            if (InputSystemType == null || KeyboardStateType == null)
            {
                return null;
            }

            foreach (var method in InputSystemType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (!method.IsGenericMethodDefinition)
                {
                    continue;
                }

                if (method.Name != "QueueStateEvent")
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length >= 2)
                {
                    return method.MakeGenericMethod(KeyboardStateType);
                }
            }

            return null;
        }
    }
}
