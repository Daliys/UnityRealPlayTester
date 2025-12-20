using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Diagnostics
{
    /// <summary>
    /// Generates comprehensive failure bundles when tests fail.
    /// Collects logs, screenshots, diagnostics, and hierarchy dumps.
    /// </summary>
    public static class FailureBundleWriter
    {
        private static readonly string BasePath = Path.Combine(Application.persistentDataPath, "TestReports", "FailureBundles");

        /// <summary>
        /// Write a failure bundle for the given test.
        /// </summary>
        public static string WriteFailureBundle(string testName, TestRunContext context, string screenshotPath = null, string hierarchyDump = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return null;
            }

            try
            {
                // Create bundle directory
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string bundlePath = Path.Combine(BasePath, timestamp, SanitizeFileName(testName));
                Directory.CreateDirectory(bundlePath);

                // Write diagnostics.json
                string diagnosticsPath = Path.Combine(bundlePath, "diagnostics.json");
                File.WriteAllText(diagnosticsPath, context.ToJson());

                // Write diagnostics.md for human readability
                string markdownPath = Path.Combine(bundlePath, "diagnostics.md");
                File.WriteAllText(markdownPath, context.ToMarkdown());

                // Copy screenshot if available
                if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                {
                    string destScreenshot = Path.Combine(bundlePath, "failure_screenshot.png");
                    File.Copy(screenshotPath, destScreenshot, true);
                }

                // Write hierarchy dump if provided
                if (!string.IsNullOrEmpty(hierarchyDump))
                {
                    string hierarchyPath = Path.Combine(bundlePath, "hierarchy.txt");
                    File.WriteAllText(hierarchyPath, hierarchyDump);
                }

                // Copy test results if available
                string testResultsSource = Path.Combine(Application.persistentDataPath, "TestReports", "test-results.json");
                if (File.Exists(testResultsSource))
                {
                    string testResultsDest = Path.Combine(bundlePath, "test-results.json");
                    File.Copy(testResultsSource, testResultsDest, true);
                }

                // Copy game logs
                CopyGameLogs(bundlePath);

                TestLog.Info($"Failure bundle written to: {bundlePath}");
                return bundlePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write failure bundle: {ex.Message}");
                return null;
            }
        }

        private static void CopyGameLogs(string bundlePath)
        {
            // Standard Unity log locations
            List<string> logPaths = new List<string>();

#if UNITY_EDITOR
            // Editor log
            string editorLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
            if (File.Exists(editorLogPath))
            {
                logPaths.Add(editorLogPath);
            }
#else
            // Player log
            string playerLogPath = Path.Combine(Application.persistentDataPath, "Player.log");
            if (File.Exists(playerLogPath))
            {
                logPaths.Add(playerLogPath);
            }
#endif

            // Custom game.log if it exists
            string gameLogPath = Path.Combine(Application.persistentDataPath, "Logs", "game.log");
            if (File.Exists(gameLogPath))
            {
                logPaths.Add(gameLogPath);
            }

            // Custom game_session.log if it exists
            string sessionLogPath = Path.Combine(Application.persistentDataPath, "Logs", "game_session.log");
            if (File.Exists(sessionLogPath))
            {
                logPaths.Add(sessionLogPath);
            }

            // Copy all found logs
            string logsDir = Path.Combine(bundlePath, "Logs");
            Directory.CreateDirectory(logsDir);

            foreach (string logPath in logPaths)
            {
                try
                {
                    string fileName = Path.GetFileName(logPath);
                    string destPath = Path.Combine(logsDir, fileName);
                    File.Copy(logPath, destPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not copy log {logPath}: {ex.Message}");
                }
            }
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
