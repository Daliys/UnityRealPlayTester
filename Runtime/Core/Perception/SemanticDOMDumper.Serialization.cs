using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealPlayTester.Core.Perception
{
    public static partial class RealPlaySemanticDOMDumper
    {
        private static readonly string[] _indentCache = GenerateIndentCache(100);

        private static string[] GenerateIndentCache(int maxDepth)
        {
            var cache = new string[maxDepth];
            for (int i = 0; i < maxDepth; i++) cache[i] = new string(' ', i * 2);
            return cache;
        }

        private static string GetIndent(int indent)
        {
            if (indent < 0) return "";
            if (indent >= _indentCache.Length) return new string(' ', indent * 2);
            return _indentCache[indent];
        }

        private static string SerializeNode(SemanticNode node)
        {
            var sb = new StringBuilder(1024); // Start with reasonable capacity
            SerializeRecursive(node, sb, 0);
            return sb.ToString();
        }

        private static void SerializeRecursive(SemanticNode node, StringBuilder sb, int indent)
        {
            if (indent > 1024) return; // M002: Hard recursion limit

            string tab = GetIndent(indent);
            sb.AppendLine("{");
            sb.AppendLine($"{tab}  \"Name\": \"{EscapeJson(node.Name)}\",");
            sb.AppendLine($"{tab}  \"Role\": \"{EscapeJson(node.Role)}\",");
            sb.AppendLine($"{tab}  \"Active\": {node.Active.ToString().ToLower()},");
            sb.AppendLine($"{tab}  \"Interactable\": {node.Interactable.ToString().ToLower()},");
            sb.AppendLine($"{tab}  \"ScreenRect\": {{ \"x\": {node.ScreenRect.x}, \"y\": {node.ScreenRect.y}, \"width\": {node.ScreenRect.width}, \"height\": {node.ScreenRect.height} }},");
            sb.AppendLine($"{tab}  \"Text\": \"{EscapeJson(node.Text)}\",");
            sb.AppendLine($"{tab}  \"ZDepth\": {node.ZDepth},");
            sb.AppendLine($"{tab}  \"IsOccluded\": {node.IsOccluded.ToString().ToLower()},");
            sb.AppendLine($"{tab}  \"BlockingObject\": \"{EscapeJson(node.BlockingObject)}\",");
            sb.AppendLine($"{tab}  \"LogicalBlocked\": {node.LogicalBlocked.ToString().ToLower()},");
            sb.AppendLine($"{tab}  \"BlockedReason\": \"{EscapeJson(node.BlockedReason)}\",");
            sb.AppendLine($"{tab}  \"CollapsedCount\": {node.CollapsedCount},");
            
            AppendAffordancesJson(node, sb, tab);
            AppendChildrenJson(node, sb, indent, tab);
            sb.Append("}");
        }

        private static void AppendAffordancesJson(SemanticNode node, StringBuilder sb, string tab)
        {
            sb.Append($"{tab}  \"Affordances\": [");
            if (node.Affordances != null && node.Affordances.Count > 0)
            {
                for(int i=0; i<node.Affordances.Count; i++)
                {
                    sb.Append($"\"{node.Affordances[i]}\"");
                    if (i < node.Affordances.Count - 1) sb.Append(", ");
                }
            }
            sb.AppendLine("],");
        }

        private static void AppendChildrenJson(SemanticNode node, StringBuilder sb, int indent, string tab)
        {
            sb.AppendLine($"{tab}  \"Children\": [");
            if (node.Children != null && node.Children.Count > 0 && indent < 1024)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    sb.Append($"{tab}    ");
                    SerializeRecursive(node.Children[i], sb, indent + 2);
                    if (i < node.Children.Count - 1) sb.AppendLine(","); else sb.AppendLine();
                }
            }
            sb.Append($"{tab}  ]");
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
