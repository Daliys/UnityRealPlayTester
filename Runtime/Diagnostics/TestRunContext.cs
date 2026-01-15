using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Tracks diagnostic context for a single test run.
    /// Provides structured data for debugging test failures.
    /// </summary>
    public class TestRunContext
    {
        // Test Identity
        public string TestName { get; set; }
        public string TestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Environment
        public string SceneName { get; set; }
        public string UnityVersion { get; set; }
        public string PackageVersion { get; set; }

        // Input System
        public string ActiveInputMode { get; set; }

        // Test State Tracking
        public string LastAction { get; set; }
        public string LastPanel { get; set; }
        public string LastIntent { get; set; }
        public PlacementAttempt LastPlacementAttempt { get; set; }
        public List<LogEntry> Logs { get; } = new List<LogEntry>();
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();

        public TestRunContext()
        {
            TestId = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            UnityVersion = Application.unityVersion;
            PackageVersion = "2.4.0"; // Will be updated with package
            
            // Detect input mode
#if ENABLE_INPUT_SYSTEM
            ActiveInputMode = "InputSystem";
#else
            ActiveInputMode = "Legacy";
#endif
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendMetadataJson(sb);
            AppendPlacementJson(sb);
            AppendBreadcrumbsJson(sb);
            AppendLogsJson(sb);
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void AppendMetadataJson(StringBuilder sb)
        {
            sb.AppendLine($"  \"testName\": \"{EscapeJson(TestName)}\",");
            sb.AppendLine($"  \"testId\": \"{TestId}\",");
            sb.AppendLine($"  \"startTime\": \"{StartTime:O}\",");
            sb.AppendLine($"  \"endTime\": \"{EndTime:O}\",");
            sb.AppendLine($"  \"sceneName\": \"{EscapeJson(SceneName)}\",");
            sb.AppendLine($"  \"unityVersion\": \"{UnityVersion}\",");
            sb.AppendLine($"  \"packageVersion\": \"{PackageVersion}\",");
            sb.AppendLine($"  \"activeInputMode\": \"{ActiveInputMode}\",");
            sb.AppendLine($"  \"lastAction\": \"{EscapeJson(LastAction)}\",");
            sb.AppendLine($"  \"lastPanel\": \"{EscapeJson(LastPanel)}\",");
            sb.AppendLine($"  \"lastIntent\": \"{EscapeJson(LastIntent)}\",");
        }

        private void AppendPlacementJson(StringBuilder sb)
        {
            if (LastPlacementAttempt != null)
            {
                sb.AppendLine($"  \"lastPlacementAttempt\": {{");
                sb.AppendLine($"    \"position\": \"{LastPlacementAttempt.Position}\",");
                sb.AppendLine($"    \"definitionId\": \"{EscapeJson(LastPlacementAttempt.DefinitionId)}\",");
                sb.AppendLine($"    \"result\": \"{EscapeJson(LastPlacementAttempt.Result)}\"");
                sb.AppendLine($"  }},");
            }
            else
            {
                sb.AppendLine($"  \"lastPlacementAttempt\": null,");
            }
        }

        private void AppendBreadcrumbsJson(StringBuilder sb)
        {
            sb.AppendLine($"  \"breadcrumbs\": [");
            for (int i = 0; i < Breadcrumbs.Count; i++)
            {
                var crumb = Breadcrumbs[i];
                sb.Append($"    {{ \"frame\": {crumb.Frame}, \"time\": {crumb.Timestamp:F3}, \"type\": \"{crumb.Type}\", \"message\": \"{EscapeJson(crumb.Message)}\" }}");
                if (i < Breadcrumbs.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");
        }

        private void AppendLogsJson(StringBuilder sb)
        {
            sb.AppendLine($"  \"logs\": [");
            for (int i = 0; i < Logs.Count; i++)
            {
                var log = Logs[i];
                sb.Append($"    {{ \"frame\": {log.Frame}, \"time\": {log.Timestamp:F3}, \"type\": \"{log.Type}\", \"severity\": \"{log.Severity}\", \"message\": \"{EscapeJson(log.Message)}\", \"repeats\": {log.RepeatCount} }}");
                if (i < Logs.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
        }

        /// <summary>
        /// Exports context as human-readable Markdown.
        /// </summary>
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Test Run Context\n");
            
            AppendMetadataMarkdown(sb);
            AppendEnvironmentMarkdown(sb);
            AppendStateMarkdown(sb);
            AppendBreadcrumbsMarkdown(sb);
            
            return sb.ToString();
        }

        private void AppendMetadataMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Test Information");
            sb.AppendLine($"- **Test Name**: {TestName}");
            sb.AppendLine($"- **Test ID**: {TestId}");
            sb.AppendLine($"- **Start Time**: {StartTime:yyyy-MM-dd HH:mm:ss}");
            if (EndTime != default)
            {
                sb.AppendLine($"- **End Time**: {EndTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"- **Duration**: {(EndTime - StartTime).TotalSeconds:F2}s");
            }
            else
            {
                sb.AppendLine($"- **Duration (Active)**: {(DateTime.Now - StartTime).TotalSeconds:F2}s");
            }
            sb.AppendLine();
        }

        private void AppendEnvironmentMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Environment");
            sb.AppendLine($"- **Scene**: {SceneName}");
            sb.AppendLine($"- **Unity Version**: {UnityVersion}");
            sb.AppendLine($"- **Package Version**: {PackageVersion}");
            sb.AppendLine($"- **Input Mode**: {ActiveInputMode}");
            sb.AppendLine();
        }

        private void AppendStateMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## Test State");
            sb.AppendLine($"- **Last Action**: {LastAction ?? "N/A"}");
            sb.AppendLine($"- **Last Panel**: {LastPanel ?? "N/A"}");
            sb.AppendLine($"- **Current Intent**: {LastIntent ?? "N/A"}");
            
            if (LastPlacementAttempt != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Last Placement Attempt");
                sb.AppendLine($"- **Position**: {LastPlacementAttempt.Position}");
                sb.AppendLine($"- **Definition ID**: {LastPlacementAttempt.DefinitionId}");
                sb.AppendLine($"- **Result**: {LastPlacementAttempt.Result}");
            }
            sb.AppendLine();
        }

        private void AppendBreadcrumbsMarkdown(StringBuilder sb)
        {
            sb.AppendLine("## 🥖 Breadcrumbs (High-Level Timeline)");
            sb.AppendLine("| Frame | Time | Type | Message |");
            sb.AppendLine("|-------|------|------|---------|");
            foreach (var crumb in Breadcrumbs)
            {
                sb.AppendLine($"| {crumb.Frame} | {crumb.Timestamp:F2}s | **{crumb.Type}** | {crumb.Message} |");
            }
            sb.AppendLine();
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";
            
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }

    /// <summary>
    /// A single high-level event in the test timeline.
    /// </summary>
    public class Breadcrumb
    {
        public int Frame { get; set; }
        public float Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public Breadcrumb(string type, string message)
        {
            Frame = UnityEngine.Time.frameCount;
            Timestamp = UnityEngine.Time.time;
            Type = type;
            Message = message;
        }
    }

    /// <summary>
    /// A single log entry in the unified diagnostic stream.
    /// </summary>
    public class LogEntry
    {
        public int Frame { get; set; }
        public float Timestamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Severity { get; set; }
        public int RepeatCount { get; set; } = 1;

        public LogEntry(string type, string message, string stackTrace = "", LogType severity = LogType.Log)
        {
            Frame = UnityEngine.Time.frameCount;
            Timestamp = UnityEngine.Time.time;
            Type = type;
            Message = message;
            StackTrace = stackTrace;
            Severity = severity;
        }
    }

    /// <summary>
    /// Represents a building placement attempt for diagnostics.
    /// </summary>
    public class PlacementAttempt
    {
        public Vector2Int Position { get; set; }
        public string DefinitionId { get; set; }
        public string Result { get; set; }

        public PlacementAttempt(Vector2Int position, string definitionId, string result)
        {
            Position = position;
            DefinitionId = definitionId;
            Result = result;
        }
    }
}
