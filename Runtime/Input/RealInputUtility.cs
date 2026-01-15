using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    public static class RealInputUtility
    {
        private static readonly Type InputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        private static readonly Type PanelRaycasterType = Type.GetType("UnityEngine.UIElements.PanelRaycaster, UnityEngine.UIElementsModule");
        private static readonly System.Reflection.MethodInfo PanelRaycastMethod = PanelRaycasterType?.GetMethod("Raycast", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        private static readonly List<RaycastResult> RaycastCache = new List<RaycastResult>(16);
        private static PointerEventData _pooledPointerData;
        private static Vector2? _lastSimulatedPosition;

        public static Vector2 LastSimulatedPosition 
        { 
            get => _lastSimulatedPosition ??= new Vector2(Screen.width / 2f, Screen.height / 2f);
            private set => _lastSimulatedPosition = value;
        }
        public static bool IsSimulatedButtonPressed { get; private set; }
        public static bool IsSimulatedDragging { get; private set; }

        public static EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null) return EventSystem.current;

            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (existing != null) return ActivateEventSystem(existing);

            return CreateDefaultEventSystem();
        }

        private static EventSystem ActivateEventSystem(EventSystem existing)
        {
            if (!existing.gameObject.activeInHierarchy)
            {
                RealPlayLog.Warn("Found inactive EventSystem; activating it for tests.");
                existing.gameObject.SetActive(true);
            }
            if (!existing.enabled) existing.enabled = true;
            EventSystem.current = existing;
            return existing;
        }

        private static EventSystem CreateDefaultEventSystem()
        {
            var go = new GameObject("RealPlayTester_EventSystem");
            var es = go.AddComponent<EventSystem>();
            
            // Prefer InputSystemUIInputModule if the Input System is ready
            if (SimulatedInputGuard.InputSystemReady() && InputSystemUIModuleType != null)
            {
                go.AddComponent(InputSystemUIModuleType);
            }
            else
            {
                // Fallback to legacy only if package is missing or not active
                go.AddComponent<StandaloneInputModule>();
            }
            
            UnityEngine.Object.DontDestroyOnLoad(go);
            es.UpdateModules();
            return es;
        }

        public static PointerEventData GetPooledPointerData(EventSystem eventSystem)
        {
            if (_pooledPointerData == null || EventSystem.current != eventSystem)
                _pooledPointerData = new PointerEventData(eventSystem);
            _pooledPointerData.Reset();
            _pooledPointerData.pointerId = -1;
            return _pooledPointerData;
        }

        public static List<RaycastResult> Raycasts(PointerEventData data)
        {
            RaycastCache.Clear();
            Canvas.ForceUpdateCanvases();
            var es = EventSystem.current ?? EnsureEventSystem();
            es.RaycastAll(data, RaycastCache);
            if (RaycastCache.Count == 0 && (Application.isBatchMode || Tester.Settings.ForceBatchmodeVisibility))
                ManualRaycast(data, RaycastCache);
            PanelRaycast(data, RaycastCache);
            return RaycastCache;
        }

        private static void ManualRaycast(PointerEventData data, List<RaycastResult> results)
        {
            var raycasters = UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            foreach (var gr in raycasters)
            {
                if (gr == null || !gr.enabled || !gr.gameObject.activeInHierarchy) continue;
                
                int preCount = results.Count;
                gr.Raycast(data, results);
                
                // Inject canvas sorting order into results for reliable sorting
                var canvas = gr.GetComponent<Canvas>();
                if (canvas != null)
                {
                    for (int i = preCount; i < results.Count; i++)
                    {
                        var res = results[i];
                        res.sortingOrder = canvas.sortingOrder;
                        results[i] = res;
                    }
                }
            }
            if (results.Count > 1) 
            {
                results.Sort((a, b) => {
                    if (a.sortingOrder != b.sortingOrder) return b.sortingOrder.CompareTo(a.sortingOrder);
                    return b.depth.CompareTo(a.depth);
                });
            }
        }

        public static bool TryRaycastWorld(Camera camera, Vector2 screenPosition, out RaycastHit hit)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, camera.farClipPlane);
        }

        public static Vector2 ClampToScreen(Vector2 pos)
        {
            float w = Screen.width > 0 ? Screen.width : 1024;
            float h = Screen.height > 0 ? Screen.height : 768;
            return new Vector2(Mathf.Clamp(pos.x, 0.1f, w - 0.1f), Mathf.Clamp(pos.y, 0.1f, h - 0.1f));
        }

        private static void PanelRaycast(PointerEventData data, List<RaycastResult> results)
        {
            if (PanelRaycastMethod == null) return;
            var raycasters = UnityEngine.Object.FindObjectsByType(PanelRaycasterType, FindObjectsSortMode.None);
            foreach (var pr in raycasters)
            {
                var prResults = new List<RaycastResult>();
                PanelRaycastMethod.Invoke(pr, new object[] { data, prResults });
                results.AddRange(prResults);
            }
        }

        public static void SimulateMouseMove(Vector2 screenPos)
        {
            var clamped = ClampToScreen(screenPos);
            LastSimulatedPosition = clamped;
            if (InputSystemShim.IsAvailable) InputSystemShim.MouseMove(clamped);
        }
        
        public static void SimulateMouseClick(bool down)
        {
            IsSimulatedButtonPressed = down;
            if (InputSystemShim.IsAvailable) InputSystemShim.MouseButton(0, down);
        }

        public static void SimulateDragging(bool dragging) => IsSimulatedDragging = dragging;

        /// <summary>
        /// Checks if a screen point is occluded by any UI element other than the target.
        /// </summary>
        public static bool IsPointOccludedByUI(Vector2 screenPos, GameObject target)
        {
            var es = EventSystem.current ?? EnsureEventSystem();
            var data = GetPooledPointerData(es);
            data.position = screenPos;

            var results = Raycasts(data);
            if (results.Count == 0) return false;

            // If the top-most result isn't the target (or its child/parent relationship), it's occluded
            var hit = results[0].gameObject;
            if (hit == target) return false;
            
            // Check relationship
            Transform t = target.transform;
            if (hit.transform.IsChildOf(t) || t.IsChildOf(hit.transform)) return false;

            return true;
        }

        public static Vector2 GetScreenCenter(GameObject go, Camera camera = null)
        {
            if (go == null) return LastSimulatedPosition;
            if (go.transform is RectTransform rect) return GetUIScreenCenter(rect, camera);
            return GetWorldScreenCenter(go, camera);
        }

        private static Vector2 GetUIScreenCenter(RectTransform rect, Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = rect.GetComponentInParent<Canvas>();
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var worldCenter = (corners[0] + corners[2]) * 0.5f;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) return ClampToScreen(worldCenter);
            var cam = camera ?? canvas?.worldCamera ?? GetCameraForObject(rect.gameObject);
            return cam != null ? ClampToScreen(RectTransformUtility.WorldToScreenPoint(cam, worldCenter)) : ClampToScreen(worldCenter);
        }

        private static Vector2 GetWorldScreenCenter(GameObject go, Camera camera)
        {
            var cam = camera ?? GetCameraForObject(go);
            if (cam != null)
            {
                var screenPoint = cam.WorldToScreenPoint(go.transform.position);
                if (screenPoint.z >= 0) return ClampToScreen(screenPoint);
            }
            return LastSimulatedPosition;
        }

        public static GameObject FindFallbackInteractionTarget(Vector2 screenPosition)
        {
            var selectables = UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            GameObject best = FindClosest(selectables, screenPosition, 75f);
            if (best != null) return best;
            var scrolls = UnityEngine.Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None);
            return FindClosest(scrolls, screenPosition, 75f);
        }

        private static GameObject FindClosest<T>(T[] objects, Vector2 pos, float maxDist) where T : Component
        {
            GameObject best = null;
            float dist = maxDist;
            foreach (var obj in objects)
            {
                if (obj == null || !obj.gameObject.activeInHierarchy) continue;
                float d = Vector2.Distance(GetScreenCenter(obj.gameObject), pos);
                if (d < dist) { dist = d; best = obj.gameObject; }
            }
            return best;
        }

        private static Camera GetCameraForObject(GameObject go)
        {
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas?.worldCamera != null) return canvas.worldCamera;
            int layer = go.layer;
            foreach (var cam in Camera.allCameras) if (cam.enabled && (cam.cullingMask & (1 << layer)) != 0) return cam;
            return Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
        }
    }
}
