using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RealPlayTester.Core
{
    public sealed partial class TestRunner
    {
        private void GenerateJSONReport(List<TestResult> results)
        {
            var report = BuildReport(results);
            FireReportEvents(report, results);

            if (CustomReportHandler != null)
            {
                ExecuteCustomHandler(report, results);
                return;
            }

            WriteReportToFile(report);
        }

        private TestReport BuildReport(List<TestResult> results)
        {
            return new TestReport
            {
                totalTests = results.Count,
                passed = results.Count(r => r.Passed),
                failed = results.Count(r => !r.Passed),
                duration = results.Sum(r => r.DurationSeconds),
                results = results
            };
        }

        private void FireReportEvents(TestReport report, List<TestResult> results)
        {
            try
            {
                OnReportGenerated?.Invoke(report);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Report event handler error: " + ex.Message);
            }
        }

        private void ExecuteCustomHandler(TestReport report, List<TestResult> results)
        {
            try
            {
                CustomReportHandler.HandleReport(report, results);
                RealPlayLog.Info("Custom report handler executed.");
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Custom report handler error: " + ex.Message);
            }
        }

        private void WriteReportToFile(TestReport report)
        {
            try
            {
                string path = string.IsNullOrEmpty(ReportOutputPath)
                    ? Path.Combine(RealPlayEnvironment.TestReportsPath, "test-results.json")
                    : ReportOutputPath;

                EnsureDirectoryExists(path);
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                RealPlayLog.Info("Wrote test report to " + path);
            }
            catch (Exception ex)
            {
                RealPlayLog.Warn("Failed to write test report: " + ex.Message);
            }
        }

        private void EnsureDirectoryExists(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
