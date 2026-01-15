using UnityEngine;

namespace RealPlayTester.Core
{
    public static partial class Screenshot
    {
        private static Rect GetScreenRect(GameObject go)
        {
            if (go.transform is RectTransform rt) return GetUIScreenRect(rt);
            var renderer = go.GetComponent<Renderer>();
            return (renderer != null) ? GetWorldScreenRect(renderer) : Rect.zero;
        }

        private static Rect GetUIScreenRect(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                float minX = Mathf.Min(corners[0].x, corners[2].x);
                float minY = Mathf.Min(corners[0].y, corners[2].y);
                float maxX = Mathf.Max(corners[0].x, corners[2].x);
                float maxY = Mathf.Max(corners[0].y, corners[2].y);
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            var cam = Camera.main;
            if (cam == null) return Rect.zero;
            return CalculateScreenRect(rt, cam);
        }

        private static Rect CalculateScreenRect(RectTransform rt, Camera cam)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;

            foreach (var corner in c)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                xMin = Mathf.Min(xMin, screenPos.x); yMin = Mathf.Min(yMin, screenPos.y);
                xMax = Mathf.Max(xMax, screenPos.x); yMax = Mathf.Max(yMax, screenPos.y);
            }
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Rect GetWorldScreenRect(Renderer renderer)
        {
            var cam = Camera.main;
            if (cam == null) return Rect.zero;

            var bounds = renderer.bounds;
            Vector3 min = bounds.min, max = bounds.max;
            Vector3[] corners = new Vector3[8] {
                new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
            };

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var corner in corners)
            {
                Vector2 screenPos = cam.WorldToScreenPoint(corner);
                minX = Mathf.Min(minX, screenPos.x); minY = Mathf.Min(minY, screenPos.y);
                maxX = Mathf.Max(maxX, screenPos.x); maxY = Mathf.Max(maxY, screenPos.y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
