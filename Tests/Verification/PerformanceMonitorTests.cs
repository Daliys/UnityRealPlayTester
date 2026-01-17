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
    public class PerformanceMonitorTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestRunContextTracker.BeginTest("PerformanceTest", "EmptyScene");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Tester.Performance.StopMonitoring();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_StartsAndStops()
        {
            Tester.Performance.StartMonitoring(1024, 33.3f);
            var monitor = PerformanceMonitor.Instance;
            NUnitAssert.IsNotNull(monitor, "PerformanceMonitor instance should exist");
            
            yield return null;
            
            Tester.Performance.StopMonitoring();
        }

        [UnityTest]
        public IEnumerator PerformanceMonitor_DetectsFPSViolation() => ToCoroutine(async () =>
        {
            // Set a very aggressive threshold (0.1ms = 10000fps) 
            // and 1 violation to trigger immediately.
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0, maxFrameTimeMs: 0.1f, maxConsecutiveViolations: 1);
            
            // Ignore the logs themselves to avoid "Unhandled log message" issues
            LogAssert.ignoreFailingMessages = true;
            
            // Wait several frames
            await Tester.Await.Frames(10);
            
            Tester.Performance.StopMonitoring();
            LogAssert.ignoreFailingMessages = false;

            // Verify via breadcrumbs
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            string mdContent = File.ReadAllText(mdPath);
            NUnitAssert.IsTrue(mdContent.Contains("FPS violation"), "Timeline missing FPS violation");
        });

        [Test]
        public void PerformanceMonitor_ReportsCurrentStats()
        {
            float fps = Tester.Performance.CurrentFPS;
            long mem = Tester.Performance.TotalAllocatedMemory;
            
            NUnitAssert.Greater(fps, 0, "FPS should be reported");
            NUnitAssert.Greater(mem, 0, "Memory should be reported");
        }

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