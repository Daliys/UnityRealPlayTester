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

        public static OcclusionResult CheckOcclusion(GameObject target, bool forceUpdateCanvas = true)
        {
            if (target == null) return new OcclusionResult { IsOccluded = false }; // Can't occlude nothing

            if (Application.isBatchMode && Tester.Settings.ForceBatchmodeVisibility)
            {
                return new OcclusionResult { IsOccluded = false };
            }

            var cam = Camera.main;
            if (cam == null) return new OcclusionResult { IsOccluded = true, BlockingObjectName = "No Main Camera" };

            // 1. Check UI Occlusion first
            if (IsOccludedByUI(target, forceUpdateCanvas))
            {
                return new OcclusionResult { IsOccluded = true, BlockingObjectName = "UI Element" };
            }

            try
            {
                // Skip world occlusion if target is on Overlay Canvas
                var canvas = target.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return new OcclusionResult { IsOccluded = false };
                }

                // 2. Check World Occlusion
                return CheckWorldOcclusion(target, cam, forceUpdateCanvas);
            }
            catch (UnityEngine.MissingReferenceException)
            {
                return new OcclusionResult { IsOccluded = true, BlockingObjectName = "Destroyed During Check" };
            }
        }

        private static bool IsOccludedByUI(GameObject target, bool forceUpdateCanvas)
        {
            if (target == null) return false;
            if (forceUpdateCanvas) RealPlayTester.Input.RealInputUtility.ThrottleCanvasUpdate();
            
            // 1. Center Check (Primary)
            var center = RealPlayTester.Input.RealInputUtility.GetScreenCenter(target, null, false);
            if (!RealPlayTester.Input.RealInputUtility.IsPointOccludedByUI(center, target, false))
                return false;

            // 2. Corner/Surface Checks (Redundancy for partially obscured items)
            try
            {
                if (target.transform is RectTransform rt)
                {
                    if (IsSurfaceVisible(rt, target, forceUpdateCanvas)) return false;
                }
            }
            catch (UnityEngine.MissingReferenceException) { return true; }

            return true;
        }

        private static readonly Vector3[] CornerBuffer = new Vector3[4];
        private static readonly Vector2[] PointBuffer = new Vector2[4];

        private static bool IsSurfaceVisible(RectTransform rt, GameObject target, bool forceUpdateCanvas)
        {
            if (rt == null || target == null) return false;
            rt.GetWorldCorners(CornerBuffer);
            
            var canvas = rt.GetComponentInParent<Canvas>();
            bool isOverlay = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
            Camera cam = isOverlay ? null : (Camera.main ?? canvas?.worldCamera);

            if (isOverlay)
            {
                PointBuffer[0] = Vector2.Lerp(CornerBuffer[0], CornerBuffer[2], 0.1f);
                PointBuffer[1] = Vector2.Lerp(CornerBuffer[0], CornerBuffer[2], 0.9f);
                PointBuffer[2] = new Vector2(Mathf.Lerp(CornerBuffer[0].x, CornerBuffer[2].x, 0.1f), Mathf.Lerp(CornerBuffer[0].y, CornerBuffer[2].y, 0.9f));
                PointBuffer[3] = new Vector2(Mathf.Lerp(CornerBuffer[0].x, CornerBuffer[2].x, 0.9f), Mathf.Lerp(CornerBuffer[0].y, CornerBuffer[2].y, 0.1f));
            }
            else if (cam != null)
            {
                for (int i = 0; i < 4; i++) PointBuffer[i] = cam.WorldToScreenPoint(CornerBuffer[i]);
                // Redefine to inset points after projection
                Vector2 min = PointBuffer[0], max = PointBuffer[0];
                foreach(var p in PointBuffer) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                PointBuffer[0] = Vector2.Lerp(min, max, 0.1f);
                PointBuffer[1] = Vector2.Lerp(min, max, 0.9f);
                PointBuffer[2] = new Vector2(Mathf.Lerp(min.x, max.x, 0.1f), Mathf.Lerp(min.y, max.y, 0.9f));
                PointBuffer[3] = new Vector2(Mathf.Lerp(min.x, max.x, 0.9f), Mathf.Lerp(min.y, max.y, 0.1f));
            }

            foreach (var p in PointBuffer)
            {
                if (!RealPlayTester.Input.RealInputUtility.IsPointOccludedByUI(p, target, false)) return true;
            }
            return false;
        }

        private static OcclusionResult CheckWorldOcclusion(GameObject target, Camera cam, bool forceUpdateCanvas)
        {
            if (target == null) return new OcclusionResult { IsOccluded = false };
            var screenPos = RealPlayTester.Input.RealInputUtility.GetScreenCenter(target, cam, forceUpdateCanvas);
            var ray = cam.ScreenPointToRay(screenPos);
            
            int mask = cam.cullingMask;
            
            // 3D Raycast
            bool hit3D = Physics.Raycast(ray, out RaycastHit hit, cam.farClipPlane, mask);
            float dist3D = hit3D ? hit.distance : float.MaxValue;
            GameObject go3D = hit3D ? hit.collider.gameObject : null;

            // 2D Raycast
            var hitInfo2D = Physics2D.GetRayIntersection(ray, cam.farClipPlane, mask);
            float dist2D = hitInfo2D.collider != null ? Vector3.Distance(ray.origin, hitInfo2D.point) : float.MaxValue;
            GameObject go2D = hitInfo2D.collider != null ? hitInfo2D.collider.gameObject : null;

            GameObject bestHit = (dist3D < dist2D) ? go3D : go2D;
            return ValidateHit(target, bestHit);
        }

        private static OcclusionResult ValidateHit(GameObject target, GameObject bestHit)
        {
            if (bestHit == null) return new OcclusionResult { IsOccluded = false };
            try
            {
                if (bestHit == target || bestHit.transform.IsChildOf(target.transform) || target.transform.IsChildOf(bestHit.transform))
                {
                    return new OcclusionResult { IsOccluded = false };
                }
                return new OcclusionResult { IsOccluded = true, BlockingObjectName = bestHit.name };
            }
            catch (UnityEngine.MissingReferenceException)
            {
                return new OcclusionResult { IsOccluded = false };
            }
        }
    }
}
