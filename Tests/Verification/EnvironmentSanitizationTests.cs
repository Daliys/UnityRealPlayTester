using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class EnvironmentSanitizationTests
    {
        [UnityTest]
        public IEnumerator Reset_CleansPersistentLeaks() => ToCoroutine(async () =>
        {
            // 1. Create a leak in DDOL
            var leak = new GameObject("TestLeak_Persistent");
            Object.DontDestroyOnLoad(leak);
            
            // 2. Execute HardReset
            await HardReset.Execute();
            
            // 3. Verify leak is gone
            var found = GameObject.Find("TestLeak_Persistent");
            NUnitAssert.IsNull(found, "Persistent leak should have been destroyed by HardReset");
        });

        [UnityTest]
        public IEnumerator Reset_RestoresTimeScale() => ToCoroutine(async () =>
        {
            // 1. Mess up time
            Time.timeScale = 0.123f;
            
            // 2. Execute HardReset
            await HardReset.Execute();
            
            // 3. Verify restored
            NUnitAssert.AreEqual(1.0f, Time.timeScale, "Time.timeScale should have been reset to 1.0");
        });

        [UnityTest]
        public IEnumerator Reset_CleansInputState() => ToCoroutine(async () =>
        {
            // 1. Press a key
            await Tester.Interaction.Keyboard.Down(KeyCode.A);
            NUnitAssert.IsTrue(InputSystemShim.GetKeyHeld(KeyCode.A), "Key should be held initially");
            
            // 2. Execute HardReset
            await HardReset.Execute();
            
            // 3. Verify released
            NUnitAssert.IsFalse(InputSystemShim.GetKeyHeld(KeyCode.A), "Key should be released by HardReset");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
