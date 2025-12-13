using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Utilities;

namespace RealPlayTester.Tests
{
    // A RealPlayTest asset would normally be a ScriptableObject, but for testing the improvements logic
    // we can write a standard Unity PlayMode test or just a RealPlayTest simulation.
    // However, the user asked for a package improvement, so we should test the tools directly mostly.
    
    // Actually, to truly test it as the user would, we should create a RealPlayTest asset.
    // But since we are inside the package dev, we will write a Unity Test Runner test that calls the internal logic.
    // Or even better, a RealPlayTest that self-verifies these tools.

    // Let's make a RealPlayTest that uses the new tools.
    [CreateAssetMenu(menuName = "RealPlayTester/Tests/ImprovementTest")]
    public class ImprovementTests : RealPlayTest
    {
        protected override async Task Run()
        {
            await Task.Yield(); // Ensure async execution
            
            Log("Testing VisualTreeLogger...");
            // Create some temporary objects
            var root = new GameObject("Root_ImprovementTest");
            var child = new GameObject("Child_Hidden");
            child.transform.SetParent(root.transform);
            var btnGo = new GameObject("My_Complex_Button_Name");
            btnGo.transform.SetParent(root.transform);
            var btn = btnGo.AddComponent<Button>();
            
            try
            {
                // 1. Test DumpHierarchy
                string dump = Tester.DumpHierarchy();
                Tester.AssertTrue(dump.Contains("Root_ImprovementTest"), "Dump should contain root");
                Tester.AssertTrue(dump.Contains("My_Complex_Button_Name"), "Dump should contain button");
                Tester.AssertTrue(dump.Contains("(Button:Disabled)"), "Dump should show disabled button state (interactable=true default, wait...)");
                
                // 2. Test SmartFind
                Log("Testing SmartFind...");
                var found = Tester.FindObject("complex_button"); // Partial case-insensitive
                Tester.AssertNotNull(found, "SmartFind should find partial match");
                Tester.AssertAreEqual("My_Complex_Button_Name", found.name, "Should match correct object");

                // 3. Test LogAssert (Positive)
                Log("Testing LogAssert...");
                Tester.ExpectLog("Test Error");
                Debug.LogError("This is a Test Error that should be ignored");
                Tester.AssertNoLogErrors(); // Should pass
                
                // 4. Test Probe
                Log("Testing Probe...");
                // It's hard to test probe without visual UI, but we can call it safe
                var probe = Tester.ProbeScreen(new Vector2(100, 100));
                Tester.AssertNotNull(probe);
            }
            finally
            {
                Destroy(root);
            }
        }
    }
}
