using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core.Perception
{
    public class StateSnapshot
    {
        public Dictionary<string, string> TextValues = new Dictionary<string, string>();
        public Dictionary<string, bool> ToggleStates = new Dictionary<string, bool>();
        public Dictionary<string, float> SliderValues = new Dictionary<string, float>();
        public Dictionary<string, bool> ActiveStates = new Dictionary<string, bool>();
    }

    public static class StateDiffer
    {
        public static StateSnapshot CaptureState()
        {
            var snapshot = new StateSnapshot();

            // Capture Text
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (var t in texts) snapshot.TextValues[GetPath(t.transform)] = t.text;

            // Capture TMPro (Reflection to avoid hard dep)
            var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmps = UnityEngine.Object.FindObjectsByType(tmpType, FindObjectsSortMode.None);
                var textProp = tmpType.GetProperty("text");
                foreach (var t in tmps)
                {
                     var str = textProp.GetValue(t) as string;
                     snapshot.TextValues[GetPath(((Component)t).transform)] = str;
                }
            }

            // Capture Toggles
            var toggles = UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None);
            foreach (var t in toggles) snapshot.ToggleStates[GetPath(t.transform)] = t.isOn;

            // Capture Sliders
            var sliders = UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
            foreach (var s in sliders) snapshot.SliderValues[GetPath(s.transform)] = s.value;

            return snapshot;
        }

        public static StateSnapshot CaptureActiveState(StateSnapshot snapshot)
        {
            // Supplement with active hierarchy paths
             var transforms = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
             foreach(var t in transforms)
             {
                 snapshot.ActiveStates[GetPath(t)] = t.gameObject.activeInHierarchy;
             }
             return snapshot;
        }

        public static string GetDiff(StateSnapshot before, StateSnapshot after, string actionDescription)
        {
            var sb = new StringBuilder();
            sb.Append($"Action: {actionDescription} -> Result: ");
            bool changed = false;

            // Diff Texts
            foreach (var kvp in before.TextValues)
            {
                if (after.TextValues.TryGetValue(kvp.Key, out string newVal))
                {
                    if (kvp.Value != newVal)
                    {
                        sb.Append($"Text '{GetShortName(kvp.Key)}' changed to '{newVal}', ");
                        changed = true;
                    }
                }
            }
            // Check for new texts
             foreach (var kvp in after.TextValues)
            {
                if (!before.TextValues.ContainsKey(kvp.Key))
                {
                     sb.Append($"Text '{GetShortName(kvp.Key)}' appeared ('{kvp.Value}'), ");
                     changed = true;
                }
            }

            // Diff Toggles
            foreach (var kvp in before.ToggleStates)
            {
                if (after.ToggleStates.TryGetValue(kvp.Key, out bool newVal))
                {
                    if (kvp.Value != newVal)
                    {
                        sb.Append($"Toggle '{GetShortName(kvp.Key)}' set to {newVal}, ");
                        changed = true;
                    }
                }
            }

            // Diff Sliders
            foreach (var kvp in before.SliderValues)
            {
                if (after.SliderValues.TryGetValue(kvp.Key, out float newVal))
                {
                    if (Mathf.Abs(kvp.Value - newVal) > 0.01f)
                    {
                        sb.Append($"Slider '{GetShortName(kvp.Key)}' changed {kvp.Value:F1}->{newVal:F1}, ");
                        changed = true;
                    }
                }
            }

            // Diff Active (Appeared)
            foreach (var kvp in after.ActiveStates)
            {
                if (kvp.Value && (!before.ActiveStates.ContainsKey(kvp.Key) || !before.ActiveStates[kvp.Key]))
                {
                    // Item became active
                    sb.Append($"'{GetShortName(kvp.Key)}' appeared, ");
                    changed = true;
                }
            }

            // Diff Active (Disappeared)
            foreach (var kvp in before.ActiveStates)
            {
                if (kvp.Value && (!after.ActiveStates.ContainsKey(kvp.Key) || !after.ActiveStates[kvp.Key]))
                {
                    // Item became inactive or missing from snapshot (destroyed/disabled)
                    sb.Append($"'{GetShortName(kvp.Key)}' disappeared, ");
                    changed = true;
                }
            }

            if (!changed) return "No observable state change.";

            string result = sb.ToString();
            if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);
            return result + ".";
        }

        private static string GetPath(Transform t)
        {
            if (t.parent == null) return $"{t.name}";
            // Include sibling index to handle duplicate names
            return $"{GetPath(t.parent)}/{t.name}:{t.GetSiblingIndex()}";
        }

        private static string GetShortName(string path)
        {
            var parts = path.Split('/');
            var last = parts[parts.Length - 1];
            // Remove the sibling index for display if desired, or keep it for precision.
            // Let's remove it for cleaner NL output unless it's critical.
            int idx = last.LastIndexOf(':');
            if (idx > 0) return last.Substring(0, idx);
            return last;
        }
    }
}