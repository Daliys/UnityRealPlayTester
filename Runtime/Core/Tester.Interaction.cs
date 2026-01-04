using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static class Interaction
        {
            /// <summary>Mouse/Pointer simulation helpers.</summary>
            public static class Mouse
            {
                public static Task Click(Vector2 screenPos) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ScreenPixels(new RealPlayTester.Input.Click.ScreenPosition { x = screenPos.x, y = screenPos.y }) : Task.CompletedTask;
                public static Task ClickPercent(float x, float y) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ScreenPercent(x, y) : Task.CompletedTask;
                public static Task ClickWorld(Vector3 worldPos, Camera camera = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.WorldPosition(worldPos, camera) : Task.CompletedTask;
                public static Task ClickObject(GameObject target, Camera camera = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.WorldObject(target, camera) : Task.CompletedTask;
                
                public static Task MoveTo(Vector2 screenPos, float duration = 0f) => RealPlayEnvironment.IsEnabled ? MouseMove.To(screenPos, duration) : Task.CompletedTask;
                public static Task MoveToCenter(GameObject target, float duration = 0f) => RealPlayEnvironment.IsEnabled ? MouseMove.To(RealInputUtility.GetScreenCenter(target), duration) : Task.CompletedTask;
                
                public static Task Down(int button = 0) => RealPlayEnvironment.IsEnabled ? InputSystemShim.MouseDown(button) : Task.CompletedTask;
                public static Task Up(int button = 0) => RealPlayEnvironment.IsEnabled ? InputSystemShim.MouseUp(button) : Task.CompletedTask;
            }

            /// <summary>Keyboard simulation helpers.</summary>
            public static class Keyboard
            {
                public static Task Press(KeyCode key, float duration = 0.1f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Press.Key(key, duration) : Task.CompletedTask;
                public static Task Down(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.QueueKeyState(key, true) : Task.CompletedTask;
                public static Task Up(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.QueueKeyState(key, false) : Task.CompletedTask;
                public static bool Held(KeyCode key) => RealPlayEnvironment.IsEnabled ? InputSystemShim.GetKeyHeld(key) : false;
                
                public static Task Type(string text, float delay = 0.05f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Text.Type(text, delay) : Task.CompletedTask;
                public static Task TypeIntoField(string fieldName, string text, float delay = 0.05f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Text.TypeIntoField(fieldName, text, delay) : Task.CompletedTask;
            }

            /// <summary>Touch gesture simulation helpers.</summary>
            public static class Touch
            {
                public static Task Tap(Vector2 pos, float duration = 0.1f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Tap(pos, duration) : Task.CompletedTask;
                public static Task Swipe(Vector2 from, Vector2 to, float duration = 0.3f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Swipe(from, to, duration) : Task.CompletedTask;
                public struct PinchParams { public Vector2 center; public float start; public float end; public float duration; }
                public static Task Pinch(PinchParams p) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.Pinch(p.center, p.start, p.end, p.duration) : Task.CompletedTask;
                public static Task LongPress(Vector2 pos, float duration = 1.0f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Touch.LongPress(pos, duration) : Task.CompletedTask;
            }
        }

        public static class Advanced
        {
            public static Task Drag(Vector2 from, Vector2 to, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Drag.FromTo(from, to, duration) : Task.CompletedTask;
            public static Task ScrollToBottom(ScrollRect scroll, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.ToBottom(scroll, duration) : Task.CompletedTask;
            public static Task ScrollUntilVisible(ScrollRect scroll, RectTransform target, float timeout = 5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Scroll.UntilVisible(scroll, target, timeout) : Task.CompletedTask;
            
            public static Task ClickButtonWithText(string text) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ButtonWithText(text) : Task.CompletedTask;
            public static Task ClickComponent<T>() where T : Component => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.Component<T>() : Task.CompletedTask;
        }
    }
}
