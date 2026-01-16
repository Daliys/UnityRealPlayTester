using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using RealPlayTester.Input;

namespace RealPlayTester.Core.Perception
{
    /// <summary>
    /// Captures visual snapshots. Includes a 'Mock Render' fallback for headless environments.
    /// </summary>
    public static class RealPlayComputerVisionInterface
    {
        public static async Task CaptureVisionSnapshot(string fileName)
        {
            await Task.Yield();

            int width = Mathf.Max(Screen.width, 1024);
            int height = Mathf.Max(Screen.height, 768);
            
            RenderTexture rt = new RenderTexture(width, height, 24);
            if (!rt.Create())
            {
                Debug.LogWarning("[ComputerVision] RenderTexture.Create failed. Falling back to Mock DOM Render.");
                GenerateMockSnapshotFromDOM(fileName, width, height);
                if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
                return;
            }

            Texture2D tex = RenderCameraToTexture(rt, width, height);
            if (tex != null)
            {
                DrawAnnotationsInternal(tex);
                DrawGrid(tex);
                SaveTextureToDisk(tex, fileName);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            else
            {
                Debug.LogWarning("[ComputerVision] No main camera found. Falling back to Mock DOM Render.");
                GenerateMockSnapshotFromDOM(fileName, width, height);
            }
            
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void DrawAnnotationsInternal(Texture2D tex)
        {
            var actionable = RealPlayTester.Input.SmartFind.FindAllInteractables();
            int id = 1; char series = 'A';
            foreach(var go in actionable)
            {
                if (go == null) continue;
                Rect r = RealInputUtility.GetScreenRect(go);
                if (r.width <= 0) continue;
                
                string label = $"#{series}{id}";
                Screenshot.DrawBox(tex, r, Color.yellow);
                Screenshot.DrawText(new Screenshot.TextParams { Tex = tex, X = (int)r.x, Y = (int)r.yMax, Text = label, Color = Color.red, Scale = 2 });
                if (++id > 9) { id = 1; series++; }
            }
        }

        public static void GenerateMockSnapshotFromDOM(string fileName, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            ClearTexture(tex, Color.black);

            string domJson = RealPlaySemanticDOMDumper.Dump();
            var root = JsonUtility.FromJson<SemanticNode>(domJson);

            if (root != null)
            {
                DrawNodeRecursive(tex, root, width, height);
            }

            DrawGrid(tex);
            SaveTextureToDisk(tex, fileName);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ClearTexture(Texture2D tex, Color color)
        {
            var cols = tex.GetPixels();
            for (int i = 0; i < cols.Length; i++) cols[i] = color;
            tex.SetPixels(cols);
        }

        private static void DrawNodeRecursive(Texture2D tex, SemanticNode node, int screenW, int screenH)
        {
            if (node.Active && node.ScreenRect != Rect.zero)
            {
                Color boxColor = GetRoleColor(node.Role);
                DrawWireRect(tex, node.ScreenRect, boxColor, screenH);
            }

            if (node.Children != null)
            {
                foreach (var child in node.Children) DrawNodeRecursive(tex, child, screenW, screenH);
            }
        }

        public static void DrawWireRect(Texture2D tex, Rect rect, Color color, int screenH)
        {
            int xMin = Mathf.Clamp((int)rect.x, 0, tex.width - 1);
            int xMax = Mathf.Clamp((int)(rect.x + rect.width), 0, tex.width - 1);
            int yMin = Mathf.Clamp(screenH - (int)(rect.y + rect.height), 0, tex.height - 1);
            int yMax = Mathf.Clamp(screenH - (int)rect.y, 0, tex.height - 1);

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

        public static Color GetRoleColor(string role)
        {
            switch (role)
            {
                case "Button": return Color.green;
                case "Input": return Color.blue;
                case "Universe": return Color.grey;
                case "Scene": return Color.cyan;
                default: return Color.white;
            }
        }

        private static Texture2D RenderCameraToTexture(RenderTexture rt, int width, int height)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                // Fallback handled by caller if this returns null
                return null;
            }

            RenderTexture prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = null;
            return tex;
        }

        private static void SaveTextureToDisk(Texture2D tex, string fileName)
        {
            byte[] bytes = tex.EncodeToPNG();
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[ComputerVision] {(Application.isBatchMode ? "MOCK " : "")}Screenshot saved to: {path}");
        }

        private static void DrawGrid(Texture2D tex)
        {
            Color gridColor = new Color(1, 0, 0, 0.5f);
            int gridCount = 10;
            int stepX = tex.width / gridCount;
            int stepY = tex.height / gridCount;

            for (int x = 0; x < tex.width; x += stepX)
            {
                for (int y = 0; y < tex.height; y++) tex.SetPixel(x, y, gridColor);
            }

            for (int y = 0; y < tex.height; y += stepY)
            {
                for (int x = 0; x < tex.width; x++) tex.SetPixel(x, y, gridColor);
            }
            tex.Apply();
        }
    }
}