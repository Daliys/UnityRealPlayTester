using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class GamepadVerificationTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestRunContextTracker.BeginTest("GamepadTest", "EmptyScene");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Gamepad_ButtonClick_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gamepad.Click(GamepadButton.South);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Gamepad: Press South"), "Timeline missing Press South");
            NUnitAssert.IsTrue(md.Contains("Gamepad: Release South"), "Timeline missing Release South");
        });

        [UnityTest]
        public IEnumerator Gamepad_StickMove_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gamepad.MoveStick(true, new Vector2(0.5f, -0.5f));
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Gamepad: Left Stick to (0.50, -0.50)"), "Timeline missing Stick move");
        });

        [UnityTest]
        public IEnumerator Gamepad_TriggerPress_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gamepad.Press(GamepadButton.RightTrigger);
            
            string md = GetContextMarkdown();
            NUnitAssert.IsTrue(md.Contains("Gamepad: Press RightTrigger"), "Timeline missing Trigger press");
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
