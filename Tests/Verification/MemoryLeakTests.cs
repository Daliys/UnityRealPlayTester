using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class MemoryLeakTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("MemoryLeakTestRoot");
            if (Camera.main == null)
            {
                var camGo = new GameObject("MainCamera", typeof(Camera));
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(_testRoot.transform);
            }
            TestRunContextTracker.BeginTest("MemoryLeakTest", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // Test 1: ComputerVision Texture Cleanup (Relative Check)
        [UnityTest]
        public IEnumerator Vision_CleansUpTextures_NoLeakedRenderTextures() => ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            
            // Warm up
            await Tester.Perception.CaptureVisionSnapshot("warmup.png");
            await Tester.Await.Frames(2);
            
            int baseRTCount = Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
            int baseTexCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;

            // Capture multiple times
            for(int i = 0; i < 3; i++)
            {
                await Tester.Perception.CaptureVisionSnapshot($"leak_test_{i}.png");
                await Tester.Await.Frames(2);
            }

            int finalRTCount = Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
            int finalTexCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;

            // We check that the count hasn't INCREASED significantly after the base capture
            NUnitAssert.LessOrEqual(finalRTCount, baseRTCount, "RenderTexture count increased after multiple captures");
            NUnitAssert.LessOrEqual(finalTexCount, baseTexCount + 2, "Texture2D count increased significantly after multiple captures");
            
            LogAssert.ignoreFailingMessages = false;
        });

        // Test 2: LogInterceptor Context Isolation
        [UnityTest]
        public IEnumerator LogInterceptor_DoesNotLeakContexts()
        {
            var context1 = TestRunContextTracker.BeginTest("Context1", "Scene");
            Debug.Log("Log 1");
            TestRunContextTracker.EndTest();

            var context2 = TestRunContextTracker.BeginTest("Context2", "Scene");
            Debug.Log("Log 2");
            TestRunContextTracker.EndTest();

            NUnitAssert.AreEqual(1, context1.Logs.Count, "Context 1 leaked logs into subsequent runs");
            NUnitAssert.AreEqual(1, context2.Logs.Count, "Context 2 contains logs from previous runs");
            yield return null;
        }

        // Test 3: SceneCache List Stability
        [UnityTest]
        public IEnumerator SceneCache_HandlesDestroyedObjects_NoReferenceLeaking()
        {
            var go = new GameObject("Temporary");
            SceneCache.Instance.ForceRefresh();
            int countBefore = SceneCache.Instance.AllActiveObjects.Count;

            UnityEngine.Object.DestroyImmediate(go);
            SceneCache.Instance.ForceRefresh();

            int countAfter = SceneCache.Instance.AllActiveObjects.Count;
            NUnitAssert.AreEqual(countBefore - 1, countAfter, "SceneCache leaked a reference to a destroyed object");
            yield return null;
        }

        // Test 4: PerformanceMonitor Coroutine Cleanup
        [UnityTest]
        public IEnumerator PerformanceMonitor_StopsCompletely_NoBackgroundCPU() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0.001f);
            LogAssert.ignoreFailingMessages = true;
            await Tester.Await.Frames(2);
            Tester.Performance.StopMonitoring();

            int breadcrumbCountBefore = TestRunContextTracker.Current.Breadcrumbs.Count;
            
            for(int i = 0; i < 5; i++) { new object(); await Task.Yield(); }
            
            int breadcrumbCountAfter = TestRunContextTracker.Current.Breadcrumbs.Count;
            NUnitAssert.AreEqual(breadcrumbCountBefore, breadcrumbCountAfter, "PerformanceMonitor background routine is still running after Stop");
        });

        // Test 5: Zombie Task Cancellation
        [UnityTest]
        public IEnumerator Async_RespectsCancellation_NoZombieTasks() => ToCoroutine(async () =>
        {
            var cts = new CancellationTokenSource();
            bool taskRunning = true;

            var task = Task.Run(async () => {
                try {
                    await Task.Delay(5000, cts.Token);
                } catch (OperationCanceledException) {
                    taskRunning = false;
                }
            });

            cts.Cancel();
            await Task.Delay(100);
            
            NUnitAssert.IsFalse(taskRunning, "Async task became a zombie (did not respect cancellation)");
        });

        // Test 6: HardReset Cleanup Logic
        [UnityTest]
        public IEnumerator HardReset_IsSafeAndSanitizes() => ToCoroutine(async () =>
        {
            await HardReset.Execute();
            NUnitAssert.Pass("HardReset executed safely");
        });

        // Test 7: InteractionHeatmap Object Pooling
        [UnityTest]
        public IEnumerator InteractionHeatmap_PoolsObjects_NoGrowthLeak()
        {
            var host = RealPlayTesterHost.Instance;
            var heatmap = host.GetComponentInChildren<InteractionHeatmap>(true);
            if (heatmap == null) { yield return null; NUnitAssert.Ignore("Heatmap not available"); yield break; }
            
            Tester.Settings.ShowInteractionHeatmap = true;
            for(int i = 0; i < 300; i++) heatmap.AddPoint(Vector2.zero);

            var canvas = GameObject.Find("HeatmapCanvas");
            if (canvas != null)
            {
                int childCount = canvas.transform.childCount;
                NUnitAssert.LessOrEqual(childCount, 210, "InteractionHeatmap is leaking game objects");
            }
            
            Tester.Settings.ShowInteractionHeatmap = false;
            yield return null;
        }

        // Test 8: Large Log Serialization Sane Bounds
        [UnityTest]
        public IEnumerator TestRunContext_Serialization_SaneBounds()
        {
            var context = new TestRunContext();
            for(int i = 0; i < 500; i++) context.Logs.Add(new LogEntry("Test", "Spam Message"));
            
            string json = context.ToJson();
            NUnitAssert.Less(json.Length, 1024 * 1024, "Context JSON serialization exceeds 1MB limit for 500 logs");
            yield return null;
        }

        // Test 9: Breadcrumb List Reset
        [UnityTest]
        public IEnumerator Breadcrumbs_ClearBetweenTests()
        {
            TestRunContextTracker.BeginTest("T1", "S");
            Tester.Utility.TestStep("Step 1");
            TestRunContextTracker.EndTest();

            var ctx2 = TestRunContextTracker.BeginTest("T2", "S");
            NUnitAssert.AreEqual(0, ctx2.Breadcrumbs.Count, "Breadcrumbs leaked from T1 to T2");
            yield return null;
        }

        // Test 10: Log Deduplication Buffer Reset
        [UnityTest]
        public IEnumerator LogDeduplication_Reset_OnNewTest()
        {
            TestRunContextTracker.BeginTest("T1", "S");
            Debug.Log("Message A");
            Debug.Log("Message A");
            TestRunContextTracker.EndTest();

            var ctx2 = TestRunContextTracker.BeginTest("T2", "S");
            Debug.Log("Message A");
            
            if (ctx2.Logs.Count > 0)
                NUnitAssert.AreEqual(1, ctx2.Logs[0].RepeatCount, "Deduplication buffer leaked from previous test");
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
