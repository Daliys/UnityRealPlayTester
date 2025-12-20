using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;

namespace RealPlayTester.Await
{
    /// <summary>
    /// Async-friendly wait helpers that keep execution on Unity's main thread.
    /// Uses scaled time (Time.time) by default. Set unscaled=true for realtime.
    /// </summary>
    public static class Wait
    {
        /// <summary>
        /// Wait for the specified number of seconds using scaled time (affected by Time.timeScale).
        /// </summary>
        /// <param name="seconds">Duration to wait in seconds.</param>
        /// <param name="unscaled">If true, uses realtime (unaffected by Time.timeScale).</param>
        public static async Task Seconds(float seconds, bool unscaled = false)
        {
            if (!RealPlayEnvironment.IsEnabled || seconds <= 0f)
            {
                return;
            }

            var token = RealPlayExecutionContext.Token;
            
            if (unscaled)
            {
                float startTime = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - startTime < seconds)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            else
            {
                // Use scaled time - handles timeScale changes gracefully
                float elapsed = 0f;
                while (elapsed < seconds)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                    // Use deltaTime for scaled, but fall back to unscaledDeltaTime if timeScale is 0
                    elapsed += Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                }
            }
        }

        /// <summary>
        /// Wait for the specified number of frames.
        /// </summary>
        /// <param name="frames">Number of frames to wait.</param>
        public static async Task Frames(int frames)
        {
            if (!RealPlayEnvironment.IsEnabled || frames <= 0)
            {
                return;
            }

            var token = RealPlayExecutionContext.Token;
            for (int i = 0; i < frames; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        /// <summary>
        /// Wait until the predicate returns true.
        /// </summary>
        /// <param name="predicate">Condition to check each frame.</param>
        /// <param name="timeoutSeconds">Optional timeout in realtime seconds.</param>
        public static async Task Until(Func<bool> predicate, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (predicate == null)
            {
                return;
            }

            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? float.MaxValue;
            var token = RealPlayExecutionContext.Token;
            
            while (!predicate())
            {
                token.ThrowIfCancellationRequested();
                if (Time.realtimeSinceStartup - startTime >= timeout)
                {
                    throw new TimeoutException("Wait.Until timed out.");
                }

                await Task.Yield();
            }
        }

        /// <summary>
        /// Wait until a scene with the specified name is loaded.
        /// </summary>
        /// <param name="sceneName">Name of the scene to wait for.</param>
        /// <param name="timeoutSeconds">Optional timeout in realtime seconds.</param>
        public static async Task SceneLoaded(string sceneName, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            bool loaded = SceneManager.GetActiveScene().name == sceneName;
            if (loaded)
            {
                return; // Already loaded, no need to wait
            }
            
            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? float.MaxValue;

            void OnLoaded(Scene scene, LoadSceneMode mode)
            {
                if (scene.name == sceneName)
                {
                    loaded = true;
                }
            }

            SceneManager.sceneLoaded += OnLoaded;
            try
            {
                var token = RealPlayExecutionContext.Token;
                while (!loaded)
                {
                    token.ThrowIfCancellationRequested();
                    if (Time.realtimeSinceStartup - startTime >= timeout)
                    {
                        throw new TimeoutException($"Wait.SceneLoaded timed out waiting for scene '{sceneName}'.");
                    }

                    await Task.Yield();
                }
            }
            finally
            {
                SceneManager.sceneLoaded -= OnLoaded;
            }
        }

        public static Task ForObject(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return Until(() => GameObject.Find(name) != null, timeoutSeconds);
        }

        public static Task ForComponent<T>(float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return Until(() => GameObject.FindFirstObjectByType<T>() != null, timeoutSeconds);
        }

        public static Task ForUIVisible(string name, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return Until(() =>
            {
                var obj = GameObject.Find(name);
                return IsFullyVisibleAndInteractable(obj);
            }, timeoutSeconds);
        }

        public static Task ForInteractable<T>(string textFilter = null, float? timeoutSeconds = null) where T : Component
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            return Until(() =>
            {
                var candidates = GameObject.FindObjectsByType<T>(FindObjectsSortMode.None);
                foreach (var c in candidates)
                {
                     if (!c.gameObject.activeInHierarchy) continue;
                     if (!IsFullyVisibleAndInteractable(c.gameObject)) continue;
                     
                     if (!string.IsNullOrEmpty(textFilter))
                     {
                         // Basic text check for buttons/TMP
                         var text = c.GetComponentInChildren<UnityEngine.UI.Text>();
                         if (text != null && text.text.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                         
                         // TMP support via reflection
                         var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
                         if (tmpType != null)
                         {
                             var tmp = c.GetComponentInChildren(tmpType);
                             if (tmp != null)
                             {
                                 var prop = tmpType.GetProperty("text");
                                 string val = prop?.GetValue(tmp) as string;
                                 if (val != null && val.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                             }
                         }
                     }
                     else
                     {
                         return true;
                     }
                }
                return false;
            }, timeoutSeconds);
        }

        private static bool IsFullyVisibleAndInteractable(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return false;

            // Check upstream CanvasGroups
            var groups = obj.GetComponentsInParent<CanvasGroup>();
            foreach (var g in groups)
            {
                // 'IgnoreParentGroups' allows breaking the chain, but usually we care about the net result
                if (g.ignoreParentGroups) break; 
                
                if (g.alpha <= 0f || !g.interactable) return false;
            }
            return true;
        }

        public static Task ForAnimationState(Animator animator, string stateName, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled || animator == null)
            {
                return Task.CompletedTask;
            }

            return Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName), timeoutSeconds);
        }

