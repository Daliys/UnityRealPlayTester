using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class FrameworkFeaturesTests
    {
        // Test 12: SmartFind Role Identification
        [UnityTest]
        public IEnumerator SmartFind_IdentifiesRoles()
        {
            var go = new GameObject("ActionBtn", typeof(Button), typeof(RectTransform));
            yield return null;

            var found = SmartFind.Object("ActionBtn");
            NUnitAssert.IsNotNull(found);
            
            var role = RealPlayTester.Utilities.SemanticProbe.GetRoleName(found);
            NUnitAssert.AreEqual("Button", role, "Semantic role should be Button");
            
            Object.DestroyImmediate(go);
        }

        // Test 13: Failure Bundle Creation
        [UnityTest]
        public IEnumerator FailureBundle_IsCreatedOnDisk() => ToCoroutine(async () =>
        {
            var context = TestRunContextTracker.BeginTest("BundleVerification", "DummyScene");
            context.Logs.Add(new LogEntry("Library", "Dummy failure log"));
            
            string bundleDir = await FailureBundleWriter.WriteFailureBundleAsync("BundleVerification", context, null, "Hierarchy Dump");
            
            NUnitAssert.IsTrue(Directory.Exists(bundleDir), "Bundle directory should exist");
            NUnitAssert.IsTrue(File.Exists(Path.Combine(bundleDir, "diagnostics.json")), "JSON missing from bundle");
            NUnitAssert.IsTrue(File.Exists(Path.Combine(bundleDir, "ai_report.md")), "Markdown missing from bundle");
            
            TestRunContextTracker.EndTest();
        });

        // Test 14: Wait.SceneLoaded (Already Loaded)
        [UnityTest]
        public IEnumerator Wait_SceneLoaded_CompletesIfAlreadyLoaded() => ToCoroutine(async () =>
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(currentScene))
            {
                await RealPlayTester.Await.Wait.SceneLoaded(currentScene, 1f);
            }
        });

        // Test 15: Breadcrumbs Interaction Auto-Logging
        [UnityTest]
        public IEnumerator Breadcrumbs_AutoLogsComplexInteractions() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("BreadcrumbAutoLogTest", "Virtual");
            
            await Tester.Interaction.Gestures.MultiTap(new Vector2(50, 50), 2);
            
            string jsonPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.json");
            string json = File.ReadAllText(jsonPath);
            
            NUnitAssert.IsTrue(json.Contains("Gesture: MultiTap"), "Timeline missing multi-tap");
            
            TestRunContextTracker.EndTest();
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                else
                    throw new System.Exception("Task failed without exception");
            }
        }
    }
}
