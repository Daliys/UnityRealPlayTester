using System;
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
    public partial class IdleMotionExtendedTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("IdleMotionExtendedTestRoot");
            TestRunContextTracker.BeginTest("IdleMotionExtendedVerification", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
            RealPlaySettings.IgnoreLoopingAnimations = true;
            RealPlaySettings.IdleVelocityEpsilon = 0.05f;
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // 1. 3D Ambient Filter
        [UnityTest]
        public IEnumerator SteadyState_3D_IgnoresAmbientTag() => ToCoroutine(async () =>
        {
            var go = new GameObject("[Ambient] WindFlag", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            go.GetComponent<Rigidbody>().linearVelocity = Vector3.one * 100f;
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 2. VFX Prefix Filter
        [UnityTest]
        public IEnumerator SteadyState_Ignores_VFX_Prefix() => ToCoroutine(async () =>
        {
            var go = new GameObject("vfx_FireSparks", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            go.GetComponent<Rigidbody>().linearVelocity = Vector3.one * 100f;
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 3. Particle Prefix Filter
        [UnityTest]
        public IEnumerator SteadyState_Ignores_Particle_Prefix() => ToCoroutine(async () =>
        {
            var go = new GameObject("ParticleSmoke", typeof(Rigidbody2D));
            go.transform.SetParent(_testRoot.transform);
            go.GetComponent<Rigidbody2D>().linearVelocity = Vector2.one * 100f;
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 4. Waits for 3D Angular Velocity
        [UnityTest]
        public IEnumerator SteadyState_WaitsFor3DAngularVelocity() => ToCoroutine(async () =>
        {
            var go = new GameObject("SpinningTop", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.angularVelocity = new Vector3(0, 100, 0);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 0.5f);
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should wait for angular velocity to stop");

            rb.angularVelocity = Vector3.zero;
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 5. Waits for 2D Angular Velocity
        [UnityTest]
        public IEnumerator SteadyState_WaitsFor2DAngularVelocity() => ToCoroutine(async () =>
        {
            var go = new GameObject("SpinningCoin", typeof(Rigidbody2D));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.angularVelocity = 100f;

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 0.5f);
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should wait for 2D angular velocity to stop");

            rb.angularVelocity = 0f;
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 6. Hierarchy: Detects Activation
        [UnityTest]
        public IEnumerator SteadyState_Hierarchy_DetectsActivation() => ToCoroutine(async () =>
        {
            var hidden = new GameObject("Hidden");
            hidden.transform.SetParent(_testRoot.transform);
            hidden.SetActive(false);
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 1f);
            await Tester.Await.Frames(2);
            
            hidden.SetActive(true);
            SceneCache.Instance.ForceRefresh();
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Activation should have reset stability timer");
            
            await waitTask;
        });

        // 7. Hierarchy: Detects Deactivation
        [UnityTest]
        public IEnumerator SteadyState_Hierarchy_DetectsDeactivation() => ToCoroutine(async () =>
        {
            var visible = new GameObject("Active");
            visible.transform.SetParent(_testRoot.transform);
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 1f);
            await Tester.Await.Frames(2);
            
            visible.SetActive(false);
            SceneCache.Instance.ForceRefresh();
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Deactivation should have reset stability timer");
            
            await waitTask;
        });

        // 8. Hierarchy: Detects Destruction
        [UnityTest]
        public IEnumerator SteadyState_Hierarchy_DetectsDestruction() => ToCoroutine(async () =>
        {
            var doomed = new GameObject("Bye");
            doomed.transform.SetParent(_testRoot.transform);
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 1f);
            await Tester.Await.Frames(2);
            
            UnityEngine.Object.DestroyImmediate(doomed);
            SceneCache.Instance.ForceRefresh();
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Destruction should have reset stability timer");
            
            await waitTask;
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