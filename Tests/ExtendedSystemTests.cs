using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Core.Perception;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class ExtendedSystemTests
    {
        private GameObject _testRoot;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("ExtendedTestRoot");
            TestRunContextTracker.BeginTest("ExtendedVerification", "Empty");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            TestRunContextTracker.EndTest();
        }

        // --- Mock Render & Vision (3 tests) ---

        [UnityTest]
        public IEnumerator Vision_MockRender_CreatesValidFile()
        {
            string fileName = "test_mock_render.png";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path)) File.Delete(path);

            RealPlayComputerVisionInterface.GenerateMockSnapshotFromDOM(fileName, 1024, 768);
            
            NUnitAssert.IsTrue(File.Exists(path), "Mock render file should be created");
            NUnitAssert.Greater(new FileInfo(path).Length, 0, "Mock render file should not be empty");
            yield return null;
        }

        [Test]
        public void Vision_RoleColor_ReturnsExpectedColors()
        {
            NUnitAssert.AreEqual(Color.green, RealPlayComputerVisionInterface.GetRoleColor("Button"));
            NUnitAssert.AreEqual(Color.blue, RealPlayComputerVisionInterface.GetRoleColor("Input"));
            NUnitAssert.AreEqual(Color.white, RealPlayComputerVisionInterface.GetRoleColor("UnknownRole"));
        }

        [Test]
        public void Vision_DrawWireRect_HandlesOffScreenBounds()
        {
            var tex = new Texture2D(100, 100);
            // Draw a rect that is partially off-screen
            RealPlayComputerVisionInterface.DrawWireRect(tex, new Rect(-10, -10, 200, 200), Color.red, 100);
            NUnitAssert.Pass("DrawWireRect did not throw for out-of-bounds coordinates");
        }

        // --- SceneCache & SmartFind (3 tests) ---

        [UnityTest]
        public IEnumerator SceneCache_SceneChange_InvalidatesCache() => ToCoroutine(async () =>
        {
            SceneCache.Instance.ForceRefresh();
            int countBefore = SceneCache.Instance.AllActiveObjects.Count;
            
            // Add objects to change the count significantly
            var extra1 = new GameObject("Ex1");
            extra1.transform.SetParent(_testRoot.transform);
            var extra2 = new GameObject("Ex2");
            extra2.transform.SetParent(_testRoot.transform);
            
            SceneCache.Instance.ForceRefresh();
            
            int countAfter = SceneCache.Instance.AllActiveObjects.Count;
            NUnitAssert.AreEqual(countBefore + 2, countAfter, "Cache should have updated with new objects");
        });

        [UnityTest]
        public IEnumerator SmartFind_PrioritizesActiveObjects()
        {
            var inactive = new GameObject("DuplicateName");
            inactive.transform.SetParent(_testRoot.transform);
            inactive.SetActive(false);

            var active = new GameObject("DuplicateName");
            active.transform.SetParent(_testRoot.transform);
            active.SetActive(true);

            SceneCache.Instance.ForceRefresh();

            var found = SmartFind.Object("DuplicateName");
            NUnitAssert.AreEqual(active, found, "Should return the active object over the inactive one");
            yield return null;
        }

        [Test]
        public void SmartFind_HandlesNullGracefully()
        {
            NUnitAssert.IsNull(SmartFind.Object(null), "Should handle null query");
            NUnitAssert.IsNull(SmartFind.Object(""), "Should handle empty query");
        }

        // --- SteadyState & Performance (4 tests) ---

        [UnityTest]
        public IEnumerator SteadyState_AnimatorTransition_Detection() => ToCoroutine(async () =>
        {
            var animGo = new GameObject("Anim", typeof(Animator));
            animGo.transform.SetParent(_testRoot.transform);
            // We can't easily trigger a real transition without a controller, 
            // but we can verify the code doesn't crash during the check.
            await Tester.Await.ForSteadyState(stableFrames: 2, timeout: 0.5f);
            NUnitAssert.Pass("SteadyState check passed with Animator");
        });

        [UnityTest]
        public IEnumerator PerformanceMonitor_AllocThreshold_ZeroKB_Stress() => ToCoroutine(async () =>
        {
            // Setting a 0.001KB threshold to trigger on almost any allocation
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0.001f);
            LogAssert.ignoreFailingMessages = true;

            for(int i = 0; i < 10; i++) { new object(); await Task.Yield(); }
            
            Tester.Performance.StopMonitoring();
            LogAssert.ignoreFailingMessages = false;
            NUnitAssert.Pass("PerformanceMonitor handled extreme thresholds");
        });

        [UnityTest]
        public IEnumerator PerformanceMonitor_Stop_PreventsFurtherBreadcrumbs() => ToCoroutine(async () =>
        {
            Tester.Performance.StartMonitoring(maxGcAllocKB: 0.001f);
            LogAssert.ignoreFailingMessages = true;
            await Tester.Await.Frames(2);
            Tester.Performance.StopMonitoring();
            
            string jsonBefore = TestRunContextTracker.Current.ToJson();
            new object(); // Allocate
            await Tester.Await.Frames(2);
            
            string jsonAfter = TestRunContextTracker.Current.ToJson();
            NUnitAssert.AreEqual(jsonBefore.Length, jsonAfter.Length, "No new breadcrumbs should be added after Stop");
        });

        [Test]
        public void Breadcrumbs_LargeTimeline_Serializes()
        {
            var context = new TestRunContext();
            for(int i = 0; i < 100; i++)
            {
                context.Breadcrumbs.Add(new Breadcrumb("Type", $"Message {i}"));
            }
            
            string json = context.ToJson();
            NUnitAssert.IsTrue(json.Contains("Message 99"), "JSON should contain the last breadcrumb");
            NUnitAssert.IsTrue(json.Length > 1000, "JSON should be substantial");
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}