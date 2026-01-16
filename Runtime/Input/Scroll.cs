using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Scroll helpers for ScrollRect and element visibility.
    /// </summary>
    public static class Scroll
    {
        private static readonly Vector3[] ViewCornersCache = new Vector3[4];
        private static readonly Vector3[] TargetCornersCache = new Vector3[4];

        /// <summary>Scroll the provided ScrollRect to the bottom over a duration.</summary>
        public static async Task ToBottom(ScrollRect scrollRect, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled || scrollRect == null)
            {
                return;
            }

            float elapsed = 0f;
            float start = scrollRect.verticalNormalizedPosition;
            while (elapsed < duration)
            {
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, 0f, t);
                await Task.Yield();
                elapsed += Time.unscaledDeltaTime;
            }

            scrollRect.verticalNormalizedPosition = 0f;
        }

        public static async Task UntilVisible(ScrollRect scrollRect, RectTransform target, float timeoutSeconds = 5f)
        {
            if (!RealPlayEnvironment.IsEnabled || scrollRect == null || target == null) return;
            
            float startTime = Time.realtimeSinceStartup;
            while (!CheckVisibility(scrollRect.viewport, target))
            {
                if (!PerformScrollStep(scrollRect, target)) break;

                await Task.Yield();
                if (Time.realtimeSinceStartup - startTime > timeoutSeconds)
                {
                    RealPlayLog.Warn($"Scroll.UntilVisible timed out for {target.name}");
                    break;
                }
            }
        }

        private static bool CheckVisibility(RectTransform viewport, RectTransform target)
        {
            viewport.GetWorldCorners(ViewCornersCache);
            var viewRect = GetWorldRect(ViewCornersCache);

            target.GetWorldCorners(TargetCornersCache);
            var targetRect = GetWorldRect(TargetCornersCache);

            return viewRect.Overlaps(targetRect);
        }

        private static Rect GetWorldRect(Vector3[] corners)
        {
            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private static bool PerformScrollStep(ScrollRect scrollRect, RectTransform target)
        {
            bool scrolled = false;
            Vector3 targetLocal = scrollRect.viewport.InverseTransformPoint(target.position);
            float timeMult = Time.unscaledDeltaTime * 60f; // Use unscaled

            if (scrollRect.vertical)
            {
                // Calculate normalized size of viewport relative to content
                float contentSize = scrollRect.content.rect.height;
                float viewSize = scrollRect.viewport.rect.height;
                if (contentSize > viewSize)
                {
                    float shift = (viewSize / contentSize) * 0.5f * timeMult;
                    if (targetLocal.y < -viewSize * 0.5f) 
                        scrollRect.verticalNormalizedPosition = Mathf.Max(0, scrollRect.verticalNormalizedPosition - shift);
                    else if (targetLocal.y > viewSize * 0.5f)
                        scrollRect.verticalNormalizedPosition = Mathf.Min(1, scrollRect.verticalNormalizedPosition + shift);
                    scrolled = true;
                }
            }
            
            if (scrollRect.horizontal)
            {
                float contentSize = scrollRect.content.rect.width;
                float viewSize = scrollRect.viewport.rect.width;
                if (contentSize > viewSize)
                {
                    float shift = (viewSize / contentSize) * 0.5f * timeMult;
                    if (targetLocal.x > viewSize * 0.5f) 
                        scrollRect.horizontalNormalizedPosition = Mathf.Min(1, scrollRect.horizontalNormalizedPosition + shift);
                    else if (targetLocal.x < -viewSize * 0.5f)
                        scrollRect.horizontalNormalizedPosition = Mathf.Max(0, scrollRect.horizontalNormalizedPosition - shift);
                    scrolled = true;
                }
            }

            return scrolled;
        }

        private static bool IsVisibleInViewport(RectTransform viewport, RectTransform target)
        {
            viewport.GetWorldCorners(ViewCornersCache);
            target.GetWorldCorners(TargetCornersCache);

            float viewportMinY = ViewCornersCache[0].y;
            float viewportMaxY = ViewCornersCache[2].y;
            float targetMinY = TargetCornersCache[0].y;
            float targetMaxY = TargetCornersCache[2].y;

            // Simple Y-axis check for vertical lists, can be expanded for X
            return targetMaxY <= viewportMaxY && targetMinY >= viewportMinY;
        }

        /// <summary>
        /// Checks if the target is inside a ScrollRect and scrolls it into view if needed.
        /// </summary>
        public static async Task EnsureVisible(GameObject target)
        {
            if (target == null) return;

            var scrollRect = target.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                var rt = target.transform as RectTransform;
                if (rt != null)
                {
                    await UntilVisible(scrollRect, rt, 2f);
                }
            }
        }
    }
}
