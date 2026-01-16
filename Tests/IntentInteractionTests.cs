using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class IntentInteractionTests
    {
        private VerificationSceneSetup _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _scene = VerificationSceneSetup.Create();
            TestRunContextTracker.BeginTest("IntentInteractionTest", "VirtualScene");
            yield return null;
            
            Screen.SetResolution(1024, 768, false);
            yield return null;

            Tester.Settings.ForceBatchmodeVisibility = true;
            Tester.Settings.EnableInputHeartbeat = true;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (_scene != null) _scene.Cleanup();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Perform_Select_ClicksButton() => ToCoroutine(async () =>
        {
            bool clicked = false;
            _scene.TestButton.onClick.AddListener(() => clicked = true);

            await Tester.Interaction.Perform("Select", "TestButton");
            
            await Tester.Await.Seconds(0.2f);
            NUnitAssert.IsTrue(clicked, "Intent 'Select' should have clicked the button");
        });

        // Test 4: Alias 'Click'
        [UnityTest]
        public IEnumerator Perform_Click_AliasForSelect() => ToCoroutine(async () =>
        {
            bool clicked = false;
            _scene.TestButton.onClick.AddListener(() => clicked = true);

            await Tester.Interaction.Perform("Click", "TestButton");
            
            await Tester.Await.Seconds(0.2f);
            NUnitAssert.IsTrue(clicked, "Intent 'Click' should have clicked the button");
        });

        // Test 5: Alias 'Tap'
        [UnityTest]
        public IEnumerator Perform_Tap_AliasForSelect() => ToCoroutine(async () =>
        {
            bool clicked = false;
            _scene.TestButton.onClick.AddListener(() => clicked = true);

            await Tester.Interaction.Perform("Tap", "TestButton");
            
            await Tester.Await.Seconds(0.2f);
            NUnitAssert.IsTrue(clicked, "Intent 'Tap' should have clicked the button");
        });

        // Test 6: Alias 'Back'
        [UnityTest]
        public IEnumerator Perform_Back_AliasForCancel() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Perform("Back", (GameObject)null);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Pressing key: Escape"), "Intent 'Back' should have triggered Escape");
        });

        // Test 7: World Object Interaction
        [UnityTest]
        public IEnumerator Perform_Select_WorldObject() => ToCoroutine(async () =>
        {
            // Just verify it doesn't crash and records the intent for a 3D object
            await Tester.Interaction.Perform("Select", "TestCube");
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Perform 'Select' on 'TestCube'"), "Timeline missing world object interaction");
        });

        // Test 8: Submit on real InputField
        [UnityTest]
        public IEnumerator Perform_Submit_WithInputField() => ToCoroutine(async () =>
        {
            var go = new GameObject("ActualInput", typeof(RectTransform), typeof(InputField));
            go.transform.SetParent(_scene.CanvasObj.transform);
            
            await Tester.Interaction.Perform("Submit", go);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Pressing key: Return"), "Submit on InputField should press Return");
        });

        [UnityTest]
        public IEnumerator Perform_Submit_PressesEnter() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Perform("Submit", _scene.TestButton.gameObject);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Pressing key: Return"), "Intent 'Submit' should have pressed Return");
        });

        [UnityTest]
        public IEnumerator Perform_Cancel_PressesEscapeAndGamepadB() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Perform("Cancel", (GameObject)null);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Pressing key: Escape"), "Intent 'Cancel' should have pressed Escape");
            NUnitAssert.IsTrue(md.Contains("Gamepad: Release East"), "Intent 'Cancel' should have pressed/released Gamepad East/B");
        });

        private string GetContextMarkdown()
        {
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(mdPath);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}