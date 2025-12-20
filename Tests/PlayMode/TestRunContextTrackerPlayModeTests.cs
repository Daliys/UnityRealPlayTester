using System.Collections;
using NUnit.Framework;
using RealPlayTester.Await;
using RealPlayTester.Diagnostics;
using UnityEngine.TestTools;

namespace RealPlayTester.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for TestRunContextTracker updates via Wait.Step.
    /// </summary>
    public class TestRunContextTrackerPlayModeTests
    {
        [UnityTest]
        /// <summary>
        /// Ensures Wait.Step updates the current test context action.
        /// </summary>
        public IEnumerator WaitStep_UpdatesContextAction()
        {
            var context = TestRunContextTracker.BeginTest("PlayModeTrackerTest", "PlayModeScene");
            Assert.IsNotNull(context, "Context should be created in PlayMode.");

            Wait.Step("PlayMode Step");
            yield return null;

            Assert.AreEqual("PlayMode Step", TestRunContextTracker.Current.LastAction);
            TestRunContextTracker.EndTest();
        }
    }
}
