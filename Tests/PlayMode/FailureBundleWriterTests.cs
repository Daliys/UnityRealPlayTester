using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Diagnostics;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.PlayMode
{
    [TestFixture]
    public class FailureBundleWriterTests
    {
        [UnityTest]
        public IEnumerator WriteFailureBundle_CreatesDirectory()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "TestFailure",
                SceneName = "TestScene"
            };
            context.EndTime = context.StartTime.AddSeconds(1);

            // Act
            string bundlePath = FailureBundleWriter.WriteFailureBundle("TestFailure", context);
            
            yield return null;

            // Assert
            NUnit.Framework.Assert.IsNotNull(bundlePath, "Bundle path should not be null");
            NUnit.Framework.Assert.IsTrue(Directory.Exists(bundlePath), $"Bundle directory should exist at: {bundlePath}");

            // Cleanup
            if (Directory.Exists(bundlePath))
            {
                Directory.Delete(Path.GetDirectoryName(bundlePath), true);
            }
        }

        [UnityTest]
        public IEnumerator WriteFailureBundle_CreatesDiagnosticsJson()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "TestJsonOutput",
                SceneName = "TestScene",
                LastAction = "ClickButton"
            };
            context.EndTime = context.StartTime.AddSeconds(2);

            // Act
            string bundlePath = FailureBundleWriter.WriteFailureBundle("TestJsonOutput", context);
            
            yield return null;

            // Assert
            string diagnosticsPath = Path.Combine(bundlePath, "diagnostics.json");
            NUnit.Framework.Assert.IsTrue(File.Exists(diagnosticsPath), $"diagnostics.json should exist at: {diagnosticsPath}");

            string jsonContent = File.ReadAllText(diagnosticsPath);
            StringAssert.Contains("\"testName\":", jsonContent);
            StringAssert.Contains("TestJsonOutput", jsonContent);

            // Cleanup
            if (Directory.Exists(bundlePath))
            {
                Directory.Delete(Path.GetDirectoryName(bundlePath), true);
            }
        }

        [UnityTest]
        public IEnumerator WriteFailureBundle_CreatesDiagnosticsMarkdown()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "TestMarkdownOutput",
                SceneName = "TestScene"
            };
            context.EndTime = context.StartTime.AddSeconds(1.5f);

            // Act
            string bundlePath = FailureBundleWriter.WriteFailureBundle("TestMarkdownOutput", context);
            
            yield return null;

            // Assert
            string markdownPath = Path.Combine(bundlePath, "diagnostics.md");
            NUnit.Framework.Assert.IsTrue(File.Exists(markdownPath), $"diagnostics.md should exist at: {markdownPath}");

            string mdContent = File.ReadAllText(markdownPath);
            StringAssert.Contains("# Test Run Context", mdContent);

            // Cleanup
            if (Directory.Exists(bundlePath))
            {
                Directory.Delete(Path.GetDirectoryName(bundlePath), true);
            }
        }

        [UnityTest]
        public IEnumerator WriteFailureBundle_CreatesLogsDirectory()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "TestLogs",
                SceneName = "TestScene"
            };

            // Act
            string bundlePath = FailureBundleWriter.WriteFailureBundle("TestLogs", context);
            
            yield return null;

            // Assert
            string logsPath = Path.Combine(bundlePath, "Logs");
            NUnit.Framework.Assert.IsTrue(Directory.Exists(logsPath), $"Logs directory should exist at: {logsPath}");

            // Cleanup
            if (Directory.Exists(bundlePath))
            {
                Directory.Delete(Path.GetDirectoryName(bundlePath), true);
            }
        }
    }
}
