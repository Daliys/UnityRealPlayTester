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
    public class GhostHandsTests
    {
        private VerificationSceneSetup _scene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _scene = VerificationSceneSetup.Create();
            TestRunContextTracker.BeginTest("GhostHandsTest", "VirtualScene");
            yield return null;
            
            Screen.SetResolution(1024, 768, false);
            yield return null;

            Tester.Settings.ForceBatchmodeVisibility = true;
            Tester.Settings.EnableInputHeartbeat = true;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            RealPlaySettings.PreferredMode = PreferredInputMode.Auto;
            if (_scene != null) _scene.Cleanup();
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Perform_SwitchMode_MouseToGamepad() => ToCoroutine(async () =>
        {
            // 1. Mouse Mode
            RealPlaySettings.PreferredMode = PreferredInputMode.Mouse;
            await Tester.Interaction.Perform("Select", "TestButton");
            
            TestRunContextTracker.ForceWriteSnapshot();
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Clicking object: TestButton"), "Should have used Mouse simulation");

            // 2. Gamepad Mode
            RealPlaySettings.PreferredMode = PreferredInputMode.Gamepad;
            await Tester.Interaction.Perform("Select", "TestButton");
            
            TestRunContextTracker.ForceWriteSnapshot();
            md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Gamepad: Press South"), "Should have used Gamepad simulation");
        });

        [UnityTest]
        public IEnumerator Perform_SwitchMode_ToTouch() => ToCoroutine(async () =>
        {
            RealPlaySettings.PreferredMode = PreferredInputMode.Touch;
            await Tester.Interaction.Perform("Select", "TestButton");
            
            TestRunContextTracker.ForceWriteSnapshot();
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Tap at"), "Should have used Touch simulation");
        });

        private string GetContextMarkdown()
        {
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(mdPath);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
