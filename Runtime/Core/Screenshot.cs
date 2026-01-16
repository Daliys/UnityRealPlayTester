using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Core screenshot management. Handles capture, baseline comparison, and visual annotations.
    /// </summary>
    public static partial class Screenshot
    {
        private const string BaselineFolder = "TestBaselines";
        private const string FailureFolder = "TestFailures";
        private const float DefaultTolerance = 0.05f;

        public static void CaptureAndCompare(string testName, float tolerance = DefaultTolerance)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            if (!CompareToBaselineInternal(testName, null, tolerance))
                RealPlayTester.Assert.Assert.Fail($"Screenshot comparison failed for '{testName}'");
        }

        public static void CaptureAndCompareRegion(string testName, Rect region, float tolerance = DefaultTolerance)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            if (!CompareToBaselineInternal(testName, region, tolerance))
                RealPlayTester.Assert.Assert.Fail($"Region screenshot comparison failed for '{testName}'");
        }

        public static Texture2D CaptureWithAnnotations(List<GameObject> targets, out Dictionary<string, string> labelMap)
        {
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) { labelMap = new Dictionary<string, string>(); return new Texture2D(2, 2); }
            labelMap = DrawAnnotations(tex, targets);
            return tex;
        }

        public static void SaveHeatmapAsPNG(string testName)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            var heatmap = RealPlayTesterHost.Instance.GetComponentInChildren<RealPlayTester.Input.InteractionHeatmap>();
            if (heatmap == null) return;

            string path = Path.Combine(RealPlayEnvironment.TestReportsPath, "Heatmaps", testName + "_Heatmap.png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            // For now, we capture the full screen including the heatmap overlay
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex != null)
            {
                SaveTexture(tex, path);
                UnityEngine.Object.Destroy(tex);
                RealPlayLog.Info($"Heatmap saved to: {path}");
            }
        }

        internal static bool CompareToBaselineInternal(string testName, Rect? region, float tolerance = DefaultTolerance)
        {
            Texture2D actual = CaptureScreenshotWithRegion(region);
            string baselinePath = Path.Combine(RealPlayEnvironment.ProjectRoot, BaselineFolder, testName + ".png");

            if (!File.Exists(baselinePath)) return HandleMissingBaseline(actual, baselinePath);

            Texture2D baseline = LoadTexture(baselinePath);
            if (baseline == null) return false;

            bool match = CompareTextures(baseline, actual, tolerance, out float difference);
            if (!match) HandleMismatch(testName, actual, difference, tolerance);

            UnityEngine.Object.Destroy(actual);
            UnityEngine.Object.Destroy(baseline);
            return match;
        }

        private static Texture2D CaptureScreenshotWithRegion(Rect? region)
        {
            Texture2D fullScreen = ScreenCapture.CaptureScreenshotAsTexture();
            if (!region.HasValue) return fullScreen;

            int x = Mathf.Clamp((int)region.Value.x, 0, fullScreen.width);
            int y = Mathf.Clamp((int)region.Value.y, 0, fullScreen.height);
            int w = Mathf.Clamp((int)region.Value.width, 0, fullScreen.width - x);
            int h = Mathf.Clamp((int)region.Value.height, 0, fullScreen.height - y);

            if (w <= 0 || h <= 0) return fullScreen;

            Color[] pixels = fullScreen.GetPixels(x, y, w, h);
            Texture2D cropped = new Texture2D(w, h);
            cropped.SetPixels(pixels);
            cropped.Apply();
            UnityEngine.Object.Destroy(fullScreen);
            return cropped;
        }

        private static bool HandleMissingBaseline(Texture2D actual, string path)
        {
            SaveTexture(actual, path);
            return true;
        }

        private static void HandleMismatch(string testName, Texture2D actual, float diff, float tolerance)
        {
            string failureDir = Path.Combine(RealPlayEnvironment.TestReportsPath, FailureFolder);
            Directory.CreateDirectory(failureDir);
            SaveTexture(actual, Path.Combine(failureDir, testName + "_Actual.png"));
            RealPlayLog.Error($"Screenshot mismatch for '{testName}'. Difference: {diff:P2} (Tolerance: {tolerance:P2})");
        }

        private static bool CompareTextures(Texture2D baseline, Texture2D actual, float tolerance, out float difference)
        {
            if (baseline.width != actual.width || baseline.height != actual.height)
            {
                difference = 1.0f; return false;
            }

            Color32[] basePixels = baseline.GetPixels32(), actualPixels = actual.GetPixels32();
            int diffCount = 0;
            for (int i = 0; i < basePixels.Length; i++)
            {
                var b = basePixels[i]; var a = actualPixels[i];
                if (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 30) diffCount++;
            }

            difference = (float)diffCount / basePixels.Length;
            return difference <= tolerance;
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            try { 
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG()); 
            } catch (Exception ex) { RealPlayLog.Error($"Failed to save texture: {ex.Message}"); }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            return tex.LoadImage(bytes) ? tex : null;
        }
    }
}
