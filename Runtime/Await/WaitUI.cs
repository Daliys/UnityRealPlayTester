using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static async Task ForUIVisible(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? 10f;

            while (true)
            {
                var obj = GameObject.Find(name);
                if (IsFullyVisibleAndInteractable(obj)) return;

                if (Time.realtimeSinceStartup - startTime >= timeout)
                {
                    string trace = AnalyzeVisibilityAlphaChain(obj, name);
                    throw new TimeoutException($"Wait.ForUIVisible timed out waiting for '{name}'. {trace}");
                }

                await Task.Yield();
            }
        }

        public static Task ForInteractable<T>(string textFilter = null, float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            return Until(() =>
            {
                var candidates = GameObject.FindObjectsByType<T>(FindObjectsSortMode.None);
                foreach (var c in candidates)
                {
                     if (!c.gameObject.activeInHierarchy) continue;
                     if (!IsFullyVisibleAndInteractable(c.gameObject)) continue;
                     
                     if (!string.IsNullOrEmpty(textFilter))
                     {
                         if (HasMatchingText(c, textFilter)) return true;
                     }
                     else return true;
                }
                return false;
            }, timeoutSeconds);
        }

        private static bool HasMatchingText(Component c, string textFilter)
        {
            var text = c.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null && text.text.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            
            var tmpType = Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmp = c.GetComponentInChildren(tmpType);
                if (tmp != null)
                {
                    var prop = tmpType.GetProperty("text");
                    string val = prop?.GetValue(tmp) as string;
                    if (val != null && val.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
            return false;
        }

        private static bool IsFullyVisibleAndInteractable(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return false;

            var groups = obj.GetComponentsInParent<CanvasGroup>();
            foreach (var g in groups)
            {
                if (g.ignoreParentGroups) break; 
                
                if (g.alpha <= 0.95f || !g.interactable)
                {
                    if (Tester.Settings.ForceBatchmodeVisibility)
                    {
                        g.alpha = 1f;
                        g.interactable = true;
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        private static string AnalyzeVisibilityAlphaChain(GameObject obj, string targetName)
        {
            if (obj == null) return $"Object '{targetName}' not found in scene.";

            var sb = new System.Text.StringBuilder();
            sb.Append("Visibility Trace: ");
            
            var current = obj.transform;
            while (current != null)
            {
                var go = current.gameObject;
                sb.Append($"{go.name}(");
                sb.Append(go.activeSelf ? "Active" : "INACTIVE");
                
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    sb.Append($", Alpha:{cg.alpha:F2}");
                    if (!cg.interactable) sb.Append(", Non-Interactable");
                }
                
                if (current.localScale.sqrMagnitude < 0.0001f) sb.Append(", Scale:0");
                
                sb.Append(") -> ");
                current = current.parent;
            }
            sb.Append("Root");
            return sb.ToString();
        }
    }
}
