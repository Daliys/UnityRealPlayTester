using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    /// <summary>
    /// Utilities for capturing screenshots and simple visual comparisons.
    /// Recording is stubbed with warnings for now.
    /// </summary>
    public static class Capture
    {
        private const string CaptureFolder = "Captures";

        /// <summary>
        /// Take a screenshot immediately and return the saved path.
        /// WARNING: May fail if called outside of drawing frame. Use ScreenshotAsync when possible.
        /// </summary>
        public static string Screenshot(string name = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return string.Empty;
            string path = PreparePath(name);

            // Synchronous capture (risky)
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                return path;
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn($"Capture.Screenshot failed (likely outside drawing frame): {ex.Message}. Using fallback ScreenCapture.");
                ScreenCapture.CaptureScreenshot(path);
                return path; 
            }
            finally { UnityEngine.Object.Destroy(tex); }
        }

        /// <summary>
        /// Safely captures a screenshot by waiting for the end of the current frame.
        /// </summary>
        public static async Task<string> ScreenshotAsync(string name = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return string.Empty;
            string path = PreparePath(name);

            await RealPlayTester.Await.Wait.Frames(1); // Ensure we are in a valid state
            
            // Unity's ScreenCapture is the most reliable for files
            ScreenCapture.CaptureScreenshot(path);
            
            // Give it a frame to actually write
            await RealPlayTester.Await.Wait.Frames(1);
            return path;
        }

        private static string PreparePath(string name)
        {
            string fileName = string.IsNullOrEmpty(name)
                ? DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                : name + ".png";

            string dir = Path.Combine(RealPlayEnvironment.TestReportsPath, CaptureFolder);
            try { Directory.CreateDirectory(dir); } catch { }
            return Path.Combine(dir, fileName);
        }

        public static void CompareToBaseline(string baselinePath, float threshold = 0.95f, Rect? region = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var screenTex = CaptureScreenRegion(region);

            if (!File.Exists(baselinePath))
            {
                RealPlayLog.Warn($"Baseline not found at {baselinePath}. Saving current screen as baseline.");
                File.WriteAllBytes(baselinePath, screenTex.EncodeToPNG());
                UnityEngine.Object.Destroy(screenTex);
                return;
            }

            if (!TryLoadBaseline(baselinePath, out var baselineTex))
            {
                UnityEngine.Object.Destroy(screenTex);
                return;
            }
            
            float similarity = ComputeSimilarity(screenTex, baselineTex);
            
            UnityEngine.Object.Destroy(screenTex);
            UnityEngine.Object.Destroy(baselineTex);

            HandleComparisonResult(similarity, threshold, baselinePath);
        }

        private static Texture2D CaptureScreenRegion(Rect? region = null)
        {
            Rect r = region ?? new Rect(0, 0, Screen.width, Screen.height);
            var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false);
            tex.ReadPixels(r, 0, 0);
            tex.Apply();
            return tex;
        }

        private static bool TryLoadBaseline(string path, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                RealPlayLog.Warn("Baseline not found: " + path);
                return false;
            }

            var bytes = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(bytes))
            {
                RealPlayLog.Warn("Failed to load baseline: " + path);
                UnityEngine.Object.Destroy(texture);
                return false;
            }
            return true;
        }


        private static void HandleComparisonResult(float similarity, float threshold, string path)
        {
            if (similarity < threshold)
            {
                Assert.Fail($"Visual regression detected. Similarity {similarity:F2} below threshold {threshold:F2}.");
            }
            else
            {
                RealPlayLog.Info($"Visual compare passed. Similarity {similarity:F2} (threshold {threshold:F2}).");
            }
        }

        /// <summary>
        /// Start recording video (stub). Integrate Unity Recorder if needed.
        /// </summary>
        public static void StartRecording()
        {
            RealPlayLog.Warn("StartRecording is not implemented. Integrate Unity Recorder for full video capture.");
        }

        /// <summary>
        /// Stop recording video (stub). Integrate Unity Recorder if needed.
        /// </summary>
        public static void StopRecording(string outputPath = null)
        {
            RealPlayLog.Warn("StopRecording is not implemented. Integrate Unity Recorder for full video capture.");
        }

        private static float ComputeSimilarity(Texture2D a, Texture2D b)
        {
            int width = Mathf.Min(a.width, b.width);
            int height = Mathf.Min(a.height, b.height);
            int step = Mathf.Max(1, Mathf.Max(width, height) / 256); // sample grid to cap comparisons

            double total = 0;
            double matches = 0;
            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    Color ca = a.GetPixel(x, y);
                    Color cb = b.GetPixel(x, y);
                    float diff = Mathf.Abs(ca.r - cb.r) +
                                 Mathf.Abs(ca.g - cb.g) +
                                 Mathf.Abs(ca.b - cb.b);
                    if (diff / 3f < 0.05f)
                    {
                        matches++;
                    }
                    total++;
                }
            }

            return total > 0 ? (float)(matches / total) : 0f;
        }
    }
}
