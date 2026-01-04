using System.Threading.Tasks;
using UnityEngine;
using RealPlayTester.Core;

namespace RealPlayTester.Await
{
    public static partial class Wait
    {
        public static Task ForAnimationState(Animator animator, string stateName, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled || animator == null) return Task.CompletedTask;
            return Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName(stateName), timeoutSeconds);
        }

        public static Task ForAudioComplete(AudioSource source, float? timeoutSeconds = null)
        {
            if (!RealPlayEnvironment.IsEnabled || source == null) return Task.CompletedTask;
            return Until(() => !source.isPlaying, timeoutSeconds);
        }
    }
}
