using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// AI-friendly object finder that uses fuzzy matching logic.
    /// </summary>
    public static class SmartFind
    {
        public static GameObject Object(string fuzzyName)
        {
            if (string.IsNullOrEmpty(fuzzyName)) return null;

            // 1. Precise match (fastest)
            var exact = GameObject.Find(fuzzyName);
            if (exact != null) return exact;

            // 2. Single-pass scan for both case-insensitive exact and partial matches
            Transform bestPartial = null;
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    var result = FindRecursiveCombined(root.transform, fuzzyName, ref bestPartial);
                    if (result != null) return result.gameObject; // Found exact (case-insensitive)
                }
            }

            // 3. Fallback to partial if found
            if (bestPartial != null)
            {
                RealPlayLog.Warn($"SmartFind: Exact match for '{fuzzyName}' not found. Using partial match: '{bestPartial.name}'");
                return bestPartial.gameObject;
            }

            return null;
        }

        private static Transform FindRecursiveCombined(Transform t, string target, ref Transform bestPartial)
        {
            // Check self
            string name = t.name;
            if (string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
            {
                return t; // Found exact (case-insensitive)
            }
            
            // Track best partial (first one found is usually best in hierarchy order)
            if (bestPartial == null && name.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPartial = t;
            }

            // Recurse
            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var result = FindRecursiveCombined(t.GetChild(i), target, ref bestPartial);
                if (result != null) return result;
            }

            return null;
        }

        private static bool CheckMatch(string name, string target, bool contains)
        {
            if (contains)
            {
                return name.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            else
            {
                return string.Equals(name, target, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
