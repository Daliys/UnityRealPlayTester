using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RealPlayTester.Input;

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
            // ... (Existing logic remains)
            var sb = new StringBuilder();
            sb.AppendLine("=== Scene Hierarchy Dump ===");
            
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
                    DumpRecursive(root.transform, sb, 0, 15); // limit depth to 15
                }
            }
            
            return sb.ToString();
        }

        public static string DumpHierarchyJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"scenes\": [");
            
            int sceneCount = SceneManager.sceneCount;
            bool firstScene = true;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                if (!firstScene) sb.AppendLine(",");
                firstScene = false;

                sb.AppendLine("    {");
                sb.AppendLine($"      \"name\": \"{EscapeJson(scene.name)}\",");
                sb.AppendLine("      \"roots\": [");
                
                var roots = scene.GetRootGameObjects();
                bool firstRoot = true;
                foreach (var root in roots)
                {
                    if (!firstRoot) sb.AppendLine(",");
                    firstRoot = false;
                    DumpRecursiveJson(root.transform, sb, 3);
                }
                sb.AppendLine();
                sb.AppendLine("      ]");
                sb.Append("    }");
            }
            
            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void DumpRecursiveJson(Transform t, StringBuilder sb, int indent)
        {
            string spaces = new string(' ', indent * 2);
            var go = t.gameObject;
            var roleName = SemanticProbe.GetRoleName(go);
            var pos = RealInputUtility.GetScreenCenter(go);

            sb.AppendLine($"{spaces}{{");
            sb.AppendLine($"{spaces}  \"name\": \"{EscapeJson(go.name)}\",");
            if (!string.IsNullOrEmpty(roleName)) sb.AppendLine($"{spaces}  \"role\": \"{roleName}\",");
            sb.AppendLine($"{spaces}  \"active\": {go.activeInHierarchy.ToString().ToLower()},");
            sb.AppendLine($"{spaces}  \"pos\": {{ \"x\": {Mathf.RoundToInt(pos.x)}, \"y\": {Mathf.RoundToInt(pos.y)} }},");

            if (t.childCount > 0)
            {
                sb.AppendLine($"{spaces}  \"children\": [");
                for (int i = 0; i < t.childCount; i++)
                {
                    if (i > 0) sb.AppendLine(",");
                    DumpRecursiveJson(t.GetChild(i), sb, indent + 2);
                }
                sb.AppendLine();
                sb.AppendLine($"{spaces}  ]");
            }
            
            sb.Append($"{spaces}}}");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static void DumpRecursive(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                sb.Append(' ', (depth + 1) * 2).AppendLine("... (max depth reached)");
                return;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                int groupCount = GetRepetitiveGroupCount(t, i);
                if (groupCount > 5)
                {
                    sb.Append(' ', (depth + 1) * 2).AppendLine($"... ({groupCount} similar '{GetBaseName(child.name)}' objects)");
                    i += groupCount - 1;
                    continue;
                }

                AppendObjectLine(sb, child.gameObject, depth);
                if (child.childCount > 0) DumpRecursive(child, sb, depth + 1, maxDepth);
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
                var pos = RealInputUtility.GetScreenCenter(go);
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

        private static int GetRepetitiveGroupCount(Transform parent, int startIndex)
        {
            int count = 1;
            Transform first = parent.GetChild(startIndex);
            string baseName = GetBaseName(first.name);
            bool boring = IsBoring(first);

            for (int i = startIndex + 1; i < parent.childCount; i++)
            {
                Transform next = parent.GetChild(i);
                bool sameBase = GetBaseName(next.name) == baseName;
                bool bothBoring = IsBoring(next) && boring;
                
                if (sameBase && bothBoring)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }

        private static string GetBaseName(string name)
        {
            // Remove trailing numbers and coordinates like Tile_0_0 or Object (1)
            int underscoreIdx = name.LastIndexOf('_');
            if (underscoreIdx > 0 && underscoreIdx < name.Length - 1 && char.IsDigit(name[underscoreIdx + 1]))
                return name.Substring(0, underscoreIdx);
            
            int spaceIdx = name.LastIndexOf(' ');
            if (spaceIdx > 0 && name.EndsWith(")"))
                return name.Substring(0, spaceIdx);

            return name;
        }

        private static bool IsBoring(Transform t)
        {
            // Objects with no interesting components and no children are boring
            if (t.childCount > 0) return false;
            if (t.GetComponent<Button>() != null) return false;
            if (t.GetComponent<UnityEngine.UI.Text>() != null) return false;
            if (t.GetComponent("TMPro.TMP_Text") != null) return false;
            return true;
        }

        private static string Truncate(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length > 20) return s.Substring(0, 20) + "...";
            return s;
        }
    }
}
