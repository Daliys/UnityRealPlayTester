using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>Gamepad simulation helpers.</summary>
            public static class Gamepad
            {
                /// <summary>Simulates pressing a gamepad button.</summary>
                public static Task Press(GamepadButton button)
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gamepad: Press {button}");
                    return InputSystemShim.GamepadButton(button, true);
                }

                /// <summary>Simulates releasing a gamepad button.</summary>
                public static Task Release(GamepadButton button)
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gamepad: Release {button}");
                    return InputSystemShim.GamepadButton(button, false);
                }

                /// <summary>Simulates clicking a gamepad button (Press then Release).</summary>
                public static async Task Click(GamepadButton button, float duration = 0.1f)
                {
                    await Press(button);
                    await Await.Seconds(duration);
                    await Release(button);
                }

                /// <summary>Simulates moving a gamepad stick.</summary>
                /// <param name="left">If true, move left stick. If false, move right stick.</param>
                /// <param name="value">Direction and magnitude (-1 to 1).</param>
                public static Task MoveStick(bool left, Vector2 value)
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Gamepad: {(left ? "Left" : "Right")} Stick to {value}");
                    return InputSystemShim.GamepadStick(left, value);
                }
            }
        }
    }
}
