using System;
using System.IO;
using UnityEngine;

namespace RealPlayTester.Core
{
    public static class Screenshot
    {
        private const string BaselineFolder = "TestBaselines";
        private const string FailureFolder = "TestFailures";
        private const float DefaultTolerance = 0.05f; // 5% difference allowed

        public static void CaptureAndCompare(string testName, float tolerance = DefaultTolerance)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            bool matches = CompareToBaselineInternal(testName, null, tolerance);
            if (!matches)
            {
                RealPlayTester.Assert.Assert.Fail($"Screenshot comparison failed for '{testName}'");
            }
        }

        public static void CaptureAndCompareRegion(string testName, Rect region, float tolerance = DefaultTolerance)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            bool matches = CompareToBaselineInternal(testName, region, tolerance);
            if (!matches)
            {
                RealPlayTester.Assert.Assert.Fail($"Region screenshot comparison failed for '{testName}'");
            }
        }

        internal static bool CompareToBaselineInternal(string testName, Rect? region = null, float tolerance = DefaultTolerance)
        {
            // 1. Capture current screen
            Texture2D fullScreen = ScreenCapture.CaptureScreenshotAsTexture();
            Texture2D actual = fullScreen;

            // Crop if region specified
            if (region.HasValue)
            {
                actual = CropTexture(fullScreen, region.Value);
                if (actual != fullScreen)
                {
                    UnityEngine.Object.Destroy(fullScreen);
                }
            }
            
            // 2. Load baseline
            string baselinePath = Path.Combine(RealPlayEnvironment.ProjectRoot, BaselineFolder, testName + ".png");
            if (!File.Exists(baselinePath))
            {
                RealPlayLog.Warn($"Baseline not found for '{testName}' at {baselinePath}. Saving current as baseline.");
                SaveTexture(actual, baselinePath);
                return true;
            }

            Texture2D baseline = LoadTexture(baselinePath);
            if (baseline == null)
            {
                RealPlayLog.Error($"Failed to load baseline texture from {baselinePath}");
                return false;
            }

            // 3. Compare
            bool match = CompareTextures(baseline, actual, tolerance, out float difference);

            // 4. If fail, save diff/actual
            if (!match)
            {
                string failureDir = Path.Combine(RealPlayEnvironment.TestReportsPath, FailureFolder);
                Directory.CreateDirectory(failureDir);
                SaveTexture(actual, Path.Combine(failureDir, testName + "_Actual.png"));
                RealPlayLog.Error($"Screenshot mismatch for '{testName}'. Difference: {difference:P2} (Tolerance: {tolerance:P2})");
            }

            return match;
        }

        private static Texture2D CropTexture(Texture2D source, Rect region)
        {
            int x = Mathf.Clamp((int)region.x, 0, source.width);
            int y = Mathf.Clamp((int)region.y, 0, source.height);
            int w = Mathf.Clamp((int)region.width, 0, source.width - x);
            int h = Mathf.Clamp((int)region.height, 0, source.height - y);

            if (w <= 0 || h <= 0) return source; // Invalid region returns full

            Color[] pixels = source.GetPixels(x, y, w, h);
            Texture2D cropped = new Texture2D(w, h);
            cropped.SetPixels(pixels);
            cropped.Apply();
            return cropped;
        }

        private static bool CompareTextures(Texture2D baseline, Texture2D actual, float tolerance, out float difference)
        {
            difference = 1.0f;

            if (baseline.width != actual.width || baseline.height != actual.height)
            {
                RealPlayLog.Error($"Dimensions mismatch: Baseline {baseline.width}x{baseline.height} vs Actual {actual.width}x{actual.height}");
                return false;
            }

            // Use GetPixels32 for performance
            Color32[] basePixels = baseline.GetPixels32();
            Color32[] actualPixels = actual.GetPixels32();

            int diffCount = 0;
            int totalPixels = basePixels.Length;

            for (int i = 0; i < totalPixels; i++)
            {
                if (!AreColorsSimilar(basePixels[i], actualPixels[i]))
                {
                    diffCount++;
                }
            }

            difference = (float)diffCount / totalPixels;
            return difference <= tolerance;
        }

        private static bool AreColorsSimilar(Color32 a, Color32 b)
        {
            // Simple Euclidean distance or just check absolute diff
            // 25 seems like a reasonable per-channel tolerance (roughly 10%)
            int diff = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
            return diff < 30; 
        }

        private static void SaveTexture(Texture2D texture, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                RealPlayLog.Info($"Saved screenshot to {path}");
            }
            catch (Exception ex)
            {
                RealPlayLog.Error($"Failed to save texture to {path}: {ex.Message}");
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                return tex;
            }
            return null;
        }
    }
}
