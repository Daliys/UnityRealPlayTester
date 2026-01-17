using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.UI;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_Thrashing_B
    {
        [UnityTest]
        public IEnumerator PanelState_Race()
        {
            var go = new GameObject("P", typeof(CanvasGroup));
            // Simulate frequent polling while destroying
            for (int i = 0; i < 10; i++)
            {
                try { PanelStateMonitor.CheckPanelState("P"); } catch { }
                if (i == 5) Object.DestroyImmediate(go);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator HardReset_Thrash() => ToCoroutine(async () =>
        {
            await HardReset.Execute();
            await Task.Yield();
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
