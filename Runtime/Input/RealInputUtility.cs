using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    public static partial class RealInputUtility
    {
        private static readonly Type InputSystemUIModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        private static PointerEventData _pooledPointerData;
        private static Vector2? _lastSimulatedPosition;
        private static readonly Dictionary<int, Vector2> PointerPositions = new Dictionary<int, Vector2>();

        public static Vector2 LastSimulatedPosition 
        { 
            get => _lastSimulatedPosition ??= new Vector2(Screen.width / 2f, Screen.height / 2f);
            private set 
            {
                _lastSimulatedPosition = value;
                UpdatePointerPosition(-1, value); // Default mouse ID
            }
        }

        public static void UpdatePointerPosition(int pointerId, Vector2 position) => PointerPositions[pointerId] = position;
        public static void RemovePointer(int pointerId) => PointerPositions.Remove(pointerId);
        public static IReadOnlyDictionary<int, Vector2> GetActivePointers() => PointerPositions;
        
        public static bool IsSimulatedButtonPressed { get; private set; }
        public static bool IsSimulatedDragging { get; private set; }

        private static EventSystem _cachedEventSystem;

        public static EventSystem EnsureEventSystem()
        {
            if (_cachedEventSystem != null && _cachedEventSystem.isActiveAndEnabled) return _cachedEventSystem;
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled) { _cachedEventSystem = EventSystem.current; return _cachedEventSystem; }

            var all = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length > 0)
            {
                foreach (var es in all) if (es.isActiveAndEnabled) { _cachedEventSystem = es; EventSystem.current = es; return es; }
                _cachedEventSystem = ActivateEventSystem(all[0]);
                return _cachedEventSystem;
            }

            if (!RealPlaySettings.AutoCreateEventSystem)
            {
                // Only log if we REALLY can't find one and can't create one
                return null;
            }

            _cachedEventSystem = CreateDefaultEventSystem();
            return _cachedEventSystem;
        }

        private static EventSystem ActivateEventSystem(EventSystem existing)
        {
            if (!existing.gameObject.activeInHierarchy) existing.gameObject.SetActive(true);
            if (!existing.enabled) existing.enabled = true;
            EventSystem.current = existing;
            return existing;
        }

        private static EventSystem CreateDefaultEventSystem()
        {
            var go = new GameObject("RealPlayTester_EventSystem");
            var es = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            if (InputSystemUIModuleType != null) go.AddComponent(InputSystemUIModuleType);
            else go.AddComponent<StandaloneInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.Object.DontDestroyOnLoad(go);
            es.UpdateModules();
            return es;
        }

        public static PointerEventData GetPooledPointerData(EventSystem eventSystem)
        {
            if (_pooledPointerData == null || _pooledPointerData.button != PointerEventData.InputButton.Left)
            {
                if (_pooledPointerData == null || EventSystem.current != eventSystem) _pooledPointerData = new PointerEventData(eventSystem);
                else _pooledPointerData.Reset();
            }
            _pooledPointerData.pointerId = -100;
            return _pooledPointerData;
        }

        public static Vector2 ClampToScreen(Vector2 pos)
        {
            float w = Screen.width > 0 ? Screen.width : 1920;
            float h = Screen.height > 0 ? Screen.height : 1080;
            return new Vector2(Mathf.Clamp(pos.x, 1.0f, w - 1.0f), Mathf.Clamp(pos.y, 1.0f, h - 1.0f));
        }

        private static GameObject _lastLegacyHover;

        public static void SimulateMouseMove(Vector2 screenPos)
        {
            var clamped = ClampToScreen(screenPos);
            LastSimulatedPosition = clamped;
            if (InputSystemShim.IsAvailable)
            {
                InputSystemShim.MouseMove(clamped);
            }
            else
            {
                var es = EventSystem.current ?? EnsureEventSystem();
                var data = GetPooledPointerData(es);
                data.position = clamped;
                var results = Raycasts(data);
                var current = results.Count > 0 ? results[0].gameObject : null;

                if (current != _lastLegacyHover)
                {
                    if (_lastLegacyHover != null) ExecuteEvents.Execute(_lastLegacyHover, data, ExecuteEvents.pointerExitHandler);
                    if (current != null) ExecuteEvents.Execute(current, data, ExecuteEvents.pointerEnterHandler);
                    _lastLegacyHover = current;
                }

                if (IsSimulatedButtonPressed && current != null)
                {
                    ExecuteEvents.Execute(current, data, ExecuteEvents.dragHandler);
                }
            }
        }
        
        public static void SimulateMouseClick(bool down)
        {
            IsSimulatedButtonPressed = down;
            if (InputSystemShim.IsAvailable)
            {
                InputSystemShim.MouseButton(0, down);
            }
            else
            {
                var es = EventSystem.current ?? EnsureEventSystem();
                var data = GetPooledPointerData(es);
                data.position = LastSimulatedPosition;
                var results = Raycasts(data);
                if (results.Count > 0)
                {
                    var target = results[0].gameObject;
                    if (down)
                    {
                        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
                        es.SetSelectedGameObject(target);
                    }
                    else
                    {
                        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
                    }
                }
            }
        }

        public static void SimulateDragging(bool dragging) => IsSimulatedDragging = dragging;

        private static int _lastCanvasUpdateFrame = -1;
        public static void ThrottleCanvasUpdate()
        {
            if (Time.frameCount == _lastCanvasUpdateFrame) return;
            Canvas.ForceUpdateCanvases();
            _lastCanvasUpdateFrame = Time.frameCount;
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
    }
}