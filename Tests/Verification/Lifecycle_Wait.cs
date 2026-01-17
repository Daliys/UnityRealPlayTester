using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RealPlayTester.Await;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_Wait
    {
        [UnityTest]
        public IEnumerator WaitUntil_ObjectDestroyed() => ToCoroutine(async () =>
        {
            var go = new GameObject("T");
            var task = Wait.Until(() => go.activeSelf, timeoutSeconds: 0.2f);
            await Task.Delay(10);
            Object.DestroyImmediate(go);
            try { await task; } catch { }
        });

        [UnityTest]
        public IEnumerator WaitForUIVisible_ParentDestroyed() => ToCoroutine(async () =>
        {
            var p = new GameObject("P");
            var c = new GameObject("C", typeof(RectTransform));
            c.transform.SetParent(p.transform);
            var task = Wait.ForUIVisible("C", 0.2f);
            await Task.Delay(20);
            Object.DestroyImmediate(p);
            try { await task; } catch { }
        });

        [UnityTest]
        public IEnumerator WaitForAudio_SourceDestroyed() => ToCoroutine(async () =>
        {
            var s = new GameObject("A").AddComponent<AudioSource>();
            var task = Wait.ForAudioComplete(s, 0.2f);
            await Task.Delay(10);
            Object.DestroyImmediate(s.gameObject);
            await task;
        });

        [UnityTest]
        public IEnumerator WaitForAnimation_AnimatorDestroyed() => ToCoroutine(async () =>
        {
            var a = new GameObject("An").AddComponent<Animator>();
            var task = Wait.ForAnimationState(a, "Id", 0.2f);
            await Task.Delay(10);
            Object.DestroyImmediate(a.gameObject);
            await task;
        });

        [UnityTest]
        public IEnumerator SteadyState_Physics_Null_Body() => ToCoroutine(async () =>
        {
            var go = new GameObject("B", typeof(Rigidbody));
            var task = Wait.ForSteadyState(stableFrames: 2, timeout: 0.2f);
            await Task.Delay(10);
            Object.DestroyImmediate(go);
            await task;
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
