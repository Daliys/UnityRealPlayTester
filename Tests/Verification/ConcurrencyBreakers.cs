using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ConcurrencyBreakers
    {
        [UnityTest]
        public IEnumerator ISSUE_041_TaskRun_Access_Violation_Detection() => ToCoroutine(async () =>
        {
            // This lambda will attempt to touch a Unity API from a background thread
            // The library should now throw an InvalidOperationException due to our EnsureMainThread guard,
            // rather than allowing a native crash.
            
            bool exceptionCaught = false;
            string errorMsg = "";

            await Task.Run(async () => {
                try 
                {
                    // This should trigger the guard in SmartFind.Object or Interaction.Perform
                    SmartFind.Object("NonExistent");
                }
                catch (InvalidOperationException ex)
                {
                    exceptionCaught = true;
                    errorMsg = ex.Message;
                }
                catch (Exception ex)
                {
                    errorMsg = "Wrong exception type: " + ex.GetType().Name;
                }
            });

            NUnitAssert.IsTrue(exceptionCaught, "Background Unity API access was NOT caught by the thread guard! " + errorMsg);
            NUnitAssert.IsTrue(errorMsg.ToLower().Contains("background thread"), "Exception message should mention background thread violation. Was: " + errorMsg);
        });

        [UnityTest]
        public IEnumerator ISSUE_041_Interaction_Perform_Thread_Safety() => ToCoroutine(async () =>
        {
            bool exceptionCaught = false;
            
            await Task.Run(async () => {
                try
                {
                    await Tester.Interaction.Perform("click", "Button");
                }
                catch (InvalidOperationException)
                {
                    exceptionCaught = true;
                }
                catch (Exception) {}
            });

            NUnitAssert.IsTrue(exceptionCaught, "Interaction.Perform allowed background thread execution!");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
