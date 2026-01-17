using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ReliabilityFeatureTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("ReliabilityFeatureRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) UnityEngine.Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        // 21. SteadyState: High-Velocity 3D
        [UnityTest]
        public IEnumerator SteadyState_Handles_FastMovingPhysics() => ToCoroutine(async () =>
        {
            var go = new GameObject("FastBall", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = Vector3.one * 500f; // Bullet speed

            SceneCache.Instance.ForceRefresh();
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 0.5f);
            
            await Task.Yield();
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should be unstable due to speed");
            
            rb.linearVelocity = Vector3.zero;
            await waitTask;
        });

        // 22. Wait.Until: Exception Propagation
        [UnityTest]
        public IEnumerator Wait_Until_PropagatesInternalExceptions() => ToCoroutine(async () =>
        {
            bool caught = false;
            try {
                await Wait.Until(() => { throw new InvalidOperationException("BOOM"); });
            } catch (InvalidOperationException) {
                caught = true;
            }
            NUnitAssert.IsTrue(caught, "Wait.Until swallowed internal exception");
        });

        // 23. ForUIVisible: Culled Renderer Check
        [UnityTest]
        public IEnumerator Wait_ForUIVisible_Detects_CulledCanvas() => ToCoroutine(async () =>
        {
            var go = new GameObject("Culled", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            go.GetComponent<CanvasRenderer>().cull = true;

            // Should time out because it is culled
            bool timedOut = false;
            try { await Wait.ForUIVisible("Culled", 0.5f); }
            catch (TimeoutException) { timedOut = true; }
            
            NUnitAssert.IsTrue(timedOut, "Wait failed to detect culled state");
        });

        // 24. Explainable Trace Depth
        [UnityTest]
        public IEnumerator Wait_ExplainableTrace_ShowsMultiParentChain() => ToCoroutine(async () =>
        {
            var p1 = new GameObject("P1");
            p1.transform.SetParent(_testRoot.transform);
            p1.SetActive(false);
            
            var p2 = new GameObject("P2");
            p2.transform.SetParent(p1.transform);
            
            var target = new GameObject("Leaf");
            target.transform.SetParent(p2.transform);

            try { await Wait.ForUIVisible("Leaf", 0.2f); }
            catch (TimeoutException ex) {
                NUnitAssert.IsTrue(ex.Message.Contains("P1"), "Trace missing parent");
            }
        });

        // 25. SteadyState: Rapid Oscillation Reset
        [UnityTest]
        public IEnumerator SteadyState_Resets_OnRapidOscillation() => ToCoroutine(async () =>
        {
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 10, timeout: 1f);
            
            // Jitter the hierarchy every frame
            for(int i = 0; i < 5; i++)
            {
                var jitter = new GameObject("Jitter");
                jitter.transform.SetParent(_testRoot.transform);
                SceneCache.Instance.ForceRefresh();
                await Task.Yield();
                UnityEngine.Object.DestroyImmediate(jitter);
                SceneCache.Instance.ForceRefresh();
                await Task.Yield();
            }

            NUnitAssert.IsFalse(waitTask.IsCompleted, "Stability timer failed to reset on hierarchy oscillation");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}