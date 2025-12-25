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

            bool matches = CompareToBaselineInternal(testName, tolerance);
            if (!matches)
            {
                RealPlayTester.Assert.Assert.Fail($"Screenshot comparison failed for '{testName}'");
            }
        }

        internal static bool CompareToBaselineInternal(string testName, float tolerance = DefaultTolerance)
        {
            // 1. Capture current screen
            Texture2D currentScreen = ScreenCapture.CaptureScreenshotAsTexture();
            
            // 2. Load baseline
            string baselinePath = Path.Combine(RealPlayEnvironment.ProjectRoot, BaselineFolder, testName + ".png");
            if (!File.Exists(baselinePath))
            {
                RealPlayLog.Warn($"Baseline not found for '{testName}' at {baselinePath}. Saving current as baseline.");
                SaveTexture(currentScreen, baselinePath);
                return true;
            }

            Texture2D baseline = LoadTexture(baselinePath);
            if (baseline == null)
            {
                RealPlayLog.Error($"Failed to load baseline texture from {baselinePath}");
                return false;
            }

            // 3. Compare
            bool match = CompareTextures(baseline, currentScreen, tolerance, out float difference);

            // 4. If fail, save diff/actual
            if (!match)
            {
                string failureDir = Path.Combine(RealPlayEnvironment.TestReportsPath, FailureFolder);
                Directory.CreateDirectory(failureDir);
                SaveTexture(currentScreen, Path.Combine(failureDir, testName + "_Actual.png"));
                // Ideally we'd save a diff image too
                RealPlayLog.Error($"Screenshot mismatch for '{testName}'. Difference: {difference:P2} (Tolerance: {tolerance:P2})");
            }

            return match;
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
