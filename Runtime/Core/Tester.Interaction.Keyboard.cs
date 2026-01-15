using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>Keyboard simulation helpers.</summary>
            public static class Keyboard
            {
                public static Task Press(KeyCode key, float duration = 0.1f) 
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Pressing key: {key}");
                    return RealPlayTester.Input.Press.Key(key, duration);
                }

                public static Task Down(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.QueueKeyState(key, true) : Task.CompletedTask;
                public static Task Up(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.QueueKeyState(key, false) : Task.CompletedTask;
                public static bool Held(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.GetKeyHeld(key) : false;
                
                public static Task Type(string text, float delay = 0.05f) 
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Typing text: {text}");
                    return RealPlayTester.Input.TextInput.Type(text, delay);
                }

                public static Task TypeIntoField(string fieldName, string text, float delay = 0.05f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.TextInput.TypeIntoField(fieldName, text, delay) : Task.CompletedTask;
            }
        }
    }
}
