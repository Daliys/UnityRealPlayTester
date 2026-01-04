using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Captures lightweight snapshots of the UI state to track changes over time.
    /// Helps AI agents understand the consequences of their actions.
    /// </summary>
    public static class StateTracker
    {
        [Serializable]
        public class StateNode
        {
            public string Name;
            public string Role;
            public bool IsActive;

            public string GetKey() => $"{Name}|{Role}";
        }

        [Serializable]
        public class StateSnapshot
        {
            public DateTime Timestamp;
            public List<StateNode> Nodes = new List<StateNode>();

            public override string ToString() => $"Snapshot with {Nodes.Count} nodes at {Timestamp:HH:mm:ss}";
        }

        /// <summary>
        /// Captures the current state of all semantically relevant objects in the scene.
        /// </summary>
        public static StateSnapshot Capture()
        {
            var snap = new StateSnapshot { Timestamp = DateTime.Now };
            
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
            {
                var roleName = SemanticProbe.GetRoleName(go);
                if (!string.IsNullOrEmpty(roleName) || go.transform.parent == null) // Include roots and semantic items
                {
                    snap.Nodes.Add(new StateNode
                    {
                        Name = go.name,
                        Role = roleName,
                        IsActive = go.activeInHierarchy
                    });
                }
            }
            
            return snap;
        }

        /// <summary>
        /// Captures the state of a specific object using JsonUtility.
        /// </summary>
        public static string CaptureObjectState<T>(T stateObject)
        {
            if (stateObject == null) return "null";
            return JsonUtility.ToJson(stateObject, true);
        }

        /// <summary>
        /// Compares two JSON strings and returns a diff if they are different.
        /// Note: This is a simple line-by-line diff for readability in logs.
        /// </summary>
        public static string GetJsonDiff(string beforeJson, string afterJson)
        {
            if (beforeJson == afterJson) return string.Empty;

            var beforeLines = beforeJson.Split('\n');
            var afterLines = afterJson.Split('\n');
            var diff = new System.Text.StringBuilder();

            int max = Math.Max(beforeLines.Length, afterLines.Length);
            for (int i = 0; i < max; i++)
            {
                string b = i < beforeLines.Length ? beforeLines[i].Trim() : null;
                string a = i < afterLines.Length ? afterLines[i].Trim() : null;

                if (b != a)
                {
                    diff.AppendLine($"Line {i + 1}: Expected '{b ?? "EOF"}', But was '{a ?? "EOF"}'");
                }
            }

            return diff.ToString();
        }

        /// <summary>
        /// Compares two snapshots and returns a human-readable summary of differences.
        /// </summary>
        public static string GetDiff(StateSnapshot before, StateSnapshot after)
        {
            if (before == null || after == null) return "Unknown change (missing snapshot).";

            var beforeDict = before.Nodes.ToLookup(n => n.GetKey());
            var afterDict = after.Nodes.ToLookup(n => n.GetKey());

            var added = after.Nodes.Where(n => !beforeDict.Contains(n.GetKey())).ToList();
            var removed = before.Nodes.Where(n => !afterDict.Contains(n.GetKey())).ToList();
            
            var visibilityChanged = new List<string>();
            foreach (var node in after.Nodes)
            {
                var beforeNode = beforeDict[node.GetKey()].FirstOrDefault();
                if (beforeNode != null && beforeNode.IsActive != node.IsActive)
                {
                    string status = node.IsActive ? "appeared" : "hidden";
                    visibilityChanged.Add($"{node.Name} ([{node.Role}]) {status}");
                }
            }

            if (added.Count == 0 && removed.Count == 0 && visibilityChanged.Count == 0)
                return "No state changes detected.";

            var results = new List<string>();
            if (added.Count > 0) results.Add($"{added.Count} added: {string.Join(", ", added.Take(3).Select(n => $"[{n.Role}] {n.Name}"))}{(added.Count > 3 ? "..." : "")}");
            if (removed.Count > 0) results.Add($"{removed.Count} removed: {string.Join(", ", removed.Take(3).Select(n => $"[{n.Role}] {n.Name}"))}{(removed.Count > 3 ? "..." : "")}");
            if (visibilityChanged.Count > 0) results.Add($"{string.Join(", ", visibilityChanged.Take(3))}{(visibilityChanged.Count > 3 ? "..." : "")}");

            return "Changes detected: " + string.Join(" | ", results);
        }
    }
}
