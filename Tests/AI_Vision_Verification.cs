using System.Collections;
using System.IO;
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
    public class AI_Vision_Verification
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("TestRoot");
            Tester.Settings.ShowVisualAnchors = true;
            Tester.Settings.ShowInteractionHeatmap = true;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
        }

        [UnityTest]
        public IEnumerator SemanticProbe_IdentifiesBasicRoles()
        {
            var btnGo = new GameObject("LoginButton", typeof(Button));
            btnGo.transform.SetParent(_testRoot.transform);
            
            var role = SemanticProbe.GetRole(btnGo);
            NUnitAssert.AreEqual(SemanticRole.Button, role);
            
            var panelGo = new GameObject("SettingsPanel");
            panelGo.transform.SetParent(_testRoot.transform);
            role = SemanticProbe.GetRole(panelGo);
            NUnitAssert.AreEqual(SemanticRole.Dialog, role);
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator DescribeScreenRegion_ReturnsCorrectSummary()
        {
            // Create a button at center
            var btnGo = new GameObject("CenterBtn", typeof(Button), typeof(RectTransform));
            btnGo.transform.SetParent(_testRoot.transform);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            rt.position = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

            yield return null; // Wait for layout

            // Describe a region around center
            var rect = new Rect(Screen.width / 2f - 50, Screen.height / 2f - 50, 100, 100);
            string description = Tester.Perception.DescribeRegion(rect);
            
            NUnitAssert.IsTrue(description.Contains("[Button] CenterBtn"), $"Description was: {description}");
        }

        [UnityTest]
        public IEnumerator VisualTreeLogger_IncludesSemanticRoles()
        {
            var btnGo = new GameObject("DumpingBtn", typeof(Button));
            btnGo.transform.SetParent(_testRoot.transform);

            string dump = Tester.Perception.DumpHierarchy();
            NUnitAssert.IsTrue(dump.Contains("[Button] DumpingBtn"), "Hierarchy dump missing semantic role.");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator TriggerFailure_GeneratesAiReport()
        {
            // Use a Task wrapper because UnityTest must return IEnumerator
            var task = TriggerFailure_GeneratesAiReportAsync();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        private async Task TriggerFailure_GeneratesAiReportAsync()
        {
            // This is a meta-test. We'll manually call the failure writer to avoid crashing the test runner itself.
            var context = new RealPlayTester.Diagnostics.TestRunContext { TestName = "FailureReportVerification" };
            string bundlePath = await RealPlayTester.Diagnostics.FailureBundleWriter.WriteFailureBundleAsync("FailureReportVerification", context);
            
            NUnitAssert.IsNotNull(bundlePath);
            NUnitAssert.IsTrue(Directory.Exists(bundlePath));
            NUnitAssert.IsTrue(File.Exists(Path.Combine(bundlePath, "ai_report.md")));
            NUnitAssert.IsTrue(File.Exists(Path.Combine(bundlePath, "hierarchy.txt")));
            NUnitAssert.IsTrue(File.Exists(Path.Combine(bundlePath, "failure_screenshot.png")));
        }

        [UnityTest]
        public IEnumerator VisualTreeLogger_GeneratesValidJson()
        {
            var btnGo = new GameObject("JsonBtn", typeof(Button));
            btnGo.transform.SetParent(_testRoot.transform);

            string json = Tester.Perception.DumpHierarchyJson();
            NUnitAssert.IsTrue(json.Contains("\"name\": \"JsonBtn\""), "JSON missing object name.");
            NUnitAssert.IsTrue(json.Contains("\"role\": \"Button\""), "JSON missing semantic role.");
            NUnitAssert.IsTrue(json.Contains("\"active\": true"), "JSON missing active state.");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealPlayRoleAttribute_OverridesHeuristics()
        {
            var customGo = new GameObject("CustomObj", typeof(MyCustomComponent));
            customGo.transform.SetParent(_testRoot.transform);

            string roleName = SemanticProbe.GetRoleName(customGo);
            NUnitAssert.AreEqual("SpecialUIModule", roleName, "RealPlayRoleAttribute failed to override role.");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator StateTracker_DetectsVisibilityChanges()
        {
            var btnGo = new GameObject("ToggleBtn", typeof(Button));
            btnGo.transform.SetParent(_testRoot.transform);
            btnGo.SetActive(false);

            var before = Tester.State.Capture();
            btnGo.SetActive(true);
            var after = Tester.State.Capture();

            string diff = Tester.State.GetDiff(before, after);
            NUnitAssert.IsTrue(diff.Contains("appeared"), $"Diff failed to detect appearance: {diff}");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator Tester_GetActionableElements_ReturnsList()
        {
            var btnGo = new GameObject("MyActionButton", typeof(Button));
            btnGo.transform.SetParent(_testRoot.transform);

            string elements = Tester.Perception.GetActionableElements();
            NUnitAssert.IsTrue(elements.Contains("[Button] MyActionButton"), $"Actionable elements missing button: {elements}");
            
            yield return null;
        }

        [RealPlayRole("SpecialUIModule")]
        public class MyCustomComponent : MonoBehaviour { }
    }
}
