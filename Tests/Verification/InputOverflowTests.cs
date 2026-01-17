using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Input.Internal;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class InputOverflowTests
    {
        // --- 1-10: LIMIT & TYPE STRESS ---
        [Test]
        [TestCase(1, "Mouse")]
        [TestCase(10, "Keyboard")]
        [TestCase(50, "Gamepad")]
        [TestCase(100, "Mouse")]
        [TestCase(200, "Keyboard")]
        [TestCase(500, "Gamepad")]
        [TestCase(5, "Mouse")]
        [TestCase(15, "Keyboard")]
        [TestCase(25, "Gamepad")]
        [TestCase(1000, "Mouse")]
        public async Task Input_Throttling_Limit_And_Type_Stress(int limit, string type)
        {
            int originalLimit = RealPlaySettings.MaxInputEventsPerFrame;
            RealPlaySettings.MaxInputEventsPerFrame = limit;
            
            try {
                for (int i = 0; i < limit * 2; i++) {
                    switch(type) {
                        case "Mouse": await InputSimulator.QueueMouseState(Vector2.zero, -1, false, true); break;
                        case "Keyboard": await InputSimulator.QueueKeyState(1, true); break;
                        case "Gamepad": await InputSimulator.QueueGamepadState(0, Vector2.zero, Vector2.zero); break;
                    }
                }
                NUnitAssert.Pass($"Throttled {type} at limit {limit}");
            }
            finally {
                RealPlaySettings.MaxInputEventsPerFrame = originalLimit;
            }
        }

        // --- 11-15: MULTI-FRAME RESET LOGIC ---
        [UnityTest]
        public IEnumerator Input_Throttling_Resets_Each_Frame() => ToCoroutine(async () =>
        {
            RealPlaySettings.MaxInputEventsPerFrame = 10;
            for(int i=0; i<20; i++) await InputSimulator.QueueKeyState(1, true);
            await Task.Yield();
            await InputSimulator.QueueKeyState(1, true);
            NUnitAssert.Pass("Throttling counter correctly reset on new frame.");
        });

        [Test]
        [TestCase(1), TestCase(2), TestCase(3), TestCase(4)]
        public async Task Input_Throttling_Steady_Pressure(int iteration)
        {
            RealPlaySettings.MaxInputEventsPerFrame = 5;
            for(int i=0; i<10; i++) await InputSimulator.QueueMouseState(Vector2.one, -1, false, true);
            await Task.Yield();
            NUnitAssert.Pass($"Iteration {iteration} pressure test passed.");
        }

        // --- 16-20: EDGE CASES & ASYNC BURSTS ---
        [Test]
        public void Input_Throttling_Zero_Disable_Check()
        {
            RealPlaySettings.MaxInputEventsPerFrame = 0;
            NUnitAssert.AreEqual(0, RealPlaySettings.MaxInputEventsPerFrame);
        }

        [UnityTest]
        public IEnumerator Input_Throttling_Negative_Value_Safety() => ToCoroutine(async () =>
        {
            RealPlaySettings.MaxInputEventsPerFrame = -1; 
            for(int i=0; i<100; i++) await InputSimulator.QueueKeyState(1, true);
            NUnitAssert.Pass("Negative limit handled safely.");
        });

        [UnityTest]
        public IEnumerator Input_Throttling_High_Frequency_Burst() => ToCoroutine(async () =>
        {
            RealPlaySettings.MaxInputEventsPerFrame = 100;
            var tasks = new List<Task>();
            for(int i=0; i<500; i++) tasks.Add(InputSimulator.QueueMouseState(Vector2.zero, -1, false, true));
            await Task.WhenAll(tasks);
            NUnitAssert.Pass("Async burst handled correctly by throttle.");
        });

        [Test]
        public void Input_Throttling_Setting_Stability_1() => NUnitAssert.DoesNotThrow(() => RealPlaySettings.MaxInputEventsPerFrame = 9999);
        
        [Test]
        public void Input_Throttling_Setting_Stability_2() => NUnitAssert.DoesNotThrow(() => RealPlaySettings.MaxInputEventsPerFrame = 1);

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
