using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Input;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Forcefully resets the game state to ensure test atomicity.
    /// Kills tweens, clears input buffers, and reloads the scene.
    /// </summary>
    public static class HardReset
    {
        public static async Task Execute()
        {
            RealPlayLog.Info("[HEALER] Initiating Hard Reset...");

            // 1. Kill Tweens (Attempt to find DOTween via reflection)
            KillAllTweens();

            // 2. Clear Input Buffers
            InputSystemShim.ClearEvents();

            // 3. Clear Static Singletons (Registry-based if available, otherwise scene will handle most)
            // Note: Users can hook into this if they have custom persistent state.

            // 4. Reload Current Scene
            string sceneName = SceneManager.GetActiveScene().name;
            await ReloadScene(sceneName);

            RealPlayLog.Info("[HEALER] Hard Reset Complete.");
        }

        private static void KillAllTweens()
        {
            try
            {
                var dotween = Type.GetType("DG.Tweening.DOTween, DOTween");
                if (dotween != null)
                {
                    var killMethod = dotween.GetMethod("KillAll", new Type[] { typeof(bool) });
                    killMethod?.Invoke(null, new object[] { false });
                    RealPlayLog.Info("[HEALER] Killed all DOTween instances.");
                }
            }
            catch { /* DOTween not found or failed */ }
        }

        private static async Task ReloadScene(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                await Task.Yield();
            }
            // Wait extra frame for initialization
            await Task.Yield();
        }
    }
}
