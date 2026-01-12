using System;
using System.IO;
using System.Collections.Generic;
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

        public static Texture2D CaptureWithAnnotations(List<GameObject> targets, out Dictionary<string, string> labelMap)
        {
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null)
            {
                labelMap = new Dictionary<string, string>();
                return new Texture2D(2, 2);
            }

            labelMap = DrawAnnotations(tex, targets);
            return tex;
        }

        private static Dictionary<string, string> DrawAnnotations(Texture2D tex, List<GameObject> targets)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            int counter = 1;
            char series = 'A';

            foreach (var go in targets)
            {
                if (go == null) continue;

                Rect rect = GetScreenRect(go);
                if (rect.width <= 0 || rect.height <= 0) continue;

                string label = $"#{series}{counter}";
                map[label] = go.name;

                DrawBox(tex, rect, Color.yellow);

                // Draw Label Top-Left of Box
                DrawText(tex, (int)rect.x, (int)rect.yMax - 8, label, Color.red);

                counter++;
                if (counter > 9)
                {
                    counter = 1;
                    series++;
                }
            }
            tex.Apply();
            return map;
        }

        private static void DrawBox(Texture2D tex, Rect rect, Color color)
        {
            int xMin = Mathf.Clamp((int)rect.x, 0, tex.width - 1);
            int yMin = Mathf.Clamp((int)rect.y, 0, tex.height - 1);
            int xMax = Mathf.Clamp((int)rect.xMax, 0, tex.width - 1);
            int yMax = Mathf.Clamp((int)rect.yMax, 0, tex.height - 1);

            for (int x = xMin; x <= xMax; x++)
            {
                tex.SetPixel(x, yMin, color);
                tex.SetPixel(x, yMax, color);
            }
            for (int y = yMin; y <= yMax; y++)
            {
                tex.SetPixel(xMin, y, color);
                tex.SetPixel(xMax, y, color);
            }
        }

        private static void DrawText(Texture2D tex, int x, int y, string text, Color color)
        {
            int charWidth = 3;
            int charHeight = 5;
            int spacing = 1;

            int currentX = x;
            foreach (char c in text)
            {
                bool[,] bitmap = GetCharBitmap(c);
                for (int r = 0; r < charHeight; r++)
                {
                    for (int cCol = 0; cCol < charWidth; cCol++)
                    {
                        // Flip Y for bitmap reading vs texture writing
                        // bitmap[0,0] is top-left, we want to write it at y+height-r?
                        // Let's assume bitmap[0,0] is top-left. Texture y is bottom-up.
                        // So row 0 of bitmap goes to y + charHeight - 1.

                        if (bitmap[r, cCol])
                        {
                            int px = currentX + cCol;
                            int py = y + (charHeight - 1 - r);

                            if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            {
                                tex.SetPixel(px, py, color);
                            }
                        }
                    }
                }
                currentX += charWidth + spacing;
            }
        }

        private static bool[,] GetCharBitmap(char c)
        {
            // 3x5 Bitmap Font
            bool[,] b = new bool[5, 3];

            switch (char.ToUpperInvariant(c))
            {
                case '#': return new bool[,] { {false,true,false}, {true,true,true}, {false,true,false}, {true,true,true}, {false,true,false} };
                case '0': return new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,true} };
                case '1': return new bool[,] { {false,true,false}, {true,true,false}, {false,true,false}, {false,true,false}, {true,true,true} };
                case '2': return new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {true,false,false}, {true,true,true} };
                case '3': return new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {false,false,true}, {true,true,true} };
                case '4': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {false,false,true}, {false,false,true} };
                case '5': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {false,false,true}, {true,true,true} };
                case '6': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {true,false,true}, {true,true,true} };
                case '7': return new bool[,] { {true,true,true}, {false,false,true}, {false,true,false}, {false,true,false}, {false,true,false} };
                case '8': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {true,false,true}, {true,true,true} };
                case '9': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {false,false,true}, {true,true,true} };
                case 'A': return new bool[,] { {false,true,false}, {true,false,true}, {true,true,true}, {true,false,true}, {true,false,true} };
                case 'B': return new bool[,] { {true,true,false}, {true,false,true}, {true,true,false}, {true,false,true}, {true,true,false} };
                // Limited set for now (A-Z requires full map, but we only strictly use A-Z for series and 0-9 for count)
                // Assuming we won't go past 'Z'.
                default:
                    // Fallback block
                    return new bool[,] { {true,true,true}, {true,true,true}, {true,true,true}, {true,true,true}, {true,true,true} };
            }
        }

        private static Rect GetScreenRect(GameObject go)
        {
             // Reusing the same logic as SemanticDOMDumper by checking components
             // Duplication accepted for now to keep classes decoupled without a shared "PerceptionUtility"
             // (though ideally we would move this there).

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector3[] corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    float minX = Mathf.Min(corners[0].x, corners[2].x);
                    float minY = Mathf.Min(corners[0].y, corners[2].y);
                    float maxX = Mathf.Max(corners[0].x, corners[2].x);
                    float maxY = Mathf.Max(corners[0].y, corners[2].y);
                    return new Rect(minX, minY, maxX - minX, maxY - minY);
                }

                var cam = Camera.main;
                if (cam == null) return Rect.zero;
                 Vector3[] c = new Vector3[4];
                rt.GetWorldCorners(c);
                float xMin = float.MaxValue, yMin = float.MaxValue;
                float xMax = float.MinValue, yMax = float.MinValue;
                foreach (var corner in c)
                {
                    Vector2 screenPos = cam.WorldToScreenPoint(corner);
                    xMin = Mathf.Min(xMin, screenPos.x);
                    yMin = Mathf.Min(yMin, screenPos.y);
                    xMax = Mathf.Max(xMax, screenPos.x);
                    yMax = Mathf.Max(yMax, screenPos.y);
                }
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
            else
            {
                var renderer = go.GetComponent<Renderer>();
                if(renderer == null) return Rect.zero;
                var cam = Camera.main;
                if (cam == null) return Rect.zero;

                var bounds = renderer.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                Vector3[] corners = new Vector3[8] {
                    new Vector3(min.x, min.y, min.z), new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z),
                };
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                foreach (var corner in corners)
                {
                    Vector2 screenPos = cam.WorldToScreenPoint(corner);
                    minX = Mathf.Min(minX, screenPos.x);
                    minY = Mathf.Min(minY, screenPos.y);
                    maxX = Mathf.Max(maxX, screenPos.x);
                    maxY = Mathf.Max(maxY, screenPos.y);
                }
                return new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        internal static bool CompareToBaselineInternal(string testName, Rect? region = null, float tolerance = DefaultTolerance)
        {
            Texture2D actual = CaptureScreenshotWithRegion(region);
            string baselinePath = Path.Combine(RealPlayEnvironment.ProjectRoot, BaselineFolder, testName + ".png");

            if (!File.Exists(baselinePath)) return HandleMissingBaseline(testName, actual, baselinePath);

            Texture2D baseline = LoadTexture(baselinePath);
            if (baseline == null)
            {
                RealPlayLog.Error($"Failed to load baseline texture from {baselinePath}");
                return false;
            }

            bool match = CompareTextures(baseline, actual, tolerance, out float difference);
            if (!match) HandleMismatch(testName, actual, difference, tolerance);

            return match;
        }

        private static Texture2D CaptureScreenshotWithRegion(Rect? region)
        {
            Texture2D fullScreen = ScreenCapture.CaptureScreenshotAsTexture();
            if (!region.HasValue) return fullScreen;

            Texture2D actual = CropTexture(fullScreen, region.Value);
            if (actual != fullScreen) UnityEngine.Object.Destroy(fullScreen);
            return actual;
        }

        private static bool HandleMissingBaseline(string testName, Texture2D actual, string baselinePath)
        {
            RealPlayLog.Warn($"Baseline not found for '{testName}' at {baselinePath}. Saving current as baseline.");
            SaveTexture(actual, baselinePath);
            return true;
        }

        private static void HandleMismatch(string testName, Texture2D actual, float difference, float tolerance)
        {
            string failureDir = Path.Combine(RealPlayEnvironment.TestReportsPath, FailureFolder);
            Directory.CreateDirectory(failureDir);
            SaveTexture(actual, Path.Combine(failureDir, testName + "_Actual.png"));
            RealPlayLog.Error($"Screenshot mismatch for '{testName}'. Difference: {difference:P2} (Tolerance: {tolerance:P2})");
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