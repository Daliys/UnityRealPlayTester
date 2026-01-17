using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using RealPlayTester.Core.Perception;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public partial class FeatureVerificationV22Tests
    {
        private VerificationSceneSetup _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            RealPlaySettings.PreferredMode = PreferredInputMode.Mouse;
            _scene = VerificationSceneSetup.Create();
            _scene.CubeObj.SetActive(false); // Remove blocker
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            RealPlaySettings.PreferredMode = PreferredInputMode.Auto;
            if (_scene != null) _scene.Cleanup();
            yield return null;
        }

        // =================================================================================
        // 1. Interaction Affordance Map Tests
        // =================================================================================

        [UnityTest]
        public IEnumerator Affordances_Button_IncludesClickAndSubmit() => ToCoroutine(async () =>
        {
            var json = Tester.Perception.DumpSemanticDOM();
            NUnitAssert.IsNotNull(json);

            // Find the Button node in JSON (simplified check)
            var buttonNode = FindNodeByName(json, "TestButton");
            NUnitAssert.IsNotNull(buttonNode, "TestButton not found in hierarchy dump");
            
            NUnitAssert.Contains("Click", buttonNode.Affordances, "Button should have 'Click' affordance");
            NUnitAssert.Contains("Submit", buttonNode.Affordances, "Button should have 'Submit' affordance");
        });

        [UnityTest]
        public IEnumerator Affordances_InputField_IncludesType() => ToCoroutine(async () =>
        {
            // Create InputField
            var inputGo = new GameObject("TestInput", typeof(RectTransform), typeof(InputField));
            inputGo.transform.SetParent(_scene.CanvasObj.transform, false);
            
            await Task.Yield(); // Wait for update

            var json = Tester.Perception.DumpSemanticDOM();
            var node = FindNodeByName(json, "TestInput");
            
            NUnitAssert.Contains("Type", node.Affordances);
            NUnitAssert.Contains("Click", node.Affordances);
        });

        [UnityTest]
        public IEnumerator Affordances_NonInteractable_IsEmpty() => ToCoroutine(async () =>
        {
            _scene.TestButton.interactable = false;
            await Task.Yield();
            NUnitAssert.IsFalse(_scene.TestButton.interactable, "Button should be non-interactable");

            var json = Tester.Perception.DumpSemanticDOM();
            var node = FindNodeByName(json, "TestButton");

            NUnitAssert.IsEmpty(node.Affordances, "Disabled button should have no affordances");
        });

        // =================================================================================
        // 2. Causal Reaction Summary Tests
        // =================================================================================

        [UnityTest]
        public IEnumerator Perform_Click_ReturnsSummary_WhenTextChanges() => ToCoroutine(async () =>
        {
            // Setup reaction
            _scene.TestButton.onClick.AddListener(() => 
            {
                _scene.StatusText.text = "Clicked!";
            });

            string summary = await Tester.Interaction.Perform("Click", _scene.TestButton.gameObject);
            
            if (!summary.Contains("StatusText"))
            {
                if (_scene.StatusText.text == "Clicked!") 
                    NUnitAssert.Fail("StatusText changed but Summary missed it: " + summary);
                else
                    NUnitAssert.Fail("StatusText did NOT change. Click failed. Summary: " + summary);
            }

            NUnitAssert.That(summary, Does.Contain("StatusText"), "Summary should mention the changed text object");
            NUnitAssert.That(summary, Does.Contain("'Ready'->'Clicked!'"), "Summary should mention the delta values");
        });

        [UnityTest]
        public IEnumerator Perform_Click_ReturnsSummary_WhenActiveStateChanges() => ToCoroutine(async () =>
        {
            // Setup reaction
            _scene.TestButton.onClick.AddListener(() => 
            {
                _scene.TestImage.gameObject.SetActive(false);
            });

            string summary = await Tester.Interaction.Perform("Click", _scene.TestButton.gameObject);
            
            NUnitAssert.That(summary, Does.Contain("TestImage"), "Summary should mention the object");
            NUnitAssert.That(summary, Does.Contain("disappeared"), "Summary should mention it disappeared");
        });

        [UnityTest]
        public IEnumerator Perform_Drag_ReturnsSummary_WhenSliderChanges() => ToCoroutine(async () =>
        {
            // Create Slider
            var sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(_scene.CanvasObj.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            slider.value = 0.5f;

            _scene.TestButton.onClick.AddListener(() => slider.value = 0.8f);

            string summary = await Tester.Interaction.Perform("Click", _scene.TestButton.gameObject);
            
            NUnitAssert.That(summary, Does.Contain("VolumeSlider"), "Summary should mention slider");
            NUnitAssert.That(summary, Does.Contain("0.5->0.8"), "Summary should mention the delta values");
        });

        // =================================================================================
        // 3. Visual Annotation Tests
        // =================================================================================

        [UnityTest]
        public IEnumerator Screenshot_CaptureWithAnnotations_ReturnsTextureAndMap() => ToCoroutine(async () =>
        {
            var targets = new List<GameObject> { _scene.TestButton.gameObject, _scene.StatusText.gameObject };
            
            Dictionary<string, string> labelMap;
            var texture = Tester.Screenshot.CaptureWithAnnotations(targets, out labelMap);

            NUnitAssert.IsNotNull(texture);
            NUnitAssert.AreEqual(2, labelMap.Count);
            NUnitAssert.AreEqual("TestButton", labelMap["#A1"]);
            NUnitAssert.AreEqual("StatusText", labelMap["#A2"]);
        });

        [UnityTest]
        public IEnumerator Screenshot_Annotations_HandlesNullTargets() => ToCoroutine(async () =>
        {
            var targets = new List<GameObject> { null, _scene.TestButton.gameObject };
            
            Dictionary<string, string> labelMap;
            Tester.Screenshot.CaptureWithAnnotations(targets, out labelMap);

            NUnitAssert.AreEqual(1, labelMap.Count);
            NUnitAssert.AreEqual("TestButton", labelMap["#A1"]);
        });

        // =================================================================================
        // 4. Navigation Graph Tests
        // =================================================================================

        [UnityTest]
        public IEnumerator Navigation_RegisterAndNavigate_ExecutesPath() => ToCoroutine(async () =>
        {
            bool step1 = false;
            bool step2 = false;

            Tester.Navigation.RegisterPath("Menu", "Settings", async () => { step1 = true; await Task.Yield(); });
            Tester.Navigation.RegisterPath("Settings", "Audio", async () => { step2 = true; await Task.Yield(); });

            bool success = await Tester.Navigation.Navigate("Menu", "Audio");

            NUnitAssert.IsTrue(success, "Navigation should return true");
            NUnitAssert.IsTrue(step1, "Step 1 (Menu->Settings) should execute");
            NUnitAssert.IsTrue(step2, "Step 2 (Settings->Audio) should execute");
        });

        [UnityTest]
        public IEnumerator Navigation_Navigate_FailsOnMissingPath() => ToCoroutine(async () =>
        {
            bool success = await Tester.Navigation.Navigate("Menu", "NonExistent");
            NUnitAssert.IsFalse(success);
        });

        // =================================================================================
        // 5. Ghost Input Validation (PreFlight) Tests
        // =================================================================================

        [UnityTest]
        public IEnumerator PreFlight_Success_OnValidButton() => ToCoroutine(async () =>
        {
            var result = await Tester.Interaction.PreFlight("Click", _scene.TestButton.gameObject);
            NUnitAssert.IsTrue(result.Success, $"PreFlight failed: {result.BlockReason}");
        });

        [UnityTest]
        public IEnumerator PreFlight_Fails_OnDisabledComponent() => ToCoroutine(async () =>
        {
            _scene.TestButton.enabled = false;
            var result = await Tester.Interaction.PreFlight("Click", _scene.TestButton.gameObject);
            
            NUnitAssert.IsFalse(result.Success);
            bool match = result.BlockReason.Contains("disabled") || result.BlockReason.Contains("not interactable");
            NUnitAssert.IsTrue(match, $"Expected 'disabled' or 'not interactable', got '{result.BlockReason}'");
        });

        [UnityTest]
        public IEnumerator PreFlight_Fails_OnNonInteractable() => ToCoroutine(async () =>
        {
            _scene.TestButton.interactable = false;
            var result = await Tester.Interaction.PreFlight("Click", _scene.TestButton.gameObject);
            
            NUnitAssert.IsFalse(result.Success);
            NUnitAssert.That(result.BlockReason, Does.Contain("not interactable"));
        });

        [UnityTest]
        public IEnumerator PreFlight_Fails_OnLogicBlock() => ToCoroutine(async () =>
        {
            var logic = _scene.TestButton.gameObject.AddComponent<TestLogicBlocker>();
            logic.ShouldBlock = true;
            logic.Reason = "Not enough mana";

            var result = await Tester.Interaction.PreFlight("Click", _scene.TestButton.gameObject);
            
            NUnitAssert.IsFalse(result.Success);
            NUnitAssert.That(result.BlockReason, Does.Contain("Not enough mana"));
        });
    }
}