using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Utilities;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static class Perception
        {
            /// <summary>Find objects using fuzzy matching.</summary>
            public static GameObject Find(string fuzzyName) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.SmartFind.Object(fuzzyName) : null;

            /// <summary>Probe elements at specific screen position.</summary>
            public static string Probe(Vector2 screenPos) => RealPlayEnvironment.IsEnabled ? InteractionProbe.ProbeScreen(screenPos) : string.Empty;

            /// <summary>Describe all elements in a screen region.</summary>
            public static string DescribeRegion(Rect screenRect)
            {
                if (!RealPlayEnvironment.IsEnabled) return string.Empty;
                var sb = new System.Text.StringBuilder();
                var all = RealPlayTester.Input.SmartFind.FindAllInteractables();
                
                int id = 1; char series = 'A';
                foreach (var go in all)
                {
                    var pos = RealInputUtility.GetScreenCenter(go);
                    if (screenRect.Contains(pos))
                    {
                        var role = SemanticProbe.GetRoleName(go);
                        string visualId = $"#{series}{id}";
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append($"{visualId}: [{role}] {go.name}");
                    }
                    if (++id > 9) { id = 1; series++; }
                }
                return sb.Length > 0 ? "Region contains: " + sb.ToString() : "Region is empty.";
            }

            /// <summary>Get list of all actionable/interactable elements.</summary>
            public static string GetActionableElements()
            {
                if (!RealPlayEnvironment.IsEnabled) return string.Empty;
                var sb = new System.Text.StringBuilder();
                var all = RealPlayTester.Input.SmartFind.FindAllInteractables();
                
                int id = 1; char series = 'A';
                foreach (var go in all)
                {
                    var role = SemanticProbe.GetRoleName(go);
                    var pos = RealInputUtility.GetScreenCenter(go);
                    string visualId = $"#{series}{id}";
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{visualId}: [{role}] {go.name} at ({Mathf.RoundToInt(pos.x)}, {Mathf.RoundToInt(pos.y)})");
                    if (++id > 9) { id = 1; series++; }
                }
                return sb.Length > 0 ? "Interactables: " + sb.ToString() : "None found.";
            }

            /// <summary>Text-based hierarchy dump for AI context.</summary>
            public static string DumpHierarchy() => RealPlayEnvironment.IsEnabled ? VisualTreeLogger.DumpHierarchy() : string.Empty;
            
            /// <summary>Detailed JSON hierarchy dump (AI-Native Semantic DOM).</summary>
            public static string DumpHierarchyJson() => DumpSemanticDOM();

        /// <summary> Dumps the current scene hierarchy as an AI-optimized JSON string. </summary>
        public static string DumpSemanticDOM() => RealPlayEnvironment.IsEnabled ? RealPlayTester.Core.Perception.RealPlaySemanticDOMDumper.Dump(RealPlaySettings.Perception.FilterToViewport) : string.Empty;
        
        /// <summary> Dumps the current scene hierarchy as an AI-optimized Markdown string. </summary>

            /// <summary>Checks if a target object is occluded from the main camera view.</summary>
            public static bool IsOccluded(GameObject target, out string blocker)
            {
                if (!RealPlayEnvironment.IsEnabled) { blocker = string.Empty; return false; }
                var result = RealPlayTester.Core.Perception.RealPlayOcclusionRaycaster.CheckOcclusion(target);
                blocker = result.BlockingObjectName;
                return result.IsOccluded;
            }

            /// <summary>Captures a screenshot with a coordinate grid for AI analysis.</summary>
            public static Task CaptureVisionSnapshot(string fileName) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Core.Perception.RealPlayComputerVisionInterface.CaptureVisionSnapshot(fileName) : Task.CompletedTask;
        }
    }
}
