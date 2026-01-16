using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using System.IO;
using System.Linq;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class BreadcrumbTests
    {
        [UnityTest]
        public IEnumerator Breadcrumbs_CapturedAndSaved()
        {
            // 1. Start a test context
            var context = TestRunContextTracker.BeginTest("BreadcrumbVerificationTest", "EmptyScene");
            
            // 2. Record some breadcrumbs manually
            TestRunContextTracker.RecordBreadcrumb("TestEvent", "Manual Breadcrumb 1");
            Tester.Utility.TestStep("Logical Step A");

            // 3. Record some interactions (which now trigger breadcrumbs automatically)
            // Note: These will log warnings about missing InputSystem in some environments, but breadcrumbs should still record
            yield return Tester.Interaction.Keyboard.Press(KeyCode.Space);
            yield return Tester.Interaction.Mouse.Click(new Vector2(100, 100));
            
            // 4. End the test
            TestRunContextTracker.EndTest();

            // 5. Verify the files exist
            string dir = RealPlayEnvironment.TestReportsPath;
            string jsonPath = Path.Combine(dir, "current-test-context.json");
            string mdPath = Path.Combine(dir, "current-test-context.md");

            NUnitAssert.IsTrue(File.Exists(jsonPath), "JSON context should exist");
            NUnitAssert.IsTrue(File.Exists(mdPath), "Markdown context should exist");

            // 6. Verify content
            string json = File.ReadAllText(jsonPath);
            NUnitAssert.IsTrue(json.Contains("breadcrumbs"), "JSON should contain breadcrumbs key");
            NUnitAssert.IsTrue(json.Contains("Manual Breadcrumb 1"), "JSON should contain manual breadcrumb");
            NUnitAssert.IsTrue(json.Contains("Logical Step A"), "JSON should contain logical step");
            NUnitAssert.IsTrue(json.Contains("Pressing key: Space"), "JSON should contain keyboard interaction");
            NUnitAssert.IsTrue(json.Contains("Clicking at (100.00, 100.00)"), "JSON should contain mouse interaction");

            string md = File.ReadAllText(mdPath);
            NUnitAssert.IsTrue(md.Contains("Breadcrumbs"), "Markdown should contain Breadcrumbs section");
            NUnitAssert.IsTrue(md.Contains("Logical Step A"), "Markdown should contain logical step");
            
            yield return null;
        }
    }
}
