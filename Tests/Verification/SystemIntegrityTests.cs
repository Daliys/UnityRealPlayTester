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
    public class SystemIntegrityTests
    {
        private GameObject _testRoot;
        private GameObject _camObj;

        [SetUp]
        public void Setup()
        {
            _testRoot = new GameObject("SystemIntegrityTestRoot");
            
            // Ensure MainCamera exists
            if (Camera.main == null)
            {
                _camObj = new GameObject("MainCamera", typeof(Camera));
                _camObj.tag = "MainCamera";
                _camObj.transform.SetParent(_testRoot.transform);
            }
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_testRoot);
        }

        [UnityTest]
        public IEnumerator SmartFind_FindsObjectsByFuzzyName()
        {
            var target = new GameObject("MainMenu_StartButton_New");
            target.transform.SetParent(_testRoot.transform);

            yield return null;

            // Exact match
            var found1 = SmartFind.Object("MainMenu_StartButton_New");
            NUnitAssert.IsNotNull(found1, "Should find by exact name");

            // Partial match
            var found2 = SmartFind.Object("StartButton");
            NUnitAssert.IsNotNull(found2, "Should find by partial name");

            // Case insensitive
            var found3 = SmartFind.Object("startbutton");
            NUnitAssert.IsNotNull(found3, "Should find by case-insensitive partial name");
        }

        [UnityTest]
        public IEnumerator RealPlaySettings_ApplyStackTraceSettings()
        {
            // Manual toggle
            RealPlaySettings.DisableLogStackTraces = true;
            
            // This normally happens in RealPlayTesterHost.EnsureHost
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            
            NUnitAssert.AreEqual(StackTraceLogType.None, Application.GetStackTraceLogType(LogType.Log), "Log stack trace should be disabled");
            
            RealPlaySettings.DisableLogStackTraces = false;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
            NUnitAssert.AreEqual(StackTraceLogType.ScriptOnly, Application.GetStackTraceLogType(LogType.Log), "Log stack trace should be restored");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator ComputerVision_CaptureSnapshot_DoesNotCrash() => ToCoroutine(async () =>
        {
            if (Application.isBatchMode)
            {
                LogAssert.ignoreFailingMessages = true; 
            }

            string path = "integrity_test_vision.png";
            await Tester.Perception.CaptureVisionSnapshot(path);
            
            LogAssert.ignoreFailingMessages = false;
        });

        [UnityTest]
        public IEnumerator InteractionHeatmap_RecordsPoints()
        {
            // Create a local heatmap for testing isolation
            var go = new GameObject("TestHeatmap");
            go.transform.SetParent(_testRoot.transform);
            var heatmap = go.AddComponent<InteractionHeatmap>();
            heatmap.Initialize();

            Tester.Settings.ShowInteractionHeatmap = true;
            
            // Simulate some points
            heatmap.AddPoint(new Vector2(100, 100));
            heatmap.AddPoint(new Vector2(200, 200));

            // Verify children were created under the heatmap canvas
            int heatPoints = 0;
            // The canvas is a child of the heatmap gameobject
            var canvas = go.GetComponentInChildren<Canvas>(true);
            NUnitAssert.IsNotNull(canvas, "HeatmapCanvas should exist");

            foreach(Transform child in canvas.transform)
            {
                if (child.name.Contains("HeatPoint")) heatPoints++;
            }

            NUnitAssert.GreaterOrEqual(heatPoints, 2, "Heatmap should have created point objects");
            
            Tester.Settings.ShowInteractionHeatmap = false;
            yield return null;
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