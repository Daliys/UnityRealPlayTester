using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class BulkIssueDiscovery
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("BulkRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_035_Physics_Simulates_When_Paused() => ToCoroutine(async () =>
        {
            Time.timeScale = 0f;
            var go = new GameObject("PhysicsObj", typeof(Rigidbody));
            go.transform.SetParent(_root.transform);
            var rb = go.GetComponent<Rigidbody>();
            rb.useGravity = true;
            Vector3 startPos = go.transform.position;

            // Use unscaled wait so the test itself doesn't hang, 
            // but UpdateInput (and physics simulation) will still be called inside the loop.
            await RealPlayTester.Await.Wait.Seconds(0.5f, unscaled: true); 

            UnityEngine.Debug.Log($"[ISSUE-035] Position after 0.5s pause: {go.transform.position.y}");
            
            NUnitAssert.AreEqual(startPos.y, go.transform.position.y, 0.001f, "Physics moved while game was paused during a Wait!");
            Time.timeScale = 1f;
        });

        [UnityTest]
        public IEnumerator ISSUE_038_Collapse_Conservative() => ToCoroutine(async () =>
        {
            RealPlaySettings.CollapseRepetitiveSiblings = true;
            for (int i = 0; i < 20; i++)
            {
                var btn = new GameObject($"Slot_{i}", typeof(Button), typeof(Image));
                btn.transform.SetParent(_root.transform);
            }

            string dump = Tester.Perception.DumpHierarchy();
            UnityEngine.Debug.Log($"[ISSUE-038] Dump length with 20 buttons: {dump.Split('\n').Length} lines");
            
            // Should be collapsed to save tokens, but current logic skips non-boring objects
            NUnitAssert.Less(dump.Split('\n').Length, 10, "Repetitive buttons were not collapsed!");
        });

        [UnityTest]
        public IEnumerator ISSUE_039_Collapse_Unique_Objects() => ToCoroutine(async () =>
        {
            RealPlaySettings.CollapseRepetitiveSiblings = true;
            // These are unique objects but base name matches
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject($"Cell_0_{i}");
                go.transform.SetParent(_root.transform);
            }

            string dump = Tester.Perception.DumpHierarchy();
            UnityEngine.Debug.Log($"[ISSUE-039] Dump:\n{dump}");
            
            NUnitAssert.IsTrue(dump.Contains("Cell_0_5") || !dump.Contains("similar"), "Unique grid cells were collapsed!");
        });

        [UnityTest]
        public IEnumerator ISSUE_041_WaitUntil_Timeout_Message_Missing_Description() => ToCoroutine(async () =>
        {
            try
            {
                await Tester.Await.Until(() => false, timeout: 0.1f);
                NUnitAssert.Fail("Should have timed out");
            }
            catch (System.TimeoutException ex)
            {
                UnityEngine.Debug.Log($"[ISSUE-041] Exception: {ex.Message}");
                // It should have some default context if description is missing
            }
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
