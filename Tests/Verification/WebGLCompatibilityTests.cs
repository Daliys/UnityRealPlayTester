using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Await;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class WebGLCompatibilityTests
    {
        [Test]
        public void WebGL_Threading_Awareness_Check()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NUnitAssert.IsFalse(SimulatedInputGuard.IsThreadingSupported);
#else
            NUnitAssert.IsTrue(SimulatedInputGuard.IsThreadingSupported);
#endif
        }

        [UnityTest]
        public IEnumerator WebGL_Safe_Wait_Seconds_NoDeadlock() => ToCoroutine(async () =>
        {
            float start = Time.realtimeSinceStartup;
            await Wait.Seconds(0.1f, unscaled: true);
            float elapsed = Time.realtimeSinceStartup - start;
            NUnitAssert.GreaterOrEqual(elapsed, 0.09f, "Wait.Seconds finished too early!");
        });

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4), TestCase(5)]
        [TestCase(6), TestCase(7), TestCase(8), TestCase(9), TestCase(10)]
        [TestCase(11), TestCase(12), TestCase(13), TestCase(14), TestCase(15)]
        [TestCase(16), TestCase(17), TestCase(18), TestCase(19)]
        public async Task WebGL_Stress_Wait_Burst(int iteration)
        {
            await Wait.Seconds(0.01f);
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