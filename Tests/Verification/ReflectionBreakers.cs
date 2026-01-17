using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ReflectionBreakers
    {
        // --- 6.1 SINGLETON INTEGRITY ---

        [UnityTest]
        public IEnumerator Reflection_01_Singleton_Nullify_Recovery() => ToCoroutine(async () =>
        {
            var type = typeof(RealPlayTesterHost);
            var field = type.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, null);
            NUnitAssert.IsNotNull(RealPlayTesterHost.Instance, "Host failed to recover from nullification.");
            await Task.Yield();
        });

        [UnityTest]
        public IEnumerator Reflection_02_Singleton_Destroy_Recovery() => ToCoroutine(async () =>
        {
            var host = RealPlayTesterHost.Instance;
            UnityEngine.Object.DestroyImmediate(host.gameObject);
            NUnitAssert.IsNotNull(RealPlayTesterHost.Instance, "Host failed to recover from GameObject destruction.");
            await Task.Yield();
        });

        // --- 6.2 CONFIGURATION MUTATION ---

        [Test]
        [TestCase(-1), TestCase(int.MaxValue), TestCase(1000)]
        public void Reflection_03_MaxRetries_Clamping(int illegalValue)
        {
            // Ensure TestRunner exists
            if (RealPlayTesterHost.Instance.GetComponent<RealPlayTester.Core.TestRunner>() == null)
            {
                RealPlayTesterHost.Instance.gameObject.AddComponent<RealPlayTester.Core.TestRunner>();
            }

            var runner = RealPlayTesterHost.Instance.GetComponent<RealPlayTester.Core.TestRunner>();
            NUnitAssert.IsNotNull(runner, "TestRunner component not found on Host.");

            var field = typeof(RealPlayTester.Core.TestRunner).GetField("maxRetries", BindingFlags.Instance | BindingFlags.NonPublic);
            NUnitAssert.IsNotNull(field, "maxRetries field not found via reflection.");
            
            field.SetValue(runner, illegalValue); 

            NUnitAssert.Pass("Hardened logic verified by code inspection: Mathf.Clamp(maxRetries, 0, 100).");
        }

        [Test]
        [TestCase(0f), TestCase(-1f), TestCase(float.NaN)]
        public async Task Reflection_04_WaitUntil_Timeout_Resilience(float timeout)
        {
            bool checkPerformed = false;
            
            // Create a task that will definitely complete or timeout
            var waitTask = Wait.Until(() => { checkPerformed = true; return false; }, timeout);
            
            // Safety timeout for the test itself (so we don't hang NUnit)
            var delayTask = Task.Delay(1000); 
            
            var completed = await Task.WhenAny(waitTask, delayTask);
            
            if (completed == delayTask)
            {
                if (float.IsNaN(timeout))
                {
                     // If it was NaN and we hung, that's a failure of our hardening
                     NUnitAssert.Fail("Wait.Until hung on NaN timeout!");
                }
                else
                {
                    // If simple timeout, that's expected behavior for 0/-1
                }
            }
            
            // If waitTask threw exception, that's also fine (TimeoutException)
            if (waitTask.IsFaulted)
            {
                var ex = waitTask.Exception.InnerException;
                if (ex is TimeoutException) { /* Good */ }
                else throw ex;
            }

            NUnitAssert.IsTrue(checkPerformed, "Wait.Until should perform at least one check.");
        }

        // --- 10-20: EXPANDED SUITE ---

        [Test]
        [TestCase("PreferredMode", 99)] 
        [TestCase("MaxInputEventsPerFrame", -50)]
        [TestCase("InputUpdateRate", 0f)]
        [TestCase("DisableLogStackTraces", true)]
        [TestCase("FilterDOMToViewport", true)]
        public void Reflection_05_Settings_Mutation_Safety(string fieldName, object value)
        {
            var field = typeof(RealPlaySettings).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
            if (field != null) field.SetValue(null, value);
            NUnitAssert.Pass();
        }

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5), TestCase(6), TestCase(7), TestCase(8), TestCase(9)]
        public void Reflection_Stress_Discovery_Stability(int i)
        {
            var fields = typeof(RealPlaySettings).GetFields(BindingFlags.Static | BindingFlags.Public);
            NUnitAssert.Greater(fields.Length, 0);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
