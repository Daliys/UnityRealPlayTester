using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Await; // Ensure this is correct based on Wait.cs
using RealPlayTester.Core;  // Ensure RealPlayExecutionContext is available
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class AsyncVerificationTests
    {
        [UnityTest]
        public IEnumerator Wait_Until_Succeeds() => ToCoroutine(async () =>
        {
            bool condition = false;
            
            // Set condition to true after a short delay
            _ = Task.Run(async () => 
            {
                await Task.Delay(100);
                condition = true;
            });

            await Wait.Until(() => condition, timeoutSeconds: 1.0f);
            NAssert.IsTrue(condition, "Condition should be true after waiting.");
        });

        [UnityTest]
        public IEnumerator Wait_Until_Timeouts() => ToCoroutine(async () =>
        {
            bool condition = false;
            float timeout = 0.2f;

            try
            {
                await Wait.Until(() => condition, timeoutSeconds: timeout);
                NAssert.Fail("Wait.Until should have thrown TimeoutException");
            }
            catch (TimeoutException)
            {
                // Expected
            }
        });

        [UnityTest]
        public IEnumerator Wait_Seconds_Realtime() => ToCoroutine(async () =>
        {
            float waitTime = 0.5f;
            float start = Time.realtimeSinceStartup;
            
            await Wait.Seconds(waitTime, unscaled: true);
            
            float elapsed = Time.realtimeSinceStartup - start;
            NAssert.GreaterOrEqual(elapsed, waitTime - 0.05f, "Should wait at least the specified time (minus small delta)");
        });

        [UnityTest]
        public IEnumerator Wait_UntilWithDiagnostics_Timeouts_WithContext() => ToCoroutine(async () =>
        {
            float timeout = 0.1f;
            string context = "CheckingMyCondition";

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Wait.UntilWithDiagnostics timed out.*"));

            try
            {
                await Wait.UntilWithDiagnostics(() => false, timeout: timeout, context: context);
                NAssert.Fail("Should have thrown TimeoutException");
            }
            catch (TimeoutException ex)
            {
                NAssert.IsTrue(ex.Message.Contains(context), $"Exception message should contain context '{context}'. Got: {ex.Message}");
                NAssert.IsTrue(ex.Message.Contains("timed out"), "Exception message should mention timeout.");
            }
        });

        // Helper to run async Tasks as Unity Coroutines
        public static IEnumerator ToCoroutine(Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                else
                    throw new Exception("Task failed without exception");
            }
        }
    }
}
