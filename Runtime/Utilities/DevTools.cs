using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Await;
using RealPlayTester.Core;
using UnityEngine.UI;
using RealPlayTester.Input;
using UnityEngine.EventSystems;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Development helpers for tests (breakpoints, markers, slow-mo, inspections).
    /// </summary>
    public static class DevTools
    {
        public static async Task Breakpoint(KeyCode resumeKey = KeyCode.Space)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            float original = Time.timeScale;
            Time.timeScale = 0f;
            RealPlayLog.Info($"Test paused. Press {resumeKey} to continue...");
            RealPlayLog.Info($"Test paused. Press {resumeKey} to continue...");
            try
            {
                while (!IsKeyPressed(resumeKey))
                {
                    await Wait.Seconds(0.1f, unscaled: true);
                }
            }
            finally
            {
                Time.timeScale = original;
            }
        }

        public static void ShowClickMarker(Vector2 screenPos, float duration = 0.5f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            var host = RealPlayTesterHost.Instance;
            host.StartCoroutine(ShowMarkerRoutine(screenPos, duration));
        }

        public static void SetSlowMotion(float timeScale = 0.25f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            Time.timeScale = Mathf.Max(0f, timeScale);
        }

        public static void Inspect<T>(string name, T value)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            RealPlayLog.Info($"{name} = {value}");
        }

        private static System.Collections.IEnumerator ShowMarkerRoutine(Vector2 screenPos, float duration)
        {
            var canvas = new GameObject("RealPlayTester_ClickMarker", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 32766;
            canvas.hideFlags = HideFlags.HideAndDontSave;

            var marker = new GameObject("Marker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(canvas.transform, false);
            var rect = marker.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20f, 20f);
            rect.position = screenPos;
            var img = marker.GetComponent<Image>();
            img.color = Color.yellow;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                img.color = new Color(1f, 1f, 0f, alpha);
                yield return null;
            }

            Object.Destroy(marker);
            Object.Destroy(canvas);
        }

        private static bool IsKeyPressed(KeyCode key)
        {
            if (InputSystemShim.IsAvailable)
            {
                if (InputSystemShim.GetKeyDown(key))
                {
                    return true;
                }
            }

            return UnityEngine.Input.GetKey(key) || UnityEngine.Input.GetKeyDown(key);
        }
    }
}
