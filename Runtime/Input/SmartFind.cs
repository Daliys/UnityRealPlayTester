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
        private static Camera _cachedMainCam;
        private static int _lastCamFrame = -1;

        private struct Candidate
        {
            public GameObject go;
            public float score;
        }

        public static GameObject Object(string query)
        {
            SimulatedInputGuard.EnsureMainThread();
            if (string.IsNullOrEmpty(query)) return null;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Try tag match first (fast)
            try { 
                var tagged = GameObject.FindWithTag(query);
                if (tagged != null) return tagged;
            } catch { }

            var result = ScanAndRank(query);
            if (result == null)
            {
                SceneCache.Instance.ForceRefresh();
                result = ScanAndRank(query);
            }

            return result;
        }

        private static Camera GetMainCamera()
        {
            if (Time.frameCount != _lastCamFrame)
            {
                _cachedMainCam = Camera.main;
                _lastCamFrame = Time.frameCount;
            }
            return _cachedMainCam;
        }

        private static GameObject ScanAndRank(string fuzzyName)
        {
            var initialCandidates = GetInitialCandidates(fuzzyName);
            if (initialCandidates.Count == 0) return null;

            var topCandidates = initialCandidates.OrderByDescending(c => c.score).Take(10).ToList();
            var cam = GetMainCamera();
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < topCandidates.Count; i++)
            {
                var c = topCandidates[i];
                if (c.go == null) continue;
                try
                {
                    c.score += CalculateSpatialPriority(c.go, cam);
                    topCandidates[i] = c;
                }
                catch (UnityEngine.MissingReferenceException) { topCandidates.RemoveAt(i--); }
            }

            if (topCandidates.Count == 0) return null;
            return PickBestCandidate(topCandidates);
        }

        private static List<Candidate> GetInitialCandidates(string fuzzyName)
        {
            var all = SceneCache.Instance.AllActiveObjects;
            var candidates = new List<Candidate>();
            foreach (var go in all)
            {
                if (go == null) continue;
                try
                {
                    float matchScore = GetMatchScore(go.name, fuzzyName);
                    if (matchScore > 0) candidates.Add(new Candidate { go = go, score = matchScore });
                }
                catch (UnityEngine.MissingReferenceException) { }
            }
            return candidates;
        }

        private static GameObject PickBestCandidate(List<Candidate> topCandidates)
        {
            return topCandidates.OrderByDescending(c => c.score)
                                .ThenByDescending(c => {
                                    try { return c.go.transform.GetSiblingIndex(); }
                                    catch { return 0; }
                                })
                                .First().go;
        }

        public static List<GameObject> FindAllInteractables()
        {
            var all = SceneCache.Instance.AllActiveObjects;
            var interactables = new List<GameObject>();
            foreach (var go in all)
            {
                if (go == null) continue;
                try
                {
                    var role = SemanticProbe.GetRoleName(go);
                    if (string.IsNullOrEmpty(role) || role == "View" || role == "Scene") continue;
                    if (!RealPlayTester.UI.PanelStateMonitor.IsPanelReady(go)) continue;
                    interactables.Add(go);
                }
                catch (UnityEngine.MissingReferenceException) { continue; }
            }
            return interactables;
        }

        private static float GetMatchScore(string name, string query)
        {
            if (string.IsNullOrEmpty(name)) return 0f;
            if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase)) return 5000f;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 1000f;
            return 0f;
        }

        private static float CalculateSpatialPriority(GameObject go, Camera cam)
        {
            if (go == null) return 0f;
            float score = 0f;

            try
            {
                // 1. Visibility Priority (HUGE BOOST)
                var occlusion = RealPlayOcclusionRaycaster.CheckOcclusion(go, false);
                if (!occlusion.IsOccluded) score += 10000f;

                // 2. Logical Interactivity (HUGE PENALTY if blocked)
                var logic = RealPlayTester.Utilities.InteractionLogic.CheckLogicalInteractivity(go);
                if (logic.IsBlocked) score -= 20000f;

                // 3. UI Layer Priority
                var canvas = go.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    score += 50000f; 
                    score += canvas.sortingOrder * 100f;
                }

                // 4. Screen Center Proximity (NEW)
                Vector2 screenPos = RealInputUtility.GetScreenCenter(go, cam, false);
                Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
                float screenDistNorm = Vector2.Distance(screenPos, center) / Mathf.Max(Screen.width, Screen.height);
                score += (1f - screenDistNorm) * 1000f;

                // 5. World Depth Priority
                if (cam != null)
                {
                    float dist = Vector3.Distance(cam.transform.position, go.transform.position);
                    score += Mathf.Max(0, 100f - dist);
                }
            }
            catch (UnityEngine.MissingReferenceException) { return -1000000f; }

            return score;
        }
    }
}