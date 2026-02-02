using UnityEngine;

namespace RealPlayTester.Core
{
    public enum PreferredInputMode { Auto, Mouse, Touch, Gamepad }

    public static partial class RealPlaySettings
    {
        public static PreferredInputMode PreferredMode = PreferredInputMode.Auto;
        public static bool ForceBatchmodeVisibility = false;
        public static bool MirrorLogsToStdout = false;
        public static bool SilenceLogs = false;
        public static bool CompactJSON = false;
        public static bool DisableLogStackTraces = false;

        private static bool _isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => _isInitialized = false;

        public static void Initialize(bool force = false)
        {
            if (!_isInitialized) Physics.OriginalFixedDeltaTime = Time.fixedDeltaTime;
            if (_isInitialized && !force) return;
            _isInitialized = true;

            ApplyDefaults();
            if (Application.isBatchMode) ApplyBatchmodeDefaults();
        }

        private static void ApplyDefaults()
        {
            PreferredMode = PreferredInputMode.Auto;
            ForceBatchmodeVisibility = false;
            MirrorLogsToStdout = false;
            SilenceLogs = false;
            CompactJSON = false;
            DisableLogStackTraces = false;

            Visual.ShowPointer = true;
            Visual.ShowAnchors = false;
            Visual.ShowHeatmap = false;

            Input.EnableHeartbeat = true;
            Input.UpdateRate = 0.016f;
            Input.MaxEventsPerFrame = 1000;
            Input.AutoCreateEventSystem = true;
            Input.VirtualDevicesMakeCurrent = true;

            Physics.AutoSimulate2D = true;
            Physics.AutoSimulate3D = true;
            Physics.IdleVelocityEpsilon = 0.05f;

            Perception.FilterToViewport = false;
            Perception.CollapseRepetitiveSiblings = true;
            Perception.IgnoreLoopingAnimations = true;
            Perception.EnableNavigationLearning = false;
        }

        private static void ApplyBatchmodeDefaults()
        {
            RealPlayTester.Utilities.PerformanceMonitor.IsEnabled = false;
            if (!ForceBatchmodeVisibility) ForceBatchmodeVisibility = true;
            MirrorLogsToStdout = true;
            SilenceLogs = true;
            CompactJSON = true;
            Input.EnableHeartbeat = true;
            Physics.AutoSimulate2D = true;
            Physics.AutoSimulate3D = true;
            DisableLogStackTraces = true;
        }
    }
}