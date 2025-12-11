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

            // Simple viewport check
            bool IsVisible()
            {
                var targetCorners = new Vector3[4];
                target.GetWorldCorners(targetCorners);
                
                var viewCorners = new Vector3[4];
                scrollRect.viewport.GetWorldCorners(viewCorners);
                
                Rect viewRect = new Rect(viewCorners[0].x, viewCorners[0].y, viewCorners[2].x - viewCorners[0].x, viewCorners[2].y - viewCorners[0].y);
                return viewRect.Contains(targetCorners[0]) && viewRect.Contains(targetCorners[2]);
            }

            // Heuristic scroll
            while (!IsVisible())
            {
                bool scrolled = false;
                
                if (scrollRect.vertical)
                {
                    Vector3 targetLocal = scrollRect.viewport.InverseTransformPoint(target.position);
                    float shift = 0.05f * (Time.timeScale > 0 ? Time.deltaTime * 60f : 1f);
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
