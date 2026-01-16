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
        private static readonly Type PanelRaycasterType = Type.GetType("UnityEngine.UIElements.PanelRaycaster, UnityEngine.UIElementsModule");
        private static readonly System.Reflection.MethodInfo PanelRaycastMethod = PanelRaycasterType?.GetMethod("Raycast", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        private static readonly object[] PanelRaycastArgs = new object[2];

        private static readonly List<RaycastResult> RaycastCache = new List<RaycastResult>(16);
        private static readonly List<RaycastResult> PanelRaycastCache = new List<RaycastResult>(8);

        public static List<RaycastResult> Raycasts(PointerEventData data, bool forceUpdateCanvas = true)
        {
            RaycastCache.Clear();
            if (forceUpdateCanvas) Canvas.ForceUpdateCanvases();
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

        private static void PanelRaycast(PointerEventData data, List<RaycastResult> results)
        {
            if (PanelRaycastMethod == null) return;
            var raycasters = UnityEngine.Object.FindObjectsByType(PanelRaycasterType, FindObjectsSortMode.None);
            foreach (var pr in raycasters)
            {
                PanelRaycastCache.Clear();
                PanelRaycastArgs[0] = data;
                PanelRaycastArgs[1] = PanelRaycastCache;
                PanelRaycastMethod.Invoke(pr, PanelRaycastArgs);
                results.AddRange(PanelRaycastCache);
            }
        }

        public static bool IsPointOccludedByUI(Vector2 screenPos, GameObject target, bool forceUpdateCanvas = true)
        {
            var es = EventSystem.current ?? EnsureEventSystem();
            var data = GetPooledPointerData(es);
            data.position = screenPos;

            var results = Raycasts(data, forceUpdateCanvas);
            if (results.Count == 0) { Debug.Log($"[Occlusion] No hits at {screenPos}"); return false; }

            // Debug logging for occlusion analysis
            foreach(var r in results) if(r.gameObject != null) Debug.Log($"[Occlusion] Hit: {r.gameObject.name} (target={target.name})");

            GameObject firstVisibleHit = null;
            foreach (var res in results)
            {
                if (res.gameObject == null) continue;
                
                // M010: Skip objects that are effectively invisible (alpha ~ 0)
                if (IsActuallyVisible(res.gameObject))
                {
                    firstVisibleHit = res.gameObject;
                    break;
                }
            }

            if (firstVisibleHit == null || firstVisibleHit == target) return false;
            
            Transform t = target.transform;
            if (firstVisibleHit.transform.IsChildOf(t) || t.IsChildOf(firstVisibleHit.transform)) return false;

            return true;
        }

        private static bool IsActuallyVisible(GameObject go)
        {
            // Check CanvasGroup alpha in parent chain
            var groups = go.GetComponentsInParent<CanvasGroup>();
            foreach (var g in groups)
            {
                if (g.alpha < 0.01f) return false;
                if (g.ignoreParentGroups) break;
            }

            // Check Image/Text color alpha if present
            var graphic = go.GetComponent<Graphic>();
            if (graphic != null && graphic.color.a < 0.01f) return false;

            return true;
        }
    }
}
