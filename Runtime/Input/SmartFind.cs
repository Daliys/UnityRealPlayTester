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

            // 1. Precise match (fastest) - only matches active objects by default
            var exact = GameObject.Find(fuzzyName);
            if (exact != null) return exact;

            // 2. Scan all objects, prioritizing active scene objects
            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject partialMatch = null;

            foreach (var go in all)
            {
                // Skip internal Unity objects and persistent assets (prefabs)
                if (go.hideFlags != HideFlags.None) continue;
                if (!go.scene.IsValid()) continue; 

                string name = go.name;
                
                // Case-insensitive exact match
                if (string.Equals(name, fuzzyName, StringComparison.OrdinalIgnoreCase))
                {
                    // If we are looking for "Scroll", prioritize things with ScrollRect
                    // And explicitly ignore common scrollbar children names
                    bool isScrollingQuery = fuzzyName.IndexOf("Scroll", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isScrollingQuery)
                    {
                        if (go.GetComponent<ScrollRect>() != null) return go;
                        if (name.IndexOf("bar", StringComparison.OrdinalIgnoreCase) >= 0) continue; // Skip "Scrollbar"
                    }

                    // Prioritize active ones
                    if (go.activeInHierarchy) return go;
                    // Otherwise keep as fallback
                    partialMatch = go;
                }
                
                // Partial match fallback
                if (partialMatch == null && name.IndexOf(fuzzyName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool isScrollingQuery = fuzzyName.IndexOf("Scroll", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isScrollingQuery)
                    {
                        // Highly prioritize ScrollRect for anything related to "Scroll"
                        if (go.GetComponent<ScrollRect>() != null) return go;
                        
                        // Ignore common scrollbar parts that might have "Scroll" or "Sliding" in name
                        if (name.IndexOf("bar", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (name.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (name.IndexOf("Area", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    }

                    partialMatch = go;
                }
            }

            return partialMatch;
        }

    }
}
