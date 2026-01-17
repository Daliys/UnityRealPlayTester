using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.UI;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class SemanticDisambiguationTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("DisambiguationRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetActiveState_PrioritizesRealPlayState() => ToCoroutine(async () =>
        {
            var panel = new GameObject("GenericPanel", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(_testRoot.transform);
            
            // Default heuristic should be GenericPanel
            string state1 = PanelStateMonitor.GetActiveStateName();
            NUnitAssert.That(state1, Does.Contain("GenericPanel"));

            // Add explicit state
            var rps = panel.AddComponent<RealPlayState>();
            rps.StateName = "CustomState";
            
            await Task.Yield();

            // Should now be CustomState
            string state2 = PanelStateMonitor.GetActiveStateName();
            NUnitAssert.That(state2, Does.Contain("CustomState"));
            NUnitAssert.That(state2, Is.Not.Contain("GenericPanel"));
        });

        [UnityTest]
        public IEnumerator GetActiveState_SupportsSubStates() => ToCoroutine(async () =>
        {
            var go = new GameObject("WorldState", typeof(RealPlayState));
            go.transform.SetParent(_testRoot.transform);
            var rps = go.GetComponent<RealPlayState>();
            rps.StateName = "GameWorld";
            rps.SubState = "Level1";

            await Task.Yield();

            string state = PanelStateMonitor.GetActiveStateName();
            NUnitAssert.That(state, Does.Contain("GameWorld.Level1"));
        });

        [UnityTest]
        public IEnumerator GetActiveState_PicksDeepestExplicitState() => ToCoroutine(async () =>
        {
            var parent = new GameObject("ParentState", typeof(RealPlayState));
            parent.transform.SetParent(_testRoot.transform);
            parent.GetComponent<RealPlayState>().StateName = "Outer";

            var child = new GameObject("ChildState", typeof(RealPlayState));
            child.transform.SetParent(parent.transform);
            child.GetComponent<RealPlayState>().StateName = "Inner";

            await Task.Yield();

            string state = PanelStateMonitor.GetActiveStateName();
            NUnitAssert.That(state, Does.Contain("Inner"), "Should pick the most specific (deepest) state");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
