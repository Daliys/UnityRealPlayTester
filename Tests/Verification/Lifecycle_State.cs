using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.UI;
using RealPlayTester.Utilities;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_State
    {
        [UnityTest]
        public IEnumerator StaticReset_Verification()
        {
            Tester.Settings.ShowVisualPointer = false;
            var method = typeof(RealPlayTesterHost).GetMethod("ResetStaticState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, null);
            NUnitAssert.IsTrue(Tester.Settings.ShowVisualPointer);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Probe_Destroyed()
        {
            var go = new GameObject("T", typeof(RectTransform));
            Vector2 pos = RealPlayTester.Input.RealInputUtility.GetScreenCenter(go);
            Object.DestroyImmediate(go);
            NUnitAssert.IsFalse(InteractionProbe.ProbeScreen(pos).Contains("["));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetActiveState_Unload() => ToCoroutine(async () =>
        {
            var op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            while(!op.isDone) { PanelStateMonitor.GetActiveStateName(); await Task.Yield(); }
        });

        [UnityTest]
        public IEnumerator Host_Persistence()
        {
            var host = RealPlayTesterHost.Instance;
            var cache = host.GetComponent<SceneCache>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            yield return null;
            NUnitAssert.AreEqual(cache, RealPlayTesterHost.Instance.GetComponent<SceneCache>());
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
