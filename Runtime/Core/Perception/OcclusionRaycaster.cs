using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace RealPlayTester.Core.Perception
{
    public static class RealPlayOcclusionRaycaster
    {
        public struct OcclusionResult
        {
            public bool IsOccluded;
            public string BlockingObjectName;
        }

        public static OcclusionResult CheckOcclusion(GameObject target)
        {
            var cam = Camera.main;
            if (cam == null) return new OcclusionResult { IsOccluded = true, BlockingObjectName = "No Main Camera" };

            // 1. Check UI Occlusion first
            if (IsOccludedByUI(target))
            {
                return new OcclusionResult { IsOccluded = true, BlockingObjectName = "UI Element" };
            }

            // 2. Check World Occlusion
            return CheckWorldOcclusion(target, cam);
        }

        private static bool IsOccludedByUI(GameObject target)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new PointerEventData(eventSystem);
            var screenPos = RealPlayTester.Input.RealInputUtility.GetScreenCenter(target);
            pointerData.position = screenPos;

            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            if (results.Count == 0 && Application.isBatchMode)
            {
                // Fallback: Manually query GraphicRaycasters if EventSystem is idle
                var raycasters = Object.FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                foreach (var gr in raycasters)
                {
                    gr.Raycast(pointerData, results);
                }
            }

            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    if (result.gameObject == target || IsParentOf(target, result.gameObject))
                        return false;
                    
                    return true;
                }
            }

            return false;
        }

        private static OcclusionResult CheckWorldOcclusion(GameObject target, Camera cam)
        {
            var screenPos = RealPlayTester.Input.RealInputUtility.GetScreenCenter(target);
            var ray = cam.ScreenPointToRay(screenPos);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == target || IsParentOf(target, hit.collider.gameObject))
                {
                    return new OcclusionResult { IsOccluded = false };
                }
                else
                {
                    return new OcclusionResult { IsOccluded = true, BlockingObjectName = hit.collider.gameObject.name };
                }
            }

            return new OcclusionResult { IsOccluded = false };
        }

        private static bool IsParentOf(GameObject child, GameObject potentialParent)
        {
            Transform t = child.transform;
            while (t != null)
            {
                if (t.gameObject == potentialParent) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
