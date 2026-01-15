using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Captures lightweight snapshots of the UI state to track changes over time.
    /// Helps AI agents understand the consequences of their actions.
    /// </summary>
    public static class StateTracker
    {
        private static readonly object _lock = new object();

        [Serializable]
        public class StateNode
        {
            public string Name;
            public string Path;
            public string Role;
            public bool IsActive;
            public string Text;
            public bool ToggleValue;
            public float SliderValue;
            public Vector3 Position;
            public Color Color;

            public string GetKey() => Path;
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
            lock (_lock)
            {
                var snap = new StateSnapshot { Timestamp = DateTime.Now };
                // PERFORMANCE FIX: Use SceneCache instead of FindObjectsByType
                var all = SceneCache.Instance.AllActiveObjects;
                foreach (var go in all)
                {
                    var node = CaptureNode(go);
                    if (node != null)
                    {
                        snap.Nodes.Add(node);
                    }
                }
                return snap;
            }
        }

        private static StateNode CaptureNode(GameObject go)
        {
            var roleName = SemanticProbe.GetRoleName(go);
            if (string.IsNullOrEmpty(roleName) && go.transform.parent != null)
            {
                return null;
            }

            var node = new StateNode
            {
                Name = go.name,
                Path = GetPath(go.transform),
                Role = roleName,
                IsActive = go.activeInHierarchy,
                Position = go.transform.position
            };

            CaptureText(go, node);
            CaptureSelectables(go, node);
            CaptureVisuals(go, node);
            return node;
        }

        private static void CaptureText(GameObject go, StateNode node)
        {
            var txt = go.GetComponent<UnityEngine.UI.Text>();
            if (txt != null)
            {
                node.Text = txt.text;
            }
            else
            {
                var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
                var tmp = tmpType != null ? go.GetComponent(tmpType) : null;
                if (tmp != null)
                {
                    var prop = tmpType.GetProperty("text");
                    node.Text = prop?.GetValue(tmp) as string;
                }
            }
        }

        private static void CaptureSelectables(GameObject go, StateNode node)
        {
            var toggle = go.GetComponent<UnityEngine.UI.Toggle>();
            if (toggle != null)
            {
                node.ToggleValue = toggle.isOn;
            }

            var slider = go.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                node.SliderValue = slider.value;
            }

            // NEW: Support for more complex UI components
            var scrollRect = go.GetComponent<UnityEngine.UI.ScrollRect>();
            if (scrollRect != null) node.SliderValue = scrollRect.verticalNormalizedPosition;

            var dropdown = go.GetComponent<UnityEngine.UI.Dropdown>();
            if (dropdown != null) node.Text = dropdown.options[dropdown.value].text;

            var tmpDropdownType = Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
            if (tmpDropdownType != null)
            {
                var tmpDropdown = go.GetComponent(tmpDropdownType);
                if (tmpDropdown != null)
                {
                    var valProp = tmpDropdownType.GetProperty("value");
                    var optionsProp = tmpDropdownType.GetProperty("options");
                    int val = (int)(valProp?.GetValue(tmpDropdown) ?? 0);
                    var options = optionsProp?.GetValue(tmpDropdown) as System.Collections.IList;
                    if (options != null && val >= 0 && val < options.Count)
                    {
                        var textProp = options[val].GetType().GetProperty("text");
                        node.Text = textProp?.GetValue(options[val]) as string;
                    }
                }
            }
        }

        private static void CaptureVisuals(GameObject go, StateNode node)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                node.Color = r.sharedMaterial.color;
            }
            
            var g = go.GetComponent<Graphic>();
            if (g != null)
            {
                node.Color = g.color;
            }
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null) return $"{t.name}";
            return $"{GetPath(t.parent)}/{t.name}:{t.GetSiblingIndex()}";
        }

        private static string GetShortName(string path)
        {
            var parts = path.Split('/');
            var last = parts[parts.Length - 1];
            int idx = last.LastIndexOf(':');
            if (idx > 0) return last.Substring(0, idx);
            return last;
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
        /// Compares two snapshots and returns a human-readable summary of differences.
        /// </summary>
        public static string GetDiff(StateSnapshot before, StateSnapshot after, string actionDescription = null)
        {
            if (before == null || after == null) return "Unknown change (missing snapshot).";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(actionDescription))
            {
                sb.Append($"Action: {actionDescription} -> Result: ");
            }

            var beforeDict = before.Nodes.ToLookup(n => n.GetKey());
            var afterDict = after.Nodes.ToLookup(n => n.GetKey());

            var deltas = new List<string>();
            foreach (var node in after.Nodes)
            {
                var beforeNode = beforeDict[node.GetKey()].FirstOrDefault();
                if (beforeNode != null)
                {
                    AddDeltas(beforeNode, node, deltas);
                }
                else
                {
                    deltas.Add($"'{node.Name}' appeared");
                }
            }

            foreach (var node in before.Nodes)
            {
                if (!afterDict.Contains(node.GetKey()))
                {
                    deltas.Add($"'{node.Name}' disappeared");
                }
            }

            if (deltas.Count == 0) return "No observable state change.";

            sb.Append(string.Join(", ", deltas));
            return sb.ToString() + ".";
        }

        private static void AddDeltas(StateNode before, StateNode after, List<string> deltas)
        {
            if (before.IsActive != after.IsActive)
            {
                deltas.Add($"'{after.Name}' {(after.IsActive ? "appeared" : "hidden")}");
            }
            
            if (before.IsActive && after.IsActive)
            {
                if (before.Text != after.Text)
                    deltas.Add($"Text '{after.Name}' changed '{before.Text}'->'{after.Text}'");
                
                if (before.ToggleValue != after.ToggleValue)
                    deltas.Add($"Toggle '{after.Name}' changed {before.ToggleValue}->{after.ToggleValue}");
                
                if (Mathf.Abs(before.SliderValue - after.SliderValue) > 0.01f)
                    deltas.Add($"Slider '{after.Name}' changed {before.SliderValue:F2}->{after.SliderValue:F2}");

                if (Vector3.Distance(before.Position, after.Position) > 0.1f)
                    deltas.Add($"'{after.Name}' moved {before.Position:F1}->{after.Position:F1}");

                if (ColorDiff(before.Color, after.Color) > 0.1f)
                    deltas.Add($"'{after.Name}' color changed {ToHtml(before.Color)}->{ToHtml(after.Color)}");
            }
        }

        private static float ColorDiff(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
        private static string ToHtml(Color c) => $"#{ColorUtility.ToHtmlStringRGBA(c)}";

        public static string GetJsonDiff(string beforeJson, string afterJson)
        {
            if (beforeJson == afterJson) return string.Empty;
            var beforeLines = beforeJson.Split('\n');
            var afterLines = afterJson.Split('\n');
            var diff = new StringBuilder();
            int max = Math.Max(beforeLines.Length, afterLines.Length);
            for (int i = 0; i < max; i++)
            {
                string b = i < beforeLines.Length ? beforeLines[i].Trim() : null;
                string a = i < afterLines.Length ? afterLines[i].Trim() : null;
                if (b != a) diff.AppendLine($"Line {i + 1}: Expected '{b ?? "EOF"}', But was '{a ?? "EOF"}'");
            }
            return diff.ToString();
        }
    }
}
