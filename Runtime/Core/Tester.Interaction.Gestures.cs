using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        public static partial class Interaction
        {
            /// <summary>Complex multi-touch gesture simulation.</summary>
            public static class Gestures
            {
                public static Task Pinch(Vector2 center, float startDistance, float endDistance, float angle = 0f, float duration = 0.5f) 
                    => RealPlayEnvironment.IsEnabled ? GestureSimulator.Pinch(center, startDistance, endDistance, angle, duration) : Task.CompletedTask;

                public static Task Twist(Vector2 center, float distance, float startAngle, float endAngle, float duration = 0.5f)
                    => RealPlayEnvironment.IsEnabled ? GestureSimulator.Twist(center, distance, startAngle, endAngle, duration) : Task.CompletedTask;

                public static Task MultiTap(Vector2 screenPos, int count, float interval = 0.1f)
                    => RealPlayEnvironment.IsEnabled ? GestureSimulator.MultiTap(screenPos, count, interval) : Task.CompletedTask;
            }
        }
    }
}
