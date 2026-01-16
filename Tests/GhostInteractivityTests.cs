using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using RealPlayTester.Input;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public partial class GhostInteractivityTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("GhostInteractivityTestRoot");
            TestRunContextTracker.BeginTest("GhostInteractivityVerification", "Empty");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_testRoot != null) UnityEngine.Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
            yield return null;
        }

        // --- Mock Scripts ---
        private class MockLockedScript : MonoBehaviour, IInteractableLogic
        {
            public bool CanInteract() => false;
            public string GetBlockedReason() => "Not enough gold";
        }

        private class HeuristicLockedScript : MonoBehaviour
        {
            public bool IsLocked = true;
        }

        private class HeuristicMethodScript : MonoBehaviour
        {
            public bool CanInteract() => false;
        }

        private class HeuristicPropertyScript : MonoBehaviour
        {
            public bool CanInteract { get { return false; } }
        }

        private class MemberVariationScript : MonoBehaviour
        {
            [SerializeField] private bool m_IsLocked = true;
        }

        private class InternalLockedScript : MonoBehaviour
        {
            private bool IsLocked = true; // Private field
        }

        private class BooleanPropScript : MonoBehaviour
        {
            public bool CanInteract => false;
        }

        // Test 1: Explicit Interface Detection
        [Test]
        public void Logic_Detects_ExplicitInterface()
        {
            var go = new GameObject("LockedButton");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MockLockedScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Should be logically blocked");
            NUnitAssert.AreEqual("Not enough gold", result.Reason);
        }

        // Test 2: Heuristic Reflection Detection (IsLocked)
        [Test]
        public void Logic_Detects_Heuristic_IsLocked()
        {
            var go = new GameObject("HeuristicLocked");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<HeuristicLockedScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Should be logically blocked via heuristic");
            NUnitAssert.AreEqual("Locked (heuristic)", result.Reason);
        }

        // Test 3: Semantic DOM includes logical state
        [UnityTest]
        public IEnumerator DOM_Includes_LogicalInteractivity()
        {
            var go = new GameObject("LockedDOM", typeof(RectTransform));
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MockLockedScript>();

            yield return null;

            string domJson = RealPlaySemanticDOMDumper.Dump();
            NUnitAssert.IsTrue(domJson.Contains("LogicalBlocked"), "DOM missing LogicalBlocked flag");
            NUnitAssert.IsTrue(domJson.Contains("Not enough gold"), "DOM missing BlockedReason");
        }

        // Test 4: Perform() records blocker breadcrumb
        [UnityTest]
        public IEnumerator Perform_Records_BlockerBreadcrumb() => ToCoroutine(async () =>
        {
            var go = new GameObject("BlockerBreadcrumb", typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MockLockedScript>();

            await Tester.Interaction.Perform("Select", go);

            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            string md = File.ReadAllText(mdPath);
            
            NUnitAssert.IsTrue(md.Contains("[BLOCKER]"), "Timeline missing [BLOCKER] tag");
            NUnitAssert.IsTrue(md.Contains("Not enough gold"), "Timeline missing logical reason");
        });

        // Test 5: Heuristic Method Detection
        [Test]
        public void Logic_Detects_Heuristic_Method()
        {
            var go = new GameObject("MethodBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<HeuristicMethodScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked);
            NUnitAssert.AreEqual("CanInteract is false (heuristic)", result.Reason);
        }

        // Test 6: Heuristic Property Detection
        [Test]
        public void Logic_Detects_Heuristic_Property()
        {
            var go = new GameObject("PropBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<HeuristicPropertyScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked);
            NUnitAssert.AreEqual("CanInteract is false (heuristic)", result.Reason);
        }

        // Test 7: Multiple Components Aggregation
        [Test]
        public void Logic_MultipleComponents_AnyBlock_BlocksAll()
        {
            var go = new GameObject("MultiBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<Image>(); 
            go.AddComponent<MockLockedScript>(); 

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Object with one blocked component should be blocked");
        }

        // Test 8: Member Variation Detection (m_IsLocked)
        [Test]
        public void Logic_Detects_MemberVariation_m_IsLocked()
        {
            var go = new GameObject("VariationBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MemberVariationScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked);
            NUnitAssert.AreEqual("Locked (heuristic)", result.Reason);
        }

        // Test 9: Ghost Button Scenario
        [Test]
        public void Logic_Detects_GhostButton_Mismatch()
        {
            var go = new GameObject("GhostBtn", typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            var btn = go.GetComponent<Button>();
            btn.interactable = true; 
            go.AddComponent<MockLockedScript>(); 

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Button should be blocked even if interactable=true");
        }

        // Test 10: Waiter respects logic
        [UnityTest]
        public IEnumerator Wait_ForUIVisible_WaitsForLogic() => ToCoroutine(async () =>
        {
            var go = new GameObject("LogicWaiter", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            var logic = go.AddComponent<DynamicLogicScript>();
            logic.IsLocked = true;

            var waitTask = RealPlayTester.Await.Wait.ForUIVisible("LogicWaiter", 2f);
            await Tester.Await.Frames(5);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Should still be waiting while logically blocked");

            logic.IsLocked = false; 
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        private class DynamicLogicScript : MonoBehaviour
        {
            public bool IsLocked = true;
        }

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