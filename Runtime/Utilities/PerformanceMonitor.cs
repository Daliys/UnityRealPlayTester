using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealPlayTester.Core;
using UnityEngine.Profiling;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Monitors real-time performance metrics (FPS, Memory, DrawCalls) during tests.
    /// Supports 'Diagnostic & Integrity Tools' requirements.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static bool IsEnabled = true;
        private bool _isMonitoring = false;
        
        // Thresholds
        private float _maxGcAllocKB = 0;
        private float _maxFrameTimeMs = 0;
        private int _maxConsecutiveFpsViolations = 0;
        private int _currentConsecutiveViolations = 0;

        private long _lastAllocatedMemory = 0;

        public static PerformanceMonitor Instance
        {
            get
            {
                var host = RealPlayTesterHost.Instance;
                if (host == null) return null;
                return host.GetComponent<PerformanceMonitor>() ?? host.gameObject.AddComponent<PerformanceMonitor>();
            }
        }

        public void StartMonitoring(float maxGcAllocKB = 1024, float maxFrameTimeMs = 33.3f, int maxConsecutiveViolations = 5)
        {
            if (!IsEnabled) return;
            _maxGcAllocKB = maxGcAllocKB;
            _maxFrameTimeMs = maxFrameTimeMs;
            _maxConsecutiveFpsViolations = maxConsecutiveViolations;
            _currentConsecutiveViolations = 0;
            _lastAllocatedMemory = GC.GetTotalMemory(false);
            
            if (!_isMonitoring)
            {
                _isMonitoring = true;
                StartCoroutine(MonitorRoutine());
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Performance", $"Started monitoring: MaxAlloc={maxGcAllocKB}KB, MaxFrameTime={maxFrameTimeMs}ms");
            }
        }

        public void StopMonitoring()
        {
            if (_isMonitoring)
            {
                _isMonitoring = false;
                StopAllCoroutines();
                RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Performance", "Stopped monitoring");
            }
        }

        private IEnumerator MonitorRoutine()
        {
            var wait = new WaitForSecondsRealtime(0.5f);
            while (_isMonitoring && IsEnabled)
            {
                yield return wait;
                UpdateAllocMonitor();
                UpdateFPSWatcher();
                UpdateDrawCallCounter();
            }
        }

        private void UpdateAllocMonitor()
        {
            long currentAllocated = GC.GetTotalMemory(false);
            long delta = currentAllocated - _lastAllocatedMemory;
            if (delta > 0)
            {
                float deltaKB = delta / 1024f;
                if (_maxGcAllocKB > 0 && deltaKB > _maxGcAllocKB)
                {
                    HandleViolation($"GC.Alloc violation: {deltaKB:F2}KB > {_maxGcAllocKB}KB detected since last check.");
                }
            }
            _lastAllocatedMemory = currentAllocated;
        }

        private void UpdateFPSWatcher()
        {
            float currentFps = 1.0f / Time.unscaledDeltaTime;
            if (_maxFrameTimeMs > 0 && (1000f / currentFps) > _maxFrameTimeMs)
            {
                _currentConsecutiveViolations++;
                if (_currentConsecutiveViolations >= _maxConsecutiveFpsViolations)
                {
                    HandleViolation($"FPS violation: Current FPS {currentFps:F1} is below threshold for {_currentConsecutiveViolations} check cycles.");
                    _currentConsecutiveViolations = 0;
                }
            }
            else
            {
                _currentConsecutiveViolations = 0;
            }
        }

        private void UpdateDrawCallCounter()
        {
#if UNITY_EDITOR
            try {
                int drawCalls = UnityEditor.UnityStats.drawCalls;
                int batches = UnityEditor.UnityStats.batches;
                if (drawCalls > batches * 2 && batches > 10)
                {
                    if (Time.frameCount % 300 == 0)
                    {
                        RealPlayLog.Warn($"Possible batching issue: {drawCalls} draw calls for {batches} batches.");
                    }
                }
            } catch { }
#endif
        }

        private void HandleViolation(string message)
        {
            RealPlayLog.Error("[Performance] " + message);
            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Performance", "VIOLATION: " + message);
        }

        public float GetCurrentFPS() => 1.0f / Time.unscaledDeltaTime;
        public long GetTotalAllocatedMemory() => Profiler.GetTotalAllocatedMemoryLong();
    }
}
