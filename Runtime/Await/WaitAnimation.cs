using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static Task ForAnimationState(Animator animator, string stateName, float? timeoutSeconds = null, int layerIndex = 0)
        {
            if (!RealPlayEnvironment.IsEnabled || animator == null) return Task.CompletedTask;
            string desc = $"Animator '{animator.name}' reaching state '{stateName}' on layer {layerIndex}";
            return Until(() => 
            {
                if (animator == null) return true; // Object destroyed, stop waiting
                return animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
            }, timeoutSeconds, desc);
        }

        public static Task ForAudioComplete(AudioSource source, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled || source == null) return Task.CompletedTask;
            string desc = $"AudioSource '{source.name}' finishing playback (Clip: {(source.clip != null ? source.clip.name : "none")})";
            
            return Until(() => 
            {
                if (source == null) return true; // Object destroyed, stop waiting
                return !source.isPlaying;
            }, timeoutSeconds, desc);
        }
    }
}
