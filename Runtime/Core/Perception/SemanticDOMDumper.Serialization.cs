using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealPlayTester.Core.Perception
{
    public static partial class RealPlaySemanticDOMDumper
    {
        private static readonly string[] _indentCache = GenerateIndentCache(100);

        private struct SerializationContext
        {
            public bool Compact;
            public string Tab;
            public string Nl;
            public string Space;
            public int Indent;
        }

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

        internal static string SerializeNode(SemanticNode node)
        {
            var sb = new StringBuilder(1024);
            SerializeRecursive(node, sb, 0);
            return sb.ToString();
        }

        private static void SerializeRecursive(SemanticNode node, StringBuilder sb, int indent)
        {
            if (indent > 2048) return;

            bool compact = RealPlaySettings.CompactJSON;
            var ctx = new SerializationContext
            {
                Compact = compact,
                Tab = compact ? "" : GetIndent(indent),
                Nl = compact ? "" : "\n",
                Space = compact ? "" : " ",
                Indent = indent
            };

            sb.Append("{"); sb.Append(ctx.Nl);
            sb.Append(ctx.Tab); sb.Append(ctx.Space);
            sb.Append("\""); sb.Append(compact ? "n" : "Name"); sb.Append("\""); sb.Append(":\"");
            sb.Append(EscapeJson(node.Name)); sb.Append("\"");

            AppendMetadataFields(node, sb, ctx);
            AppendGeometryFields(node, sb, ctx);
            AppendOcclusionFields(node, sb, ctx);
            AppendLogicFields(node, sb, ctx);
            
            if (node.CollapsedCount > 0) 
            {
                sb.Append(",\""); sb.Append(compact ? "cc" : "CollapsedCount");
                sb.Append("\":"); sb.Append(node.CollapsedCount);
            }
            
            AppendAffordances(node, sb, ctx);
            AppendChildren(node, sb, ctx);
            
            sb.Append(ctx.Nl); sb.Append(ctx.Tab); sb.Append("}");
        }

        private static void AppendMetadataFields(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact;
            if (!string.IsNullOrEmpty(node.Role)) { sb.Append(",\""); sb.Append(c ? "r" : "Role"); sb.Append("\":\""); sb.Append(EscapeJson(node.Role)); sb.Append("\""); }
            if (!string.IsNullOrEmpty(node.VisualID)) { sb.Append(",\""); sb.Append(c ? "id" : "VisualID"); sb.Append("\":\""); sb.Append(EscapeJson(node.VisualID)); sb.Append("\""); }
            if (!string.IsNullOrEmpty(node.Command)) { sb.Append(",\""); sb.Append(c ? "cmd" : "Command"); sb.Append("\":\""); sb.Append(EscapeJson(node.Command)); sb.Append("\""); }
            if (!node.Active) { sb.Append(",\""); sb.Append(c ? "act" : "Active"); sb.Append("\":false"); }
            if (node.Interactable) { sb.Append(",\""); sb.Append(c ? "int" : "Interactable"); sb.Append("\":true"); }
        }

        private static void AppendGeometryFields(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact;
            if (node.ScreenRect.width > 0 || node.ScreenRect.height > 0)
            {
                if (c)
                {
                    sb.Append(",\"rect\":["); sb.Append(Mathf.RoundToInt(node.ScreenRect.x)); sb.Append(",");
                    sb.Append(Mathf.RoundToInt(node.ScreenRect.y)); sb.Append(",");
                    sb.Append(Mathf.RoundToInt(node.ScreenRect.width)); sb.Append(",");
                    sb.Append(Mathf.RoundToInt(node.ScreenRect.height)); sb.Append("]");
                }
                else
                {
                    sb.Append(", "); sb.Append(ctx.Nl); sb.Append(ctx.Tab); sb.Append(ctx.Space);
                    sb.Append("\"ScreenRect\": { \"x\": "); sb.Append(node.ScreenRect.x);
                    sb.Append(", \"y\": "); sb.Append(node.ScreenRect.y);
                    sb.Append(", \"width\": "); sb.Append(node.ScreenRect.width);
                    sb.Append(", \"height\": "); sb.Append(node.ScreenRect.height); sb.Append(" }");
                }
            }

            if (!string.IsNullOrEmpty(node.Text)) { sb.Append(",\""); sb.Append(c ? "txt" : "Text"); sb.Append("\":\""); sb.Append(EscapeJson(node.Text)); sb.Append("\""); }
            if (Mathf.Abs(node.ZDepth) > 0.0001f) { sb.Append(",\""); sb.Append(c ? "z" : "ZDepth"); sb.Append("\":"); sb.Append(node.ZDepth.ToString("F2")); }
        }

        private static void AppendOcclusionFields(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact;
            if (node.IsOccluded)
            {
                sb.Append(",\""); sb.Append(c ? "occ" : "IsOccluded"); sb.Append("\":true");
                if (!string.IsNullOrEmpty(node.BlockingObject)) { sb.Append(",\""); sb.Append(c ? "block" : "BlockingObject"); sb.Append("\":\""); sb.Append(EscapeJson(node.BlockingObject)); sb.Append("\""); }
            }
        }

        private static void AppendLogicFields(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact;
            if (node.LogicalBlocked)
            {
                sb.Append(",\""); sb.Append(c ? "lblock" : "LogicalBlocked"); sb.Append("\":true");
                if (!string.IsNullOrEmpty(node.BlockedReason)) { sb.Append(",\""); sb.Append(c ? "msg" : "BlockedReason"); sb.Append("\":\""); sb.Append(EscapeJson(node.BlockedReason)); sb.Append("\""); }
            }
        }

        private static void AppendAffordances(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact;
            if (node.Affordances != null && node.Affordances.Count > 0)
            {
                sb.Append(",\""); sb.Append(c ? "aff" : "Affordances"); sb.Append("\":[");
                for(int i=0; i<node.Affordances.Count; i++)
                {
                    sb.Append("\""); sb.Append(node.Affordances[i]); sb.Append("\"");
                    if (i < node.Affordances.Count - 1) sb.Append(c ? "," : ", ");
                }
                sb.Append("]");
            }
        }

        private static void AppendChildren(SemanticNode node, StringBuilder sb, SerializationContext ctx)
        {
            bool c = ctx.Compact; string nl = ctx.Nl; string t = ctx.Tab;
            if (node.Children != null && node.Children.Count > 0 && ctx.Indent < 2048)
            {
                sb.Append(",\""); sb.Append(c ? "c" : "Children"); sb.Append("\":["); sb.Append(nl);
                for (int i = 0; i < node.Children.Count; i++)
                {
                    if (!c) { sb.Append(t); sb.Append("    "); }
                    SerializeRecursive(node.Children[i], sb, ctx.Indent + 2);
                    if (i < node.Children.Count - 1) sb.Append(",");
                    sb.Append(nl);
                }
                sb.Append(t); sb.Append(ctx.Space); sb.Append("]");
            }
        }

        private static string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\u005C", "\u005C\u005C")
                      .Replace("\u0022", "\u005C\u0022")
                      .Replace("\n", "\u005Cn")
                      .Replace("\r", "\u005Cr");
        }
    }
}