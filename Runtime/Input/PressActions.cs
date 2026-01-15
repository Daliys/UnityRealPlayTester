using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Await;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Keyboard press helpers. New Input System is fully supported; legacy Input has limited simulation.
    /// </summary>
    public static class Press
    {
        public static Task Key(KeyCode key, float durationSeconds)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, durationSeconds);
        }

        public static Task KeyDown(KeyCode key)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, 0f, true, false);
        }

        public static Task KeyUp(KeyCode key)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return PressInternal(key, 0f, false, true);
        }

        private static async Task PressInternal(KeyCode key, float durationSeconds, bool downOnly = false, bool upOnly = false)
        {
            if (!upOnly)
            {
                if (InputSystemShim.IsAvailable)
                {
                    InputSystemShim.KeyDown(key);
                }
                else
                {
                    LegacyInputFallback.WarnLimited();
                    await LegacyInputFallback.SimulateKeyPress(key, durationSeconds, downOnly, upOnly, RealPlayExecutionContext.Token);
                }
            }

            if (!upOnly && durationSeconds > 0f)
            {
                await Wait.Seconds(durationSeconds);
            }

            if (!downOnly)
            {
                if (InputSystemShim.IsAvailable)
                {
                    InputSystemShim.KeyUp(key);
                }
            }
        }
    }
}
