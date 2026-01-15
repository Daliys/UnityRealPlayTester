using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Input;
using RealPlayTester.Utilities;

namespace RealPlayTester.Core
{
    /// <summary>
    /// Forcefully resets the game state to ensure test atomicity.
    /// Kills tweens, clears input buffers, resets time, and reloads the scene.
    /// </summary>
    public static class HardReset
    {
        public static async Task Execute()
        {
            RealPlayLog.Info("[HEALER] Initiating Hard Reset for Environment Sanitization...");

            // 1. Reset Time state (Critical for tests that use slow-mo or pause)
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f; // Default

            // 2. Kill Tweens (Attempt to find DOTween via reflection)
            KillAllTweens();

            // 3. Clear Input Buffers
            InputSystemShim.ClearEvents();

            // 4. Sanitize DontDestroyOnLoad (Except for our core host)
            CleanupPersistentObjects();

            // 5. Clear SceneCache
            SceneCache.Instance.ForceRefresh();

            // 6. Reload Current Scene
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName))
            {
                await ReloadScene(sceneName);
            }

            RealPlayLog.Info("[HEALER] Environment Sanitized.");
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

        private static void CleanupPersistentObjects()
        {
            // Find all root objects in the 'DontDestroyOnLoad' scene
            // This is tricky in Unity but we can find objects where transform.parent is null 
            // and they aren't in the active scene.
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in all)
            {
                if (go.transform.parent != null) continue;
                if (go.scene.name == "DontDestroyOnLoad")
                {
                    // PROTECT the core library host
                    if (go.name == "RealPlayTesterHost") continue;
                    
                    // Also protect internal Unity objects
                    if (go.hideFlags != HideFlags.None) continue;

                    RealPlayLog.Info($"[HEALER] Destroying leaked persistent object: {go.name}");
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private static async Task ReloadScene(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                await Task.Yield();
            }
            // Wait extra frames for initialization
            await Task.Yield();
            await Task.Yield();
        }
    }
}