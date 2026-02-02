using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Diagnostics
{
    public struct TestMetadata
    {
        public string TestName;
        public string TestId;
        public DateTime StartTime;
        public DateTime EndTime;
        public string SceneName;
        public string UnityVersion;
        public string PackageVersion;
        public string ActiveInputMode;
    }

    public struct TestRunState
    {
        public string LastAction;
        public string LastPanel;
        public string LastIntent;
        public PlacementAttempt LastPlacementAttempt;
    }

    /// <summary>
    /// Tracks diagnostic context for a single test run.
    /// Provides structured data for debugging test failures.
    /// </summary>
    public class TestRunContext
    {
        public TestMetadata Info;
        public TestRunState State;

        // BACKWARD COMPATIBILITY PROPERTIES
        public string TestName { get => Info.TestName; set => Info.TestName = value; }
        public string SceneName { get => Info.SceneName; set => Info.SceneName = value; }
        public DateTime StartTime { get => Info.StartTime; set => Info.StartTime = value; }
        public DateTime EndTime { get => Info.EndTime; set => Info.EndTime = value; }
        public string LastAction { get => State.LastAction; set => State.LastAction = value; }
        public string LastPanel { get => State.LastPanel; set => State.LastPanel = value; }
        public string LastIntent { get => State.LastIntent; set => State.LastIntent = value; }
        public PlacementAttempt LastPlacementAttempt { get => State.LastPlacementAttempt; set => State.LastPlacementAttempt = value; }
        
        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly List<Breadcrumb> _breadcrumbs = new List<Breadcrumb>();
        private readonly object _collectionLock = new object();

        // COMPATIBILITY: return the actual list for existing tests, but ADDING should use AddLog/AddBreadcrumb for thread safety
        public List<LogEntry> Logs => _logs;
        public List<Breadcrumb> Breadcrumbs => _breadcrumbs;
        public object SyncLock => _collectionLock;

        public void AddLog(LogEntry log) { lock(_collectionLock) _logs.Add(log); }
        public void AddBreadcrumb(Breadcrumb crumb) 
        { 
            lock(_collectionLock) 
            {
                // PERFORMANCE: Cap breadcrumbs to prevent memory exhaustion in infinite loops
                if (_breadcrumbs.Count >= 2000) _breadcrumbs.RemoveAt(0);
                _breadcrumbs.Add(crumb); 
            }
        }
        public int GetLogCount() { lock(_collectionLock) return _logs.Count; }
        public LogEntry GetLogAt(int index) { lock(_collectionLock) return (index >= 0 && index < _logs.Count) ? _logs[index] : null; }

        public TestRunContext()
        {
            Info.TestId = Guid.NewGuid().ToString();
            Info.StartTime = DateTime.Now;
            Info.UnityVersion = Application.unityVersion;
            Info.PackageVersion = "2.9.2"; 
            
#if ENABLE_INPUT_SYSTEM
            Info.ActiveInputMode = "InputSystem";
#else
            Info.ActiveInputMode = "Legacy";
#endif
        }

        public string ToJson()
        {
            lock (_collectionLock)
            {
                bool compact = RealPlaySettings.CompactJSON;
                var sb = new StringBuilder();
                sb.Append("{"); if (!compact) sb.AppendLine();
                AppendMetadataJson(sb, compact);
                AppendPlacementJson(sb, compact);
                AppendBreadcrumbsJson(sb, compact);
                AppendLogsJson(sb, compact);
                sb.Append("}"); if (!compact) sb.AppendLine();
                return sb.ToString();
            }
        }

        private void AppendMetadataJson(StringBuilder sb, bool compact)
        {
            string t = compact ? "" : "  ";
            string nl = compact ? "" : "\n";
            sb.Append($"{t}\"testName\": \"{EscapeJson(Info.TestName)}\",{nl}");
            sb.Append($"{t}\"testId\": \"{Info.TestId}\",{nl}");
            sb.Append($"{t}\"startTime\": \"{Info.StartTime.ToString("O")}\",{nl}");
            sb.Append($"{t}\"endTime\": \"{Info.EndTime.ToString("O")}\",{nl}");
            sb.Append($"{t}\"sceneName\": \"{EscapeJson(Info.SceneName)}\",{nl}");
            sb.Append($"{t}\"unityVersion\": \"{Info.UnityVersion}\",{nl}");
            sb.Append($"{t}\"packageVersion\": \"{Info.PackageVersion}\",{nl}");
            sb.Append($"{t}\"activeInputMode\": \"{Info.ActiveInputMode}\",{nl}");
            sb.Append($"{t}\"lastAction\": \"{EscapeJson(State.LastAction)}\",{nl}");
            sb.Append($"{t}\"lastPanel\": \"{EscapeJson(State.LastPanel)}\",{nl}");
            sb.Append($"{t}\"lastIntent\": \"{EscapeJson(State.LastIntent)}\",{nl}");
        }

        private void AppendPlacementJson(StringBuilder sb, bool compact)
        {
            string t = compact ? "" : "  ";
            string nl = compact ? "" : "\n";
            if (State.LastPlacementAttempt != null)
            {
                sb.Append($"{t}\"lastPlacementAttempt\": {{{nl}");
                sb.Append($"{t}{(compact ? "" : "  ")}\"position\": \"{State.LastPlacementAttempt.Position}\",{nl}");
                sb.Append($"{t}{(compact ? "" : "  ")}\"definitionId\": \"{EscapeJson(State.LastPlacementAttempt.DefinitionId)}\",{nl}");
                sb.Append($"{t}{(compact ? "" : "  ")}\"result\": \"{EscapeJson(State.LastPlacementAttempt.Result)}\"{nl}");
                sb.Append($"{t}}}, {nl}");
            }
            else if (!compact)
            {
                sb.AppendLine("  \"lastPlacementAttempt\": null,");
            }
        }

        private void AppendBreadcrumbsJson(StringBuilder sb, bool compact)
        {
            string t = compact ? "" : "  ";
            string nl = compact ? "" : "\n";
            if (compact && _breadcrumbs.Count == 0) return;

            sb.Append($"{t}\"breadcrumbs\": ["); if (!compact) sb.AppendLine();
            for (int i = 0; i < _breadcrumbs.Count; i++)
            {
                var crumb = _breadcrumbs[i];
                sb.Append($"{(compact ? "" : "    ")}{{ \"frame\": {crumb.Frame}, \"time\": {crumb.Timestamp.ToString("F3")}, \"type\": \"{crumb.Type}\", \"message\": \"{EscapeJson(crumb.Message)}\" }}");
                if (i < _breadcrumbs.Count - 1) sb.Append(",");
                if (!compact) sb.AppendLine();
            }
            sb.Append($"{t}],{nl}");
        }

        private void AppendLogsJson(StringBuilder sb, bool compact)
        {
            string t = compact ? "" : "  ";
            string nl = compact ? "" : "\n";
            if (compact && _logs.Count == 0) return;

            sb.Append($"{t}\"logs\": ["); if (!compact) sb.AppendLine();
            for (int i = 0; i < _logs.Count; i++)
            {
                var log = _logs[i];
                sb.Append($"{(compact ? "" : "    ")}{{ \"frame\": {log.Frame}, \"time\": {log.Timestamp.ToString("F3")}, \"type\": \"{log.Type}\", \"severity\": \"{log.Severity}\", \"message\": \"{EscapeJson(log.Message)}\", \"repeats\": {log.RepeatCount} }}");
                if (i < _logs.Count - 1) sb.Append(",");
                if (!compact) sb.AppendLine();
            }
            sb.Append($"{t}]{nl}");
        }

        public string ToMarkdown()
        {
            lock (_collectionLock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Test Run Context\n");
                AppendMetadataMarkdown(sb);
                AppendEnvironmentMarkdown(sb);
                AppendStateMarkdown(sb);
                AppendBreadcrumbsMarkdown(sb);
                return sb.ToString();
            }
        }

        private void AppendMetadataMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Test Information");
            sb.AppendLine("- **Test Name**: " + Info.TestName);
            sb.AppendLine("- **Test ID**: " + Info.TestId);
            sb.AppendLine("- **Start Time**: " + Info.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            if (Info.EndTime != default)
            {
                sb.AppendLine("- **End Time**: " + Info.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("- **Duration**: " + (Info.EndTime - Info.StartTime).TotalSeconds.ToString("F2") + "s");
            }
            else
            {
                sb.AppendLine("- **Duration (Active)**: " + (DateTime.Now - Info.StartTime).TotalSeconds.ToString("F2") + "s");
            }
            sb.AppendLine();
        }

        private void AppendEnvironmentMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Environment");
            sb.AppendLine("- **Scene**: " + Info.SceneName);
            sb.AppendLine("- **Unity Version**: " + Info.UnityVersion);
            sb.AppendLine("- **Package Version**: " + Info.PackageVersion);
            sb.AppendLine("- **Input Mode**: " + Info.ActiveInputMode);
            sb.AppendLine();
        }

        private void AppendStateMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Test State");
            sb.AppendLine("- **Last Action**: " + (State.LastAction ?? "N/A"));
            sb.AppendLine("- **Last Panel**: " + (State.LastPanel ?? "N/A"));
            sb.AppendLine("- **Current Intent**: " + (State.LastIntent ?? "N/A"));
            
            if (State.LastPlacementAttempt != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Last Placement Attempt");
                sb.AppendLine("- **Position**: " + State.LastPlacementAttempt.Position);
                sb.AppendLine("- **Definition ID**: " + State.LastPlacementAttempt.DefinitionId);
                sb.AppendLine("- **Result**: " + State.LastPlacementAttempt.Result);
            }
            sb.AppendLine();
        }

        private void AppendBreadcrumbsMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## 🥖 Breadcrumbs (High-Level Timeline)");
            sb.AppendLine("| Frame | Time | Type | Message |");
            sb.AppendLine("|-------|------|------|---------|");
            foreach (var crumb in _breadcrumbs)
            {
                sb.AppendLine("| " + crumb.Frame + " | " + crumb.Timestamp.ToString("F2") + "s | **" + crumb.Type + "** | " + crumb.Message + " |");
            }
            sb.AppendLine();
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            
            // OPTIMIZATION: Check if escaping is actually needed to avoid allocations
            bool needsEscape = false;
            foreach (char c in str)
            {
                if (c == '\\' || c == '\"' || c == '\n' || c == '\r' || c == '\t')
                {
                    needsEscape = true;
                    break;
                }
            }
            if (!needsEscape) return str;

            var sb = new StringBuilder(str.Length + 16);
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
