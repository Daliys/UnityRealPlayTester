using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using RealPlayTester.Await;

namespace RealPlayTester.Tests.Verification
{
    public class BlackBoxRecorderTests
    {
        [UnityTest]
        public IEnumerator LogCategorization_IsCorrect()
        {
            // Setup a context
            var context = TestRunContextTracker.BeginTest("LogCategorizationTest", "UnitTestScene");
            
            try
            {
                // Ensure environment is ready
                Canvas.ForceUpdateCanvases();
                yield return null;

                // Inject various logs
                Debug.Log("[INPUT] Click at (100, 100)");
                TestLog.Info("Starting test step");
                Wait.Step("First Step");
                Debug.Log("Generic game log");
                
                // Yield for one frame to let LogInterceptor process if async (it's sync but good practice)
                yield return null;
                
                // Verify Categorization
                NUnit.Framework.Assert.IsTrue(context.Logs.Any(l => l.Type == "Input" && l.Message.Contains("Click")), "Input log not categorized correctly");
                NUnit.Framework.Assert.IsTrue(context.Logs.Any(l => l.Type == "Test" && l.Message.Contains("Starting test step")), "Test log not categorized correctly");
                NUnit.Framework.Assert.IsTrue(context.Logs.Any(l => (l.Type == "Step" || l.Type == "Test") && l.Message.Contains("First Step")), "Step log not categorized correctly");
                NUnit.Framework.Assert.IsTrue(context.Logs.Any(l => l.Type == "Game" && l.Message.Contains("Generic game log")), "Game log not categorized correctly");
            }
            finally
            {
                TestRunContextTracker.EndTest();
            }
        }

        [UnityTest]
        public IEnumerator FailureBundle_IncludesJsonHierarchy()
        {
            var context = TestRunContextTracker.BeginTest("BundleJsonTest", "UnitTestScene");
            
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ReadPixels"));
                string bundlePath = FailureBundleWriter.WriteFailureBundle("BundleJsonTest", context);
                NUnit.Framework.Assert.IsNotNull(bundlePath, "Failure bundle path should not be null");
                
                string jsonPath = Path.Combine(bundlePath, "hierarchy.json");
                NUnit.Framework.Assert.IsTrue(File.Exists(jsonPath), "hierarchy.json should exist in the failure bundle");
                
                string jsonContent = File.ReadAllText(jsonPath);
                NUnit.Framework.Assert.IsTrue(jsonContent.Trim().StartsWith("{"), "hierarchy.json should be a valid JSON object");
            }
            finally
            {
                TestRunContextTracker.EndTest();
            }
            
            yield return null;
        }
    }
}
