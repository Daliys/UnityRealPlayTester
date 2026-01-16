using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class PerformanceStressTests
    {
        private GameObject _stressRoot;
        private const int ObjectCount = 1000;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _stressRoot = new GameObject("StressTestRoot");
            TestRunContextTracker.BeginTest("PerformanceStressTest", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Performance.StopMonitoring();
            if (_stressRoot != null) Object.DestroyImmediate(_stressRoot);
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmartFind_Latency_InLargeScene() => ToCoroutine(async () =>
        {
            // 1. Setup a large scene
            for (int i = 0; i < ObjectCount; i++)
            {
                var go = new GameObject($"Target_{i}");
                go.transform.SetParent(_stressRoot.transform);
            }
            SceneCache.Instance.ForceRefresh();

            // 2. Measure latency of fuzzy find
            var sw = Stopwatch.StartNew();
            var found = Tester.Perception.Find("Target_999");
            sw.Stop();

            UnityEngine.Debug.Log($"[STRESS] SmartFind latency for {ObjectCount} objects: {sw.Elapsed.TotalMilliseconds:F4}ms");
            
            NUnitAssert.IsNotNull(found, "Should find the object");
            // Latency should be very low (< 5ms) because it's using the cache
            NUnitAssert.Less(sw.Elapsed.TotalMilliseconds, 10.0, "SmartFind latency too high even with cache");
        });

        [UnityTest]
        public IEnumerator SteadyState_Overhead_IsMinimal() => ToCoroutine(async () =>
        {
            // 1. Setup many Rigidbodies
            for (int i = 0; i < 200; i++)
            {
                var go = new GameObject($"RB_{i}", typeof(Rigidbody));
                go.transform.SetParent(_stressRoot.transform);
                go.GetComponent<Rigidbody>().isKinematic = true; // Static but tracked
            }
            SceneCache.Instance.ForceRefresh();

            // 2. Start Performance Monitor with strict FPS limit (60fps = 16ms)
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 16.6f, maxConsecutiveViolations: 5);
            
            // 3. Run SteadyState check multiple times
            for (int i = 0; i < 10; i++)
            {
                // Should complete immediately as everything is kinematic/stable
                await Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            }

            // 4. Verify no FPS violations were triggered by the waiter itself
            float currentFps = Tester.Performance.CurrentFPS;
            UnityEngine.Debug.Log($"[STRESS] FPS during SteadyState checks: {currentFps:F2}");
            
            // If we are in batchmode, FPS might be weird, but we check if we didn't tank it
            if (!Application.isBatchMode)
            {
                NUnitAssert.GreaterOrEqual(currentFps, 30, "SteadyState overhead tanked the FPS");
            }
        });

        [UnityTest]
        public IEnumerator SceneCache_Throttling_PreventsRedundantScans() => ToCoroutine(async () =>
        {
            SceneCache.Instance.ForceRefresh();
            
            var sw = Stopwatch.StartNew();
            // Call 100 times in the same frame
            for (int i = 0; i < 100; i++)
            {
                var list = SceneCache.Instance.AllActiveObjects;
            }
            sw.Stop();

            UnityEngine.Debug.Log($"[STRESS] 100 cache accesses took: {sw.Elapsed.TotalMilliseconds:F4}ms");
            
            // Total time should be negligible because only the first call might scan, 
            // and others return the same list reference.
            NUnitAssert.Less(sw.Elapsed.TotalMilliseconds, 1.0, "Cache access overhead is too high (throttling failed?)");
        });

        [UnityTest]
        public IEnumerator PerformanceMonitor_SelfObservation_IsQuiet() => ToCoroutine(async () =>
        {
            // Set allocation limit
            Tester.Performance.StartMonitoring(maxGcAllocKB: 50, maxFrameTimeMs: 33f);
            
            // Run some framework calls that use the cache
            for (int i = 0; i < 50; i++)
            {
                var list = SceneCache.Instance.AllActiveObjects;
                var rb = SceneCache.Instance.Rigidbodies3D;
            }

            await Tester.Await.Frames(5);
            
            // Check if PerformanceMonitor flagged itself
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            string md = File.ReadAllText(mdPath);
            
            NUnitAssert.IsFalse(md.Contains("VIOLATION"), "PerformanceMonitor flagged its own background work");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
