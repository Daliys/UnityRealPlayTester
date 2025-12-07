using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using RealPlayTester.Core;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Utility to probe the screen and see what UI/World elements are under a point.
    /// </summary>
    public static class InteractionProbe
    {
        public static string ProbeScreen(Vector2 screenPos)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Probe at {screenPos}:");

            // 1. UI Probe
            var es = EventSystem.current;
            if (es != null)
            {
                var pointer = new PointerEventData(es) { position = screenPos };
                var results = new List<RaycastResult>();
                es.RaycastAll(pointer, results);

                if (results.Count > 0)
                {
                    sb.AppendLine("  UI Hits:");
                    foreach (var hit in results)
                    {
                        sb.AppendLine($"    - {hit.gameObject.name} (Sorting: {hit.sortingOrder}, Depth: {hit.depth})");
                    }
                }
                else
                {
                    sb.AppendLine("  UI Hits: None");
                }
            }
            else
            {
                sb.AppendLine("  UI: No EventSystem found.");
            }

            // 2. World Probe (Main Camera)
            var cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    sb.AppendLine($"  World Hit: {hit.collider.gameObject.name} (Dist: {hit.distance:F2})");
                }
                else
                {
                    sb.AppendLine("  World Hit: None");
                }
            }
            else
            {
                sb.AppendLine("  World: No MainCamera found.");
            }

            return sb.ToString().Trim();
        }
    }
}
