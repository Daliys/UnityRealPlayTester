using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using RealPlayTester.Utilities;

namespace RealPlayTester.Input
{
    /// <summary>
    /// AI-friendly object finder that uses fuzzy matching and spatial ranking to resolve Z-order ambiguity.
    /// </summary>
    public static class SmartFind
    {
        private struct Candidate
        {
            public GameObject go;
            public float score;
        }

        public static GameObject Object(string fuzzyName)
        {
            if (string.IsNullOrEmpty(fuzzyName)) return null;
            var result = ScanAndRank(fuzzyName);
            if (result == null)
            {
                SceneCache.Instance.ForceRefresh();
                result = ScanAndRank(fuzzyName);
            }
            return result;
        }

        private static GameObject ScanAndRank(string fuzzyName)
        {
            var all = SceneCache.Instance.AllActiveObjects;
            var candidates = new List<Candidate>();

            foreach (var go in all)
            {
                if (go == null) continue;
                
                float matchScore = GetMatchScore(go.name, fuzzyName);
                if (matchScore <= 0) continue;

                float priorityScore = matchScore + CalculateSpatialPriority(go);
                candidates.Add(new Candidate { go = go, score = priorityScore });
            }

            if (candidates.Count == 0) return null;

            // Sort by score descending and pick best.
            return candidates.OrderByDescending(c => c.score).ThenByDescending(c => c.go.GetInstanceID()).First().go;
        }

        private static float GetMatchScore(string name, string query)
        {
            if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase)) return 5000f;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 1000f;
            return 0f;
        }

        private static float CalculateSpatialPriority(GameObject go)
        {
            float score = 0f;

            // 1. Visibility Priority (HUGE BOOST)
            var occlusion = RealPlayOcclusionRaycaster.CheckOcclusion(go);
            if (!occlusion.IsOccluded) score += 10000f;

            // 2. Logical Interactivity (HUGE PENALTY if blocked)
            var logic = RealPlayTester.Utilities.InteractionLogic.CheckLogicalInteractivity(go);
            if (logic.IsBlocked) score -= 20000f;

            // 3. UI Layer Priority
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // UI objects are almost always prioritized over world objects
                score += 50000f; 
                score += canvas.sortingOrder * 100f;
                score += GetHierarchyDepth(go.transform) * 50f;
            }

            // 3. World Depth Priority
            if (Camera.main != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, go.transform.position);
                score += Mathf.Max(0, 100f - dist);
            }

            return score;
        }

        private static int GetHierarchyDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null) { depth++; t = t.parent; }
            return depth;
        }
    }
}