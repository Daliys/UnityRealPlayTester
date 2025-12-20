using System;
using System.IO;
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
        /// </summary>
        public static string Screenshot(string name = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return string.Empty;
            }

            string fileName = string.IsNullOrEmpty(name)
                ? DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                : name + ".png";

            string dir = Path.Combine(RealPlayEnvironment.TestReportsPath, CaptureFolder);
            try { Directory.CreateDirectory(dir); } catch { }

            string path = Path.Combine(dir, fileName);

            // Synchronous capture
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                RealPlayLog.Info("Captured screenshot: " + path);
                return path;
            }
            finally
            {
                UnityEngine.Object.Destroy(tex);
            }
        }

        /// <summary>
        /// Compare the current screen to a baseline image and fail if similarity is below threshold.
        /// </summary>
        public static void CompareToBaseline(string baselinePath, float threshold = 0.95f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(baselinePath) || !File.Exists(baselinePath))
            {
                RealPlayLog.Warn("Baseline not found for comparison: " + baselinePath);
                return;
            }

            // Capture current frame synchronously.
            var screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenTex.Apply();

            var baselineBytes = File.ReadAllBytes(baselinePath);
            var baselineTex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!baselineTex.LoadImage(baselineBytes))
            {
                RealPlayLog.Warn("Failed to load baseline image: " + baselinePath);
                UnityEngine.Object.Destroy(screenTex);
                UnityEngine.Object.Destroy(baselineTex);
                return;
            }

            float similarity = ComputeSimilarity(screenTex, baselineTex);
            UnityEngine.Object.Destroy(screenTex);
            UnityEngine.Object.Destroy(baselineTex);

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
