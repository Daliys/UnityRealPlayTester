using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ArchBreakers
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("ArchBreakersRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_080_LogInterceptor_ThreadSafety() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("ThreadLogTest", "None");
            
            string uniqueMsg = "Threaded Log Message " + System.Guid.NewGuid().ToString();
            
            // Use a real thread for more rigorous testing
            bool threadFinished = false;
            var thread = new System.Threading.Thread(() => {
                UnityEngine.Debug.Log(uniqueMsg);
                threadFinished = true;
            });
            thread.Start();

            // Wait for thread and propagation
            float start = Time.realtimeSinceStartup;
            while (!threadFinished && Time.realtimeSinceStartup - start < 2f) await Task.Yield();
            
            await Task.Delay(500); // Wait for Unity's internal log queue
            await RealPlayTester.Await.Wait.Frames(5);

            // Check if it's in logs
            var logs = TestRunContextTracker.Current?.Logs;
            bool found = false;
            if (logs != null)
            {
                lock (TestRunContextTracker.Current.SyncLock)
                {
                    foreach (var l in logs)
                    {
                        if (l.Message.Contains(uniqueMsg)) { found = true; break; }
                    }
                }
            }

            UnityEngine.Debug.Log($"[ISSUE-080] Found threaded log: {found}");
            NUnitAssert.IsTrue(found, "Log from background thread was NOT captured by LogInterceptor!");
            
            TestRunContextTracker.EndTest();
        });

        [UnityTest]
        public IEnumerator ISSUE_081_GC_Alloc_In_Idle() => ToCoroutine(async () =>
        {
            // Just having the host active should not cause allocations every frame
            System.GC.Collect();
            long before = System.GC.GetTotalMemory(true);
            
            // Wait 100 frames
            for (int i = 0; i < 100; i++) await Task.Yield();
            
            long after = System.GC.GetTotalMemory(true);
            long diff = after - before;
            
            UnityEngine.Debug.Log($"[ISSUE-081] GC Alloc after 100 idle frames: {diff / 1024} KB");
            
            // If it's more than a few KB, it's likely per-frame allocations
            NUnitAssert.Less(diff, 50 * 1024, "High GC allocations in idle state!");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
