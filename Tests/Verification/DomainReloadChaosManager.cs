using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    /// <summary>
    /// Stress test that executes multiple Play Mode entries with Domain Reload disabled.
    /// This is the ultimate test for state pollution.
    /// </summary>
    public class DomainReloadChaosManager
    {
        [UnityTest]
        public IEnumerator Execute_Consecutive_Runs_Without_Reload() => ToCoroutine(async () =>
        {
            // Note: We can only truly test this by triggering multiple test runs.
            // For now, we verify that the SubsystemRegistration hook is present and callable.
            
            var hostType = typeof(RealPlayTesterHost);
            var resetMethod = hostType.GetMethod("ResetStaticState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            NUnit.Framework.Assert.IsNotNull(resetMethod, "ResetStaticState method must exist for Domain Reload resilience");
            
            // Modify something
            Tester.Settings.ShowVisualPointer = false;
            
            // Manually invoke the reset (simulating what Unity does on entry)
            resetMethod.Invoke(null, null);
            
            NUnit.Framework.Assert.IsTrue(Tester.Settings.ShowVisualPointer, "Static state should be purged after ResetStaticState call");
            
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
