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
            if (!RealPlayEnvironment.IsEnabled || scrollRect == null || target == null)
            {
                return;
            }

            float elapsed = 0f;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            while (elapsed < timeoutSeconds)
            {
                if (IsVisibleInViewport(viewport, target))
                {
                    return;
                }

                // Nudge scroll toward target based on anchored position.
                Vector3 localPos = viewport.InverseTransformPoint(target.position);
                float viewportHeight = viewport.rect.height;
                float targetY = localPos.y;

                // Adjust normalized position heuristically.
                float step = Mathf.Sign(targetY) * 0.1f;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + step);

                await Task.Yield();
                elapsed += Time.deltaTime;
            }

            RealPlayLog.Warn("Scroll.UntilVisible timed out.");
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

            return targetMaxY <= viewportMaxY && targetMinY >= viewportMinY;
        }
    }
}
