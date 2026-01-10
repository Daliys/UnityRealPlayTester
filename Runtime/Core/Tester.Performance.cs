using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core
{
    public static partial class Tester
    {
        /// <summary>
        /// Real-time performance monitoring and profiling tools.
        /// </summary>
        public static class Performance
        {
            /// <summary>
            /// Starts tracking performance metrics. Will log errors if thresholds are exceeded.
            /// </summary>
            /// <param name="maxGcAllocKB">Max memory allocation allowed per frame.</param>
            /// <param name="maxFrameTimeMs">Max duration of a single frame (e.g. 33.3ms for 30fps).</param>
            /// <param name="maxConsecutiveViolations">Number of consecutive frames before a frame-time error is logged.</param>
            public static void StartMonitoring(float maxGcAllocKB = 1024, float maxFrameTimeMs = 33.3f, int maxConsecutiveViolations = 5)
            {
                if (RealPlayEnvironment.IsEnabled)
                {
                    PerformanceMonitor.Instance.StartMonitoring(maxGcAllocKB, maxFrameTimeMs, maxConsecutiveViolations);
                }
            }

            /// <summary>
            /// Stops performance tracking.
            /// </summary>
            public static void StopMonitoring()
            {
                if (RealPlayEnvironment.IsEnabled)
                {
                    PerformanceMonitor.Instance.StopMonitoring();
                }
            }

            /// <summary>Returns the current FPS (unscaled).</summary>
            public static float CurrentFPS => RealPlayEnvironment.IsEnabled ? PerformanceMonitor.Instance.GetCurrentFPS() : 0f;

            /// <summary>Returns total allocated memory in bytes.</summary>
            public static long TotalAllocatedMemory => RealPlayEnvironment.IsEnabled ? PerformanceMonitor.Instance.GetTotalAllocatedMemory() : 0;
        }
    }
}
