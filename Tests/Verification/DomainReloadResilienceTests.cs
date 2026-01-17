using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class DomainReloadResilienceTests
    {
        [UnityTest]
        public IEnumerator Settings_Reset_BetweenRuns_EvenWithoutReload() => ToCoroutine(async () =>
        {
            // This test is designed to be run TWICE in a row with Domain Reload OFF.
            // On the first run, it modifies a setting.
            // On the second run, it expects the setting to be back to default.
            
            bool isFirstRun = !RealPlayTester.Core.EventTracker.WasFired("SecondRun_Sentinel");

            if (isFirstRun)
            {
                Debug.Log("[RESILIENCE] First Run: Modifying setting...");
                Tester.Settings.ShowVisualPointer = false;
                RealPlayTester.Core.EventTracker.Record("SecondRun_Sentinel");
                
                // Assert it changed
                NUnit.Framework.Assert.IsFalse(Tester.Settings.ShowVisualPointer);
            }
            else
            {
                Debug.Log("[RESILIENCE] Second Run: Verifying reset...");
                // If the library reset itself correctly via SubsystemRegistration, 
                // this should be TRUE even if the static memory wasn't wiped by Unity.
                NUnit.Framework.Assert.IsTrue(Tester.Settings.ShowVisualPointer, "ShowVisualPointer should have been reset to default (true) by SubsystemRegistration hook");
            }

            await Task.Yield();
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }
    }
}
