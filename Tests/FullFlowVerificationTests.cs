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
    public class FullFlowVerificationTests
    {
        private VerificationSceneSetup _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _scene = VerificationSceneSetup.Create();
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComprehensiveFlow_VerifyBreadcrumbsAndOcclusion() => ToCoroutine(async () =>
        {
            TestRunContextTracker.BeginTest("ComprehensiveFlowTest", "VirtualScene");
            
            await PerformInitialStateCheck();
            await PerformInteractionStep();
            await PerformOcclusionCheck();
            PerformDOMCheck();

            TestRunContextTracker.EndTest();
            VerifyBreadcrumbsFile();
        });

        private async Task PerformInitialStateCheck()
        {
            Tester.Utility.TestStep("Initialize scene and verify base state");
            NUnitAssert.AreEqual("Ready", _scene.StatusText.text);
            await Task.Yield();
        }

        private async Task PerformInteractionStep()
        {
            Tester.Utility.TestStep("Click the green button to change status");
            _scene.TestButton.onClick.AddListener(() => _scene.StatusText.text = "Clicked");
            await Tester.Interaction.Mouse.ClickObject(_scene.TestButton.gameObject);
            await Tester.Await.Seconds(0.5f);
            NUnitAssert.AreEqual("Clicked", _scene.StatusText.text, "Status text should have changed");
        }

        private async Task PerformOcclusionCheck()
        {
            Tester.Utility.TestStep("Check if cube is occluded by the blue image");
            RectTransform imgRt = _scene.TestImage.rectTransform;
            imgRt.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            imgRt.sizeDelta = new Vector2(500, 500);
            await Tester.Await.Seconds(0.2f);

            bool isOccluded = Tester.Perception.IsOccluded(_scene.CubeObj, out string blocker);
            NUnitAssert.IsTrue(isOccluded, "Cube should be occluded by the large UI image");
            NUnitAssert.AreEqual("UI Element", blocker);
        }

        private void PerformDOMCheck()
        {
            Tester.Utility.TestStep("Perform Semantic DOM Dump");
            string dom = Tester.Perception.DumpSemanticDOM();
            NUnitAssert.IsTrue(dom.Contains("TestButton"), "DOM should contain the button");
            NUnitAssert.IsTrue(dom.Contains("TestCube"), "DOM should contain the cube");
        }

        private void VerifyBreadcrumbsFile()
        {
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            string md = File.ReadAllText(mdPath);
            NUnitAssert.IsTrue(md.Contains("Initialize scene and verify base state"));
            NUnitAssert.IsTrue(md.Contains("Click the green button to change status"));
            NUnitAssert.IsTrue(md.Contains("Clicking object: TestButton"));
            NUnitAssert.IsTrue(md.Contains("Check if cube is occluded by the blue image"));
            NUnitAssert.IsTrue(md.Contains("Perform Semantic DOM Dump"));
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
