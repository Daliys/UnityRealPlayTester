using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        /// <summary>
        /// Human-like input simulation. Categorized into Mouse, Keyboard, Touch, and Gestures.
        /// </summary>
        public static partial class Interaction
        {
            // Implementation split across:
            // Tester.Interaction.Mouse.cs
            // Tester.Interaction.Keyboard.cs
            // Tester.Interaction.Touch.cs
            // Tester.Interaction.Gestures.cs
        }

        /// <summary>
        /// High-level interaction sequences and semantic helpers.
        /// </summary>
        public static class Advanced
        {
            public static Task Drag(Vector2 from, Vector2 to, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Drag.FromTo(from, to, duration) : Task.CompletedTask;
            public static Task ScrollToBottom(ScrollRect scroll, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.ToBottom(scroll, duration) : Task.CompletedTask;
            public static Task ScrollUntilVisible(ScrollRect scroll, RectTransform target, float timeout = 5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.UntilVisible(scroll, target, timeout) : Task.CompletedTask;
            
            public static Task ClickButtonWithText(string text) 
            {
                if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Clicking button with text: {text}");
                return RealPlayTester.Input.Click.ButtonWithText(text);
            }

            public static Task ClickComponent<T>() where T : Component => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.Component<T>() : Task.CompletedTask;
        }
    }
}