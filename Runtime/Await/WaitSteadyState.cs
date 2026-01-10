using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RealPlayTester.Core;
using RealPlayTester.Utilities;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        private static int _lastHierarchyHash;

        /// <summary>
        /// Waits until the scene reaches a "Steady State". Provides detailed reasoning if timeout occurs.
        /// Handles 'Idle Motion' by ignoring tagged ambient objects and looping animations.
        /// </summary>
        public static async Task ForSteadyState(int stableFrames = 10, float timeout = 5f, float? velocityEpsilon = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            float epsilon = velocityEpsilon ?? RealPlaySettings.IdleVelocityEpsilon;
            float startTime = Time.realtimeSinceStartup;
            int currentStableFrames = 0;
            _lastHierarchyHash = 0;
            string lastReason = "None";
            float lastLogTime = startTime;

            while (currentStableFrames < stableFrames)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed > timeout)
                {
                    throw new TimeoutException($"Wait.ForSteadyState timed out after {timeout}s. Stability: {currentStableFrames}/{stableFrames}. Last reason: {lastReason}");
                }

                if (IsSteady(epsilon, out lastReason)) 
                {
                    currentStableFrames++;
                }
                else 
                {
                    currentStableFrames = 0;
                    if (elapsed > 2f && Time.realtimeSinceStartup - lastLogTime > 2f)
                    {
                        RealPlayLog.Info($"[Wait] Still waiting for Steady State. Reason: {lastReason}");
                        lastLogTime = Time.realtimeSinceStartup;
                    }
                }

                RealPlayTester.Input.Internal.InputSimulator.UpdateInput();
                await Task.Yield();
            }
            
            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Wait", $"Steady state reached after {stableFrames} frames.");
        }

        private static bool IsSteady(float epsilon, out string reason)
        {
            if (!IsPhysics3DSteady(epsilon, out reason)) return false;
            if (!IsPhysics2DSteady(epsilon, out reason)) return false;
            if (!IsAnimationsSteady(out reason)) return false;
            if (!IsHierarchySteady(out reason)) return false;
            
            reason = "Steady";
            return true;
        }

        private static bool IsPhysics3DSteady(float epsilon, out string reason)
        {
            var bodies3D = SceneCache.Instance.Rigidbodies3D;
            foreach (var rb in bodies3D)
            {
                if (rb == null || rb.isKinematic || IsAmbient(rb.gameObject)) continue;
                if (rb.linearVelocity.sqrMagnitude > epsilon * epsilon || rb.angularVelocity.sqrMagnitude > epsilon * epsilon) 
                {
                    reason = $"Rigidbody3D '{rb.name}' is moving (v={rb.linearVelocity.magnitude:F2})";
                    return false;
                }
            }
            reason = null;
            return true;
        }

        private static bool IsPhysics2DSteady(float epsilon, out string reason)
        {
            var bodies2D = SceneCache.Instance.Rigidbodies2D;
            foreach (var rb in bodies2D)
            {
                if (rb == null || rb.bodyType == RigidbodyType2D.Static || IsAmbient(rb.gameObject)) continue;
                if (rb.linearVelocity.sqrMagnitude > epsilon * epsilon || Mathf.Abs(rb.angularVelocity) > epsilon) 
                {
                    reason = $"Rigidbody2D '{rb.name}' is moving (v={rb.linearVelocity.magnitude:F2})";
                    return false;
                }
            }
            reason = null;
            return true;
        }

        private static bool IsAnimationsSteady(out string reason)
        {
            var animators = SceneCache.Instance.Animators;
            foreach (var anim in animators)
            {
                if (anim == null || anim.runtimeAnimatorController == null || !anim.enabled || !anim.gameObject.activeInHierarchy) continue;
                if (IsAmbient(anim.gameObject)) continue;

                for (int i = 0; i < anim.layerCount; i++)
                {
                    if (anim.IsInTransition(i)) 
                    {
                        reason = $"Animator '{anim.name}' is in transition";
                        return false;
                    }
                    
                    if (!RealPlaySettings.IgnoreLoopingAnimations)
                    {
                        // If we are NOT ignoring loops, ANY playback counts as motion
                        var state = anim.GetCurrentAnimatorStateInfo(i);
                        if (state.length > 0) // It's playing something
                        {
                            reason = $"Animator '{anim.name}' is playing state (non-loop-ignored mode)";
                            return false;
                        }
                    }
                }
            }
            reason = null;
            return true;
        }

        private static bool IsHierarchySteady(out string reason)
        {
            int currentHash = GetHierarchyHash();
            if (currentHash != _lastHierarchyHash)
            {
                reason = $"Hierarchy is changing (prev_count={_lastHierarchyHash}, curr_count={currentHash})";
                _lastHierarchyHash = currentHash;
                return false;
            }
            reason = null;
            return true;
        }

        private static bool IsAmbient(GameObject go)
        {
            if (go == null) return false;
            string name = go.name;
            foreach (var tag in RealPlaySettings.AmbientTags)
            {
                if (name.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static int GetHierarchyHash()
        {
            return SceneCache.Instance.AllActiveObjects.Count;
        }
    }
}