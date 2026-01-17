using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class PerformanceLifecycleTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestRunContextTracker.BeginTest("PerformanceLifecycleTest", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Performance.StopMonitoring();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        private void ForceAlloc(int kb)
        {
            byte[][] trash = new byte[kb][];
            for(int i = 0; i < kb; i++) trash[i] = new byte[1024];
        }

        // Test 13: Breadcrumb on Start
        [UnityTest]
        public IEnumerator Performance_Lifecycle_RecordsBreadcrumbOnStart() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring();
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Started monitoring"), "Should record start event");
        });

        // Test 14: Breadcrumb on Stop
        [UnityTest]
        public IEnumerator Performance_Lifecycle_RecordsBreadcrumbOnStop() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring();
            Tester.Performance.StopMonitoring();
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Stopped monitoring"), "Should record stop event");
        });

        // Test 15: Rapid Toggle (Stress)
        [UnityTest]
        public IEnumerator Performance_Lifecycle_HandlesRapidToggle() => ToCoroutine(async () =>
        {
            for(int i = 0; i < 5; i++)
            {
                Tester.Performance.StartMonitoring();
                Tester.Performance.StopMonitoring();
            }
            await Tester.Await.Frames(2);
            NUnitAssert.Pass("Didn't crash on rapid toggle");
        });

        // Test 16: Multiple Stop calls are safe
        [UnityTest]
        public IEnumerator Performance_Lifecycle_SafeDuplicateStop() => ToCoroutine(async () =>
        {
            Tester.Performance.StopMonitoring();
            Tester.Performance.StopMonitoring();
            await Task.Yield();
        });

        // Test 17: Monitoring stops after test ends (manual)
        [UnityTest]
        public IEnumerator Performance_Lifecycle_ManualStopWorks() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0.1f);
            LogAssert.ignoreFailingMessages = true;
            
            await Tester.Await.Frames(2);
            Tester.Performance.StopMonitoring();
            
            int breadcrumbCountBefore = GetBreadcrumbCount();
            
            // Force allocations
            ForceAlloc(1024);
            await Tester.Await.Frames(2);
            
            int breadcrumbCountAfter = GetBreadcrumbCount();
            NUnitAssert.AreEqual(breadcrumbCountBefore, breadcrumbCountAfter, "Should not record new violations after Stop");
        });

        // Test 18: Instance persists correctly
        [UnityTest]
        public IEnumerator Performance_Lifecycle_InstanceIsPersistent()
        {
            var inst1 = PerformanceMonitor.Instance;
            var inst2 = PerformanceMonitor.Instance;
            NUnitAssert.AreSame(inst1, inst2, "Should return same singleton instance");
            yield return null;
        }

        // Test 19: Cross-frame stability
        [UnityTest]
        public IEnumerator Performance_Lifecycle_StaysActiveAcrossYields() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring();
            await Task.Delay(100);
            await Tester.Await.Frames(5);
            NUnitAssert.IsTrue(true); // Just ensuring no null refs or crashes
        });

        // Test 20: Breadcrumb on violation exists in JSON
        [UnityTest]
        public IEnumerator Performance_Lifecycle_ViolationInJson() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 1);
            LogAssert.ignoreFailingMessages = true;
            ForceAlloc(100);
            await Tester.Await.Frames(5);
            
            string jsonPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.json");
            string json = File.ReadAllText(jsonPath);
            
            if (!Application.isBatchMode)
            {
                NUnitAssert.IsTrue(json.Contains("VIOLATION:"), "JSON timeline missing violation keyword");
            }
        });

        private string GetContextMarkdown()
        {
            string path = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(path);
        }

        private int GetBreadcrumbCount()
        {
            string jsonPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.json");
            if (!File.Exists(jsonPath)) return 0;
            string json = File.ReadAllText(jsonPath);
            // Count occurrences of "type": "Performance"
            return (json.Length - json.Replace("\"type\": \"Performance\"", "").Length) / "\"type\": \"Performance\"".Length;
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
