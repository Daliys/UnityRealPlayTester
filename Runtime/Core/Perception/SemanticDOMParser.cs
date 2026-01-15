using System;
using System.Collections.Generic;
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
            return ParseNode(json, ref index);
        }

        private static SemanticNode ParseNode(string json, ref int index)
        {
            var node = new SemanticNode();
            index = json.IndexOf('{', index);
            if (index == -1) return null;
            index++;

            while (index < json.Length)
            {
                index = SkipWhitespace(json, index);
                if (json[index] == '}') { index++; break; }

                string key = ParseString(json, ref index);
                index = json.IndexOf(':', index) + 1;
                index = SkipWhitespace(json, index);

                ParseProperty(node, key, json, ref index);

                index = SkipWhitespace(json, index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return node;
        }

        private static void ParseProperty(SemanticNode node, string key, string json, ref int index)
        {
            switch (key)
            {
                case "Name": node.Name = ParseString(json, ref index); break;
                case "Role": node.Role = ParseString(json, ref index); break;
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
                case "Children": node.Children = ParseChildren(json, ref index); break;
                default: SkipValue(json, ref index); break;
            }
        }

        private static string ParseString(string json, ref int index)
        {
            index = json.IndexOf('"', index) + 1;
            int end = json.IndexOf('"', index);
            string result = json.Substring(index, end - index);
            index = end + 1;
            return result.Replace("\\\\", "\\").Replace("\\\"", "\"");
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

        private static List<SemanticNode> ParseChildren(string json, ref int index)
        {
            var list = new List<SemanticNode>();
            index = json.IndexOf('[', index) + 1;
            while(true)
            {
                index = SkipWhitespace(json, index);
                if (json[index] == ']') { index++; break; }
                var child = ParseNode(json, ref index);
                if (child != null) list.Add(child);
                index = SkipWhitespace(json, index);
                if (json[index] == ',') index++;
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
