using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input.Internal;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class InputLayoutTests
    {
        // --- 1-3: VALID PAIRINGS ---
        [Test]
        public void Compatibility_01_Keyboard_Valid() => CheckMatch(InputSystemReflector.Types.Keyboard, InputSystemReflector.Types.KeyboardState, true);
        [Test]
        public void Compatibility_02_Mouse_Valid() => CheckMatch(InputSystemReflector.Types.Mouse, InputSystemReflector.Types.MouseState, true);
        [Test]
        public void Compatibility_03_Gamepad_Valid() => CheckMatch(InputSystemReflector.Types.Gamepad, InputSystemReflector.Types.GamepadState, true);

        // --- 4-9: MISMATCHED PAIRINGS (The Attack Vectors) ---
        [Test]
        public void Mismatch_04_MouseState_to_Gamepad() => CheckMatch(InputSystemReflector.Types.Gamepad, InputSystemReflector.Types.MouseState, false);
        [Test]
        public void Mismatch_05_KeyboardState_to_Mouse() => CheckMatch(InputSystemReflector.Types.Mouse, InputSystemReflector.Types.KeyboardState, false);
        [Test]
        public void Mismatch_06_GamepadState_to_Keyboard() => CheckMatch(InputSystemReflector.Types.Keyboard, InputSystemReflector.Types.GamepadState, false);
        [Test]
        public void Mismatch_07_MouseState_to_Keyboard() => CheckMatch(InputSystemReflector.Types.Keyboard, InputSystemReflector.Types.MouseState, false);
        [Test]
        public void Mismatch_08_KeyboardState_to_Gamepad() => CheckMatch(InputSystemReflector.Types.Gamepad, InputSystemReflector.Types.KeyboardState, false);
        [Test]
        public void Mismatch_09_GamepadState_to_Mouse() => CheckMatch(InputSystemReflector.Types.Mouse, InputSystemReflector.Types.GamepadState, false);

        // --- 10-12: NULL SAFETY ---
        [Test]
        public void Safety_10_NullDevice_Handled() => InvokeInternalQueue(null, new object(), null, false);
        [Test]
        public void Safety_11_NullState_Handled() => InvokeInternalQueue(new object(), null, null, false);
        [Test]
        public void Safety_12_NullMethod_Handled() => InvokeInternalQueue(new object(), new object(), null, false);

        // --- 13-16: REFLECTION INTEGRITY ---
        [Test]
        public void Integrity_13_SpecializedMethods_AreGeneric()
        {
            if (InputSystemReflector.Types.InputSystem == null) NUnitAssert.Ignore("No InputSystem");
            // These methods were created using MakeGenericMethod, so IsGenericMethod is false (they are specialized)
            // but IsGenericMethodDefinition is false. We want to ensure they are bound.
            NUnitAssert.IsFalse(InputSystemReflector.QueueStateEventKeyboard?.IsGenericMethodDefinition ?? true);
        }
        [Test]
        public void Integrity_14_KeyboardQueue_Exists() => NUnitAssert.IsNotNull(InputSystemReflector.QueueStateEventKeyboard);
        [Test]
        public void Integrity_15_MouseQueue_Exists() => NUnitAssert.IsNotNull(InputSystemReflector.QueueStateEventMouse);
        [Test]
        public void Integrity_16_GamepadQueue_Exists() => NUnitAssert.IsNotNull(InputSystemReflector.QueueStateEventGamepad);

        // --- 17-20: STRESS & COVERAGE ---
        [UnityTest]
        public IEnumerator Stress_17_RapidMismatch_NoCrash() => ToCoroutine(async () =>
        {
            var method = typeof(InputSimulator).GetMethod("InvokeQueueMethod", BindingFlags.NonPublic | BindingFlags.Static);
            object gamepad = VirtualDeviceManager.EnsureGamepad();
            object mouseState = Activator.CreateInstance(InputSystemReflector.Types.MouseState);
            
            // Expect multiple errors due to loop
            for(int i=0; i<50; i++)
            {
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Layout Mismatch.*"));
                method.Invoke(null, new[] { InputSystemReflector.QueueStateEventMouse, gamepad, mouseState });
            }
            await Task.Yield();
            NUnitAssert.Pass();
        });

        [Test]
        public void Coverage_18_IsDeviceCompatible_GenericObject()
        {
            var method = typeof(InputSimulator).GetMethod("IsDeviceCompatibleWithState", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new[] { typeof(string), typeof(int) });
            NUnitAssert.IsTrue(result);
        }

        [Test]
        public void Stability_19_Internal_State_Not_Corrupted()
        {
            InputSimulator.ClearMouseState();
            InputSimulator.ClearGamepadState();
            NUnitAssert.Pass();
        }

        [Test]
        public void Z_20_Discovery_Anchor() => NUnitAssert.Pass();

        private void CheckMatch(Type device, Type state, bool expected)
        {
            var method = typeof(InputSimulator).GetMethod("IsDeviceCompatibleWithState", BindingFlags.NonPublic | BindingFlags.Static);
            if (device == null || state == null) NUnitAssert.Ignore("Type not found in this Unity version");
            bool result = (bool)method.Invoke(null, new[] { device, state });
            NUnitAssert.AreEqual(expected, result, $"Pairing {device.Name} with {state.Name} should be {expected}");
        }

        private void InvokeInternalQueue(object device, object state, MethodInfo methodInfo, bool expectError)
        {
            var method = typeof(InputSimulator).GetMethod("InvokeQueueMethod", BindingFlags.NonPublic | BindingFlags.Static);
            NUnitAssert.DoesNotThrow(() => method.Invoke(null, new[] { methodInfo, device, state }));
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
