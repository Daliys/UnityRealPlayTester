using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Input;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_Cache
    {
        [UnityTest]
        public IEnumerator SceneCache_Purge() => ToCoroutine(async () =>
        {
            var go = new GameObject("OLD_OBJECT");
            SceneCache.Instance.ForceRefresh();
            var op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            while(!op.isDone) await Task.Yield();
            await Task.Yield(); 
            
            bool foundOld = false;
            foreach(var obj in SceneCache.Instance.AllActiveObjects) if (obj != null && obj.name == "OLD_OBJECT") foundOld = true;
            NUnitAssert.IsFalse(foundOld);
        });

        [UnityTest]
        public IEnumerator SmartFind_Stale() => ToCoroutine(async () =>
        {
            var go = new GameObject("STALE_T");
            SceneCache.Instance.ForceRefresh();
            Object.DestroyImmediate(go);
            // No yield here to test immediate handling
            var res = SmartFind.Object("STALE_T");
            NUnitAssert.IsNull(res);
        });

        [UnityTest]
        public IEnumerator Occlusion_Destroyed()
        {
            var go = new GameObject("T");
            Object.DestroyImmediate(go);
            var res = RealPlayTester.Core.Perception.RealPlayOcclusionRaycaster.CheckOcclusion(go);
            NUnitAssert.IsFalse(res.IsOccluded);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Leak_Check() => ToCoroutine(async () =>
        {
            for(int i = 0; i < 3; i++) { Object.DestroyImmediate(new GameObject("L")); SceneCache.Instance.ForceRefresh(); }
            await Task.Yield();
            bool found = false;
            foreach(var go in SceneCache.Instance.AllActiveObjects) if (go != null && go.name == "L") found = true;
            NUnitAssert.IsFalse(found);
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