        public static Task ForAudioComplete(AudioSource source, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled || source == null)
            {
                return Task.CompletedTask;
            }

            return Until(() => !source.isPlaying, timeoutSeconds);
        }

        public static Task While(Func<bool> predicate, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            if (predicate == null)
            {
                return Task.CompletedTask;
            }

            return Until(() => !predicate(), timeoutSeconds);
        }

        public static Task ForLoadingComplete(string loadingObjectName = "LoadingScreen", float? timeoutSeconds = 30f)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return Task.CompletedTask;
            }

            return Until(() =>
            {
                var obj = GameObject.Find(loadingObjectName);
                return obj == null || obj.activeInHierarchy == false;
            }, timeoutSeconds);
        }

        /// <summary>
        /// Wait until predicate returns true with enhanced diagnostics on timeout.
        /// </summary>
        /// <param name="predicate">Condition to check each frame.</param>
        /// <param name="timeout">Timeout in realtime seconds.</param>
        /// <param name="context">Diagnostic context to include in timeout exception.</param>
        public static async Task UntilWithDiagnostics(Func<bool> predicate, float timeout, string context = null)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            if (predicate == null)
            {
                return;
            }

            float startTime = Time.realtimeSinceStartup;
            var token = RealPlayExecutionContext.Token;
            
            while (!predicate())
            {
                token.ThrowIfCancellationRequested();
                if (Time.realtimeSinceStartup - startTime >= timeout)
                {
                    string predicateName = predicate.Method?.Name ?? "unknown";
                    string contextInfo = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
                    string message = $"Wait.UntilWithDiagnostics timed out after {timeout}s. Predicate: {predicateName}.{contextInfo}";
                    TestLog.Error(message);
                    throw new TimeoutException(message);
                }

                await Task.Yield();
            }
        }

        /// <summary>
        /// Update test context with the current step/action label.
        /// Useful for tracking test progress in diagnostics.
        /// </summary>
        /// <param name="label">Human-readable label for the current step.</param>
        public static void Step(string label)
        {
            if (!RealPlayEnvironment.IsEnabled)
            {
                return;
            }

            TestLog.Info($"Test Step: {label}");
            
            // If there's a current test context, update it
            // This would require integration with TestRunner to get current context
            // For now, just log the step
        }
    }
}
