using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>Mouse/Pointer simulation helpers.</summary>
            public static class Mouse
            {
                public static Task Click(Vector2 screenPos) 
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Clicking at {screenPos}");
                    return RealPlayTester.Input.Click.ScreenPixels(new RealPlayTester.Input.Click.ScreenPosition { x = screenPos.x, y = screenPos.y });
                }

                public static Task ClickPercent(float x, float y) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.ScreenPercent(x, y) : Task.CompletedTask;
                public static Task ClickWorld(Vector3 worldPos, Camera camera = null) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Click.WorldPosition(worldPos, camera) : Task.CompletedTask;
                
                public static Task ClickObject(GameObject target, Camera camera = null) 
                {
                    if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
                    RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Input", $"Clicking object: {(target != null ? target.name : "null")}");
                    return RealPlayTester.Input.Click.WorldObject(target, camera);
                }
                
                public static Task MoveTo(Vector2 screenPos, float duration = 0f) => RealPlayEnvironment.IsEnabled ? MouseMove.To(screenPos, duration) : Task.CompletedTask;
                public static Task MoveToCenter(GameObject target, float duration = 0f) => RealPlayEnvironment.IsEnabled ? MouseMove.To(RealInputUtility.GetScreenCenter(target), duration) : Task.CompletedTask;
                
                public static Task Drag(Vector2 from, Vector2 to, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Drag.FromTo(from, to, duration) : Task.CompletedTask;
                public static Task Drag(GameObject target, Vector2 to, float duration = 0.5f) => RealPlayEnvironment.IsEnabled ? RealPlayTester.Input.Drag.FromTo(RealInputUtility.GetScreenCenter(target), to, duration) : Task.CompletedTask;

                public static Task Down(int button = 0) => RealPlayEnvironment.IsEnabled ? InputSystemShim.MouseDown(button) : Task.CompletedTask;
                public static Task Up(int button = 0) => RealPlayEnvironment.IsEnabled ? InputSystemShim.MouseUp(button) : Task.CompletedTask;
            }
        }
    }
}
