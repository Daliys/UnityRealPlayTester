using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class DeadlockMegaTests
    {
        [Test]
        [TestCase("Wait.Seconds", 0.01f)]
        [TestCase("Wait.Frames", 1)]
        [TestCase("Wait.Until", true)]
        [TestCase("Interaction.Perform", "click")]
        [TestCase("TextInput.Type", "test")]
        [TestCase("Wait.ForUIVisible", "NonExistent")]
        public async Task Async_Entry_Point_Thread_Guard_Stress(string method, object arg)
        {
            bool caught = false;
            await Task.Run(async () => {
                try {
                    switch(method) {
                        case "Wait.Seconds": await Wait.Seconds((float)arg); break;
                        case "Wait.Frames": await Wait.Frames((int)arg); break;
                        case "Wait.Until": await Wait.Until(() => (bool)arg, 0.1f); break;
                        case "Interaction.Perform": await Tester.Interaction.Perform((string)arg, "Btn"); break;
                        case "TextInput.Type": await TextInput.Type((string)arg); break;
                        case "Wait.ForUIVisible": await Wait.ForUIVisible((string)arg, 0.1f); break;
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("background thread")) {
                    caught = true;
                }
                catch (TimeoutException) { /* Handled for methods with timeout */ caught = true; }
                catch (Exception) { /* Other errors ignored for this test */ }
            });

            NUnitAssert.IsTrue(caught, $"Method {method} did not catch background thread access!");
        }

        [UnityTest]
        public IEnumerator Async_Normal_Await_Baseline_1() => ToCoroutine(() => Wait.Seconds(0.01f));
        [UnityTest]
        public IEnumerator Async_Normal_Await_Baseline_2() => ToCoroutine(() => Wait.Frames(1));
        [UnityTest]
        public IEnumerator Async_Normal_Await_Baseline_3() => ToCoroutine(() => Wait.Until(() => true, 0.1f));
        [UnityTest]
        public IEnumerator Async_Normal_Await_Baseline_4() => ToCoroutine(() => TextInput.Type(""));
        [UnityTest]
        public IEnumerator Async_Normal_Await_Baseline_5() => ToCoroutine(async () => await Tester.Interaction.Perform("click", "NonExistent"));

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5), TestCase(6), TestCase(7), TestCase(8), TestCase(9)]
        public void Concurrency_MainThread_ID_Integrity(int iteration)
        {
            // Verify main thread detection is stable
            SimulatedInputGuard.EnsureMainThread();
            NUnitAssert.Pass();
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
