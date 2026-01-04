using System.Threading.Tasks;
using RealPlayTester.Await;

namespace RealPlayTester.Utilities
{
    /// <summary>
    /// Basic shim to simulate network behavior in tests.
    /// </summary>
    public static class NetworkShim
    {
        private static int _latencyMs = 0;

        /// <summary>Sets the global simulated latency in milliseconds.</summary>
        public static void SetLatency(int ms)
        {
            _latencyMs = ms;
        }

        /// <summary>
        /// Awaits the current simulated latency. 
        /// Use this to wrap network requests in tested code.
        /// </summary>
        public static async Task SimulateWait()
        {
            if (_latencyMs > 0)
            {
                await Wait.Seconds(_latencyMs / 1000f, unscaled: true);
            }
        }
    }
}
