using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    internal static class ScreenshotUtility
    {
        private const string FolderName = "Failures";

        public static string CaptureFailureScreenshot()
        {
            string directory = Path.Combine(RealPlayEnvironment.TestReportsPath, FolderName);
            try { Directory.CreateDirectory(directory); }
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Clear();
        }

        public static void Clear()
        {
            if (_overlay != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_overlay);
                else UnityEngine.Object.DestroyImmediate(_overlay);
                _overlay = null;
            }
        }

        public static void Show(string message, string screenshotPath)
        {
            if (_overlay != null) return;

            if (!IsMainThread())
            {
                 var host = RealPlayTesterHost.Instance;
                 if (host != null)
                 {
                     host.RunOnMainThread(() => BuildOverlay(message, screenshotPath));
                     return;
                 }
            }

            BuildOverlay(message, screenshotPath);
        }

        private static bool IsMainThread()
        {
            return RealPlayTesterHost.MainContext == System.Threading.SynchronizationContext.Current;
        }

        private static void BuildOverlay(string message, string screenshotPath)
        {
            if (_overlay != null) return;

            _overlay = CreateCanvas();
            CreateBackground(_overlay.transform);
            CreateText(_overlay.transform, message, screenshotPath);

            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(_overlay);
        }

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("RealPlayTester_FailureOverlay");
            go.hideFlags = HideFlags.HideAndDontSave;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static void CreateBackground(Transform parent)
        {
            var bg = new GameObject("Background");
            bg.transform.SetParent(parent, false);
            var image = bg.AddComponent<Image>();
            image.color = new Color(1f, 0f, 0f, 0.35f);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateText(Transform parent, string message, string screenshotPath)
        {
            var textObj = new GameObject("Message");
            textObj.transform.SetParent(parent, false);
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
        }

        private static string BuildMessage(string message, string screenshotPath)
        {
            string result = "RealPlayTester Assertion Failed";
            if (!string.IsNullOrEmpty(message)) result += "\n" + message;
            if (!string.IsNullOrEmpty(screenshotPath)) result += "\nScreenshot: " + screenshotPath;
            return result;
        }
    }
}
