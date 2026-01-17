using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class SteadyStateTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("SteadyStateTestRoot");
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
        }

        [UnityTest]
        public IEnumerator ForSteadyState_WaitsForRigidbody2DToStop() => ToCoroutine(async () =>
        {
            var rbGo = new GameObject("MovingBody2D", typeof(Rigidbody2D));
            rbGo.transform.SetParent(_testRoot.transform);
            var rb = rbGo.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0;
            rb.linearVelocity = new Vector2(10, 0);

            // Start waiting for steady state
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 2f);
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should still be waiting while Rigidbody2D is moving");

            // Stop the Rigidbody
            rb.linearVelocity = Vector2.zero;
            
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted, "Should have completed after Rigidbody2D stopped");
        });

        [UnityTest]
        public IEnumerator ForSteadyState_WaitsForRigidbody3DToStop() => ToCoroutine(async () =>
        {
            var rbGo = new GameObject("MovingBody3D", typeof(Rigidbody));
            rbGo.transform.SetParent(_testRoot.transform);
            var rb = rbGo.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(0, 10, 0);

            // Start waiting
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 2f);
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should still be waiting while Rigidbody3D is moving");

            // Stop
            rb.linearVelocity = Vector3.zero;
            
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted, "Should have completed after Rigidbody3D stopped");
        });

        [UnityTest]
        public IEnumerator ForSteadyState_WaitsForHierarchyStability() => ToCoroutine(async () =>
        {
            // Start waiting
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 2f);
            
            await Tester.Await.Frames(2);
            
            // Change hierarchy
            var newGo = new GameObject("Interruption");
            newGo.transform.SetParent(_testRoot.transform);
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should still be waiting because hierarchy changed");

            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted, "Should eventually reach steady state");
        });

        [UnityTest]
        public IEnumerator ForSteadyState_ThrowsTimeoutException() => ToCoroutine(async () =>
        {
            var rbGo = new GameObject("ConstantMotion", typeof(Rigidbody));
            rbGo.transform.SetParent(_testRoot.transform);
            var rb = rbGo.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = new Vector3(100, 100, 100);

            // Start waiting with a very short timeout
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 0.5f);
            
            bool timedOut = false;
            try
            {
                await waitTask;
            }
            catch (TimeoutException)
            {
                timedOut = true;
            }

            NUnitAssert.IsTrue(timedOut, "Should have thrown TimeoutException");
        });

        [UnityTest]
        public IEnumerator ForSteadyState_HandlesAnimatorsWithoutControllers() => ToCoroutine(async () =>
        {
            var animGo = new GameObject("StaticAnimator", typeof(Animator));
            animGo.transform.SetParent(_testRoot.transform);
            
            // An animator without a controller should be considered steady
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 3, timeout: 1f);
            
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted, "Should complete with static animator");
        });

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
