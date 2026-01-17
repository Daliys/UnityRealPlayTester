using UnityEngine;
using UnityEngine.UI;

namespace RealPlayTester.Input
{
    public static partial class RealInputUtility
    {
        private static readonly Vector3[] WorldCornersCache = new Vector3[4];

        public static Rect GetScreenRect(GameObject go)
        {
            if (go == null) return Rect.zero;
            try
            {
                if (go.transform is RectTransform rt) return GetUIScreenRect(rt);
                var renderer = go.GetComponent<Renderer>();
                return (renderer != null) ? GetWorldScreenRect(renderer) : Rect.zero;
            }
            catch (UnityEngine.MissingReferenceException) { return Rect.zero; }
        }

        private static Rect GetUIScreenRect(RectTransform rt)
        {
            if (rt == null) return Rect.zero;
            var canvas = rt.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                rt.GetWorldCorners(WorldCornersCache);
                float minX = Mathf.Min(WorldCornersCache[0].x, WorldCornersCache[2].x);
                float minY = Mathf.Min(WorldCornersCache[0].y, WorldCornersCache[2].y);
                float maxX = Mathf.Max(WorldCornersCache[0].x, WorldCornersCache[2].x);
                float maxY = Mathf.Max(WorldCornersCache[0].y, WorldCornersCache[2].y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            var cam = canvas?.worldCamera ?? Camera.main;
            if (cam == null) return Rect.zero;

            rt.GetWorldCorners(WorldCornersCache);
            float xMin = float.MaxValue, yMin = float.MaxValue;
            float xMax = float.MinValue, yMax = float.MinValue;

            foreach (var corner in WorldCornersCache)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                xMin = Mathf.Min(xMin, screenPos.x);
                yMin = Mathf.Min(yMin, screenPos.y);
                xMax = Mathf.Max(xMax, screenPos.x);
                yMax = Mathf.Max(yMax, screenPos.y);
            }
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Rect GetWorldScreenRect(Renderer renderer)
        {
            if (renderer == null) return Rect.zero;
            var cam = Camera.main;
            if (cam == null) return Rect.zero;

            var bounds = renderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
            };

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var corner in corners)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                minX = Mathf.Min(minX, screenPos.x);
                minY = Mathf.Min(minY, screenPos.y);
                maxX = Mathf.Max(maxX, screenPos.x);
                maxY = Mathf.Max(maxY, screenPos.y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Vector2 GetScreenCenter(GameObject go, Camera camera = null, bool forceUpdateCanvas = true)
        {
            if (go == null) return LastSimulatedPosition;
            try
            {
                if (go.transform is RectTransform rect) return GetUIScreenCenter(rect, camera, forceUpdateCanvas);
                return GetWorldScreenCenter(go, camera);
            }
            catch (UnityEngine.MissingReferenceException) { return LastSimulatedPosition; }
        }

        private static Vector2 GetUIScreenCenter(RectTransform rect, Camera camera, bool forceUpdateCanvas)
        {
            if (rect == null) return LastSimulatedPosition;
            if (forceUpdateCanvas) ThrottleCanvasUpdate();
            var canvas = rect.GetComponentInParent<Canvas>();
            rect.GetWorldCorners(WorldCornersCache);
            var worldCenter = (WorldCornersCache[0] + WorldCornersCache[2]) * 0.5f;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) return ClampToScreen(worldCenter);
            var cam = camera ?? canvas?.worldCamera ?? GetCameraForObject(rect.gameObject);
            return cam != null ? ClampToScreen(RectTransformUtility.WorldToScreenPoint(cam, worldCenter)) : ClampToScreen(worldCenter);
        }

        private static Vector2 GetWorldScreenCenter(GameObject go, Camera camera)
        {
            if (go == null) return LastSimulatedPosition;
            var cam = camera ?? GetCameraForObject(go);
            if (cam != null)
            {
                var screenPoint = cam.WorldToScreenPoint(go.transform.position);
                if (screenPoint.z >= 0) return ClampToScreen(screenPoint);
            }
            return LastSimulatedPosition;
        }

        private static Camera GetCameraForObject(GameObject go)
        {
            if (go == null) return Camera.main;
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas?.worldCamera != null) return canvas.worldCamera;
            int layer = go.layer;
            foreach (var cam in Camera.allCameras) if (cam != null && cam.enabled && (cam.cullingMask & (1 << layer)) != 0) return cam;
            return Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
        }
    }
}
