using System;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Declarative configuration for a RealPlay test.
    /// The framework uses this to prepare the environment before Run() is called.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class PlayModeTestAttribute : Attribute
    {
        /// <summary>The name of the scene to load before the test starts.</summary>
        public string Scene { get; set; }

        /// <summary>The time scale to set (e.g., 1.0f for normal, 0.0f to start paused).</summary>
        public float TimeScale { get; set; } = 1.0f;

        /// <summary>If true, wait for CompositionRoot.Instance.Services to be non-null.</summary>
        public bool WaitForDI { get; set; } = false;

        /// <summary>Optional maximum duration for the test before auto-timeout.</summary>
        public float TimeoutSeconds { get; set; } = 120f;
    }
}
