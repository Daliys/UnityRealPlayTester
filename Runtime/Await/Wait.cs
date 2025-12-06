using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;

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
    }
}
