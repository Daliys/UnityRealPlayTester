using System;
using System.Text;
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
        public PlacementAttempt LastPlacementAttempt { get; set; }

        public TestRunContext()
        {
            TestId = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
            UnityVersion = Application.unityVersion;
            PackageVersion = "1.2.1"; // Will be updated with package
            
            // Detect input mode
#if ENABLE_INPUT_SYSTEM
            ActiveInputMode = "InputSystem";
#else
            ActiveInputMode = "Legacy";
#endif
        }

        /// <summary>
        /// Exports context as JSON string.
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
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
            
            if (LastPlacementAttempt != null)
            {
                sb.AppendLine($"  \"lastPlacementAttempt\": {{");
                sb.AppendLine($"    \"position\": \"{LastPlacementAttempt.Position}\",");
                sb.AppendLine($"    \"definitionId\": \"{EscapeJson(LastPlacementAttempt.DefinitionId)}\",");
                sb.AppendLine($"    \"result\": \"{EscapeJson(LastPlacementAttempt.Result)}\"");
                sb.AppendLine($"  }}");
            }
            else
            {
                sb.AppendLine($"  \"lastPlacementAttempt\": null");
            }
            
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Exports context as human-readable Markdown.
        /// </summary>
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Test Run Context");
            sb.AppendLine();
            sb.AppendLine("## Test Information");
            sb.AppendLine($"- **Test Name**: {TestName}");
            sb.AppendLine($"- **Test ID**: {TestId}");
            sb.AppendLine($"- **Start Time**: {StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **End Time**: {EndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Duration**: {(EndTime - StartTime).TotalSeconds:F2}s");
            sb.AppendLine();
            sb.AppendLine("## Environment");
            sb.AppendLine($"- **Scene**: {SceneName}");
            sb.AppendLine($"- **Unity Version**: {UnityVersion}");
            sb.AppendLine($"- **Package Version**: {PackageVersion}");
            sb.AppendLine($"- **Input Mode**: {ActiveInputMode}");
            sb.AppendLine();
            sb.AppendLine("## Test State");
            sb.AppendLine($"- **Last Action**: {LastAction ?? "N/A"}");
            sb.AppendLine($"- **Last Panel**: {LastPanel ?? "N/A"}");
            
            if (LastPlacementAttempt != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Last Placement Attempt");
                sb.AppendLine($"- **Position**: {LastPlacementAttempt.Position}");
                sb.AppendLine($"- **Definition ID**: {LastPlacementAttempt.DefinitionId}");
                sb.AppendLine($"- **Result**: {LastPlacementAttempt.Result}");
            }
            
            return sb.ToString();
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
