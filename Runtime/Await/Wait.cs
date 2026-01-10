using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using InputShim = RealPlayTester.Input.InputSystemShim;

namespace RealPlayTester.Await
{
    /// <summary>
    /// Async-friendly wait helpers that keep execution on Unity's main thread.
    /// Uses scaled time (Time.time) by default. Set unscaled=true for realtime.
    /// </summary>
    public static partial class Wait
    {
        /// <summary>
        /// Wait for the specified number of seconds using scaled time (affected by Time.timeScale).
        /// </summary>
        /// <param name="seconds">Duration to wait in seconds.</param>
        /// <param name="unscaled">If true, uses realtime (unaffected by Time.timeScale).</param>
        public static async Task Seconds(float seconds, bool unscaled = false, CancellationToken token = default)
        {
            if (!RealPlayEnvironment.IsEnabled || seconds <= 0f) return;

            var effectiveToken = GetEffectiveToken(token, out var linkedCts);

            try
            {
                if (unscaled) await WaitUnscaled(seconds, effectiveToken);
                else await WaitScaled(seconds, effectiveToken);
            }
            finally
            {
                linkedCts?.Dispose();
            }
        }

        private static CancellationToken GetEffectiveToken(CancellationToken token, out CancellationTokenSource linkedCts)
        {
            var ctxToken = RealPlayExecutionContext.Token;
            linkedCts = null;

            if (token != default && ctxToken != default && token != ctxToken)
            {
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ctxToken);
                return linkedCts.Token;
            }
            return token != default ? token : ctxToken;
        }

        private static async Task WaitUnscaled(float seconds, CancellationToken token)
        {
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < seconds)
            {
                token.ThrowIfCancellationRequested();
                RealPlayTester.Input.Internal.InputSimulator.UpdateInput();
                await Task.Yield();
            }
        }

        private static async Task WaitScaled(float seconds, CancellationToken token)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                token.ThrowIfCancellationRequested();
                RealPlayTester.Input.Internal.InputSimulator.UpdateInput();
                await Task.Yield();
                elapsed += Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
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
                RealPlayTester.Input.Internal.InputSimulator.UpdateInput(); // Ensure simulation ticks in Script mode
                await Task.Yield();
            }
        }

        /// <summary>
        /// Wait until the predicate returns true.
        /// </summary>
        /// <param name="predicate">Condition to check each frame.</param>
        /// <param name="timeoutSeconds">Optional timeout in realtime seconds.</param>
        /// <param name="description">Reason for waiting, used in timeout messages.</param>
        public static async Task Until(Func<bool> predicate, float? timeoutSeconds = null, string description = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;
            if (predicate == null) return;

            float startTime = Time.realtimeSinceStartup;
            float timeout = timeoutSeconds ?? float.MaxValue;
            var token = RealPlayExecutionContext.Token;
            float lastLogTime = startTime;
            
            while (!predicate())
            {
                token.ThrowIfCancellationRequested();
                float elapsed = Time.realtimeSinceStartup - startTime;

                if (elapsed >= timeout)
                {
                    throw new TimeoutException($"Wait.Until timed out after {timeout}s. {(description != null ? $"Reason: {description}" : "")}");
                }

                // Periodic logging for long waits (every 2s)
                if (elapsed > 2f && Time.realtimeSinceStartup - lastLogTime > 2f && !string.IsNullOrEmpty(description))
                {
                    RealPlayLog.Info($"[Wait] Still waiting for: {description} ({elapsed:F1}s elapsed)");
                    lastLogTime = Time.realtimeSinceStartup;
                }

                await Task.Yield();
            }
        }

        public static Task While(Func<bool> predicate, float? timeoutSeconds = null, string description = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;
            if (predicate == null) return Task.CompletedTask;

            return Until(() => !predicate(), timeoutSeconds, description != null ? $"Not({description})" : null);
        }


    }
}
