using System;
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
    public class PerformanceFPSTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestRunContextTracker.BeginTest("PerformanceFPSTest", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Performance.StopMonitoring();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // Test 6: Basic FPS violation detection
        [UnityTest]
        public IEnumerator FPSWatcher_DetectsViolation() => ToCoroutine(async () =>
        {
            // 0.1ms is impossible to hit, ensuring violation
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f, maxConsecutiveViolations: 1);
            LogAssert.ignoreFailingMessages = true;

            await Tester.Await.Frames(5);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("FPS violation"), "Should have recorded FPS violation");
        });

        // Test 7: Consecutive violation logic (Requires multiple frames)
        [UnityTest]
        public IEnumerator FPSWatcher_RequiresConsecutiveFrames() => ToCoroutine(async () =>
        {
            // Set to 5 consecutive frames
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f, maxConsecutiveViolations: 5);
            LogAssert.ignoreFailingMessages = true;

            await Tester.Await.Frames(2);
            string mdBefore = GetContextMarkdown();
            NUnitAssert.IsFalse(mdBefore.Contains("FPS violation"), "Should NOT violate after only 2 frames");

            await Tester.Await.Frames(10);
            string mdAfter = GetContextMarkdown();
            NUnitAssert.IsTrue(mdAfter.Contains("FPS violation"), "Should violate after 5+ frames");
        });

        // Test 8: Zero threshold disabled
        [UnityTest]
        public IEnumerator FPSWatcher_DisabledWhenZero() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0); 
            await Tester.Await.Frames(10);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsFalse(md.Contains("FPS violation"), "Zero threshold should disable FPS watcher");
        });

        // Test 9: Correct breadcrumb formatting
        [UnityTest]
        public IEnumerator FPSWatcher_RecordsDetailedBreadcrumb() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f, maxConsecutiveViolations: 1);
            LogAssert.ignoreFailingMessages = true;

            await Tester.Await.Frames(2);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("consecutive frames"), "Breadcrumb should contain technical details");
        });

        // Test 10: CurrentFPS Reporting
        [UnityTest]
        public IEnumerator FPSWatcher_CurrentFPS_ReturnsValidValue() => ToCoroutine(async () =>
        {
            await Tester.Await.Frames(2);
            float fps = Tester.Performance.CurrentFPS;
            NUnitAssert.Greater(fps, 0, "CurrentFPS should be greater than zero");
            NUnitAssert.Less(fps, 10000, "CurrentFPS should be within sane bounds");
        });

        // Test 11: FPS Watcher respects unscaled time (Robustness)
        [UnityTest]
        public IEnumerator FPSWatcher_WorksWithSlowMotion() => ToCoroutine(async () =>
        {
            Time.timeScale = 0.1f; // Slow motion
            try 
            {
                Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f, maxConsecutiveViolations: 1);
                LogAssert.ignoreFailingMessages = true;
                await Tester.Await.Frames(5);
                
                string md = GetContextMarkdown();
                NUnitAssert.IsTrue(md.Contains("FPS violation"), "Should still detect violation based on unscaledDeltaTime");
            }
            finally { Time.timeScale = 1.0f; }
        });

        // Test 12: Multiple starts reconfigures
        [UnityTest]
        public IEnumerator FPSWatcher_ReconfiguresOnRestart() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 1000f); // Impossible to violate
            await Tester.Await.Frames(2);
            
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f); // Immediate violation
            LogAssert.ignoreFailingMessages = true;
            await Tester.Await.Frames(5);

            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("FPS violation"), "Should reconfigure to more aggressive threshold");
        });

        private string GetContextMarkdown()
        {
            string path = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(path);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
