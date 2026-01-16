using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class StabilityBreakers
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _root = new GameObject("StabilityBreakersRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ISSUE_017_SteadyState_Ignores_Transform_Movement() => ToCoroutine(async () =>
        {
            var go = new GameObject("Mover");
            go.transform.SetParent(_root.transform);
            
            // Start moving the object via transform
            bool isMoving = true;
            var moveTask = Task.Run(async () => {
                while(isMoving) {
                    // This is not safe to do in Task.Run for Unity objects, but for a test we can use a coroutine or just a flag
                }
            });

            // Let's use a component to move it
            var mover = go.AddComponent<SimpleMover>();
            mover.speed = 1000f; // Extreme speed for guaranteed detection

            // Wait for steady state
            // It should NOT reach steady state because something is moving
            var waitTask = Tester.Await.ForSteadyState(stableFrames: 5, timeout: 1f);
            
            try {
                await waitTask;
                NUnitAssert.Fail("SteadyState reached while object was moving via Transform!");
            } catch (System.TimeoutException) {
                UnityEngine.Debug.Log("[ISSUE-017] Correctly timed out (or did it?)");
            } finally {
                mover.speed = 0f;
            }
        });

        [UnityTest]
        public IEnumerator ISSUE_018_HierarchyHash_Collision() => ToCoroutine(async () =>
        {
            var go1 = new GameObject("Obj1");
            go1.transform.SetParent(_root.transform);
            
            // Initial steady state
            await Tester.Await.ForSteadyState(stableFrames: 2);
            
            // In one frame, destroy Obj1 and create Obj2
            Object.DestroyImmediate(go1);
            var go2 = new GameObject("Obj2");
            go2.transform.SetParent(_root.transform);
            
            // The count is still 1 (plus roots).
            // Let's see if ForSteadyState(stableFrames: 2) detects it.
            // It should take at least 1 extra frame to stabilize because of the change.
            // But if it misses the change, it might return immediately.
            
            var sw = Stopwatch.StartNew();
            await Tester.Await.ForSteadyState(stableFrames: 5, timeout: 1f);
            sw.Stop();
            
            UnityEngine.Debug.Log($"[ISSUE-018] Stabilized after {sw.ElapsedMilliseconds}ms");
        });

        private class SimpleMover : MonoBehaviour {
            public float speed = 0f;
            void Update() {
                transform.Translate(Vector3.right * speed * Time.deltaTime);
            }
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
