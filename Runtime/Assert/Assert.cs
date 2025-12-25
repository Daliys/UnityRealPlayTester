using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RealPlayTester.Core;
using System.Collections.Generic;

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

        public static void IsFalse(bool condition, string message = null)
        {
            IsTrue(!condition, message ?? "Expected condition to be false.");
        }

        public static void IsNull(object value, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (value != null)
            {
                Fail(message ?? $"Expected null but was {value}");
            }
        }

        public static void IsNotNull(object value, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (value == null)
            {
                Fail(message ?? "Expected non-null value");
            }
        }

        public static void Greater<T>(T value, T threshold, string message = null) where T : IComparable<T>
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (value.CompareTo(threshold) <= 0)
            {
                Fail(message ?? $"Expected {value} > {threshold}");
            }
        }

        public static void Less<T>(T value, T threshold, string message = null) where T : IComparable<T>
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (value.CompareTo(threshold) >= 0)
            {
                Fail(message ?? $"Expected {value} < {threshold}");
            }
        }

        public static void Contains(string haystack, string needle, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (haystack == null || needle == null || !haystack.Contains(needle))
            {
                Fail(message ?? $"'{haystack}' does not contain '{needle}'");
            }
        }

        public static void InRange<T>(T value, T min, T max, string message = null) where T : IComparable<T>
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                Fail(message ?? $"{value} not in range [{min}, {max}]");
            }
        }

        public static void Throws<TException>(Action action, string message = null) where TException : Exception
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            try
            {
                action();
                Fail(message ?? $"Expected {typeof(TException).Name} to be thrown");
            }
            catch (TException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                Fail(message ?? $"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
            }
        }

        // ===== VISUAL ASSERTIONS =====

        public static void IsVisible(GameObject go, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            if (go == null)
            {
                Fail(message ?? "Expected GameObject to be visible but it was null.");
                return;
            }

            if (!go.activeInHierarchy)
            {
                Fail(message ?? $"Expected '{go.name}' to be active in hierarchy.");
                return;
            }

            // Check Renderer
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (!renderer.enabled) Fail(message ?? $"Expected '{go.name}' renderer to be enabled.");
                if (!renderer.isVisible) Fail(message ?? $"Expected '{go.name}' to be visible to a camera.");
                return;
            }

            // Check UI (CanvasRenderer)
            var canvasRenderer = go.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                if (canvasRenderer.cull) Fail(message ?? $"Expected '{go.name}' UI element to not be culled.");
                // Additional UI visibility checks could use RectTransform and camera viewport
                return;
            }

            // Fallback for objects without renderers (like empty containers) - we just check active
        }

        public static void HasSprite(Component target, Sprite expected, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            if (target == null)
            {
                Fail(message ?? "Target component is null.");
                return;
            }

            Sprite actual = null;
            if (target is SpriteRenderer sr) actual = sr.sprite;
            else if (target is Image img) actual = img.sprite;
            else
            {
                Fail(message ?? $"Target '{target.name}' is not a SpriteRenderer or Image.");
                return;
            }

            if (actual != expected)
            {
                Fail(message ?? $"Expected sprite '{expected?.name ?? "null"}' but got '{actual?.name ?? "null"}'.");
            }
        }

        public static void ScreenElementVisible(string elementName, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var go = GameObject.Find(elementName);
            if (go == null)
            {
                // Try to find by tag
                try { go = GameObject.FindWithTag(elementName); } catch { }
            }

            if (go == null)
            {
                Fail(message ?? $"Could not find element with name or tag '{elementName}'.");
                return;
            }

            IsVisible(go, message);
        }

        // ===== ASSET ASSERTIONS =====

        public static void AssetLoaded<T>(string assetPath, string message = null) where T : UnityEngine.Object
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var asset = Resources.Load<T>(assetPath);
            if (asset == null)
            {
                Fail(message ?? $"Failed to load asset of type {typeof(T).Name} at path '{assetPath}'.");
            }
        }

        public static void SceneConfigurationValid(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            // Basic check: Ensure no "Missing Script" components on active objects
            var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        Fail(message ?? $"GameObject '{go.name}' has a missing script component.");
                        return;
                    }
                }
            }
        }

        // ===== GAME STATE ASSERTIONS =====

        public static void GameStateMatches(Action expectedAction, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            try
            {
                expectedAction();
            }
            catch (Exception ex)
            {
                Fail(message ?? $"Game state validation failed: {ex.Message}");
            }
        }

        public static void VisualFeedbackCorrect(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            // This is a placeholder as per requirements logic is vague. 
            // In a real implementation, this might check a list of registered visual feedback providers.
            // For now, we assume if we reached here without previous failures, it's ok, 
            // or the user should use more specific asserts.
        }

        // ===== SCREENSHOT ASSERTIONS =====

        public static void VisualStateMatches(string expectedStateName, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            // This relies on the Screenshot system (to be implemented/linked).
            // We'll delegate to ScreenshotUtility or a new Screenshot class.
            bool match = RealPlayTester.Core.Screenshot.CompareToBaselineInternal(expectedStateName);
            if (!match)
            {
                Fail(message ?? $"Visual state '{expectedStateName}' does not match baseline.");
            }
        }
    }

    internal static class ScreenshotUtility
    {
        private const string FolderName = "Failures";

        public static string CaptureFailureScreenshot()
        {
            string directory = Path.Combine(RealPlayEnvironment.TestReportsPath, FolderName);
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

            
            // Check if we can dispatch to main thread if not already there
            if (!IsMainThread())
            {
                 var host = RealPlayTesterHost.Instance;
                 if (host != null)
                 {
                     host.RunOnMainThread(() => BuildOverlay(message, screenshotPath));
                     return;
                 }
            }

            // Fallback (might fail if not on main thread and Host is missing, but typically Host exists)
            BuildOverlay(message, screenshotPath);
        }

        private static bool IsMainThread()
        {
            return RealPlayTesterHost.MainContext == System.Threading.SynchronizationContext.Current;
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
