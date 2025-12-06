using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Await;
using AssertLib = RealPlayTester.Assert.Assert;

public class WaitAndAssertTests
{
    [UnityTest]
    public IEnumerator WaitSeconds_Completes()
    {
        var task = Wait.Seconds(0.05f);
        yield return WaitForTask(task, 1f);
        Assert.IsTrue(task.IsCompleted);
    }

    [UnityTest]
    public IEnumerator WaitFrames_Completes()
    {
        var task = Wait.Frames(2);
        yield return WaitForTask(task, 1f);
        Assert.IsTrue(task.IsCompleted);
    }

    [UnityTest]
    public IEnumerator WaitFrames_CompletesAfterFrames()
    {
        int startFrame = Time.frameCount;
        var task = Wait.Frames(3);
        yield return WaitForTask(task, 1f);
        Assert.IsTrue(task.IsCompleted);
        Assert.GreaterOrEqual(Time.frameCount - startFrame, 3);
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
        Assert.IsTrue(task.IsCompleted);
    }

    [Test]
    public void AssertFail_RaisesExceptionAndPauses()
    {
        float originalTimeScale = Time.timeScale;
        try
        {
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() =>
            {
                AssertLib.Fail("expected failure");
            });
            Assert.AreEqual(0f, Time.timeScale);
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            var overlay = GameObject.Find("RealPlayTester_FailureOverlay");
            if (overlay != null)
            {
                Object.DestroyImmediate(overlay);
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

        Assert.IsTrue(task.IsCompleted, "Task did not complete within timeout.");
    }
}
