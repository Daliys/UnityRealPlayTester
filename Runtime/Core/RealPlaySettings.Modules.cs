using UnityEngine;

namespace RealPlayTester.Core
{
    public static partial class RealPlaySettings
    {
        public static class Visual
        {
            public static bool ShowPointer = true;
            public static bool ShowAnchors = false;
            public static bool ShowHeatmap = false;
        }

        public static class Input
        {
            public static bool EnableHeartbeat = true;
            public static float UpdateRate = 0.016f;
            public static int MaxEventsPerFrame = 1000;
            public static bool AutoCreateEventSystem = true;
            public static bool VirtualDevicesMakeCurrent = true;
            internal static bool LockAssemblies = true;
        }

        public static class Physics
        {
            public static bool AutoSimulate2D = true;
            public static bool AutoSimulate3D = true;
            public static float IdleVelocityEpsilon = 0.05f;
            internal static float OriginalFixedDeltaTime = 0.02f;
        }

        public static class Perception
        {
            public static bool FilterToViewport = false;
            public static bool CollapseRepetitiveSiblings = true;
            public static bool IgnoreLoopingAnimations = true;
            public static bool EnableNavigationLearning = false;
            public static string[] AmbientTags = { "[Ambient]", "vfx_", "Particle" };
        }

        public static class Log
        {
            public static bool DisableStackTraces = false;
        }
    }
}