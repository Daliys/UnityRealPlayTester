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

        public static async Task<string> WriteFailureBundleAsync(string testName, TestRunContext context, string screenshotPath = null, string hierarchyDump = null, Exception exception = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return null;

            if (string.IsNullOrEmpty(hierarchyDump)) hierarchyDump = VisualTreeLogger.DumpHierarchy();
            string jsonDump = Tester.Perception.DumpHierarchyJson();

            try
            {
                screenshotPath = await EnsureScreenshotReady(screenshotPath);
                string bundlePath = CreateBundleDirectory(testName);
                
                WriteHierarchyFiles(bundlePath, hierarchyDump, jsonDump);
                
                var reportData = new ReportData { TestName = testName, Context = context, ScreenshotPath = screenshotPath, Exception = exception, HierarchyDump = hierarchyDump };
                WriteReportFiles(bundlePath, reportData);
                
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

        private struct ReportData
        {
            public string TestName;
            public TestRunContext Context;
            public string ScreenshotPath;
            public Exception Exception;
            public string HierarchyDump;
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

        private static void WriteHierarchyFiles(string bundlePath, string txt, string json)
        {
            if (!string.IsNullOrEmpty(txt)) File.WriteAllText(Path.Combine(bundlePath, "hierarchy.txt"), txt);
            if (!string.IsNullOrEmpty(json)) File.WriteAllText(Path.Combine(bundlePath, "hierarchy.json"), json);
        }

        private static void WriteReportFiles(string bundlePath, ReportData data)
        {
            if (!string.IsNullOrEmpty(data.ScreenshotPath) && File.Exists(data.ScreenshotPath))
            {
                File.Copy(data.ScreenshotPath, Path.Combine(bundlePath, "failure_screenshot.png"), true);
            }

            File.WriteAllText(Path.Combine(bundlePath, "ai_report.md"), GenerateAiReport(data));
            File.WriteAllText(Path.Combine(bundlePath, "diagnostics.json"), data.Context.ToJson());
        }

        private static void CopyLogsAndResults(string bundlePath)
        {
            string testResultsSource = Path.Combine(RealPlayEnvironment.TestReportsPath, "test-results.json");
            if (File.Exists(testResultsSource)) File.Copy(testResultsSource, Path.Combine(bundlePath, "test-results.json"), true);
            CopyGameLogs(bundlePath);
        }

        [Obsolete("Use WriteFailureBundleAsync")]
        public static string WriteFailureBundle(string testName, TestRunContext context, string screenshotPath = null, string hierarchyDump = null)
        {
            if (string.IsNullOrEmpty(screenshotPath)) screenshotPath = Capture.Screenshot("failure_sync_auto");
            var task = WriteFailureBundleAsync(testName, context, screenshotPath, hierarchyDump);
            return task.IsCompleted ? task.Result : null; 
        }

        private static void CopyGameLogs(string bundlePath)
        {
            List<string> logPaths = GetPotentialLogPaths();
            string logsDir = Path.Combine(bundlePath, "Logs");
            Directory.CreateDirectory(logsDir);
            foreach (string logPath in logPaths) CopyLogFile(logPath, logsDir);
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
                FileInfo fi = new FileInfo(logPath);
                if (fi.Length > 1024 * 1024)
                {
                    using (FileStream fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(-1024 * 1024, SeekOrigin.End);
                        using (FileStream ds = new FileStream(destPath, FileMode.Create, FileAccess.Write)) fs.CopyTo(ds);
                    }
                }
                else File.Copy(logPath, destPath, true);
            }
            catch (Exception ex) { Debug.LogWarning($"Could not copy log {logPath}: {ex.Message}"); }
        }

        private static string GenerateAiReport(ReportData data)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# AI Failure Analysis Report: {data.TestName}");
            sb.AppendLine($"> Generated at: {DateTime.Now}\n");

            sb.AppendLine("## 1. Test Context");
            sb.AppendLine($"- **Status**: FAILED");
            if (data.Exception != null)
            {
                sb.AppendLine($"- **Failure Type**: {data.Exception.GetType().Name}");
                sb.AppendLine($"- **Message**: {data.Exception.Message}");
            }
            sb.AppendLine($"- **Duration**: {(DateTime.Now - data.Context.Info.StartTime).TotalSeconds:F2}s");
            sb.AppendLine($"- **Last Action**: {data.Context.State.LastAction ?? "None"}");
            sb.AppendLine($"- **AI Intent**: {data.Context.State.LastIntent ?? "None Recorded"}\n");

            sb.AppendLine("![Failure Screenshot](failure_screenshot.png)\n");
            AppendTimelineMarkdown(sb, data.Context);
            
            sb.AppendLine("## 4. Semantic Hierarchy Dump\n```text");
            sb.AppendLine(data.HierarchyDump);
            sb.AppendLine("```\n");
            
            sb.AppendLine("## 5. Diagnostic Summary");
            sb.AppendLine(data.Context.ToMarkdown());

            return sb.ToString();
        }

        private static void AppendTimelineMarkdown(System.Text.StringBuilder sb, TestRunContext context)
        {
            sb.AppendLine("## 3. Diagnostic Timeline");
            var logs = context.Logs;
            if (logs == null || logs.Count == 0) { sb.AppendLine("*No diagnostic logs captured.*"); return; }

            sb.AppendLine("| Frame | Time | Type | Message |");
            sb.AppendLine("|-------|------|------|---------|");
            int start = Math.Max(0, logs.Count - 30);
            for (int i = start; i < logs.Count; i++)
            {
                var log = logs[i];
                string msg = log.Message.Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");
                if (msg.Length > 120) msg = msg.Substring(0, 117) + "...";
                sb.AppendLine($"| {log.Frame} | {log.Timestamp:F2}s | {log.Type} | {msg} |");
            }
            sb.AppendLine("\n> *Showing last 30 logs. See diagnostics.json for full trace.*\n");
        }

        private static string CreateBundleDirectory(string testName)
        {
            string sanitized = SanitizeFileName(testName);
            string bundleName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{sanitized}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string path = Path.Combine(BasePath, bundleName);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown_test";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = name;
            foreach (char c in invalidChars) sanitized = sanitized.Replace(c, '_');
            return sanitized;
        }
    }
}
