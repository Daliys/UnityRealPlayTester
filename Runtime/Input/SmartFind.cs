using System;
using UnityEngine;
using UnityEngine.UI;
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

            var exact = GameObject.Find(fuzzyName);
            if (exact != null) return exact;

            return ScanForMatch(fuzzyName);
        }

        private static GameObject ScanForMatch(string fuzzyName)
        {
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject partialMatch = null;

            foreach (var go in all)
            {
                if (go.hideFlags != HideFlags.None || !go.scene.IsValid()) continue;

                string name = go.name;
                if (string.Equals(name, fuzzyName, StringComparison.OrdinalIgnoreCase))
                {
                    var prioritized = HandleScrollingPriority(go, fuzzyName, true);
                    if (prioritized != null) return prioritized;
                    partialMatch = go;
                }
                
                if (partialMatch == null && name.IndexOf(fuzzyName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var prioritized = HandleScrollingPriority(go, fuzzyName, false);
                    if (prioritized != null) return prioritized;
                    partialMatch = go;
                }
            }
            return partialMatch;
        }

        private static GameObject HandleScrollingPriority(GameObject go, string query, bool isExact)
        {
            bool isScrollingQuery = query.IndexOf("Scroll", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isScrollingQuery) return isExact && go.activeInHierarchy ? go : null;

            if (go.GetComponent<ScrollRect>() != null) return go;
            
            string name = go.name;
            if (name.IndexOf("bar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return null;
            }

            return isExact && go.activeInHierarchy ? go : null;
        }

    }
}
