using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class SettingsResetTests
    {
        [UnityTest]
        public IEnumerator HardReset_RestoresDefaultSettings() => ToCoroutine(async () =>
        {
            // 1. Change settings
            RealPlaySettings.ShowVisualPointer = false;
            RealPlaySettings.PreferredMode = PreferredInputMode.Gamepad;
            
            NUnit.Framework.Assert.IsFalse(RealPlaySettings.ShowVisualPointer);
            NUnit.Framework.Assert.AreEqual(PreferredInputMode.Gamepad, RealPlaySettings.PreferredMode);

            // 2. Execute Hard Reset
            await HardReset.Execute();

            // 3. Verify defaults
            NUnit.Framework.Assert.IsTrue(RealPlaySettings.ShowVisualPointer, "ShowVisualPointer should be reset to true");
            NUnit.Framework.Assert.AreEqual(PreferredInputMode.Auto, RealPlaySettings.PreferredMode, "PreferredMode should be reset to Auto");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }
    }
}
