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
            var lastPositions = new Dictionary<int, Vector3>();
            string lastReason = "None";
            float lastLogTime = startTime;

            while (currentStableFrames < stableFrames)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed > timeout)
                {
                    throw new TimeoutException($"Wait.ForSteadyState timed out after {timeout}s. Stability: {currentStableFrames}/{stableFrames}. Last reason: {lastReason}");
                }

                if (IsSteady(epsilon, lastPositions, out lastReason)) 
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

                await Wait.Frames(1);
            }
            
            RealPlayTester.Diagnostics.TestRunContextTracker.RecordBreadcrumb("Wait", $"Steady state reached after {stableFrames} frames.");
        }

        private static bool IsSteady(float epsilon, Dictionary<int, Vector3> lastPositions, out string reason)
        {
            if (!IsPhysics3DSteady(epsilon, out reason)) return false;
            if (!IsPhysics2DSteady(epsilon, out reason)) return false;
            if (!IsAnimationsSteady(out reason)) return false;
            if (!IsHierarchySteady(out reason)) return false;
            if (!IsParticlesSteady(out reason)) return false;
            if (!IsTransformsSteady(epsilon, lastPositions, out reason)) return false;
            
            reason = "Steady";
            return true;
        }

        private static bool IsTransformsSteady(float epsilon, Dictionary<int, Vector3> lastPositions, out string reason)
        {
            var all = SceneCache.Instance.AllActiveObjects;
            reason = null;
            bool anyMoved = false;

            foreach (var go in all)
            {
                if (go == null || IsAmbient(go)) continue;
                
                int id = go.GetInstanceID();
                Vector3 pos = go.transform.position;

                if (lastPositions.TryGetValue(id, out Vector3 lastPos))
                {
                    float distSq = (pos - lastPos).sqrMagnitude;
                    if (distSq > epsilon * epsilon)
                    {
                        reason = $"Transform '{go.name}' is moving (d={Mathf.Sqrt(distSq):F4})";
                        anyMoved = true;
                    }
                }
                lastPositions[id] = pos;
            }

            return !anyMoved;
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

        private static bool IsParticlesSteady(out string reason)
        {
            var systems = SceneCache.Instance.ParticleSystems;
            foreach (var ps in systems)
            {
                if (ps == null || IsAmbient(ps.gameObject)) continue;
                if (ps.isPlaying && ps.particleCount > 0)
                {
                    reason = $"ParticleSystem '{ps.name}' is active ({ps.particleCount} particles)";
                    return false;
                }
            }
            reason = null;
            return true;
        }

        private static bool IsAnimationsSteady(out string reason)
        {
            var animators = SceneCache.Instance.Animators;
            int count = animators.Count;
            for (int i = 0; i < count; i++)
            {
                var anim = animators[i];
                if (anim == null || anim.runtimeAnimatorController == null || !anim.enabled || !anim.gameObject.activeInHierarchy) continue;
                if (IsAmbient(anim.gameObject)) continue;

                for (int layer = 0; layer < anim.layerCount; layer++)
                {
                    if (anim.IsInTransition(layer)) 
                    {
                        reason = $"Animator '{anim.name}' is in transition on layer {layer}";
                        return false;
                    }
                    
                    if (!RealPlaySettings.IgnoreLoopingAnimations)
                    {
                        var state = anim.GetCurrentAnimatorStateInfo(layer);
                        if (state.length > 0 && !state.loop) 
                        {
                            reason = $"Animator '{anim.name}' is playing non-looping state";
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
            var all = SceneCache.Instance.AllActiveObjects;
            int hash = all.Count;
            // Use unchecked XOR of InstanceIDs for a fast, stable-within-frame hash
            unchecked
            {
                foreach (var go in all)
                {
                    if (go != null) hash ^= go.GetInstanceID();
                }
            }
            return hash;
        }
    }
}
