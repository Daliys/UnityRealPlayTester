using System;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    /// <summary>
    /// Assertion helpers. On failure: capture screenshot, pause time, show red overlay, and throw.
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (!condition)
            {
                Fail(message ?? "Expected condition to be true.");
            }
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (!Equals(expected, actual))
            {
                Fail(message ?? $"Expected: {expected}, Actual: {actual}");
            }
        }

        public static void Fail(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            string screenshotPath = ScreenshotUtility.CaptureFailureScreenshot();
            FailureOverlay.Show(message, screenshotPath);
            Time.timeScale = 0f;
            throw new AssertionException("RealPlayTester assertion failed", ComposeMessage(message, screenshotPath));
        }

        private static string ComposeMessage(string message, string screenshotPath)
        {
            if (string.IsNullOrEmpty(screenshotPath))
            {
                return message ?? "Assertion failed.";
            }

            return (message ?? "Assertion failed.") + $" (Screenshot: {screenshotPath})";
        }
    }

    internal static class ScreenshotUtility
    {
        private const string FolderName = "RealPlayTester/Failures";

        public static string CaptureFailureScreenshot()
        {
            string directory = Path.Combine(Application.persistentDataPath, FolderName);
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Could not create screenshot directory: " + ex.Message);
                return string.Empty;
            }

            string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string path = Path.Combine(directory, fileName);
            try
            {
                ScreenCapture.CaptureScreenshot(path);
                RealPlayLog.Info("Captured screenshot: " + path);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Failed to capture screenshot: " + ex.Message);
                return string.Empty;
            }

            return path;
        }
    }

    internal static class FailureOverlay
    {
        private static GameObject _overlay;

        public static void Show(string message, string screenshotPath)
        {
            if (_overlay != null)
            {
                return;
            }

            var host = RealPlayTesterHost.Instance;
            if (host != null)
            {
                host.RunOnMainThread(() =>
                {
                    BuildOverlay(message, screenshotPath);
                });
            }
            else
            {
                BuildOverlay(message, screenshotPath);
            }
        }

        private static void BuildOverlay(string message, string screenshotPath)
        {
            if (_overlay != null)
            {
                return;
            }

            _overlay = new GameObject("RealPlayTester_FailureOverlay");
            _overlay.hideFlags = HideFlags.HideAndDontSave;
            var canvas = _overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            _overlay.AddComponent<CanvasScaler>();
            _overlay.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(_overlay.transform, false);
            var image = bg.AddComponent<Image>();
            image.color = new Color(1f, 0f, 0f, 0.35f);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var textObj = new GameObject("Message");
            textObj.transform.SetParent(_overlay.transform, false);
            var text = textObj.AddComponent<Text>();
            text.color = Color.white;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = BuildMessage(message, screenshotPath);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.1f, 0.1f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_overlay);
            }
        }

        private static string BuildMessage(string message, string screenshotPath)
        {
            string result = "RealPlayTester Assertion Failed";
            if (!string.IsNullOrEmpty(message))
            {
                result += "\n" + message;
            }

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                result += "\nScreenshot: " + screenshotPath;
            }

            return result;
        }
    }
}
