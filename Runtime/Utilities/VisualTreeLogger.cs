using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RealPlayTester.Input;
using RealPlayTester.Core;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Attach this to a GameObject to provide an explicit semantic role for AI agents.
    /// This overrides heuristic detection in SemanticProbe.
    /// </summary>
    public class RealPlayRoleAttribute : System.Attribute
    {
        public string RoleName { get; }
        public RealPlayRoleAttribute(string roleName) => RoleName = roleName;
    }

    /// <summary>
    /// Utility to dump the current scene hierarchy to a string for AI debugging.
    /// Optimized to group repetitive siblings (like tiles) to save tokens.
    /// </summary>
    public static class VisualTreeLogger
    {
        public static string DumpHierarchy()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Scene Hierarchy Dump ===");
            
            // O016: Update once before starting dump
            Canvas.ForceUpdateCanvases();

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                sb.AppendLine($"--- Scene: {scene.name} ---");
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    sb.Append(root.activeInHierarchy ? "[+] " : "[-] ");
                    sb.AppendLine(root.name);
                    DumpRecursive(root.transform, sb, 0, 50); // limit depth to 50
                }
            }
            
            return sb.ToString();
        }

        private static void DumpRecursive(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;

                if (RealPlaySettings.Perception.CollapseRepetitiveSiblings)
                {
                    int groupCount = HierarchyHelpers.GetRepetitiveGroupCount(t, i);
                    if (groupCount > 5)
                    {
                        string indent = new string(' ', depth * 2);
                        sb.AppendLine($"{indent}[...] {groupCount} similar '{HierarchyHelpers.GetBaseName(child.name)}' objects");
                        i += groupCount - 1;
                        continue;
                    }
                }

                AppendObjectLine(sb, child.gameObject, depth);
                DumpRecursive(child, sb, depth + 1, maxDepth);
            }
        }

        private static void AppendObjectLine(StringBuilder sb, GameObject go, int depth)
        {
            sb.Append(' ', (depth + 1) * 2);
            sb.Append(go.activeInHierarchy ? "[+] " : "[-] ");

            var roleName = SemanticProbe.GetRoleName(go);
            if (!string.IsNullOrEmpty(roleName)) sb.Append($"[{roleName}] ");
            sb.Append(go.name);

            if (!string.IsNullOrEmpty(roleName) && go.activeInHierarchy)
            {
                var pos = RealInputUtility.GetScreenCenter(go, null, false);
                sb.Append($" ({Mathf.RoundToInt(pos.x)}, {Mathf.RoundToInt(pos.y)})");
            }

            AppendComponentDetails(sb, go);
            sb.AppendLine();
        }

        private static void AppendComponentDetails(StringBuilder sb, GameObject go)
        {
            var btn = go.GetComponent<Button>();
            if (btn != null && !btn.interactable) sb.Append(" (Disabled)");

            var txt = go.GetComponent<UnityEngine.UI.Text>();
            if (txt != null) sb.Append($" [\"{Truncate(txt.text)}\"]");

            var tmp = go.GetComponent("TMPro.TMP_Text");
            if (tmp != null)
            {
                var textProp = tmp.GetType().GetProperty("text");
                if (textProp != null)
                {
                    var val = textProp.GetValue(tmp) as string;
                    sb.Append($" [\"{Truncate(val)}\"]");
                }
            }
        }

        private static string Truncate(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length > 20) return s.Substring(0, 20) + "...";
            return s;
        }
    }
}
