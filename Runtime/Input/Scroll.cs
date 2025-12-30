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
                elapsed += Time.deltaTime;
            }

            scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>Scroll until the target RectTransform is visible within the ScrollRect viewport.</summary>
        public static async Task UntilVisible(ScrollRect scrollRect, RectTransform target, float timeoutSeconds = 5f)
        {
            if (!RealPlayEnvironment.IsEnabled || scrollRect == null || target == null) return;
            
            float startTime = Time.realtimeSinceStartup;
            var token = RealPlayExecutionContext.Token;

            // Simple viewport check (Intersection)
            bool IsVisible()
            {
                var targetCorners = new Vector3[4];
                target.GetWorldCorners(targetCorners);
                
                var viewCorners = new Vector3[4];
                scrollRect.viewport.GetWorldCorners(viewCorners);
                
                // Create Rects in World Space (assuming 2D overlay generally works aligned)
                // For a more robust check we might need local conversion, but WorldCorners handles rotation usually
                
                float vMinX = Mathf.Min(viewCorners[0].x, viewCorners[2].x);
                float vMaxX = Mathf.Max(viewCorners[0].x, viewCorners[2].x);
                float vMinY = Mathf.Min(viewCorners[0].y, viewCorners[2].y);
                float vMaxY = Mathf.Max(viewCorners[0].y, viewCorners[2].y);
                Rect viewRect = new Rect(vMinX, vMinY, vMaxX - vMinX, vMaxY - vMinY);

                float tMinX = Mathf.Min(targetCorners[0].x, targetCorners[2].x);
                float tMaxX = Mathf.Max(targetCorners[0].x, targetCorners[2].x);
                float tMinY = Mathf.Min(targetCorners[0].y, targetCorners[2].y);
                float tMaxY = Mathf.Max(targetCorners[0].y, targetCorners[2].y);
                Rect targetRect = new Rect(tMinX, tMinY, tMaxX - tMinX, tMaxY - tMinY);

                // Use Overlaps to allow large elements to be considered "visible" even if clipped
                // But we must also ensure *enough* is visible? 
                // For now, strict overlap is better than "Contains" which fails for large objects.
                // We also check if target fully contains view (e.g. huge background) which Overlaps covers.
                return viewRect.Overlaps(targetRect);
            }

            // Heuristic scroll
            while (!IsVisible())
            {
                bool scrolled = false;
                
                if (scrollRect.vertical)
                {
                    Vector3 targetLocal = scrollRect.viewport.InverseTransformPoint(target.position);
                    // Use a slightly larger step or proportional step
                    float shift = 0.1f * (Time.timeScale > 0 ? Time.deltaTime * 60f : 1f); 

                    // Check if we are "close enough" to avoid jitter if overlap is barely failing?
                    // Actually, Overlaps should be stable.
                    
                    if (targetLocal.y < 0) 
                        scrollRect.verticalNormalizedPosition = Mathf.Max(0, scrollRect.verticalNormalizedPosition - shift);
                    else 
                        scrollRect.verticalNormalizedPosition = Mathf.Min(1, scrollRect.verticalNormalizedPosition + shift);
                    scrolled = true;
                }
                
                if (scrollRect.horizontal)
                {
                    Vector3 targetLocal = scrollRect.viewport.InverseTransformPoint(target.position);
                    float shift = 0.05f * (Time.timeScale > 0 ? Time.deltaTime * 60f : 1f);
                    if (targetLocal.x > 0) 
                        scrollRect.horizontalNormalizedPosition = Mathf.Min(1, scrollRect.horizontalNormalizedPosition + shift);
                    else 
                        scrollRect.horizontalNormalizedPosition = Mathf.Max(0, scrollRect.horizontalNormalizedPosition - shift);
                    scrolled = true;
                }

                if (!scrolled) break;

                await Task.Yield();
                if (Time.realtimeSinceStartup - startTime > timeoutSeconds)
                {
                    RealPlayLog.Warn($"Scroll.UntilVisible timed out for {target.name}");
                    break;
                }
            }
        }

        private static bool IsVisibleInViewport(RectTransform viewport, RectTransform target)
        {
            Vector3[] viewportCorners = new Vector3[4];
            Vector3[] targetCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            target.GetWorldCorners(targetCorners);

            float viewportMinY = viewportCorners[0].y;
            float viewportMaxY = viewportCorners[2].y;
            float targetMinY = targetCorners[0].y;
            float targetMaxY = targetCorners[2].y;

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
