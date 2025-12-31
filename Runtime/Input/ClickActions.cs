using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Await;
using RealPlayTester.Core;
using System.Reflection;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Click helpers for simulating pointer clicks via real EventSystem events.
    /// </summary>
    public static class Click
    {
        public static async Task ButtonWithText(string buttonText)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(buttonText))
            {
                RealPlayLog.Warn("ButtonWithText: empty text provided.");
                return;
            }

            var buttons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn == null || !btn.isActiveAndEnabled)
                {
                    continue;
                }

                string text = GetButtonLabel(btn);
                if (!string.IsNullOrEmpty(text) && text.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Auto-scroll if necessary
                    await Scroll.EnsureVisible(btn.gameObject);
                    await WorldObject(btn.gameObject);
                    return;
                }
            }

            RealPlayLog.Warn($"Button with text '{buttonText}' not found.");
        }

        public static async Task ObjectNamed(string name)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            var obj = SmartFind.Object(name);
            if (obj != null)
            {
                await WorldObject(obj);
            }
            else
            {
                RealPlayLog.Warn($"GameObject '{name}' not found.");
            }
        }

        public static async Task Component<T>() where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            var comp = GameObject.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (comp != null)
            {
                await WorldObject(comp.gameObject);
            }
            else
            {
                RealPlayLog.Warn($"Component of type {typeof(T).Name} not found.");
            }
        }

        public static Task ScreenPercent(float x, float y)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var pos = new Vector2(Mathf.Clamp01(x) * Screen.width, Mathf.Clamp01(y) * Screen.height);
            return PerformClick(pos);
        }

        public static Task ScreenPixels(float x, float y)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var pos = new Vector2(Mathf.Clamp(x, 0f, Screen.width - 1), Mathf.Clamp(y, 0f, Screen.height - 1));
            return PerformClick(pos);
        }

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

        public static async Task WorldObject(GameObject target, Camera camera = null)
        {
            if (!RealPlayEnvironment.IsEnabled || target == null)
            {
                return;
            }

            Camera cam = SelectCamera(camera);
            if (cam == null)
            {
                RealPlayLog.Warn("WorldObject click: no camera available.");
                return;
            }

            Vector3 screenPos = cam.WorldToScreenPoint(target.transform.position);

            // Auto-scroll first
            await Scroll.EnsureVisible(target);
            
            // Re-calculate after scroll
            screenPos = cam.WorldToScreenPoint(target.transform.position);

            // Safety clamp
            if (screenPos.z > 0)
            {
                // Only clamp if it's generally in front of camera
                screenPos = RealInputUtility.ClampToScreen(screenPos);
            }

            if (screenPos.z < 0f || screenPos.x < 0f || screenPos.x > Screen.width || screenPos.y < 0f || screenPos.y > Screen.height)
            {
                RealPlayLog.Warn($"WorldObject '{target.name}' is not visible on screen (Pos: {screenPos}); skipping click.");
                return;
            }

            await PerformClick(screenPos, cam, target);
        }

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

        public static Task RightClick(Vector2 screenPosition)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PerformClick(screenPosition, null, null, PointerEventData.InputButton.Right);
        }

        public static Task MiddleClick(Vector2 screenPosition)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PerformClick(screenPosition, null, null, PointerEventData.InputButton.Middle);
        }

        public static async Task DoubleClick(Vector2 screenPosition, float doubleClickTime = 0.3f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            await PerformClick(screenPosition);
            float delay = Mathf.Min(0.1f, Mathf.Max(0.01f, doubleClickTime));
            await Wait.Seconds(delay);
            await PerformClick(screenPosition);
        }

        public static async Task Hold(Vector2 screenPosition, float duration = 1f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            var token = RealPlayExecutionContext.Token;
            var host = RealPlayTesterHost.Instance;
            await host.RunCoroutineTask(HoldRoutine(screenPosition, duration), token);
        }

        private static Task PerformClick(Vector2 screenPosition, Camera camera = null, GameObject preferredTarget = null, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            var token = RealPlayExecutionContext.Token;
            var host = RealPlayTesterHost.Instance;
            return host.RunCoroutineTask(ClickRoutine(screenPosition, camera, preferredTarget, token, button), token);
        }

        private static IEnumerator ClickRoutine(Vector2 screenPosition, Camera camera, GameObject preferredTarget, CancellationToken token, PointerEventData.InputButton button)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            Camera cam = SelectCamera(camera);
            if (cam != null)
            {
                EnsurePhysicsRaycaster(cam);
            }

            var pointerData = RealInputUtility.GetPooledPointerData(eventSystem);
            pointerData.position = screenPosition;
            pointerData.button = button;
            RealInputUtility.SimulateMouseMove(screenPosition);

            var raycastResults = RealInputUtility.Raycasts(pointerData);
            GameObject target = preferredTarget != null ? preferredTarget : (raycastResults.Count > 0 ? raycastResults[0].gameObject : null);

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

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
            yield return null;
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
        }

        private static IEnumerator HoldRoutine(Vector2 screenPosition, float duration)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            var pointerData = RealInputUtility.GetPooledPointerData(eventSystem);
            pointerData.position = screenPosition;
            pointerData.button = PointerEventData.InputButton.Left;
            RealInputUtility.SimulateMouseMove(screenPosition); // Track simulated mouse position

            var raycastResults = RealInputUtility.Raycasts(pointerData);
            GameObject target = raycastResults.Count > 0 ? raycastResults[0].gameObject : null;
            if (target != null)
            {
                pointerData.pointerPressRaycast = raycastResults[0];
                pointerData.pointerCurrentRaycast = raycastResults[0];
                pointerData.pressPosition = screenPosition;
                pointerData.pointerPress = target;
                pointerData.rawPointerPress = target;

                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
                RealInputUtility.SimulateMouseClick(true); // Simulate button down
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                RealInputUtility.SimulateMouseClick(false); // Simulate button up
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
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

        private static string GetButtonLabel(Button btn)
        {
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (uiText != null)
            {
                return uiText.text;
            }

            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = btn.GetComponentInChildren(tmpType, true);
                if (tmp != null)
                {
                    var prop = tmpType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                    if (prop != null)
                    {
                        return prop.GetValue(tmp) as string;
                    }
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Drag helpers for simulating drag operations via real EventSystem events.
    /// </summary>
    public static class Drag
    {
        public static Task FromTo(Vector2 startScreenPos, Vector2 endScreenPos, float durationSeconds)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var token = RealPlayExecutionContext.Token;
            // Clamp start/end to be safe
            startScreenPos = RealInputUtility.ClampToScreen(startScreenPos);
            endScreenPos = RealInputUtility.ClampToScreen(endScreenPos);
            
            return RealPlayTesterHost.Instance.RunCoroutineTask(DragRoutine(startScreenPos, endScreenPos, durationSeconds), token);
        }

        private static IEnumerator DragRoutine(Vector2 start, Vector2 end, float duration)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            var pointer = RealInputUtility.GetPooledPointerData(eventSystem);
            
            // If we are not at start, move there first (teleport/snap for drag start is usually okay, but continuity is better)
            pointer.position = start;
            RealInputUtility.SimulateMouseMove(start);
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
            RealInputUtility.SimulateMouseMove(start);
            RealInputUtility.SimulateMouseClick(true);
            RealInputUtility.SimulateDragging(true);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.beginDragHandler);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                pointer.position = Vector2.Lerp(start, end, t);
                RealInputUtility.SimulateMouseMove(pointer.position);
                pointer.delta = (end - start) * (delta / duration);
                ExecuteEvents.Execute(target, pointer, ExecuteEvents.dragHandler);
                yield return null;
            }

            pointer.position = end;
            RealInputUtility.SimulateMouseMove(end);
            RealInputUtility.SimulateMouseClick(false);
            RealInputUtility.SimulateDragging(false);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.dropHandler);
        }
    }

    /// <summary>
    /// Mouse movement helpers for realistic simulation.
    /// </summary>
    public static class MouseMove
    {
        public static Task To(Vector2 screenPosition, float durationSeconds)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            var token = RealPlayExecutionContext.Token;
            screenPosition = RealInputUtility.ClampToScreen(screenPosition);
            
            return RealPlayTesterHost.Instance.RunCoroutineTask(MoveRoutine(screenPosition, durationSeconds), token);
        }

        private static IEnumerator MoveRoutine(Vector2 target, float duration)
        {
            var eventSystem = RealInputUtility.EnsureEventSystem();
            var pointer = RealInputUtility.GetPooledPointerData(eventSystem);
            Vector2 start = RealInputUtility.LastSimulatedPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                elapsed += delta;
                float t = duration > 0f ? Mathf.SmoothStep(0f, 1f, elapsed / duration) : 1f;
                
                Vector2 currentPos = Vector2.Lerp(start, target, t);
                pointer.position = currentPos;
                RealInputUtility.SimulateMouseMove(currentPos);
                
                // Trigger hover events during movement
                var raycastResults = RealInputUtility.Raycasts(pointer);
                GameObject hit = (raycastResults != null && raycastResults.Count > 0) ? raycastResults[0].gameObject : null;
                
                if (hit != null)
                {
                    pointer.pointerCurrentRaycast = raycastResults[0];
                    ExecuteEvents.Execute(hit, pointer, ExecuteEvents.pointerMoveHandler);
                }

                yield return null;
            }

            pointer.position = target;
            RealInputUtility.SimulateMouseMove(target);
        }
    }
}
