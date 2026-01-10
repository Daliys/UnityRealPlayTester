using System;
using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        /// <summary>
        /// Wait until no Rigidbody in the scene is moving faster than the threshold.
        /// </summary>
        public static Task ForStablePhysics(float velocityThreshold = 0.01f, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled) return Task.CompletedTask;

            string desc = $"Physics stability (velocity < {velocityThreshold})";

            return Until(() =>
            {
                var bodies = GameObject.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
                foreach (var rb in bodies)
                {
                    if (rb.isKinematic || rb.IsSleeping()) continue;
                    if (rb.linearVelocity.sqrMagnitude > velocityThreshold * velocityThreshold) return false;
                }
                
                var bodies2D = GameObject.FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
                foreach (var rb in bodies2D)
                {
                    if (rb.bodyType == RigidbodyType2D.Kinematic || rb.IsSleeping()) continue;
                    if (rb.linearVelocity.sqrMagnitude > velocityThreshold * velocityThreshold) return false;
                }
                
                return true;
            }, timeoutSeconds);
        }
    }
}
