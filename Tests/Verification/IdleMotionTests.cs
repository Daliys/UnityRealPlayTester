using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class IdleMotionTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("IdleMotionTestRoot");
            TestRunContextTracker.BeginTest("IdleMotionVerification", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SteadyState_IgnoresAmbientTaggedObject() => ToCoroutine(async () =>
        {
            // Create a moving object but tag it as ambient
            var ambientGo = new GameObject("[Ambient] FlagWave", typeof(Rigidbody2D));
            ambientGo.transform.SetParent(_testRoot.transform);
            var rb = ambientGo.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.one * 50f; // High velocity

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            // Wait for steady state - should succeed immediately because object is ignored
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            
            NUnitAssert.IsTrue(waitTask.IsCompleted, "SteadyState should have ignored the ambient tagged object");
        });

        [UnityTest]
        public IEnumerator SteadyState_RespectsIdleVelocityThreshold() => ToCoroutine(async () =>
        {
            // Set a specific idle epsilon
            float customEpsilon = 0.5f;
            RealPlaySettings.IdleVelocityEpsilon = customEpsilon;

            var go = new GameObject("MicroMotion", typeof(Rigidbody2D));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0;
            rb.linearVelocity = new Vector2(0.1f, 0.1f); // Below custom epsilon (0.5)

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            
            NUnitAssert.IsTrue(waitTask.IsCompleted, "SteadyState should have ignored motion below IdleVelocityEpsilon");
            
            // Restore default
            RealPlaySettings.IdleVelocityEpsilon = 0.05f;
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
