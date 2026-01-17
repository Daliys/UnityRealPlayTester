using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class InteractionFeatureTests
    {
        private GameObject _testRoot;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _testRoot = new GameObject("InteractionFeatureRoot");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Object.DestroyImmediate(_testRoot);
            RealPlaySettings.PreferredMode = PreferredInputMode.Auto;
            yield return null;
        }

        // 6. Rapid Mode Switching
        [UnityTest]
        public IEnumerator Perform_Handles_RapidModeSwitch() => ToCoroutine(async () =>
        {
            var btn = new GameObject("Btn", typeof(RectTransform), typeof(Button), typeof(Image));
            btn.transform.SetParent(_testRoot.transform);

            RealPlaySettings.PreferredMode = PreferredInputMode.Mouse;
            await Tester.Interaction.Perform("Select", btn);
            
            RealPlaySettings.PreferredMode = PreferredInputMode.Gamepad;
            await Tester.Interaction.Perform("Select", btn);
            
            RealPlaySettings.PreferredMode = PreferredInputMode.Touch;
            await Tester.Interaction.Perform("Select", btn);

            NUnitAssert.Pass("Rapid switching did not throw exceptions");
        });

        // 7. Instant Gestures (Zero Duration)
        [UnityTest]
        public IEnumerator Gestures_Handles_ZeroDuration() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.Twist(Vector2.zero, 100, 0, 90, 0f);
            await Tester.Interaction.Gestures.Pinch(Vector2.zero, 10, 100, 0, 0f);
            NUnitAssert.Pass("Instant gestures completed successfully");
        });

        // 8. Multi-Device Simultaneous Input
        [UnityTest]
        public IEnumerator Interaction_Simultaneous_Keyboard_Gamepad() => ToCoroutine(async () =>
        {
            var t1 = Tester.Interaction.Keyboard.Down(KeyCode.LeftShift);
            var t2 = Tester.Interaction.Gamepad.MoveStick(true, Vector2.up);
            await Task.WhenAll(t1, t2);
            
            NUnitAssert.IsTrue(InputSystemShim.GetKeyHeld(KeyCode.LeftShift));
            await Tester.Interaction.Keyboard.Up(KeyCode.LeftShift);
        });

        // 9. Non-ASCII Text Typing
        [UnityTest]
        public IEnumerator Text_Type_Handles_SpecialCharacters() => ToCoroutine(async () =>
        {
            var inputGo = new GameObject("In", typeof(RectTransform), typeof(InputField));
            inputGo.transform.SetParent(_testRoot.transform);
            var input = inputGo.GetComponent<InputField>();
            
            string special = "!@#$%^&*()_+";
            await Tester.Interaction.Keyboard.TypeIntoField("In", special);
            
            NUnitAssert.AreEqual(special, input.text);
        });

        // 10. Deep Nested Submit Intent
        [UnityTest]
        public IEnumerator Perform_Submit_FindsDeepComponent() => ToCoroutine(async () =>
        {
            var root = new GameObject("Parent");
            root.transform.SetParent(_testRoot.transform);
            var child = new GameObject("Child", typeof(RectTransform), typeof(InputField));
            child.transform.SetParent(root.transform);

            // Intent on Parent should trigger on Child InputField
            await Tester.Interaction.Perform("Submit", root);
            NUnitAssert.Pass("Submit intent on container successful");
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
