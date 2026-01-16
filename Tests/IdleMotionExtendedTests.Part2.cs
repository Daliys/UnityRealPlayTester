using System;
using System.Collections;
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
        // 9. Physics: Respects Parameter Epsilon
        [UnityTest]
        public IEnumerator SteadyState_Physics_RespectsParameterEpsilon() => ToCoroutine(async () =>
        {
            var go = new GameObject("Vibration", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Vector3.one * 0.1f; // Higher than default 0.05

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            // Should time out with default epsilon
            bool timedOut = false;
            try { await Tester.Await.ForSteadyState(stableFrames: 2, timeout: 0.2f); }
            catch (TimeoutException) { timedOut = true; }
            NUnitAssert.IsTrue(timedOut, "Should have timed out with small default epsilon");

            // Should succeed with large parameter epsilon
            await Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f, velocityEpsilon: 0.5f);
            NUnitAssert.Pass("Succeeded with custom epsilon");
        });

        // 10. Ignores Kinematic 3D
        [UnityTest]
        public IEnumerator SteadyState_IgnoresKinematic3D() => ToCoroutine(async () =>
        {
            var go = new GameObject("Kinematic", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.one * 100f; // Velocity technically set but shouldn't matter

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 11. Ignores Static 2D
        [UnityTest]
        public IEnumerator SteadyState_IgnoresStatic2D() => ToCoroutine(async () =>
        {
            var go = new GameObject("Static2D", typeof(Rigidbody2D));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 12. Combined: Wait until ALL stable
        [UnityTest]
        public IEnumerator SteadyState_Combined_WaitUntilAllStable() => ToCoroutine(async () =>
        {
            var rbGo = new GameObject("Body", typeof(Rigidbody2D));
            rbGo.transform.SetParent(_testRoot.transform);
            var rb = rbGo.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.one * 10f;

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 2f);
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Waiting for physics");

            rb.linearVelocity = Vector2.zero;
            await Tester.Await.Frames(2);
            
            // Trigger hierarchy instability
            new GameObject("Interrupt").transform.SetParent(_testRoot.transform);
            SceneCache.Instance.ForceRefresh();
            
            await Tester.Await.Frames(2);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Waiting for hierarchy");

            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 13. Animator: Ignores Looping When Configured
        [UnityTest]
        public IEnumerator SteadyState_Animator_IgnoresLooping_WhenConfigured() => ToCoroutine(async () =>
        {
            var go = new GameObject("LoopingAnim", typeof(Animator));
            go.transform.SetParent(_testRoot.transform);
            
            RealPlaySettings.IgnoreLoopingAnimations = true;
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 14. Global Settings override
        [UnityTest]
        public IEnumerator SteadyState_RespectsGlobalSettingsOverride() => ToCoroutine(async () =>
        {
            RealPlaySettings.IdleVelocityEpsilon = 100f; // Ignore EVERYTHING
            var go = new GameObject("Fast", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            go.GetComponent<Rigidbody>().linearVelocity = Vector3.one * 50f;

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var waitTask = Tester.Await.ForSteadyState(stableFrames: 2, timeout: 1f);
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // 15. Hierarchy Pruning Sanity
        [UnityTest]
        public IEnumerator SteadyState_Hierarchy_HandlesEmptyScene() => ToCoroutine(async () =>
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            await Tester.Await.ForSteadyState(stableFrames: 5, timeout: 1f);
            NUnitAssert.Pass("Empty scene is steady");
        });
    }
}
