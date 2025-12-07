using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Await;
using AssertLib = RealPlayTester.Assert.Assert;
using NAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.EditMode
{
    public class WaitAndAssertTests
    {
        [UnityTest]
        public IEnumerator WaitSeconds_Completes()
        {
            var task = Wait.Seconds(0.02f);
            yield return WaitForTask(task, 1f);
            NAssert.IsTrue(task.IsCompleted);
        }

        [UnityTest]
        public IEnumerator WaitFrames_Completes()
        {
            var task = Wait.Frames(2);
            yield return WaitForTask(task, 1f);
            NAssert.IsTrue(task.IsCompleted);
        }

        [UnityTest]
        public IEnumerator WaitUntil_CompletesPredicate()
        {
            bool flag = false;
            var task = Wait.Until(() =>
            {
                flag = true;
                return flag;
            }, 0.2f);
            yield return WaitForTask(task, 1f);
            NAssert.IsTrue(task.IsCompleted);
        }

        [Test]
        public void AssertFail_RaisesExceptionAndPauses()
        {
            float originalTimeScale = Time.timeScale;
            try
            {
                NAssert.Throws<UnityEngine.Assertions.AssertionException>(() =>
                {
                    AssertLib.Fail("expected failure");
                });
                NAssert.AreEqual(0f, Time.timeScale);
            }
            finally
            {
            Time.timeScale = originalTimeScale;
            var overlay = GameObject.Find("RealPlayTester_FailureOverlay");
            if (overlay != null)
            {
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }
    }

        private static IEnumerator WaitForTask(Task task, float timeoutSeconds)
        {
            float start = Time.realtimeSinceStartup;
            while (!task.IsCompleted && Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                yield return null;
            }

            NAssert.IsTrue(task.IsCompleted, "Task did not complete within timeout.");
        }
    }
}
