using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RealPlayTester.Core.Perception
{
    /// <summary>
    /// Manual parser for Semantic DOM JSON. 
    /// Bypasses JsonUtility depth limits and [Serializable] requirements.
    /// Used primarily for test verification and AI context processing.
    /// </summary>
    public static class SemanticDOMParser
    {
        public static SemanticNode Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            // Simple token-based parser for our specific schema
            int index = 0;
            return ParseNode(json, ref index, 0);
        }

        private static SemanticNode ParseNode(string json, ref int index, int depth)
        {
            if (depth > 2048) return null; // Hard limit to prevent StackOverflow

            index = json.IndexOf('{', index);
            if (index == -1) return null;
            index++;

            var node = new SemanticNode();
            while (index < json.Length)
            {
                index = SkipWhitespace(json, index);
                if (index >= json.Length) break;
                if (json[index] == '}') { index++; break; }

                string key = ParseString(json, ref index);
                int colonIndex = json.IndexOf(':', index);
                if (colonIndex == -1) break;
                index = colonIndex + 1;
                index = SkipWhitespace(json, index);

                ParseProperty(node, key, json, ref index, depth);

                index = SkipWhitespace(json, index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return node;
        }

        private static void ParseProperty(SemanticNode node, string key, string json, ref int index, int depth)
        {
            if (index >= json.Length) return;
            switch (key)
            {
                case "Name": node.Name = ParseString(json, ref index); break;
                case "Role": node.Role = ParseString(json, ref index); break;
                case "VisualID": node.VisualID = ParseString(json, ref index); break;
                case "Command": node.Command = ParseString(json, ref index); break;
                case "Active": node.Active = ParseBool(json, ref index); break;
                case "Interactable": node.Interactable = ParseBool(json, ref index); break;
                case "Text": node.Text = ParseString(json, ref index); break;
                case "ZDepth": node.ZDepth = ParseFloat(json, ref index); break;
                case "IsOccluded": node.IsOccluded = ParseBool(json, ref index); break;
                case "BlockingObject": node.BlockingObject = ParseString(json, ref index); break;
                case "LogicalBlocked": node.LogicalBlocked = ParseBool(json, ref index); break;
                case "BlockedReason": node.BlockedReason = ParseString(json, ref index); break;
                case "ScreenRect": node.ScreenRect = ParseRect(json, ref index); break;
                case "Affordances": node.Affordances = ParseStringList(json, ref index); break;
                case "Children": node.Children = ParseChildren(json, ref index, depth); break;
                default: SkipValue(json, ref index); break;
            }
        }

        private static string ParseString(string json, ref int index)
        {
            index = json.IndexOf('"', index);
            if (index == -1) return "";
            index++;
            
            var sb = new System.Text.StringBuilder();
            bool escaped = false;
            while (index < json.Length)
            {
                char c = json[index++];
                if (escaped)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append(c); break;
                    }
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool ParseBool(string json, ref int index)
        {
            bool val = json.Substring(index, 4).ToLower() == "true";
            index = json.IndexOfAny(new char[] { ',', '}', '\n' }, index);
            return val;
        }

        private static float ParseFloat(string json, ref int index)
        {
            int end = json.IndexOfAny(new char[] { ',', '}', '\n' }, index);
            string s = json.Substring(index, end - index);
            float.TryParse(s, out float val);
            index = end;
            return val;
        }

        private static Rect ParseRect(string json, ref int index)
        {
            index = json.IndexOf('{', index) + 1;
            float x=0, y=0, w=0, h=0;
            while(json[index] != '}')
            {
                index = SkipWhitespace(json, index);
                if (json[index] == '"')
                {
                    string key = ParseString(json, ref index);
                    index = json.IndexOf(':', index) + 1;
                    float val = ParseFloat(json, ref index);
                    if (key == "x") x = val; else if (key == "y") y = val;
                    else if (key == "width") w = val; else if (key == "height") h = val;
                }
                if (json[index] == ',') index++;
                index = SkipWhitespace(json, index);
            }
            index++;
            return new Rect(x, y, w, h);
        }

        private static List<string> ParseStringList(string json, ref int index)
        {
            var list = new List<string>();
            index = json.IndexOf('[', index) + 1;
            while(true)
            {
                index = SkipWhitespace(json, index);
                if (json[index] == ']') { index++; break; }
                list.Add(ParseString(json, ref index));
                index = SkipWhitespace(json, index);
                if (json[index] == ',') index++;
            }
            return list;
        }

        private static List<SemanticNode> ParseChildren(string json, ref int index, int depth)
        {
            var list = new List<SemanticNode>();
            int arrayEnd = json.IndexOf(']', index);
            if (arrayEnd == -1) return list;

            index = json.IndexOf('[', index) + 1;
            while(index < json.Length)
            {
                index = SkipWhitespace(json, index);
                if (index >= json.Length || json[index] == ']') { index++; break; }
                var child = ParseNode(json, ref index, depth + 1);
                if (child != null) list.Add(child);
                index = SkipWhitespace(json, index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return list;
        }

        private static int SkipWhitespace(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
            return index;
        }

        private static void SkipValue(string json, ref int index)
        {
            index = json.IndexOfAny(new char[] { ',', '}', '\n' }, index);
        }
    }
}
