using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static async Task ForUIVisible(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? 10f;
            float lastLogTime = startTime;

            while (true)
            {
                // Find object including inactive ones to provide better diagnostic traces
                var obj = FindObjectIncludingInactive(name);
                if (IsFullyVisibleAndInteractable(obj)) return;

                float elapsed = Time.realtimeSinceStartup - startTime;
                string trace = AnalyzeVisibilityAlphaChain(obj, name);

                if (elapsed >= timeout)
                {
                    throw new TimeoutException($"Wait.ForUIVisible timed out waiting for '{name}' after {timeout}s. {trace}");
                }

                if (elapsed > 2f && Time.realtimeSinceStartup - lastLogTime > 2f)
                {
                    RealPlayLog.Info($"[Wait] Still waiting for UI '{name}' to be visible. {trace}");
                    lastLogTime = Time.realtimeSinceStartup;
                }

                await Task.Yield();
            }
        }

        private static GameObject FindObjectIncludingInactive(string name)
        {
            var all = SceneCache.Instance.AllObjects;
            foreach (var go in all)
            {
                if (go != null && go.name == name) return go;
            }
            return null;
        }

        public static Task ForInteractable<T>(string textFilter = null, float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            string desc = $"Component<{typeof(T).Name}>" + (textFilter != null ? $" with text '{textFilter}'" : "");

            return Until(() =>
            {
                // PERFORMANCE FIX: Use AllObjects from SceneCache instead of FindObjectsByType
                var all = SceneCache.Instance.AllObjects;
                foreach (var go in all)
                {
                     if (go == null || !go.activeInHierarchy) continue;
                     if (!go.TryGetComponent<T>(out var c)) continue;
                     if (!IsFullyVisibleAndInteractable(go)) continue;
                     
                     if (!string.IsNullOrEmpty(textFilter))
                     {
                         if (HasMatchingText(c, textFilter)) return true;
                     }
                     else return true;
                }
                return false;
            }, timeoutSeconds, desc);
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

            // 1. Cull check
            var canvasRenderer = obj.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null && canvasRenderer.cull) return false;

            // 2. Technical Check (CanvasGroups)
            var groups = obj.GetComponentsInParent<CanvasGroup>();
            foreach (var g in groups)
            {
                if (g.ignoreParentGroups) break; 
                
                if (g.alpha <= 0.05f || !g.interactable)
                {
                    if (Tester.Settings.ForceBatchmodeVisibility && Application.isBatchMode)
                    {
                        // In batchmode, we might want to bypass alpha checks if forced, 
                        // but we MUST NOT modify the actual component state.
                        continue;
                    }
                    return false;
                }
            }

            // 3. Logical Check (Custom Scripts)
            var logic = RealPlayTester.Utilities.InteractionLogic.CheckLogicalInteractivity(obj);
            if (logic.IsBlocked) return false;

            return true;
        }

        private static string AnalyzeVisibilityAlphaChain(GameObject obj, string targetName)
        {
            if (obj == null) return $"Object '{targetName}' not found in scene.";

            // PERFORMANCE FIX: Cache string builder
            var sb = new System.Text.StringBuilder();
            
            var logic = RealPlayTester.Utilities.InteractionLogic.CheckLogicalInteractivity(obj);
            if (logic.IsBlocked) sb.Append($"[LOGIC BLOCKED: {logic.Reason}] ");

            sb.Append("Visibility Trace: ");
            
            var current = obj.transform;
            int depth = 0;
            while (current != null && depth < 10) // Limit trace depth
            {
                var go = current.gameObject;
                sb.Append($"{go.name}(");
                sb.Append(go.activeSelf ? "A" : "I");
                
                var cg = go.GetComponent<CanvasGroup>();
                if (cg != null) sb.Append($", α:{cg.alpha:F1}");
                
                if (current.localScale.sqrMagnitude < 0.0001f) sb.Append(", S:0");
                
                sb.Append(") -> ");
                current = current.parent;
                depth++;
            }
            if (current != null) sb.Append("...");
            else sb.Append("Root");
            return sb.ToString();
        }
    }
}
