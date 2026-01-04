using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using RealPlayTester.Assert;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Generates comprehensive failure bundles when tests fail.
    /// Collects logs, screenshots, diagnostics, and hierarchy dumps.
    /// </summary>
    public static class FailureBundleWriter
    {
        private static string BasePath => Path.Combine(RealPlayEnvironment.TestReportsPath, "FailureBundles");

        /// <summary>
        /// Write a failure bundle for the given test asynchronously.
        /// </summary>
        public static async Task<string> WriteFailureBundleAsync(string testName, TestRunContext context, string screenshotPath = null, string hierarchyDump = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return null;

            try
            {
                if (string.IsNullOrEmpty(hierarchyDump)) hierarchyDump = VisualTreeLogger.DumpHierarchy();
                screenshotPath = await EnsureScreenshotReady(screenshotPath);
                
                string bundlePath = CreateBundleDirectory(testName);
                WriteHierarchyFiles(bundlePath, hierarchyDump);
                WriteReportFiles(bundlePath, testName, context, hierarchyDump, screenshotPath);
                CopyLogsAndResults(bundlePath);

                RealPlayLog.Info($"Failure bundle written to: {bundlePath}");
                return bundlePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write failure bundle: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> EnsureScreenshotReady(string screenshotPath)
        {
            if (string.IsNullOrEmpty(screenshotPath))
            {
                try { return await Capture.ScreenshotAsync("failure_auto"); }
                catch { return Capture.Screenshot("failure_fallback"); }
            }
            return screenshotPath;
        }

        private static void WriteHierarchyFiles(string bundlePath, string hierarchyDump)
        {
            if (!string.IsNullOrEmpty(hierarchyDump)) File.WriteAllText(Path.Combine(bundlePath, "hierarchy.txt"), hierarchyDump);
            File.WriteAllText(Path.Combine(bundlePath, "hierarchy.json"), VisualTreeLogger.DumpHierarchyJson());
        }

        private static void WriteReportFiles(string bundlePath, string testName, TestRunContext context, string hierarchyDump, string screenshotPath)
        {
            if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
            {
                File.Copy(screenshotPath, Path.Combine(bundlePath, "failure_screenshot.png"), true);
            }

            File.WriteAllText(Path.Combine(bundlePath, "ai_report.md"), GenerateAiReport(testName, context, hierarchyDump));
            File.WriteAllText(Path.Combine(bundlePath, "diagnostics.json"), context.ToJson());
        }

        private static void CopyLogsAndResults(string bundlePath)
        {
            string testResultsSource = Path.Combine(RealPlayEnvironment.TestReportsPath, "test-results.json");
            if (File.Exists(testResultsSource)) File.Copy(testResultsSource, Path.Combine(bundlePath, "test-results.json"), true);
            CopyGameLogs(bundlePath);
        }

        /// <summary>Legacy synchronous version (uses fallback capture)</summary>
        [Obsolete("Use WriteFailureBundleAsync")]
        public static string WriteFailureBundle(string testName, TestRunContext context, string screenshotPath = null, string hierarchyDump = null)
        {
            // Fallback: This will trigger the ReadPixels warning but should work via ScreenCapture fallback
            if (string.IsNullOrEmpty(screenshotPath)) screenshotPath = Capture.Screenshot("failure_sync_auto");
            
            // We can't await here, so we just run the Task on a thread or similar? 
            // Better to just provide the sync implementation below for backward compatibility.
            var task = WriteFailureBundleAsync(testName, context, screenshotPath, hierarchyDump);
            return task.IsCompleted ? task.Result : null; 
        }

        private static void CopyGameLogs(string bundlePath)
        {
            List<string> logPaths = GetPotentialLogPaths();
            string logsDir = Path.Combine(bundlePath, "Logs");
            Directory.CreateDirectory(logsDir);

            foreach (string logPath in logPaths)
            {
                CopyLogFile(logPath, logsDir);
            }
        }

        private static List<string> GetPotentialLogPaths()
        {
            List<string> paths = new List<string>();
#if UNITY_EDITOR
            paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log"));
#else
            paths.Add(Path.Combine(Application.persistentDataPath, "Player.log"));
#endif
            paths.Add(Path.Combine(Application.persistentDataPath, "Logs", "game.log"));
            paths.Add(Path.Combine(Application.persistentDataPath, "Logs", "game_session.log"));
            return paths.FindAll(File.Exists);
        }

        private static void CopyLogFile(string logPath, string destDir)
        {
            try
            {
                string destPath = Path.Combine(destDir, Path.GetFileName(logPath));
                File.Copy(logPath, destPath, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not copy log {logPath}: {ex.Message}");
            }
        }

        private static string GenerateAiReport(string testName, TestRunContext context, string hierarchy)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# AI Failure Analysis Report: {testName}");
            sb.AppendLine($"> Generated at: {DateTime.Now}\n");

            AppendSummaryMarkdown(sb, context);
            sb.AppendLine("![Failure Screenshot](failure_screenshot.png)\n");
            
            AppendTimelineMarkdown(sb, context);
            
            sb.AppendLine("## 4. Semantic Hierarchy Dump\n```text");
            sb.AppendLine(hierarchy);
            sb.AppendLine("```\n");
            
            sb.AppendLine("## 5. Diagnostic Summary");
            sb.AppendLine(context.ToMarkdown());

            return sb.ToString();
        }

        private static void AppendSummaryMarkdown(System.Text.StringBuilder sb, TestRunContext context)
        {
            sb.AppendLine("## 1. Test Context");
            sb.AppendLine($"- **Status**: FAILED");
            sb.AppendLine($"- **Duration**: {(DateTime.Now - context.StartTime).TotalSeconds:F2}s");
            sb.AppendLine($"- **Last Action**: {context.LastAction ?? "None"}");
            sb.AppendLine($"- **AI Intent**: {context.LastIntent ?? "None Recorded"}\n");
        }

        private static void AppendTimelineMarkdown(System.Text.StringBuilder sb, TestRunContext context)
        {
            sb.AppendLine("## 3. Diagnostic Timeline");
            if (context.Logs == null || context.Logs.Count == 0)
            {
                sb.AppendLine("*No diagnostic logs captured.*\n");
                return;
            }

            sb.AppendLine("| Frame | Time | Type | Message |");
            sb.AppendLine("|-------|------|------|---------|");
            
            int start = Math.Max(0, context.Logs.Count - 30);
            for (int i = start; i < context.Logs.Count; i++)
            {
                var log = context.Logs[i];
                string msg = log.Message.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
                if (msg.Length > 120) msg = msg.Substring(0, 117) + "...";
                sb.AppendLine($"| {log.Frame} | {log.Timestamp:F2}s | {log.Type} | {msg} |");
            }
            sb.AppendLine("\n> *Showing last 30 logs. See diagnostics.json for full trace.*\n");
        }

        private static string CreateBundleDirectory(string testName)
        {
            string sanitized = SanitizeFileName(testName);
            string bundleName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{sanitized}";
            string path = Path.Combine(BasePath, bundleName);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown_test";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = name;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            return sanitized;
        }
    }
}
