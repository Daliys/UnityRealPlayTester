using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class LifeCycleVerificationTests
    {
        private static int _taskCounter = 0;
        private static CancellationTokenSource _testCts;
        
        [SetUp]
        public void Setup()
        {
            _taskCounter = 0;
            _testCts = null;
        }

        [UnityTest, Order(1)]
        public IEnumerator ProperCancellation_UsesOwnTokenSource() => ToCoroutine(async () =>
        {
            // CORRECT PATTERN: Create your own CTS for background tasks
            _testCts = new CancellationTokenSource();
            
            // Start background task with explicit token
            _ = BackgroundTask(_testCts.Token);
            
            // Give task time to start
            await Task.Delay(50);
            
            // Verify task started
            NAssert.Greater(_taskCounter, 0, "Background task should have started");
            
            // Cancel and cleanup
            _testCts.Cancel();
            _testCts.Dispose();
            _testCts = null;
        });

        [UnityTest, Order(2)]
        public IEnumerator NextTest_VerifyCleanup() => ToCoroutine(async () =>
        {
            // Wait to ensure previous task had time to stop
            await Task.Delay(100);
            
            int counterBefore = _taskCounter;
            await Task.Delay(50);
            int counterAfter = _taskCounter;
            
            // If properly cancelled, counter should not increase
            NAssert.AreEqual(counterBefore, counterAfter, 
                "Background task from previous test should have been cancelled and stopped incrementing.");
        });

        private async Task BackgroundTask(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    await RealPlayTester.Await.Wait.Seconds(0.01f, unscaled: true, token: token); 
                    _taskCounter++;
                }
            }
            catch (System.OperationCanceledException)
            {
                // Expected when cancelled
            }
            catch (System.Exception ex)
            {
                 Debug.Log("BackgroundTask error: " + ex);
            }
        }

        public static IEnumerator ToCoroutine(System.Func<Task> action)
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
