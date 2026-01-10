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
    public static partial class Assert
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
            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Assertion", $"FAILED: {message} (Screenshot: {screenshotPath})");
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

    }

}
