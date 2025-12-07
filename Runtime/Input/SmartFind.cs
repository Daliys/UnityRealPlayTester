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

            // 1. Precise match
            var exact = GameObject.Find(fuzzyName);
            if (exact != null) return exact;

            // 2. Scan all active objects for case-insensitive match
            // Iterate roots to be efficient
             var roots = SceneManager.GetActiveScene().GetRootGameObjects();
             foreach (var root in roots)
             {
                 var match = FindRecursive(root.transform, fuzzyName, false);
                 if (match != null) return match.gameObject;
             }

             // 3. Scan for contains match
             foreach (var root in roots)
             {
                 var match = FindRecursive(root.transform, fuzzyName, true);
                 if (match != null)
                 {
                     RealPlayLog.Warn($"SmartFind: Exact match for '{fuzzyName}' not found. Using partial match: '{match.name}'");
                     return match.gameObject;
                 }
             }

             return null;
        }

        private static Transform FindRecursive(Transform t, string target, bool contains)
        {
            if (CheckMatch(t.name, target, contains))
            {
                return t;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                var result = FindRecursive(t.GetChild(i), target, contains);
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
