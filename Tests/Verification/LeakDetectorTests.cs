using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class LeakDetectorTests
    {
        [UnityTest]
        public IEnumerator Detect_Static_Object_Leaks()
        {
            // 1. Force a full initialization of all services
            var host = RealPlayTesterHost.Instance;
            NUnitAssert.IsNotNull(host);
            
            // Trigger some state
            EventTracker.Record("TestEvent");
            Tester.Perception.Find("NonExistent");
            
            // 2. Destroy the host (should trigger OnDestroy cleanups)
            UnityEngine.Object.DestroyImmediate(host.gameObject);
            
            // 3. Trigger Subsystem Reset (simulates what happens between tests in some runners, or domain reload)
            var resetMethod = typeof(RealPlayTesterHost).GetMethod("ResetStaticState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            resetMethod.Invoke(null, null);
            
            yield return null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            // 4. Verify no stray references exist to destroyed objects
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                NUnitAssert.AreNotEqual("RealPlayTesterHost", go.name, "Host object still exists after destruction.");
                NUnitAssert.AreNotEqual("RealPlayTester_EventSystem", go.name, "EventSystem still exists.");
                NUnitAssert.AreNotEqual("HeatmapCanvas", go.name, "Heatmap canvas leaked.");
                NUnitAssert.AreNotEqual("AnchorCanvas", go.name, "Anchor canvas leaked.");
                NUnitAssert.AreNotEqual("PointerCanvas", go.name, "Pointer canvas leaked.");
            }
            
            // 5. Verify Static collections are empty or clean
            NUnitAssert.AreEqual(0, EventTracker.GetHistory().Count, "Event history leaked.");
            
            // Check cache for specific leaked objects
            bool foundOld = false;
            foreach(var go in SceneCache.Instance.AllObjects)
            {
                if (go != null && (go.name == "RealPlayTesterHost" || go.name == "RealPlayTester_EventSystem"))
                    foundOld = true;
            }
            NUnitAssert.IsFalse(foundOld, "SceneCache contains references to destroyed library objects.");
        }

        [UnityTest]
        public IEnumerator Detect_CrossScene_UI_Leaks()
        {
            // Show a failure overlay (DontDestroyOnLoad)
            RealPlayTester.Assert.FailureOverlay.Show("Test Failure", "");
            
            // Verify it exists
            NUnitAssert.IsNotNull(GameObject.Find("RealPlayTester_FailureOverlay"));
            
            // Trigger reset
            RealPlayTester.Assert.FailureOverlay.Clear();
            
            yield return null;
            
            NUnitAssert.IsNull(GameObject.Find("RealPlayTester_FailureOverlay"), "Failure overlay leaked.");
        }
    }
}
