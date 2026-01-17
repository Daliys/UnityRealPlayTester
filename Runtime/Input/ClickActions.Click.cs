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
    public static class Click
    {
        public static async Task ButtonWithText(string buttonText)
        {
            if (!RealPlayEnvironment.IsEnabled || string.IsNullOrEmpty(buttonText)) return;

            var buttons = GameObject.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn == null || !btn.isActiveAndEnabled) continue;
                string text = GetButtonLabel(btn);
                if (!string.IsNullOrEmpty(text) && text.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    RealPlayLog.Info($"[Click] ButtonWithText('{buttonText}') found match: '{btn.name}' with text '{text}'");
                    await Scroll.EnsureVisible(btn.gameObject);
                    await WorldObject(btn.gameObject);
                    return;
                }
            }
            RealPlayLog.Warn($"Button with text '{buttonText}' not found.");
        }

        public static async Task ObjectNamed(string name)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            var obj = SmartFind.Object(name);
            if (obj != null) await WorldObject(obj);
            else RealPlayLog.Warn($"GameObject '{name}' not found.");
        }

        public static async Task Component<T>() where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            var comp = GameObject.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (comp != null) await WorldObject(comp.gameObject);
            else RealPlayLog.Warn($"Component of type {typeof(T).Name} not found.");
        }

        public struct ScreenPosition { public float x; public float y; }
        public static Task ScreenPercent(float x, float y) => PerformClick(new Vector2(Mathf.Clamp01(x) * Screen.width, Mathf.Clamp01(y) * Screen.height));
        public static Task ScreenPixels(ScreenPosition pos) => InternalScreenPixels(pos);

        private static Task InternalScreenPixels(ScreenPosition p)
        {
            Vector2 v = new Vector2(Mathf.Clamp(p.x, 0f, Screen.width - 1), Mathf.Clamp(p.y, 0f, Screen.height - 1));
            return PerformClick(v);
        }

        public static Task WorldPosition(Vector3 worldPosition, Camera camera = null)
        {
            Camera cam = SelectCamera(camera);
            if (cam == null) return Task.CompletedTask;
            return PerformClick(cam.WorldToScreenPoint(worldPosition), cam);
        }

        public static async Task WorldObject(GameObject target, Camera camera = null)
        {
            if (!RealPlayEnvironment.IsEnabled || target == null) return;
            Camera cam = SelectCamera(camera);
            await Scroll.EnsureVisible(target);
            Vector2 screenPos = RealInputUtility.GetScreenCenter(target, cam);
            await PerformClick(screenPos, cam, target);
        }

        private static async Task PerformClick(Vector2 screenPosition, Camera camera = null, GameObject preferredTarget = null, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            var es = RealInputUtility.EnsureEventSystem();
            Camera cam = SelectCamera(camera);
            if (cam != null) EnsurePhysicsRaycaster(cam);

            var data = RealInputUtility.GetPooledPointerData(es);
            data.position = screenPosition;
            data.button = button;

#if UNITY_EDITOR
            bool wasLocked = false;
            if (RealPlaySettings.LockAssembliesDuringInteraction)
            {
                UnityEditor.EditorApplication.LockReloadAssemblies();
                wasLocked = true;
            }
            try {
#endif
            await InternalPerformClick(screenPosition, preferredTarget, es, cam, data);
#if UNITY_EDITOR
            } finally {
                if (wasLocked) UnityEditor.EditorApplication.UnlockReloadAssemblies();
            }
#endif
        }

        private static async Task InternalPerformClick(Vector2 screenPosition, GameObject preferredTarget, EventSystem es, Camera cam, PointerEventData data)
        {
            RealInputUtility.SimulateMouseMove(screenPosition);
            var results = RealInputUtility.Raycasts(data);
            GameObject target = FindClickTarget(screenPosition, preferredTarget, results, es, cam);

            if (target == null) { LogClickFailure(screenPosition, results); return; }

            ExecuteClickEvents(target, data, screenPosition);
            
            if (Time.timeScale < 0.01f)
            {
                ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
                return;
            }

            await Wait.Frames(1);
            
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        }

        private static GameObject FindClickTarget(Vector2 pos, GameObject preferred, System.Collections.Generic.List<RaycastResult> results, EventSystem es, Camera cam)
        {
            if (preferred != null)
            {
                var blocker = GetFirstVisibleBlocker(preferred, results);
                if (blocker != null && blocker != preferred)
                {
                    if (IsActuallyBlocking(preferred, blocker))
                    {
                        RealPlayLog.Warn($"[BLOCKER] '{preferred.name}' obscured by '{blocker.name}'. Redirecting click to blocker.");
                        return blocker;
                    }
                }
                return preferred;
            }

            foreach (var res in results)
            {
                if (res.gameObject != null && (ExecuteEvents.GetEventHandler<IPointerDownHandler>(res.gameObject) != null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(res.gameObject) != null))
                    return res.gameObject;
            }

            if (Application.isBatchMode || Tester.Settings.ForceBatchmodeVisibility)
            {
                var fb = RealInputUtility.FindFallbackInteractionTarget(pos);
                if (fb != null) return fb;
            }

            if (results.Count > 0) return results[0].gameObject;
            if (cam != null && RealInputUtility.TryRaycastWorld(cam, pos, out var hit)) return hit.collider.gameObject;

            return null;
        }

        private static GameObject GetFirstVisibleBlocker(GameObject preferred, System.Collections.Generic.List<RaycastResult> results)
        {
            foreach (var res in results)
            {
                if (res.gameObject == null) continue;
                if (res.gameObject == preferred) return preferred;
                if (RealInputUtility.IsActuallyVisible(res.gameObject)) return res.gameObject;
            }
            return null;
        }

        private static bool IsActuallyBlocking(GameObject preferred, GameObject blocker)
        {
            if (blocker.transform.IsChildOf(preferred.transform) || preferred.transform.IsChildOf(blocker.transform)) return false;
            
            bool isUi = preferred.GetComponent<RectTransform>() != null;
            bool blockerIsUi = blocker.GetComponent<RectTransform>() != null;
            
            if (isUi && !blockerIsUi) return false; // UI takes priority over world objects
            
            return true;
        }

        private static void ExecuteClickEvents(GameObject target, PointerEventData data, Vector2 pos)
        {
            data.pointerPress = target;
            data.rawPointerPress = target;
            data.pressPosition = pos;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
        }

        private static void LogClickFailure(Vector2 pos, System.Collections.Generic.List<RaycastResult> results)
        {
            string blockers = results.Count > 0 ? string.Join(", ", results.GetRange(0, Math.Min(3, results.Count)).ConvertAll(r => r.gameObject.name)) : "None";
            RealPlayLog.Warn($"[BLOCKER] Click failed at {pos}. Top hits: {blockers}");
        }

        private static string GetButtonLabel(Button btn)
        {
            var uiText = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (uiText != null) return uiText.text;

            var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = btn.GetComponentInChildren(tmpType, true);
                if (tmp != null) return tmpType.GetProperty("text")?.GetValue(tmp) as string;
            }
            return string.Empty;
        }

        private static Camera SelectCamera(Camera camera) => camera ?? Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
        private static void EnsurePhysicsRaycaster(Camera cam) 
        { 
            if (cam == null) return;
            if (cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>(); 
            if (cam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null) cam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        }
    }
}
