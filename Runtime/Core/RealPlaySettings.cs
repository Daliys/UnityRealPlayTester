using UnityEngine;

namespace RealPlayTester.Core
{
    public enum PreferredInputMode { Auto, Mouse, Touch, Gamepad }

    /// <summary>
    /// Global settings for the RealPlayTester framework.
    /// Can be configured at runtime or via static defaults.
    /// </summary>
    public static class RealPlaySettings
    {
        /// <summary>The input method preferred for high-level Perform() calls.</summary>
        public static PreferredInputMode PreferredMode = PreferredInputMode.Auto;

        /// <summary>Set to true to show a red dot representing the simulated mouse cursor.</summary>
        public static bool ShowVisualPointer = true;

        /// <summary>If true, render ID tags (anchors) over interactable elements.</summary>
        public static bool ShowVisualAnchors = false;

        /// <summary>If true, track and render a heatmap of simulated inputs.</summary>
        public static bool ShowInteractionHeatmap = false;

        /// <summary>In batchmode, force UI visibility (alpha=1, interactable=true) to bypass animations.</summary>
        public static bool ForceBatchmodeVisibility = false;

        /// <summary>If true, manually update Input System and Physics during Waits.</summary>
        public static bool EnableInputHeartbeat = true;

        /// <summary>Mirror framework logs to System.Console for CI/CD.</summary>
        public static bool MirrorLogsToStdout = false;
 
        /// <summary>
        /// If true, InputSimulator will call Physics2D.Simulate() during heartbeats in batchmode.
        /// </summary>
        public static bool AutoSimulatePhysics2D = true;

        /// <summary>
        /// If true, InputSimulator will call Physics.Simulate() during heartbeats in batchmode.
        /// </summary>
        public static bool AutoSimulatePhysics3D = true;

        /// <summary>
        /// The frequency (in seconds) at which the Input Heartbeat should tick.
        /// Default is 0.016f (approx 60fps).
        /// </summary>
        public static float InputUpdateRate = 0.016f;

        /// <summary>If true, disable stack traces for Info logs to save space.</summary>
        public static bool DisableLogStackTraces = false;

        /// <summary>If true, Semantic DOM will only include objects visible in the viewport.</summary>
        public static bool FilterDOMToViewport = false;

        /// <summary>If true, hierarchy dumps will collapse similar sibling objects (e.g. tiles) to save tokens.</summary>
        public static bool CollapseRepetitiveSiblings = true;

        /// <summary>Velocity below this is considered 'stopped' for SteadyState.</summary>
        public static float IdleVelocityEpsilon = 0.05f;

        /// <summary>If true, SteadyState will ignore non-transitioning looping animations.</summary>
        public static bool IgnoreLoopingAnimations = true;

        /// <summary>Objects with these prefixes will be ignored by SteadyState checks.</summary>
        public static string[] AmbientTags = { "[Ambient]", "vfx_", "Particle" };

        /// <summary>If true, virtual simulation devices will call MakeCurrent().</summary>
        public static bool VirtualDevicesMakeCurrent = true;

        /// <summary>If true, automatically learn navigation paths between game states during execution.</summary>
        public static bool EnableNavigationLearning = false;

        /// <summary>If true, RealPlayTester will create a default EventSystem if none exists.</summary>
                        public static bool AutoCreateEventSystem = true;
                        internal static bool LockAssembliesDuringInteraction = true; 
                        private static bool _isInitialized = false;        internal static float OriginalFixedDeltaTime { get; private set; } = 0.02f;
 
        /// <summary>
        /// Initialize settings based on environment.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _isInitialized = false;
        }

        public static void Initialize(bool force = false)
        {
            if (!_isInitialized) OriginalFixedDeltaTime = Time.fixedDeltaTime;
            if (_isInitialized && !force) return;
            _isInitialized = true;

            ApplyDefaults();
            if (Application.isBatchMode) ApplyBatchmodeDefaults();
        }

        private static void ApplyDefaults()
        {
            PreferredMode = PreferredInputMode.Auto;
            ShowVisualPointer = true;
            ShowVisualAnchors = false;
            ShowInteractionHeatmap = false;
            ForceBatchmodeVisibility = false;
            EnableInputHeartbeat = true;
            MirrorLogsToStdout = false;
            AutoSimulatePhysics2D = true;
            AutoSimulatePhysics3D = true;
            InputUpdateRate = 0.016f;
            DisableLogStackTraces = false;
            FilterDOMToViewport = false;
            CollapseRepetitiveSiblings = true;
            IdleVelocityEpsilon = 0.05f;
            IgnoreLoopingAnimations = true;
            EnableNavigationLearning = false;
            AutoCreateEventSystem = true;
        }

        private static void ApplyBatchmodeDefaults()
        {
            RealPlayTester.Utilities.PerformanceMonitor.IsEnabled = false;
            if (!ForceBatchmodeVisibility) ForceBatchmodeVisibility = true;
            MirrorLogsToStdout = true;
            EnableInputHeartbeat = true;
            AutoSimulatePhysics2D = true;
            AutoSimulatePhysics3D = true;
            DisableLogStackTraces = true;
        }
    }
}
