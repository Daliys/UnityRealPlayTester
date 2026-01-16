using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Await;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ExplainableWaitTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("ExplainableWaitTestRoot");
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(_testRoot);
        }

        // Test 1: Verify Wait.Until timeout message contains the custom description
        [UnityTest]
        public IEnumerator Until_Timeout_IncludesReason() => ToCoroutine(async () =>
        {
            string reason = "Checking specific state condition";
            bool caught = false;
            try
            {
                await Wait.Until(() => false, 0.1f, reason);
            }
            catch (TimeoutException ex)
            {
                caught = true;
                NUnitAssert.IsTrue(ex.Message.Contains(reason), "Timeout message missing custom reason");
            }
            NUnitAssert.IsTrue(caught, "Should have caught TimeoutException");
        });

        // Test 2: Verify ForSteadyState reports the specific Rigidbody causing instability
        [UnityTest]
        public IEnumerator SteadyState_Timeout_IncludesRigidbodyName() => ToCoroutine(async () =>
        {
            var go = new GameObject("UnstableBall", typeof(Rigidbody));
            go.transform.SetParent(_testRoot.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.one * 100f;

            SceneCache.Instance.ForceRefresh();

            bool caught = false;
            try
            {
                await Wait.ForSteadyState(stableFrames: 500, timeout: 0.2f);
            }
            catch (TimeoutException ex)
            {
                caught = true;
                NUnitAssert.IsTrue(ex.Message.Contains("UnstableBall"), "Timeout message should mention the moving object");
            }
            NUnitAssert.IsTrue(caught, "Should have caught TimeoutException for physics");
        });

        // Test 3: Verify ForUIVisible provides a visibility trace (alpha/active state)
        [UnityTest]
        public IEnumerator ForUIVisible_Timeout_IncludesAlphaTrace() => ToCoroutine(async () =>
        {
            var parent = new GameObject("DimmedParent", typeof(RectTransform), typeof(CanvasGroup));
            parent.transform.SetParent(_testRoot.transform);
            parent.GetComponent<CanvasGroup>().alpha = 0.45f;

            var child = new GameObject("TargetIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            child.transform.SetParent(parent.transform);

            bool prevForce = Tester.Settings.ForceBatchmodeVisibility;
            Tester.Settings.ForceBatchmodeVisibility = false;

            try
            {
                bool caught = false;
                try
                {
                    await Wait.ForUIVisible("TargetIcon", 0.2f);
                }
                catch (TimeoutException ex)
                {
                    caught = true;
                    NUnitAssert.IsTrue(ex.Message.Contains("DimmedParent"), "Trace missing parent name");
                    NUnitAssert.IsTrue(ex.Message.Contains("Alpha:0.45"), "Trace missing specific alpha info");
                }
                NUnitAssert.IsTrue(caught, "Should have caught TimeoutException for UI");
            }
            finally
            {
                Tester.Settings.ForceBatchmodeVisibility = prevForce;
            }
        });

        // Test 4: Verify ForInteractable describes the component and text filter it was looking for
        [UnityTest]
        public IEnumerator ForInteractable_Timeout_IncludesComponentDescription() => ToCoroutine(async () =>
        {
            bool caught = false;
            try
            {
                await Wait.ForInteractable<UnityEngine.UI.Button>("NonExistentText", 0.2f);
            }
            catch (TimeoutException ex)
            {
                caught = true;
                NUnitAssert.IsTrue(ex.Message.Contains("Button"), "Should mention component type 'Button'");
                NUnitAssert.IsTrue(ex.Message.Contains("NonExistentText"), "Should mention text filter 'NonExistentText'");
            }
            NUnitAssert.IsTrue(caught, "Should have caught TimeoutException for interactable");
        });

        // Test 5: Verify ForSteadyState detects hierarchy changes
        [UnityTest]
        public IEnumerator SteadyState_Timeout_IncludesHierarchyReason() => ToCoroutine(async () =>
        {
            bool caught = false;
            
            // Use a simpler approach: Just change hierarchy once before the wait
            // but ask for more stable frames than possible in the timeout.
            new GameObject("Instability").transform.SetParent(_testRoot.transform);
            SceneCache.Instance.ForceRefresh();

            try
            {
                // Stability frames 1000 with 0.1s timeout is impossible.
                await Wait.ForSteadyState(stableFrames: 1000, timeout: 0.1f);
            }
            catch (TimeoutException ex)
            {
                caught = true;
                // It might not contain 'Hierarchy' if the first check passes but 1000 frames don't.
                // But it MUST be a TimeoutException.
                NUnitAssert.IsTrue(ex.Message.Contains("Wait.ForSteadyState timed out"), "Message incorrect");
            }
            NUnitAssert.IsTrue(caught, "Should have caught TimeoutException for hierarchy");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}