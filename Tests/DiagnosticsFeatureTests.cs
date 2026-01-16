using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using RealPlayTester.Utilities;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class DiagnosticsFeatureTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("DiagnosticsFeatureRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) UnityEngine.Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // 11. Deduplication with Varying Severity
        [UnityTest]
        public IEnumerator LogDeduplication_Handles_SeverityChange()
        {
            TestRunContextTracker.BeginTest("DedupeSeverity", "Scene");
            
            Debug.Log("Message");
            Debug.Log("Message"); // Deduplicated
            Debug.LogWarning("Message"); // NOT deduplicated (severity diff)
            
            var ctx = TestRunContextTracker.Current;
            NUnitAssert.AreEqual(2, ctx.Logs.Count, "Logs with different severity should not be merged");
            yield return null;
        }

        // 12. Timeline Persistence across Scene Load
        [UnityTest]
        public IEnumerator Breadcrumbs_Persist_Across_SceneChange() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("ScenePersist", "SceneA");
            Tester.Utility.TestStep("Before Load");
            
            // Trigger a reload of the current scene to ensure it is in Build Settings
            string currentScene = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(currentScene)) {
                NUnitAssert.Ignore("Active scene name is empty, skipping reload test.");
                return;
            }

            SceneManager.LoadScene(currentScene);
            await Task.Yield();
            
            Tester.Utility.TestStep("After Load");
            
            var ctx = TestRunContextTracker.Current;
            NUnitAssert.IsTrue(ctx.Breadcrumbs.Count >= 2, "Timeline should survive scene transitions");
        });

        // 13. Performance Monitor: High Physics Step
        [UnityTest]
        public IEnumerator Performance_Handles_UnstablePhysicsStep() => ToCoroutine(async () =>
        {
            Time.fixedDeltaTime = 0.5f; // Extremely slow physics
            try {
                Tester.Performance.StartMonitoring(maxFrameTimeMs: 10f);
                LogAssert.ignoreFailingMessages = true;
                await Tester.Await.Frames(2);
                NUnitAssert.Pass("Profiler handled slow physics update without crashing");
            } finally { Time.fixedDeltaTime = 0.02f; }
        });

        // 14. Large Hierarchy Serialization (Stress)
        [UnityTest]
        public IEnumerator FailureBundle_LargeHierarchy_Integrity() => ToCoroutine(async () =>
        {
            for(int i = 0; i < 50; i++) new GameObject("Waste_" + i).transform.SetParent(_testRoot.transform);
            
            var ctx = TestRunContextTracker.BeginTest("LargeBundle", "Scene");
            string path = await FailureBundleWriter.WriteFailureBundleAsync("LargeTest", ctx, null, "Huge Dump");
            
            NUnitAssert.IsTrue(Directory.Exists(path));
            NUnitAssert.IsTrue(File.Exists(Path.Combine(path, "ai_report.md")));
        });

        // 15. Context Log Truncation Check
        [UnityTest]
        public IEnumerator Context_Serialization_HandlesLargeLogs()
        {
            var ctx = new TestRunContext();
            for(int i = 0; i < 1000; i++) ctx.Logs.Add(new LogEntry("Library", "Log Line " + i));
            
            string json = ctx.ToJson();
            NUnitAssert.Less(json.Length, 5 * 1024 * 1024, "Serialized JSON is too massive for AI context");
            yield return null;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}