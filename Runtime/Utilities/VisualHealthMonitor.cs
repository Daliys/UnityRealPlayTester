using System.Collections;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Continuously monitors the scene for visual issues (Missing materials/Pink shaders).
    /// Supports 'Runtime Visual Issue Monitoring' requirements.
    /// </summary>
    public class VisualHealthMonitor : MonoBehaviour
    {
        public float CheckInterval = 2.0f;
        private bool _isRunning = false;

        public static void StartMonitoring(float interval = 2.0f)
        {
            var host = RealPlayTesterHost.Instance;
            if (host == null) return;

            var monitor = host.GetComponent<VisualHealthMonitor>() ?? host.gameObject.AddComponent<VisualHealthMonitor>();
            monitor.CheckInterval = interval;
            monitor.Begin();
        }

        public static void StopMonitoring()
        {
            var host = RealPlayTesterHost.Instance;
            if (host != null)
            {
                var monitor = host.GetComponent<VisualHealthMonitor>();
                if (monitor != null) monitor.Stop();
            }
        }

        private void Begin()
        {
            if (_isRunning) return;
            _isRunning = true;
            StartCoroutine(MonitorRoutine());
        }

        private void Stop()
        {
            _isRunning = false;
            StopAllCoroutines();
        }

        private IEnumerator MonitorRoutine()
        {
            while (_isRunning)
            {
                yield return new WaitForSeconds(CheckInterval);
                try
                {
                    RealPlayTester.Assert.Assert.NoMissingMaterials("Continuous Visual Monitor detected an error.");
                }
                catch (System.Exception ex)
                {
                    RealPlayLog.Error($"Visual Health Alert: {ex.Message}");
                    // Assert.Fail already handles overlays and pauses if enabled
                }
            }
        }
    }
}
