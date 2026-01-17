using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RealPlayTester.Core;
using RealPlayTester.Diagnostics;
using System.IO;
using NUnitAssert = NUnit.Framework.Assert;

namespace RealPlayTester.Tests.Verification
{
    public class GestureVerificationTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestRunContextTracker.BeginTest("GestureTest", "EmptyScene");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            TestRunContextTracker.EndTest();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PinchGesture_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.Pinch(new Vector2(500, 500), 100, 300, 45, 0.1f);
            
            string mdContent = GetContextMarkdown();
            NUnitAssert.IsTrue(mdContent.Contains("Gesture: Pinch"), "Timeline missing Pinch gesture");
            NUnitAssert.IsTrue(mdContent.Contains("dist 100->300"), "Timeline missing distance info");
        });

        [UnityTest]
        public IEnumerator TwistGesture_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.Twist(new Vector2(500, 500), 200, 0, 90, 0.1f);
            
            string mdContent = GetContextMarkdown();
            NUnitAssert.IsTrue(mdContent.Contains("Gesture: Twist"), "Timeline missing Twist gesture");
            NUnitAssert.IsTrue(mdContent.Contains("angle 0->90"), "Timeline missing angle info");
        });

        [UnityTest]
        public IEnumerator MultiTapGesture_RecordsBreadcrumb() => ToCoroutine(async () =>
        {
            await Tester.Interaction.Gestures.MultiTap(new Vector2(100, 100), 3, 0.05f);
            
            string mdContent = GetContextMarkdown();
            NUnitAssert.IsTrue(mdContent.Contains("Gesture: MultiTap x3"), "Timeline missing MultiTap gesture");
        });

        private string GetContextMarkdown()
        {
            string mdPath = Path.Combine(RealPlayEnvironment.TestReportsPath, "current-test-context.md");
            return File.ReadAllText(mdPath);
        }

        private static IEnumerator ToCoroutine(System.Func<Task> action)
        {
            var task = action();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                else
                    throw new System.Exception("Task failed without exception");
            }
        }
    }
}
