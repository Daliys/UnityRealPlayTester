using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using RealPlayTester.Core;

namespace RealPlayTester.Tests.Verification
{
    public class Lifecycle_Thrashing_A
    {
        [UnityTest]
        public IEnumerator DumpHierarchy_During_Thrash() => ToCoroutine(async () =>
        {
            var op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            while (op != null && !op.isDone)
            {
                Tester.Perception.DumpHierarchyJson();
                await Task.Yield();
            }
        });

        [UnityTest]
        public IEnumerator Rapid_Scene_Thrashing_Loop() => ToCoroutine(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                Tester.Perception.Find("NonExistent");
                await Task.Yield();
            }
        });

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.InnerException;
        }
    }
}
