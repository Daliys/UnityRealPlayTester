using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Utility to dump the current scene hierarchy to a string for AI debugging.
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
                    DumpRecursive(root.transform, sb, 0, 15); // limit depth to 15
                }
            }

            // Also try to find DontDestroyOnLoad roots via a trick or just skip them for now.
            // A common trick is to find all ValueType roots, but scene roots are usually enough.
            
            return sb.ToString();
        }

        private static void DumpRecursive(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                sb.Append(' ', depth * 2).AppendLine("... (max depth reached)");
                return;
            }
            sb.Append(' ', depth * 2);
            sb.Append(t.gameObject.activeInHierarchy ? "[+] " : "[-] ");
            sb.Append(t.name);

            // Append key component info
            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                sb.Append(" (Button");
                if (!btn.interactable) sb.Append(":Disabled");
                sb.Append(")");
            }

            // Check for legacy Text
            var txt = t.GetComponent<Text>();
            if (txt != null)
            {
                sb.Append($" [Text: \"{Truncate(txt.text)}\"]");
            }
            
            // Check for TMP (using reflection to avoid hard dependency)
            var tmp = t.GetComponent("TMPro.TMP_Text");
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

            int childCount = t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                DumpRecursive(t.GetChild(i), sb, depth + 1, maxDepth);
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
