using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Utility to dump the current scene hierarchy to a string for AI debugging.
    /// Optimized to group repetitive siblings (like tiles) to save tokens.
    /// </summary>
    public static class VisualTreeLogger
    {
        public static string DumpHierarchy()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Active Scene Hierarchy ===");
            
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
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

        private static void DumpRecursive(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                sb.Append(' ', (depth + 1) * 2).AppendLine("... (max depth reached)");
                return;
            }

            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = t.GetChild(i);
                
                // Grouping logic: check if this child and subsequent ones are "boring" and similar
                int groupCount = GetRepetitiveGroupCount(t, i);
                if (groupCount > 5) // Group if more than 5 similar siblings
                {
                    sb.Append(' ', (depth + 1) * 2);
                    sb.AppendLine($"... ({groupCount} similar '{GetBaseName(child.name)}' objects)");
                    i += groupCount - 1; // Skip the group
                    continue;
                }

                sb.Append(' ', (depth + 1) * 2);
                sb.Append(child.gameObject.activeInHierarchy ? "[+] " : "[-] ");
                sb.Append(child.name);

                // Append key component info
                var btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    sb.Append(" (Button");
                    if (!btn.interactable) sb.Append(":Disabled");
                    sb.Append(")");
                }

                // Check for legacy Text
                var txt = child.GetComponent<Text>();
                if (txt != null)
                {
                    sb.Append($" [Text: \"{Truncate(txt.text)}\"]");
                }
                
                // Check for TMP (using reflection to avoid hard dependency)
                var tmp = child.GetComponent("TMPro.TMP_Text");
                if (tmp != null)
                {
                    var textProp = tmp.GetType().GetProperty("text");
                    if (textProp != null)
                    {
                        var val = textProp.GetValue(tmp) as string;
                        sb.Append($" [TMP: \"{Truncate(val)}\"]");
                    }
                }
                sb.AppendLine();

                if (child.childCount > 0)
                {
                    DumpRecursive(child, sb, depth + 1, maxDepth);
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
            if (t.GetComponent<Text>() != null) return false;
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
