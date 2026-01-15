using System;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Assert
{
    public static partial class Assert
    {
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

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                CheckRendererVisibility(go, renderer, message);
                return;
            }

            var canvasRenderer = go.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                CheckCanvasRendererVisibility(go, canvasRenderer, message);
                return;
            }
        }

        private static void CheckRendererVisibility(GameObject go, Renderer renderer, string message)
        {
            if (!renderer.enabled) Fail(message ?? $"Expected '{go.name}' renderer to be enabled.");
            if (!renderer.isVisible && !Application.isBatchMode) Fail(message ?? $"Expected '{go.name}' to be visible to a camera.");
            
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 direction = renderer.bounds.center - cam.transform.position;
            float distance = direction.magnitude;
            if (Physics.Raycast(cam.transform.position, direction, out RaycastHit hit, distance))
            {
                if (hit.transform != go.transform && !hit.transform.IsChildOf(go.transform))
                {
                    Fail(message ?? $"Expected '{go.name}' to be visible, but it is occluded by '{hit.transform.name}'.");
                }
            }
        }

        private static void CheckCanvasRendererVisibility(GameObject go, CanvasRenderer canvasRenderer, string message)
        {
            if (canvasRenderer.cull) Fail(message ?? $"Expected '{go.name}' UI element to not be culled.");
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
                try { go = GameObject.FindWithTag(elementName); } catch { }
            }

            if (go == null)
            {
                Fail(message ?? $"Could not find element with name or tag '{elementName}'.");
                return;
            }

            IsVisible(go, message);
        }

        public static void VisualStateMatches(string expectedStateName, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            bool match = RealPlayTester.Core.Screenshot.CompareToBaselineInternal(expectedStateName, null);
            if (!match)
            {
                Fail(message ?? $"Visual state '{expectedStateName}' does not match baseline.");
            }
        }

        public static void VisualStateMatches(string expectedStateName, Rect region, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            bool match = RealPlayTester.Core.Screenshot.CompareToBaselineInternal(expectedStateName, region);
            if (!match)
            {
                Fail(message ?? $"Visual state '{expectedStateName}' in region {region} does not match baseline.");
            }
        }

        public static void TextureWithinLimits(string assetPath, int maxWidth, int maxHeight, string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var texture = Resources.Load<Texture>(assetPath);
            if (texture == null)
            {
                Fail(message ?? $"Texture not found at path '{assetPath}'");
                return;
            }

            if (texture.width > maxWidth || texture.height > maxHeight)
            {
                Fail(message ?? $"Texture '{assetPath}' dimensions ({texture.width}x{texture.height}) exceed limits ({maxWidth}x{maxHeight}).");
            }
        }

        public static void NoMissingMaterials(string message = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null)
                    {
                        Fail(message ?? $"Renderer '{r.name}' has a null material.");
                        return;
                    }
                    if (mat.shader.name == "Hidden/InternalErrorShader" || mat.name.Contains("Default-Material"))
                    {
                         Fail(message ?? $"Renderer '{r.name}' has a missing/error material.");
                         return;
                    }
                }
            }

            var graphics = UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None);
            foreach (var g in graphics)
            {
                if (g.material == null) continue;
                if (g.material.shader.name == "Hidden/InternalErrorShader")
                {
                     Fail(message ?? $"UI Element '{g.name}' has a missing/error material.");
                     return;
                }
            }
        }
    }
}
