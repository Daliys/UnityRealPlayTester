using System.IO;
using NUnit.Framework;
using RealPlayTester.Diagnostics;
using UnityEngine;

namespace RealPlayTester.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for TestRunContextTracker snapshot updates.
    /// </summary>
    public class TestRunContextTrackerTests
    {
        [Test]
        /// <summary>
        /// Verifies tracker updates fields and writes snapshots into TestReports.
        /// </summary>
        public void Tracker_UpdatesContext_AndWritesSnapshots()
        {
            string reportsDir = Path.Combine(Application.dataPath, "..", "TestReports");
            string jsonPath = Path.Combine(reportsDir, "current-test-context.json");
            string mdPath = Path.Combine(reportsDir, "current-test-context.md");

            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(mdPath)) File.Delete(mdPath);

            var context = TestRunContextTracker.BeginTest("TrackerTest", "TestScene");
            TestRunContextTracker.UpdateAction("Step A");
            TestRunContextTracker.UpdatePanel("Panel_X");
            TestRunContextTracker.UpdatePlacementAttempt(new Vector2Int(2, 3), "test_building", "queued");
            TestRunContextTracker.EndTest();

            Assert.IsNotNull(context, "Context should be created.");
            Assert.AreEqual("TrackerTest", context.TestName);
            Assert.AreEqual("Panel_X", context.LastPanel);
            Assert.IsNotNull(context.LastPlacementAttempt);
            Assert.AreEqual("test_building", context.LastPlacementAttempt.DefinitionId);

            Assert.IsTrue(File.Exists(jsonPath), "Snapshot JSON should exist in TestReports.");
            Assert.IsTrue(File.Exists(mdPath), "Snapshot Markdown should exist in TestReports.");

            string json = File.ReadAllText(jsonPath);
            StringAssert.Contains("TrackerTest", json);

            File.Delete(jsonPath);
            File.Delete(mdPath);
        }
    }
}
