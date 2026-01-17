using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
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
        // Test 11: Detects Internal Private Field
        [Test]
        public void Logic_Detects_Internal_IsLocked_Field()
        {
            var go = new GameObject("InternalFieldBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<InternalLockedScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Should detect private IsLocked field");
        }

        // Test 12: Detects Boolean Property
        [Test]
        public void Logic_Detects_Boolean_Property_CanInteract()
        {
            var go = new GameObject("BooleanPropBlock");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<BooleanPropScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked, "Should detect CanInteract property returning false");
        }

        // Test 13: Wait.ForInteractable respects logical block
        [UnityTest]
        public IEnumerator Wait_ForInteractable_Respects_Logical_Block() => ToCoroutine(async () =>
        {
            var go = new GameObject("InteractableWaiter", typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            
            var txtGo = new GameObject("Text", typeof(UnityEngine.UI.Text));
            txtGo.transform.SetParent(go.transform);
            var txt = txtGo.GetComponent<UnityEngine.UI.Text>();
            txt.text = "InteractableWaiter";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var logic = go.AddComponent<DynamicLogicScript>();
            logic.IsLocked = true;

            var waitTask = RealPlayTester.Await.Wait.ForInteractable<Button>("InteractableWaiter", 1f);
            await Tester.Await.Frames(5);
            NUnitAssert.IsFalse(waitTask.IsCompleted, "Wait.ForInteractable should respect logical block");

            logic.IsLocked = false;
            await waitTask;
            NUnitAssert.IsTrue(waitTask.IsCompleted);
        });

        // Test 14: Timeout includes logic reason in trace
        [UnityTest]
        public IEnumerator Wait_ForUIVisible_AnalyzeChain_Includes_Logic_Reason() => ToCoroutine(async () =>
        {
            var go = new GameObject("TimeoutLogic", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MockLockedScript>();

            try { await RealPlayTester.Await.Wait.ForUIVisible("TimeoutLogic", 0.3f); }
            catch (TimeoutException ex)
            {
                NUnitAssert.IsTrue(ex.Message.Contains("LOGIC BLOCKED"), "Timeout trace missing LOGIC BLOCKED tag");
                NUnitAssert.IsTrue(ex.Message.Contains("Not enough gold"), "Timeout trace missing reason");
            }
        });

        // Test 15: Handles null components (robustness)
        [Test]
        public void Logic_Handles_Null_Components_Gracefully()
        {
            var go = new GameObject("NullComp");
            go.transform.SetParent(_testRoot.transform);
            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsFalse(result.IsBlocked);
        }

        // Test 16: Deduplication in logs for blocked interaction
        [UnityTest]
        public IEnumerator Logic_Deduplication_In_Logs() => ToCoroutine(async () =>
        {
            var go = new GameObject("DedupeBlock", typeof(RectTransform), typeof(Button), typeof(Image));
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<MockLockedScript>();

            for(int i = 0; i < 5; i++) await Tester.Interaction.Perform("Select", go);

            TestRunContextTracker.ForceWriteSnapshot();
            string jsonPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.json");
            string json = File.ReadAllText(jsonPath);
            NUnitAssert.IsTrue(json.Contains("repeats"), "Blocked interaction logs should be deduplicated");
        });

        // Test 17: SmartFind prioritizes unblocked object over blocked one
        [UnityTest]
        public IEnumerator Logic_SmartFind_Prioritizes_Unblocked() => ToCoroutine(async () =>
        {
            var blockedGo = new GameObject("DualObject", typeof(RectTransform), typeof(Button), typeof(Image));
            blockedGo.transform.SetParent(_testRoot.transform);
            blockedGo.AddComponent<MockLockedScript>(); 

            var unblockedGo = new GameObject("DualObject", typeof(RectTransform), typeof(Button), typeof(Image));
            unblockedGo.transform.SetParent(_testRoot.transform);
            unblockedGo.transform.position = new Vector3(100, 0, 0);

            await Task.Yield();
            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("DualObject");
            NUnitAssert.AreEqual(unblockedGo.GetInstanceID(), found.GetInstanceID(), "SmartFind should have picked the unblocked instance");
        });

        // Test 18: Detects Method naming variant (CanInteract)
        [Test]
        public void Logic_Detects_Heuristic_CanInteractMethod()
        {
            var go = new GameObject("VariantMethod");
            go.transform.SetParent(_testRoot.transform);
            go.AddComponent<HeuristicMethodScript>();

            var result = InteractionLogic.CheckLogicalInteractivity(go);
            NUnitAssert.IsTrue(result.IsBlocked);
            NUnitAssert.AreEqual("CanInteract is false (heuristic)", result.Reason);
        }
    }
}
